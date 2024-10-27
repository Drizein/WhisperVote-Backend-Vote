using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Application.Clients;
using Application.DTOs;
using Application.Interfaces;
using Application.UseCases;
using Application.Utils;
using Domain.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.UseCases;

[TestSubject(typeof(UcSurvey))]
public class UcSurveyTest
{
    private const string Jwt = "invalid_jwt";
    private readonly Mock<IAuthServerClient> _authServerClientMock = new();
    private readonly Mock<IJwtUtil> _jwtUtilMock = new();
    private readonly Mock<ISurveyRepository> _surveyRepositoryMock = new();
    private readonly Mock<IKeyPairRepository> _keyPairRepositoryMock = new();
    private readonly Mock<IOptionRepository> _optionRepositoryMock = new();
    private readonly Mock<IVoteRepository> _voteRepositoryMock = new();
    private readonly UcSurvey _ucSurvey;
    private readonly Mock<ILogger<UcSurvey>> _loggerMock = new();

    public UcSurveyTest()
    {
        _ucSurvey = new UcSurvey(_loggerMock.Object, _surveyRepositoryMock.Object, _keyPairRepositoryMock.Object,
            _optionRepositoryMock.Object, _voteRepositoryMock.Object, _authServerClientMock.Object,
            _jwtUtilMock.Object);
    }

