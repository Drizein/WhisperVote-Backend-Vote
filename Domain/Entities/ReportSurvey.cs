namespace Domain.Entities;

public class ReportSurvey : _BaseEntity
{
    public Survey Survey { get; set; }
    public string Reason { get; set; }
    public Guid ReporterId { get; set; }
    public bool IsResolved { get; set; }
    public Guid ResolverId { get; set; }
    public string? Resolution { get; set; }
    public Guid SurveyId { get; set; }


    public ReportSurvey()
    {
    }

    public ReportSurvey(Survey survey, string reason, Guid reporterId)
    {
        Survey = survey;
        Reason = reason;
        ReporterId = reporterId;
        IsResolved = false;
    }
}