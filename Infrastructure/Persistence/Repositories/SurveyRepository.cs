using System.Collections;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Infrastructure.Persistence.Repositories;

public class SurveyRepository(CDbContext context) : _BaseRepository<Survey>(context), ISurveyRepository
{
    public async Task<IEnumerable<Survey>> GetSurveysExcludedByTagsAsync(List<string> tags)
    {
        return await DbSet
            .Where(s => !s.Tags.Any(t => tags.Contains(t.Value)))
            .Include(s => s.Options)
            .ToListAsync();
    }

    public Survey? GetSurveyWithDetails(Guid surveyId)
    {
        return context.Surveys
            .Include(s => s.Tags)
            .Include(s => s.Options)
            .FirstOrDefault(s => s.Id == surveyId);
    }
}