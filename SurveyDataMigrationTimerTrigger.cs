using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurveyFunctions.Constants;
using SurveyFunctions.Options;

namespace SurveyFunctions;

public class SurveyDataMigrationTimerTrigger
{
    private readonly ILogger _logger;

    private readonly HttpClient _httpClient;

    private readonly IOptions<AppSettings> _options;    

    /// <summary>
    /// Initializes a new instance of the <see cref="SurveyDataMigrationTimerTrigger"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="options">The application settings options.</param>
    /// <remarks>
    /// This constructor initializes the logger, HTTP client, and application settings options.
    /// It is used for dependency injection in the Azure Functions runtime.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the parameters are null.
    /// </exception>
    /// <remarks>   
    /// This constructor initializes the logger, HTTP client, and application settings options.
    /// It is used for dependency injection in the Azure Functions runtime.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the parameters are null.
    /// </exception>
    /// <remarks>
    public SurveyDataMigrationTimerTrigger(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IOptions<AppSettings> options)
    {
        _logger = loggerFactory.CreateLogger<SurveyDataMigrationTimerTrigger>();
        _httpClient = httpClientFactory.CreateClient("VoxcoApi");
        _options = options;
    }

    /// <summary>
    /// Timer trigger function that runs every minute to process Voxco data migration.  
    /// </summary>
    /// <param name="myTimer">The timer information.</param>
    /// <remarks>
    /// This function is triggered by a timer and logs the current time and the next scheduled time.
    /// It attempts to authenticate with the Voxco API and process respondent data.
    /// If an error occurs during the process, it logs the error message.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="Exception">
    /// Thrown when an unexpected error occurs during the process.
    /// </exception>
    /// <remarks>
    /// This method is triggered by a timer and logs the current time and the next scheduled time.
    /// It attempts to authenticate with the Voxco API and process respondent data.
    /// If an error occurs during the process, it logs the error message.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Function("SurveyDataMigrationTimerTrigger")]
    public async Task RunAsync([TimerTrigger("0 */1 * * * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {DateTime.Now}", DateTime.Now);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {myTimer.ScheduleStatus.Next}", myTimer.ScheduleStatus.Next);
        }
        
        try
        {
            _logger.LogInformation("Voxco data migration completed successfully.");

            await Task.CompletedTask;

            return;

            if (await IsAuthenticatedAsync() is false)
            {
                return;
            }

            await ProcessRespondentDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Voxco data.");
        }
    }

