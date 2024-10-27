namespace Domain.Entities;

public class Option : _BaseEntity
{
    public string Value { get; set; }

    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public Guid SurveyId { get; set; }

    public Option()
    {
    }

    public Option(string value, Survey survey)
    {
        Value = value;
        SurveyId = survey.Id;
    }
}