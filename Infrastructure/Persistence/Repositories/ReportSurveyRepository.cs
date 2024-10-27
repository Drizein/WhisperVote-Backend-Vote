using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence.Repositories;

public class ReportSurveyRepository(CDbContext context)
    : _BaseRepository<ReportSurvey>(context), IReportSurveyRepository
{
}