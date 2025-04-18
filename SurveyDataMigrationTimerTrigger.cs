using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SurveyFunctions.Constants;
using SurveyFunctions.Options;

namespace SurveyFunctions;

public class SurveyDataMigrationTimerTrigger
{
    private readonly ILogger _logger;

    private readonly HttpClient _httpClient;

    private readonly IConfiguration _configuration;

    private readonly IOptions<AppSettings> _options;    

    public SurveyDataMigrationTimerTrigger(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IConfiguration configuration, IOptions<AppSettings> options)
    {
        _logger = loggerFactory.CreateLogger<SurveyDataMigrationTimerTrigger>();
        _httpClient = httpClientFactory.CreateClient("VoxcoApi");
        _configuration = configuration;
        _options = options;
    }

    [Function("SurveyDataMigrationTimerTrigger")]
    public async Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {DateTime.Now}", DateTime.Now);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {myTimer.ScheduleStatus.Next}", myTimer.ScheduleStatus.Next);
        }
        
        try
        {
            await GetDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Voxco data");
        }
    }        
    
    private async Task GetDataAsync()
    {
        if (await IsAuthenticatedAsync() is false)
        {
            return;
        }
        
        var respondents = await GetRespondentsAsync();
        
        if (respondents is { Count: 0 })
        {
            _logger.LogError("No respondents found or could not be retrieved");

            return;
        }
        
        await ProcessRespondentsAsync(respondents);
        
        _logger.LogInformation("Voxco data migration completed successfully");
    }
    
    private async Task<bool> IsAuthenticatedAsync()
    {
        try 
        {
            var requestUrl = $"{_options.Value.VoxcoApiOptions.BaseUrl}/{_options.Value.VoxcoApiOptions.AuthUrl}?userInfo.username={_options.Value.VoxcoApiOptions.Username}&userInfo.password={_options.Value.VoxcoApiOptions.Password}&userInfo.context={_options.Value.VoxcoApiOptions.Context}";

            var response = await _httpClient.GetAsync(requestUrl);

            response.EnsureSuccessStatusCode();
        
            var authData = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            if (authData == null)
            {
                _logger.LogError("Authentication response is empty or could not be deserialized");

                return false;
            }
        
            if (authData.TryGetValue("Token", out var token) is false)
            {
                _logger.LogError("Token not found in authentication response");
            
                return false;
            }
        
            _logger.LogInformation("Authentication successful, token: {token}", token);
        
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client", token);

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Authentication request failed");

            return false;
        }
    }

    /// <summary>
    /// Retrieves a list of respondents for a specified project from the Voxco API.
    /// </summary>
    /// <param name="baseUrl">The base URL of the Voxco API.</param>
    /// <param name="projectId">The ID of the project for which respondents are to be retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of dictionaries,
    /// where each dictionary represents a respondent's data. Returns an empty list if the data is empty
    /// or an error occurs.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration value for "VoxcoApi:RespondentsUrl" is missing.
    /// </exception>
    private async Task<List<Dictionary<string, object>>> GetRespondentsAsync()
    {
        var respondentsUrl = _configuration["VoxcoApi:RespondentsUrl"] ?? throw new InvalidOperationException("VoxcoApi:RespondentsUrl configuration value is missing");
        
        respondentsUrl = respondentsUrl.Replace("{projectId}", _options.Value.VoxcoApiOptions.ProjectId);
        
        try
        {
            var response = await _httpClient.PostAsync($"{_options.Value.VoxcoApiOptions.BaseUrl}/{respondentsUrl}", new StringContent(JsonPayloads.JsonPayload, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            
            var respondents = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

            if (respondents == null)
            {
                _logger.LogError("Respondent data is empty or could not be deserialized");

                return [];
            }
            
            _logger.LogInformation("Retrieved {respondents.Count} respondents", respondents.Count);
            
            return respondents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve respondents");

            return [];
        }
    }
    
    private async Task ProcessRespondentsAsync(List<Dictionary<string, object>> respondents)
    {
        foreach (var respondent in respondents)
        {
            if (respondent.TryGetValue("Id", out var idObj) is false)
            {
                _logger.LogWarning("Respondent ID not found or is null, skipping");

                continue;
            }
            
            var id = idObj.ToString();

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Respondent ID is empty, skipping");

                continue;
            }

            _logger.LogInformation("Processing respondent ID: {id}", id);

            var responsesUrl = _configuration["VoxcoApi:ResponsesUrl"] ?? throw new InvalidOperationException("VoxcoApi:ResponsesUrl configuration value is missing");
                
            responsesUrl = responsesUrl.Replace("{projectId}", _options.Value.VoxcoApiOptions.ProjectId).Replace("{id}", id);

            var response = await _httpClient.GetAsync($"{_options.Value.VoxcoApiOptions.BaseUrl}/{responsesUrl}?offset=0&mergeMentions=false");
            
            if (response.IsSuccessStatusCode)
            {
                var respondentData = await response.Content.ReadFromJsonAsync<object>();

                if (respondentData == null)
                {
                    _logger.LogWarning("Data for respondent ID {id} is empty or could not be deserialized", id);
                }
            }
            else
            {
                _logger.LogError("Failed to retrieve data for respondent ID {id}. Status code: {response.StatusCode}", id, response.StatusCode);
            }
        }
    }
}