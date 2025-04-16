using System.Text.Json.Serialization;

namespace Sus.SurveyDataMigrationFunction;

public class JsonPayload
{
    [JsonPropertyName("CaseFilter")]
    public CaseFilter CaseFilter { get; set; }

    [JsonPropertyName("Variables")]
    public string Variables { get; set; }
}