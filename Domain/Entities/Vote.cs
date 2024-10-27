namespace Domain.Entities;

public class Vote : _BaseEntity
{
    // Parameterless constructor for EF Core
    public Vote()
    {
    }

    public Vote(Option option, Survey survey)
    {
        Option = option;
        Survey = survey;
        OptionId = option.Id;
        SurveyId = survey.Id;
    }

    public Option Option { get; set; }
    public Guid OptionId { get; set; }
    public Guid SurveyId { get; set; }
    public Survey Survey { get; set; }
}