    [Fact]
    public async Task CreateSurvey_UserIsStruck_ReturnsFalseWithMessage()
    {
        CreateSurveyDto createSurveyDto = new("Survey", "Description", ["op1", "op2"], DateTime.Now.AddDays(6),
            ["tag1", "tag2"], "Info");
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("true") });

        var result = await _ucSurvey.CreateSurvey(Jwt, createSurveyDto);

        Assert.False(result.success);
        Assert.Equal("User ist gesperrt für diese Aktion", result.message);
    }

    [Fact]
    public async Task CreateSurvey_InvalidJwt_ReturnsFalseWithErrorMessage()
    {
        CreateSurveyDto createSurveyDto = new("Survey", "Description", ["op1", "op2"], DateTime.Now.AddDays(6),
            ["tag1", "tag2"], "Info");
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Throws(new Exception("Invalid JWT"));
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt))
            .ReturnsAsync(new HttpResponseMessage
                { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK });

        var result = await _ucSurvey.CreateSurvey(Jwt, createSurveyDto);

        Assert.False(result.success);
        Assert.Equal("Fehler beim parsen des JWT", result.message);
    }

    [Fact]
    public async Task CreateSurvey_ValidRequest_ReturnsTrueWithMessage()
    {
        CreateSurveyDto createSurveyDto = new("Survey", "Description", ["op1", "op2"], DateTime.Now.AddDays(6),
            ["tag1", "tag2"], "Info");
        var survey = new Survey { Id = Guid.NewGuid() };
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(Guid.NewGuid().ToString());
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt))
            .ReturnsAsync(new HttpResponseMessage
                { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK });
        _surveyRepositoryMock.Setup(repo => repo.Add(It.IsAny<Survey>()));
        _surveyRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.FromResult(true));
        _surveyRepositoryMock.Setup(repo => repo.GetSurveyWithDetails(It.IsAny<Guid>())).Returns(survey);

        var result = await _ucSurvey.CreateSurvey(Jwt, createSurveyDto);

        Assert.True(result.success);
        Assert.Equal(survey.Id.ToString(), result.message);
    }

    [Fact]
    public async Task GetAllSurveysSFW_ReturnsSurveysWithKeys()
    {
        var surveys = new List<Survey>
            { new Survey("Survey", "Description", DateTime.Now.AddDays(10), "Info", Guid.NewGuid()) };
        _surveyRepositoryMock.Setup(repo => repo.GetSurveysExcludedByTagsAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(surveys);
        _keyPairRepositoryMock.Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<KeyPair, bool>>>()))
            .ReturnsAsync(new KeyPair(surveys.FirstOrDefault()!.Id, "PublicKey", "PrivateKey"));

        var result = await _ucSurvey.GetAllSurveysSFW();

        Assert.True(result.success);
        Assert.NotEmpty(result.message);
    }

    [Fact]
    public async Task Vote_InvalidSurveyId_ReturnsFalseWithErrorMessage()
    {
        var signatureMessageDto = new SignatureMessageDto("encrypted_message", Guid.NewGuid().ToString());
        _keyPairRepositoryMock.Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<KeyPair, bool>>>()))
            .ReturnsAsync((KeyPair)null);

        var result = await _ucSurvey.Vote(signatureMessageDto);

        Assert.False(result.success);
        Assert.Equal("Schlüsselpaar nicht gefunden", result.message);
    }

    [Fact]
    public async Task Vote_ValidVote_ReturnsTrueWithMessage()
    {
        var signatureMessageDto = new SignatureMessageDto(
            "iIRyr+jEOx/Y7PQu4NPlvDyznPEaAyXzhdjKKBHtt5LcvyrvkqCCCkt9xH2FKk7BalGg8lYPcI4vqfFHnDmaOIJzS0qkndt/SEKhSPrag0Xz26vHa6In9MxR9J3CYH3jOSMZYTFZn3lo0UbiXcI+tVeOdUz1pbTV36EzWYDwy96NS/l0pyU0yacBjtLeHn2dIxa68IOfivMjkw8NvbEKbi+HoNWkzmuBBofdw2OjsD44uDy2Mn1uks1y4o+HTK8v/Dv4z53xNYCjT6hFVUKQw29BcqgzD6Fj3mZGVBlpNQAhIyNv1cQRmwhPJ0H7DWcawQaNiWtW0jZMsbBwaFI9GA==",
            Guid.NewGuid().ToString());
        var survey = new Survey("Survey", "Description", DateTime.Now.AddDays(10), "Info", Guid.NewGuid());
        var option = new Option("Option", survey)
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001")
        };
        survey.Options.Add(option);

        _keyPairRepositoryMock.Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<KeyPair, bool>>>()))
            .ReturnsAsync(new KeyPair(survey.Id,
                "MIIEowIBAAKCAQEAzNi1QoHhiD5Shr9ny9oj5p4eUuwa7IEAfWz6ePF5Y4oRHmxogjWBHY7FZPn8urmoYEkhueor54+sNVmsTmADaJ7X2ANaG0ea87r7SUpnbiAmmlMU4GeIr4HqKMQUeyp8kqSWYmI66YWQijVNPng/kwIefl5KhXIqOfeJej3GfzqKLeKxoLkystotULSUK6xtrhzZX659vN+3GyCl5+Z57GpDjqlB+hdl6B8mCBk16A4z53AeP9nT0O2m5bGd6RPSN7KaphXJifD9r8Kjlq0NdY5x2DCAnYC7SIm0gDpPMe0IlYn/tJm8Rrc3ZoCKUyULan7KyyC4U53hL9wd7D13kQIDAQABAoIBAQCRRXo+YTedVH1YPDOTGO9u5GGi8vghE1dSm8+Bp9YrZkW/qqfu95zKZm7MrpCxp2qmZha57z0Vqgk5iI+uwV6JemSeN7pWMFDOWqNMlayGJ9zYguUCQ8pmlR6HrI7NzhKsOHbB7OHUrDkWGrjd/Y8wZUdU3O/CdXVGyKrBez/jB03oKzvo+l1UgsNgCCSs00m08b4cj4qEgG47KbPv7V7WIdXHUXxn6C+obE8OPXgdDDeJWM2q6Uf/w4V2i9xtrRMLEZ7dtVvxE4n4ukdAOcM5YNgIAV6nwMv9FMgY2zZ1eJUYAGLF/o4NYeiuu3BCf9mlMlD8VLridgvpDkU23BLVAoGBANIln0Cd2iiWKaU7H7piElIzde5tVk+1PfsvXDKald+UiywCUD3qELcQ92VCZvMcv0Dqg8kl26Tn9trYHpXsIMQW7lvrsyjpZj+97+XMN/v+1H/ONE2EWwFc3iZVhLvrTSYihmjijKlaKXNDSso6JhGfpGn1lF4bp/CM6HsjwAh/AoGBAPmLA5MYG+WVQYFjOIUiONY8SW2vba7Y96ly8/wZBO2sK1MYcHW5wUW0N7OrxE2yho+t6FgwCV7ldqqYsM7h9PwMs9H61ZVcU7XUaJhJ9tj9Cn/Kcm7PlY2aY2uWxslP5PsHWZxCqvranp7fKB4tJN7lZZP2Wb4oEfljai4CvffvAoGAUD5fdiwQfskANADEl8YVGuBdmuKTP7KEbWLjQMt4iTxOfEqR22KCaXUIEtltOE301dP26JsVKP4Oa/h0jWjyBg5/jAgPjIK7MYHUlhoKEqZ2/CqAHE169qVAisDDA4LRHcu3KVvAvHYaN9ItP9U+biJYhMFqmxjSYu9bYpH/JP8CgYAKLJDIvoLkqWEyVUlIpEyM74hO4IgoSNBQKE+qR5rb1dkuWiC7rAclQGPE/4vRXyX9VanTbqHzLIfaDL+or629WQc72G482LbRAwgArYNS9X4oF6jyu6PtUg4bpNoV+xvq4DHXHSC7eY5eC9sm39BRBilODw05o4iYEmWR2qrEIQKBgGsscXo4nTVzved1zSdW01jDxU09p25069PKjbouA50MeTfT2ZfPb5h7SHMdRl7UaEmbF0OFof6bO5TZWu6MpLEuOWRJYyEIVfrTcBNntnTOdVNrPAotDaDhdpTAR3a26BHeI38Uq0X4KQVPQQbr+i4asp2iNEuVtWmCzzhmvbkK",
                "MIIBCgKCAQEAzNi1QoHhiD5Shr9ny9oj5p4eUuwa7IEAfWz6ePF5Y4oRHmxogjWBHY7FZPn8urmoYEkhueor54+sNVmsTmADaJ7X2ANaG0ea87r7SUpnbiAmmlMU4GeIr4HqKMQUeyp8kqSWYmI66YWQijVNPng/kwIefl5KhXIqOfeJej3GfzqKLeKxoLkystotULSUK6xtrhzZX659vN+3GyCl5+Z57GpDjqlB+hdl6B8mCBk16A4z53AeP9nT0O2m5bGd6RPSN7KaphXJifD9r8Kjlq0NdY5x2DCAnYC7SIm0gDpPMe0IlYn/tJm8Rrc3ZoCKUyULan7KyyC4U53hL9wd7D13kQIDAQAB"));
        _surveyRepositoryMock.Setup(repo => repo.GetSurveyWithDetails(It.IsAny<Guid>())).Returns(survey);
        _voteRepositoryMock.Setup(repo => repo.Add(It.IsAny<Vote>()));
        _surveyRepositoryMock.Setup(repo => repo.SaveChangesAsync());
        _voteRepositoryMock.Setup(repo => repo.SaveChangesAsync());

        var result = await _ucSurvey.Vote(signatureMessageDto);

        Assert.True(result.success);
        Assert.Equal("Stimme erfolgreich abgegeben", result.message);
    }


    [Fact]
    public async Task AddNewTagsToSurvey_ValidRequest_ReturnsTrueWithMessage()
    {
        AddNewTagsDto tagsDto = new(Guid.NewGuid().ToString(), ["tag1", "tag2"]);
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt))
            .ReturnsAsync(new HttpResponseMessage
                { Content = new StringContent("true"), StatusCode = HttpStatusCode.OK });
        _surveyRepositoryMock.Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<Survey, bool>>>()))
            .Returns(Task.FromResult(new Survey("Survey", "Description", DateTime.Now.AddDays(6), "Info",
                Guid.NewGuid())));

        var result = await _ucSurvey.AddNewTagsToSurvey(Jwt, tagsDto);

        Assert.True(result.success);
        Assert.Equal("Tags hinzugefügt", result.message);
    }

    [Fact]
    public async Task AddNewTagsToSurvey_InvalidJwt_ReturnsFalseWithErrorMessage()
    {
        AddNewTagsDto tagsDto = new(Guid.NewGuid().ToString(), ["tag1", "tag2"]);
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt))
            .ReturnsAsync(new HttpResponseMessage
                { Content = new StringContent("false"), StatusCode = HttpStatusCode.BadRequest });

        var result = await _ucSurvey.AddNewTagsToSurvey(Jwt, tagsDto);

        Assert.False(result.success);
        Assert.Equal("User hat keine Berechtigung", result.message);
    }
}