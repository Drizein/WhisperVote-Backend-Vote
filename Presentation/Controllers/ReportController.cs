using Application.Clients;
using Application.DTOs;
using Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace WhisperVote_Backend_Result.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class ReportController : ControllerBase
{
    private readonly ILogger<ReportController> _logger;
    private readonly UcReport _ucReport;
    private readonly IAuthServerClient _authServerClient;


    public ReportController(ILogger<ReportController> logger, UcReport ucReport, IAuthServerClient authServerClient)
    {
        _logger = logger;
        _ucReport = ucReport;
        _authServerClient = authServerClient;
    }


    [HttpPost]
    public async Task<ActionResult<string>> ReportSurvey([FromBody] ReportSurveyDto reportSurveyDto, string jwt)
    {
        _logger.LogDebug("SurveyController - reportSurvey");
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, message) = await _ucReport.ReportSurvey(jwt, reportSurveyDto);
        if (success) return Ok(message); // HTTP 200
        return BadRequest(message);
    }


    [HttpGet]
    public async Task<ActionResult<string>> GetAllOpenReports(string jwt)
    {
        _logger.LogDebug("SurveyController - getAllOpenReports");
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, message) = await _ucReport.GetAllOpenReports(jwt);
        if (success) return Ok(message); // HTTP 200
        return BadRequest(message);
    }

    [HttpPost]
    public async Task<ActionResult<string>> CloseReport(string jwt, [FromBody] CloseReportDto closeReportDto)
    {
        _logger.LogDebug("SurveyController - CloseReport");
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, message) = await _ucReport.CloseReport(jwt, closeReportDto);
        if (success) return Ok(message); // HTTP 200
        return BadRequest(message);
    }
}