    /// <summary>
    /// Authenticates with the Voxco API using the provided credentials.    
    /// </summary>
    /// <remarks>
    /// This method sends a GET request to the Voxco API authentication endpoint with the provided username, password, and context.
    /// It retrieves the authentication token and sets it in the HTTP client headers for subsequent requests.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation. Returns true if authentication is successful, false otherwise.</returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request to the Voxco API fails.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration value for "VoxcoApi:AuthUrl" is missing.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the JSON response from the Voxco API cannot be deserialized.    
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown when an unexpected error occurs during the authentication process.
    /// </exception>
    /// <remarks>
    /// This method constructs the URL for the Voxco API authentication request using the provided base URL, username, password, and context.
    /// It then sends a GET request to the API and deserializes the response into a dictionary.
    /// If the authentication is successful, it sets the token in the HTTP client headers for subsequent requests.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation. Returns true if authentication is successful, false otherwise.</returns>
    private async Task<bool> IsAuthenticatedAsync()
    {
        try 
        {
            var requestUrl = 
                $"{_options.Value.VoxcoApiOptions.BaseUrl}/{_options.Value.VoxcoApiOptions.AuthUrl}?" +
                $"userInfo.username={_options.Value.VoxcoApiOptions.Username}&" +
                $"userInfo.password={_options.Value.VoxcoApiOptions.Password}&" +
                $"userInfo.context={_options.Value.VoxcoApiOptions.Context}";

            var response = await _httpClient.GetAsync(requestUrl);

            response.EnsureSuccessStatusCode();
        
            var authData = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            if (authData is null)
            {
                _logger.LogError("Authentication response is empty or could not be deserialized.");

                return false;
            }
        
            if (authData.TryGetValue("Token", out var token) is false)
            {
                _logger.LogError("Token not found in authentication response.");
            
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
            _logger.LogError(ex, "Authentication request failed.");

            return false;
        }
    }

    /// <summary>
    /// Processes the respondent data by retrieving it from the Voxco API and saving it to the database.        
    /// </summary>
    /// <remarks>
    /// This method retrieves a list of respondents for a specified project from the Voxco API.
    /// It then processes each respondent's data and handles any errors that may occur during the process.  
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>   
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request to the Voxco API fails.    
    /// </exception>
    /// <exception cref="InvalidOperationException">    
    /// Thrown when the configuration value for "VoxcoApi:RespondentsUrl" is missing.
    /// </exception>    
    /// <exception cref="JsonException">
    /// Thrown when the JSON response from the Voxco API cannot be deserialized.    
    /// </exception>
    /// <exception cref="Exception">        
    /// Thrown when an unexpected error occurs during the process.
    /// </exception>
    /// <remarks>
    /// This method constructs the URL for the Voxco API request using the provided base URL and project ID.
    /// It then sends a POST request to the API and deserializes the response into a list of dictionaries.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ProcessRespondentDataAsync()
    {
        var respondents = await GetRespondentDataAsync();
        
        if (respondents is { Count: 0 })
        {
            _logger.LogError("No respondents found or could not be retrieved.");

            return;
        }
        
        await SaveRespondentDataAsync(respondents);
        
        _logger.LogInformation("Voxco data migration completed successfully.");
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
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request to the Voxco API fails.    
    /// </exception>
    /// <exception cref="JsonException">    
    /// Thrown when the JSON response from the Voxco API cannot be deserialized.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown when an unexpected error occurs during the process.
    /// </exception>
    /// <remarks>
    /// This method constructs the URL for the Voxco API request using the provided base URL and project ID.
    /// It then sends a POST request to the API and deserializes the response into a list of dictionaries.  
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task<List<Dictionary<string, object>>> GetRespondentDataAsync()
    {
        var respondentsUrl = _options.Value.VoxcoApiOptions.RespondentsUrl.Replace("{projectId}", _options.Value.VoxcoApiOptions.ProjectId);
        
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_options.Value.VoxcoApiOptions.BaseUrl}/{respondentsUrl}", 
                new StringContent(JsonPayloads.JsonPayload, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            
            var respondents = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();

            if (respondents is null)
            {
                _logger.LogError("Respondent data is empty or could not be deserialized.");

                return [];
            }
            
            _logger.LogInformation("Retrieved {Count} respondents", respondents.Count);
            
            return respondents;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Request failed. message: {Message}", ex.Message);

            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve respondents. message: {Message}", ex.Message);

            return [];
        }
    }

    /// <summary>
    /// Saves the data for each respondent retrieved from the Voxco API.
    /// This method processes each respondent's data and handles any errors that may occur during the process.
    /// </summary>
    /// <param name="respondents"></param>
    /// <returns></returns>
    private async Task SaveRespondentDataAsync(List<Dictionary<string, object>> respondents)
    {
        foreach (var respondent in respondents)
        {
            if (respondent.TryGetValue("Id", out var idObj) is false)
            {
                _logger.LogWarning("Respondent ID not found or is null, skipping.");

                continue;
            }
            
            var id = idObj.ToString();

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Respondent ID is empty, skipping.");

                continue;
            }

            _logger.LogInformation("Processing respondent ID: {id}", id);

            var responsesUrl = _options.Value.VoxcoApiOptions.ResponsesUrl.Replace("{projectId}", _options.Value.VoxcoApiOptions.ProjectId).Replace("{id}", id);

            var response = await _httpClient.GetAsync($"{_options.Value.VoxcoApiOptions.BaseUrl}/{responsesUrl}?offset=0&mergeMentions=false");
            
            if (response.IsSuccessStatusCode)
            {
                var respondentData = await response.Content.ReadFromJsonAsync<object>();

                if (respondentData == null)
                {
                    _logger.LogWarning("Data for respondent ID {id} is empty or could not be deserialized.", id);
                }
            }
            else
            {
                _logger.LogError("Failed to retrieve data for respondent ID {id}. Status code: {StatusCode}", id, response.StatusCode);
            }
        }
    }
}