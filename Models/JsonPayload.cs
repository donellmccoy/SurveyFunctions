using System.Text.Json.Serialization;

namespace SurveyFunctions.Models;

public class JsonPayload
{
    [JsonPropertyName("CaseFilter")]
    public CaseFilter CaseFilter { get; set; }

    [JsonPropertyName("Variables")]
    public string Variables { get; set; }
}