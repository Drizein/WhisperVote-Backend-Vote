using Application.Clients;
using Application.DTOs;
using Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace WhisperVote_Backend_Result.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class SurveyController : ControllerBase
{
    private readonly ILogger<SurveyController> _logger;
    private readonly UcSurvey _ucSurvey;
    private readonly IAuthServerClient _authServerClient;

    public SurveyController(ILogger<SurveyController> logger, UcSurvey ucSurvey, IAuthServerClient authServerClient)
    {
        _logger = logger;
        _ucSurvey = ucSurvey;
        _authServerClient = authServerClient;
    }

    [HttpPost]
    public async Task<ActionResult<string>> CreateSurvey([FromBody] CreateSurveyDto createSurveyDto, string jwt)
    {
        _logger.LogDebug("SurveyController - createSurvey");
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, message) = await _ucSurvey.CreateSurvey(jwt, createSurveyDto);
        if (success) return Ok(message); // HTTP 200
        return BadRequest(message);
    }

    [HttpGet]
    public async Task<ActionResult<GetSurveyDto>> GetAllSurveysSFW()
    {
        _logger.LogDebug("SurveyController - getAllSurveys");
        var (success, message) = await _ucSurvey.GetAllSurveysSFW();
        if (success) return Ok(message); // HTTP 200
        _logger.LogError("SurveyController - getAllSurveys \n Something bad happened while getting all surveys.");
        return BadRequest(
            "Fehler beim Abrufen der Umfragen. Bitte versuche es erneut oder wende dich an einen Administrator.");
    }

    [HttpGet]
    public async Task<ActionResult<GetSurveyDto>> GetAllSurveysExcludedByTags([FromQuery] List<string> tags, string jwt)
    {
        _logger.LogDebug("SurveyController - getAllSurveys");
        // validate jwt
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, message) = await _ucSurvey.GetAllSurveysExcludedByTags(tags);
        if (success) return Ok(message); // HTTP 200
        _logger.LogError(
            "SurveyController - getAllSurveysFilteredByTags \n Something bad happened while getting all surveys.");
        return BadRequest(
            "Fehler beim Abrufen der Umfragen. Bitte versuche es erneut oder wende dich an einen Administrator.");
    }

    [HttpPost]
    public async Task<ActionResult<string>> Vote([FromBody] SignatureMessageDto signatureMessageDto)
    {
        _logger.LogDebug("SurveyController - vote");
        var (success, message) = await _ucSurvey.Vote(signatureMessageDto);
        if (success) return Ok(message); // HTTP 200
        return BadRequest(message);
    }

    [HttpGet]
    public async Task<ActionResult<string>> GetAllTags(string jwt)
    {
        _logger.LogDebug("SurveyController - getAllTags");
        // validate jwt
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, message) = await _ucSurvey.GetAllTags();
        if (success) return Ok(message); // HTTP 200
        _logger.LogError(
            "SurveyController - getAllSurveysFilteredByTags \n Something bad happened while getting all surveys.");
        return BadRequest(
            "Fehler beim Abrufen der Tags. Bitte versuche es erneut oder wende dich an einen Administrator.");
    }

    [HttpPatch]
    public async Task<ActionResult<HashSet<string>>> AddNewTagsToSurvey(string jwt, [FromBody] AddNewTagsDto tagsDto)
    {
        _logger.LogDebug("SurveyController - getAllTags");
        // validate jwt
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, message) = await _ucSurvey.AddNewTagsToSurvey(jwt, tagsDto);
        if (success) return Ok(message); // HTTP 200
        _logger.LogError(
            "SurveyController - getAllSurveysFilteredByTags \n Something bad happened while getting all surveys.");
        return BadRequest(
            "Fehler beim Abrufen der Umfragen. Bitte versuche es erneut oder wende dich an einen Administrator.");
    }
}