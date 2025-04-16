using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Sus.SurveyDataMigrationFunction;

public class SurveyDataMigrationTimerTrigger
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SurveyDataMigrationTimerTrigger(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<SurveyDataMigrationTimerTrigger>();
        _httpClient = httpClientFactory.CreateClient("VoxcoApi");
        _configuration = configuration;
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
        var baseUrl = _configuration["VoxcoApi:BaseUrl"] ?? throw new InvalidOperationException("VoxcoApi:BaseUrl configuration value is missing");
        var authUrl = _configuration["VoxcoApi:AuthUrl"] ?? throw new InvalidOperationException("VoxcoApi:AuthUrl configuration value is missing");
        var username = _configuration["VoxcoApi:Username"] ?? throw new InvalidOperationException("VoxcoApi:Username configuration value is missing");
        var password = _configuration["VoxcoApi:Password"] ?? throw new InvalidOperationException("VoxcoApi:Password configuration value is missing");
        var context = _configuration["VoxcoApi:Context"] ?? throw new InvalidOperationException("VoxcoApi:Context configuration value is missing");
        var projectId = _configuration["VoxcoApi:ProjectId"] ?? throw new InvalidOperationException("VoxcoApi:ProjectId configuration value is missing");
        
        if (await IsAuthenticatedAsync(baseUrl, authUrl, username, password, context) is false)
        {
            return;
        }
        
        var respondents = await GetRespondentsAsync(baseUrl, projectId);
        
        if (respondents is { Count: 0 })
        {
            _logger.LogError("No respondents found or could not be retrieved");

            return;
        }
        
        await ProcessRespondentsAsync(respondents, baseUrl, projectId);
        
        _logger.LogInformation("Voxco data migration completed successfully");
    }
    
    private async Task<bool> IsAuthenticatedAsync(string baseUrl, string authUrl, string username, string password, string context)
    {
        try 
        {
            var response = await _httpClient.GetAsync($"{baseUrl}/{authUrl}?userInfo.username={username}&userInfo.password={password}&userInfo.context={context}");

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

    private async Task<List<Dictionary<string, object>>> GetRespondentsAsync(string baseUrl, string projectId)
    {
        const string jsonPayload = @"
        {
            ""CaseFilter"": {
                ""FilterId"": 0,
                ""FilterTitle"": null,
                ""ProjectId"": ""103"",
                ""QuestionList"": null,
                ""Location"": """",
                ""DialingMode"": -1,
                ""TimeSlotMode"": -1,
                ""TimeSlotId"": """",
                ""TimeSlotOperator"": """",
                ""TimeSlotHitCount"": """",
                ""LastDialingMode"": 0,
                ""RespondentCase"": 7,
                ""RespondentState"": 0,
                ""LastCallDateTime"": null,
                ""CallBackDateTime"": null,
                ""IsLastCallDateTimeTreadtedSeperatly"": false,
                ""IsCallbackDateTimeTreatedSeperatly"": false,
                ""IsCallBack"": false,
                ""IsNotRecoded"": false,
                ""ViewAdditionalColumns"": null,
                ""QuestionHasOpenEnd"": """",
                ""IsNotInClosedStrata"": false,
                ""InterviewerIds"": """",
                ""ResultCodes"": """",
                ""LastCallResultCodes"": """",
                ""Languages"": """",
                ""UserTimeZone"": 0,
                ""LinkedToA4S"": 1,
                ""IsMissingRecords"": false,
                ""IsMissingRecordsPronto"": false,
                ""ExcludeRecordsInInterview"": false,
                ""SqlStatementWithOrWithoutEquation"": null,
                ""Equation"": """",
                ""UseCurrentDateForStartCallBack"": false,
                ""CallBackDateTimeFromDate"": ""1899-12-31T00:00:00.000"",
                ""CallBackDateTimeFromTime"": ""1899-12-31T00:00:00.000"",
                ""UseCurrentDateForEndCallBack"": false,
                ""CallBackDateTimeToDate"": ""1899-12-31T00:00:00.000"",
                ""CallBackDateTimeToTime"": ""1899-12-31T00:00:00.000"",
                ""UseCurrentDateForStartLastCall"": false,
                ""LastCallDateTimeStartDate"": ""1899-12-31T00:00:00.000"",
                ""LastCallDateTimeStartTime"": ""1899-12-31T00:00:00.000"",
                ""UseCurrentDateForEndLastCall"": false,
                ""LastCallDateTimeEndDate"": ""1899-12-31T00:00:00.000"",
                ""LastCallDateTimeEndTime"": ""1899-12-31T00:00:00.000"",
                ""IsValid"": false,
                ""Summary"": null,
                ""Count"": 0,
                ""CyclePhoneNumber"": -1,
                ""KeywordFilter"": [],
                ""Selection"": 0,
                ""State"": 0,
                ""NumberOfCases"": 0,
                ""AgentId"": 0,
                ""IsAnonymized"": null,
                ""MaxRecords"": 0,
                ""CaseFilterType"": 0,
                ""LastModificationDateTimeStartDate"": null,
                ""IsLastModificationDateTimeTreatedSeperatly"": false,
                ""CompletedDateTime"": null,
                ""IsCompletedDateTimeTreatedSeperatly"": false,
                ""CompletedDateTimeStartDate"": null,
                ""CompletedDateTimeEndDate"": null
            },
            ""Variables"": """"
        }";

        var respondentsUrl = _configuration["VoxcoApi:RespondentsUrl"] ?? throw new InvalidOperationException("VoxcoApi:RespondentsUrl configuration value is missing");
        
        respondentsUrl = respondentsUrl.Replace("{projectId}", projectId);
        
        try
        {
            var response = await _httpClient.PostAsync($"{baseUrl}/{respondentsUrl}", new StringContent(jsonPayload, Encoding.UTF8, "application/json"));

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
    
    private async Task ProcessRespondentsAsync(List<Dictionary<string, object>> respondents, string baseUrl, string projectId)
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
                
            responsesUrl = responsesUrl.Replace("{projectId}", projectId).Replace("{id}", id);

            var response = await _httpClient.GetAsync($"{baseUrl}/{responsesUrl}?offset=0&mergeMentions=false");
            
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