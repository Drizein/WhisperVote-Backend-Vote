using System.Text.Json;
using Application.Clients;
using Application.DTOs;
using Application.Interfaces;
using Application.Utils;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.UseCases;

public class UcSurvey(
    ILogger<UcSurvey> logger,
    ISurveyRepository surveyRepository,
    IKeyPairRepository keyPairRepository,
    IOptionRepository optionRepository,
    IVoteRepository voteRepository,
    IAuthServerClient authServerClient,
    IJwtUtil jwtUtil
)
{
    public async Task<(bool success, string message)> CreateSurvey(string jwt, CreateSurveyDto createSurveyDto)
    {
        logger.LogDebug("UCSurvey - CreateSurvey");

        if (authServerClient.IsUserStruck(jwt).Result.Content.ReadAsStringAsync().Result.Contains("true"))
            return (false, "User ist gesperrt für diese Aktion");

        if (createSurveyDto.Runtime < DateTime.Now) return (false, "Laufzeit muss in der Zukunft liegen");

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

        var (publicKey, privateKey) = RSAUtil.GenerateKeyPair();

        var survey = new Survey(
            createSurveyDto.Title,
            createSurveyDto.Description,
            createSurveyDto.Runtime,
            createSurveyDto.Information,
            Guid.Parse(userId)
        );
        var tags = createSurveyDto.Tags.Select(tag => new Tag(tag, survey.Id)).ToList();

        survey.Tags.AddRange(tags);

        surveyRepository.Add(survey);
        await surveyRepository.SaveChangesAsync();
        survey = surveyRepository.GetSurveyWithDetails(survey.Id);

        if (survey == null) return (false, "Umfrage konnte nicht erstellt werden");

        List<Option> options = [];
        createSurveyDto.Options.ForEach(option => options.Add(new Option(option, survey)));
        List<Vote> votes = [];
        votes.AddRange(options.Select(option => new Vote(option, survey)));

        voteRepository.AddRange(votes);
        optionRepository.AddRange(options);

        var keyPair = new KeyPair(survey.Id, privateKey, publicKey);
        keyPairRepository.Add(keyPair);

        await keyPairRepository.SaveChangesAsync();
        await voteRepository.SaveChangesAsync();
        await optionRepository.SaveChangesAsync();

        return (true, survey.Id.ToString());
    }

    public async Task<(bool success, string message)> GetAllSurveysSFW()
    {
        logger.LogDebug("UCSurvey - GetAllSurveysSFW");

        var tags = new List<string>
        {
            "NSFW",
            "18+",
            "Explicit",
            "DoNotShowStruckSurvey"
        };
        var surveys = await surveyRepository.GetSurveysExcludedByTagsAsync(tags);
        List<GetSurveyDto> surveysWithKeys = [];

        foreach (var survey in surveys)
        {
            var keyPair = await keyPairRepository.FindByAsync(x => x.SurveyId == survey.Id);
            if (keyPair != null) surveysWithKeys.Add(new GetSurveyDto(keyPair.PublicKey, BuildSurveyDto(survey)));
        }

        return (true, JsonSerializer.Serialize(surveysWithKeys));
    }

    private SurveyDto BuildSurveyDto(Survey survey)
    {
        return new SurveyDto(survey.Title, survey.Description, survey.Tags, survey.Information,
            survey.Options.Select(option => survey.Runtime > DateTime.Now
                ? new OptionsDto(option.Value, 0, option.Id.ToString())
                : new OptionsDto(option.Value,
                    option.Votes.Count != 0 ? option.Votes.Count : 0, option.Id.ToString())).ToList(),
            survey.Runtime > DateTime.Now ? 0 : survey.TotalVotes, survey.Runtime, survey.Id.ToString());
    }

    public async Task<(bool success, string message)> GetAllSurveysExcludedByTags(List<string> tags)
    {
        logger.LogDebug("UCSurvey - GetAllSurveysFilteredByTags");
        tags.Add("DoNotShowStruckSurvey");
        var surveys = await surveyRepository.GetSurveysExcludedByTagsAsync(tags);
        List<GetSurveyDto> surveysWithKeys = [];

        foreach (var survey in surveys)
        {
            var keyPair = await keyPairRepository.FindByAsync(x => x.SurveyId == survey.Id);
            if (keyPair != null) surveysWithKeys.Add(new GetSurveyDto(keyPair.PublicKey, BuildSurveyDto(survey)));
        }

        return (true, JsonSerializer.Serialize(surveysWithKeys));
    }

    public async Task<(bool success, string message)> Vote(SignatureMessageDto signatureMessageDto)
    {
        logger.LogDebug("UCSurvey - Vote");

        var keyPair = await keyPairRepository.FindByAsync(x => x.SurveyId == Guid.Parse(signatureMessageDto.SurveyId));

        if (keyPair == null) return (false, "Schlüsselpaar nicht gefunden");

        var optionId = RSAUtil.Decrypt(keyPair.PrivateKey, signatureMessageDto.Message);
        if (optionId == string.Empty) return (false, "Keine Option angegeben");

        var survey = surveyRepository.GetSurveyWithDetails(Guid.Parse(signatureMessageDto.SurveyId));
        if (survey == null) return (false, "Umfrage nicht gefunden");
        if (survey.Runtime < DateTime.Now) return (false, "Umfrage ist abgelaufen");

        var option = survey.Options.FirstOrDefault(o => o.Id == Guid.Parse(optionId));
        if (option == null) return (false, "Option nicht gefunden");

        var vote = new Vote(option, survey);
        voteRepository.Add(vote);

        survey.TotalVotes++;
        survey.Options.FirstOrDefault(o => o.Id == option.Id)!.Votes.Add(vote);

        await surveyRepository.SaveChangesAsync();
        await voteRepository.SaveChangesAsync();

        return (true, "Stimme erfolgreich abgegeben");
    }

    public async Task<(bool success, string tags)> GetAllTags()
    {
        logger.LogDebug("UCSurvey - GetAllTags");
        var tags = new HashSet<string>();
        tags.UnionWith(surveyRepository.GetAllAsync().Result
            .SelectMany(survey => survey.Tags.Select(tag => tag.Value)));
        tags.Remove("DoNotShowStruckSurvey");
        return (true, JsonSerializer.Serialize(tags));
    }

    public async Task<(bool success, string message)> AddNewTagsToSurvey(string jwt, AddNewTagsDto tagsDto)
    {
        logger.LogDebug("UCSurvey - AddNewTagsToSurvey");
        var userRole = await authServerClient.GetRoleForUser(jwt);
        if (!userRole.IsSuccessStatusCode) return (false, "User hat keine Berechtigung");

        var survey = await surveyRepository.FindByAsync(x => x.Id == Guid.Parse(tagsDto.SurveyId));
        if (survey == null) return (false, "Umfrage nicht gefunden");

        var tags = tagsDto.Tags.Select(tag => new Tag(tag, survey.Id)).ToList();
        survey.Tags.AddRange(tags);
        await surveyRepository.SaveChangesAsync();

        return (true, "Tags hinzugefügt");
    }
}
