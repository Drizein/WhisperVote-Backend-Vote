using System.Text.Json;
using Application.Clients;
using Application.DTOs;
using Application.Interfaces;
using Application.Utils;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.UseCases;

public class UcReport(
    ILogger<UcReport> logger,
    IAuthServerClient authServerClient,
    ISurveyRepository surveyRepository,
    IReportSurveyRepository reportSurveyRepository,
    IJwtUtil jwtUtil)
{
    public async Task<(bool success, string message)> ReportSurvey(string jwt, ReportSurveyDto reportSurveyDto)
    {
        logger.LogDebug("UCReport - ReportSurvey");

        if (authServerClient.IsUserStruck(jwt).Result.Content.ReadAsStringAsync().Result.Contains("true"))
            return (false, "User ist gesperrt für diese Aktion");

        string userId;
        try
        {
            userId = jwtUtil.ParseJwt(jwt);
        }
        catch (Exception e)
        {
            logger.LogError("Error parsing JWT: {0a}", e.Message);
            return (false, "Fehler beim parsen des JWT");
        }

        var survey = await surveyRepository.FindByAsync(x => x.Id == Guid.Parse(reportSurveyDto.SurveyId));
        if (survey == null) return (false, "Umfrage nicht gefunden");

        var report = new ReportSurvey(survey, reportSurveyDto.Reason, Guid.Parse(userId));
        reportSurveyRepository.Add(report);
        await reportSurveyRepository.SaveChangesAsync();

        return (true, "Umfrage wurde gemeldet");
    }

    public async Task<(bool success, string message)> GetAllOpenReports(string jwt)
    {
        logger.LogDebug("UCReport - GetAllOpenReports");

        var userRole = await authServerClient.GetRoleForUser(jwt);
        if (!userRole.IsSuccessStatusCode || Enum.Parse<Role>(userRole.Content.ReadAsStringAsync().Result) == Role.User)
            return (false, "User hat keine Berechtigung");

        var reports = reportSurveyRepository.FilterAsync(r => r.IsResolved == false).Result.ToList();

        return (true,
            JsonSerializer.Serialize(reports.Select(report =>
                    new GetReportedSurveyDto(
                        new SurveyDto(report.Survey.Title, report.Survey.Description, report.Survey.Tags,
                            report.Survey.Information,
                            report.Survey.Options.Select(r => new OptionsDto(r.Value, 0, "")).ToList(),
                            0, null, report.SurveyId.ToString()), report.Reason, report.Id.ToString()))
                .ToList()));
    }

    public async Task<(bool success, string message)> CloseReport(string jwt, CloseReportDto closeReportDto)
    {
        logger.LogDebug("UCReport - CloseReport");
        string userId;
        try
        {
            userId = jwtUtil.ParseJwt(jwt);
        }
        catch (Exception e)
        {
            logger.LogError("Error parsing JWT: {0a}", e.Message);
            return (false, "Fehler beim parsen des JWT");
        }

        var userRole = await authServerClient.GetRoleForUser(jwt);
        if (!userRole.IsSuccessStatusCode || Enum.Parse<Role>(userRole.Content.ReadAsStringAsync().Result) == Role.User)
            return (false, "User hat keine Berechtigung");

        var report = await reportSurveyRepository.FindByAsync(x => x.Id == Guid.Parse(closeReportDto.ReportSurveyId));
        if (report == null) return (false, "Meldung nicht gefunden");

        if (closeReportDto.StrikeUser) await authServerClient.StrikeUser(jwt, report.ReporterId);
        if (closeReportDto.StrikeSurveyCreator)
        {
            report.Survey.Tags.Add(new Tag("DoNotShowStruckSurvey", report.SurveyId));
            await reportSurveyRepository.SaveChangesAsync();
            await authServerClient.StrikeUser(jwt, report.Survey.CreatorId);
        }


        report.IsResolved = true;
        report.ResolverId = Guid.Parse(userId);
        report.Resolution = closeReportDto.Resolution;
        await reportSurveyRepository.SaveChangesAsync();

        return (true, "Meldung geschlossen");
    }
}