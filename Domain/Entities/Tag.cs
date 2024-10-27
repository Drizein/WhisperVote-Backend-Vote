namespace Domain.Entities;

public class Tag : _BaseEntity
{
    public string Value { get; set; }
    public Guid SurveyId { get; set; }

    public Tag()
    {
    }

    public Tag(string value, Guid surveyId)
    {
        Value = value;
        SurveyId = surveyId;
    }
}