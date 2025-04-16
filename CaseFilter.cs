using System.Text.Json.Serialization;

namespace Sus.SurveyDataMigrationFunction;

public class CaseFilter
{
    [JsonPropertyName("FilterId")]
    public int FilterId { get; set; }

    [JsonPropertyName("FilterTitle")]
    public string FilterTitle { get; set; }

    [JsonPropertyName("ProjectId")]
    public string ProjectId { get; set; }

    [JsonPropertyName("QuestionList")]
    public object QuestionList { get; set; }

    [JsonPropertyName("Location")]
    public string Location { get; set; }

    [JsonPropertyName("DialingMode")]
    public int DialingMode { get; set; }

    [JsonPropertyName("TimeSlotMode")]
    public int TimeSlotMode { get; set; }

    [JsonPropertyName("TimeSlotId")]
    public string TimeSlotId { get; set; }

    [JsonPropertyName("TimeSlotOperator")]
    public string TimeSlotOperator { get; set; }

    [JsonPropertyName("TimeSlotHitCount")]
    public string TimeSlotHitCount { get; set; }

    [JsonPropertyName("LastDialingMode")]
    public int LastDialingMode { get; set; }

    [JsonPropertyName("RespondentCase")]
    public int RespondentCase { get; set; }

    [JsonPropertyName("RespondentState")]
    public int RespondentState { get; set; }

    [JsonPropertyName("LastCallDateTime")]
    public DateTime? LastCallDateTime { get; set; }

    [JsonPropertyName("CallBackDateTime")]
    public DateTime? CallBackDateTime { get; set; }

    [JsonPropertyName("IsLastCallDateTimeTreadtedSeperatly")]
    public bool IsLastCallDateTimeTreadtedSeperatly { get; set; }

    [JsonPropertyName("IsCallbackDateTimeTreatedSeperatly")]
    public bool IsCallbackDateTimeTreatedSeperatly { get; set; }

    [JsonPropertyName("IsCallBack")]
    public bool IsCallBack { get; set; }

    [JsonPropertyName("IsNotRecoded")]
    public bool IsNotRecoded { get; set; }

    [JsonPropertyName("ViewAdditionalColumns")]
    public object ViewAdditionalColumns { get; set; }

    [JsonPropertyName("QuestionHasOpenEnd")]
    public string QuestionHasOpenEnd { get; set; }

    [JsonPropertyName("IsNotInClosedStrata")]
    public bool IsNotInClosedStrata { get; set; }

    [JsonPropertyName("InterviewerIds")]
    public string InterviewerIds { get; set; }

    [JsonPropertyName("ResultCodes")]
    public string ResultCodes { get; set; }

    [JsonPropertyName("LastCallResultCodes")]
    public string LastCallResultCodes { get; set; }

    [JsonPropertyName("Languages")]
    public string Languages { get; set; }

    [JsonPropertyName("UserTimeZone")]
    public int UserTimeZone { get; set; }

    [JsonPropertyName("LinkedToA4S")]
    public int LinkedToA4S { get; set; }

    [JsonPropertyName("IsMissingRecords")]
    public bool IsMissingRecords { get; set; }

    [JsonPropertyName("IsMissingRecordsPronto")]
    public bool IsMissingRecordsPronto { get; set; }

    [JsonPropertyName("ExcludeRecordsInInterview")]
    public bool ExcludeRecordsInInterview { get; set; }

    [JsonPropertyName("SqlStatementWithOrWithoutEquation")]
    public object SqlStatementWithOrWithoutEquation { get; set; }

    [JsonPropertyName("Equation")]
    public string Equation { get; set; }

    [JsonPropertyName("UseCurrentDateForStartCallBack")]
    public bool UseCurrentDateForStartCallBack { get; set; }

    [JsonPropertyName("CallBackDateTimeFromDate")]
    public DateTime CallBackDateTimeFromDate { get; set; }

    [JsonPropertyName("CallBackDateTimeFromTime")]
    public DateTime CallBackDateTimeFromTime { get; set; }

    [JsonPropertyName("UseCurrentDateForEndCallBack")]
    public bool UseCurrentDateForEndCallBack { get; set; }

    [JsonPropertyName("CallBackDateTimeToDate")]
    public DateTime CallBackDateTimeToDate { get; set; }

    [JsonPropertyName("CallBackDateTimeToTime")]
    public DateTime CallBackDateTimeToTime { get; set; }

    [JsonPropertyName("UseCurrentDateForStartLastCall")]
    public bool UseCurrentDateForStartLastCall { get; set; }

    [JsonPropertyName("LastCallDateTimeStartDate")]
    public DateTime LastCallDateTimeStartDate { get; set; }

    [JsonPropertyName("LastCallDateTimeStartTime")]
    public DateTime LastCallDateTimeStartTime { get; set; }

    [JsonPropertyName("UseCurrentDateForEndLastCall")]
    public bool UseCurrentDateForEndLastCall { get; set; }

    [JsonPropertyName("LastCallDateTimeEndDate")]
    public DateTime LastCallDateTimeEndDate { get; set; }

    [JsonPropertyName("LastCallDateTimeEndTime")]
    public DateTime LastCallDateTimeEndTime { get; set; }
  }