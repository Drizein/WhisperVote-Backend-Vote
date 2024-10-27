using System.Collections;
using Domain.Entities;

namespace Application.Interfaces;

public interface ISurveyRepository : _IBaseRepository<Survey>
{
    Task<IEnumerable<Survey>> GetSurveysExcludedByTagsAsync(List<string> tags);

    Survey? GetSurveyWithDetails(Guid surveyId);
}