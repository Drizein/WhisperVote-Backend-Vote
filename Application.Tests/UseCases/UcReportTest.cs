using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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

[TestSubject(typeof(UcReport))]
public class UcReportTest
{
    private const string Jwt = "invalid_jwt";
    private readonly Mock<ILogger<UcReport>> _loggerMock = new();
    private readonly Mock<IAuthServerClient> _authServerClientMock = new();
    private readonly Mock<IJwtUtil> _jwtUtilMock = new();
    private readonly Mock<ISurveyRepository> _surveyRepositoryMock = new();
    private readonly Mock<IReportSurveyRepository> _reportSurveyRepositoryMock = new();
    private readonly UcReport _ucReport;

    public UcReportTest()
    {
        _ucReport = new UcReport(_loggerMock.Object, _authServerClientMock.Object, _surveyRepositoryMock.Object,
            _reportSurveyRepositoryMock.Object, _jwtUtilMock.Object);
    }

    [Fact]
    public async Task ReportSurvey_UserIsStruck_ReturnsFalseWithMessage()
    {
        // Arrange
        var reportSurveyDto = new ReportSurveyDto(Guid.NewGuid().ToString(), "Spam");
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("true") });

        // Act
        var result = await _ucReport.ReportSurvey(Jwt, reportSurveyDto);

        // Assert
        Assert.False(result.success);
        Assert.Equal("User ist gesperrt für diese Aktion", result.message);
    }

    [Fact]
    public async Task ReportSurvey_InvalidJwt_ReturnsFalseWithErrorMessage()
    {
        // Arrange
        var jwt = "invalid_jwt";
        var reportSurveyDto = new ReportSurveyDto(Guid.NewGuid().ToString(), "Spam");
        _jwtUtilMock.Setup(util => util.ParseJwt(jwt)).Throws(new Exception("Invalid JWT"));
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("false") });

        // Act
        var result = await _ucReport.ReportSurvey(jwt, reportSurveyDto);

        // Assert
        Assert.False(result.success);
        Assert.Equal("Fehler beim parsen des JWT", result.message);
    }

    [Fact]
    public async Task ReportSurvey_SurveyNotFound_ReturnsFalseWithMessage()
    {
        // Arrange
        var reportSurveyDto = new ReportSurveyDto(Guid.NewGuid().ToString(), "Spam");
        _surveyRepositoryMock.Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<Survey, bool>>>()))
            .ReturnsAsync((Survey)null);
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("false") });

        // Act
        var result = await _ucReport.ReportSurvey(Jwt, reportSurveyDto);

        // Assert
        Assert.False(result.success);
        Assert.Equal("Umfrage nicht gefunden", result.message);
    }

    [Fact]
    public async Task ReportSurvey_ValidRequest_ReturnsTrueWithMessage()
    {
        // Arrange
        var reportSurveyDto = new ReportSurveyDto(Guid.NewGuid().ToString(), "Spam");
        var survey = new Survey { Id = Guid.Parse(reportSurveyDto.SurveyId) };
        _surveyRepositoryMock.Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<Survey, bool>>>()))
            .ReturnsAsync(survey);
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("false") });
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(Guid.NewGuid().ToString);

        // Act
        var result = await _ucReport.ReportSurvey(Jwt, reportSurveyDto);

        // Assert
        Assert.True(result.success);
        Assert.Equal("Umfrage wurde gemeldet", result.message);
    }

    [Fact]
    public async Task GetAllOpenReports_UserHasNoPermission_ReturnsFalseWithMessage()
    {
        // Arrange
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("User") });

        // Act
        var result = await _ucReport.GetAllOpenReports(Jwt);

        // Assert
        Assert.False(result.success);
        Assert.Equal("User hat keine Berechtigung", result.message);
    }

    [Fact]
    public async Task GetAllOpenReports_ValidRequest_ReturnsTrueWithReports()
    {
        // Arrange
        List<ReportSurvey> reports =
        [
            new(new Survey("1", "description", DateTime.Now.AddDays(5), "information", Guid.NewGuid()),
                "Spam", Guid.NewGuid()),

            new(new Survey("2", "description", DateTime.Now.AddDays(5), "information", Guid.NewGuid()),
                "Spam", Guid.NewGuid())
        ];
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("Admin") });
        _reportSurveyRepositoryMock.Setup(repo => repo.FilterAsync(It.IsAny<Expression<Func<ReportSurvey, bool>>>()))
            .ReturnsAsync(reports);

        // Act
        var result = await _ucReport.GetAllOpenReports(Jwt);

        // Assert
        Assert.True(result.success);
        Assert.NotEmpty(result.message);
    }

    [Fact]
    public async Task CloseReport_InvalidJwt_ReturnsFalseWithErrorMessage()
    {
        // Arrange
        var closeReportDto = new CloseReportDto(Guid.NewGuid().ToString(), "Resolved", false, false);
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Throws(new Exception("Invalid JWT"));

        // Act
        var result = await _ucReport.CloseReport(Jwt, closeReportDto);

        // Assert
        Assert.False(result.success);
        Assert.Equal("Fehler beim parsen des JWT", result.message);
    }

    [Fact]
    public async Task CloseReport_UserHasNoPermission_ReturnsFalseWithMessage()
    {
        // Arrange
        var closeReportDto = new CloseReportDto(Guid.NewGuid().ToString(), "Resolved", false, false);
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("User") });

        // Act
        var result = await _ucReport.CloseReport(Jwt, closeReportDto);

        // Assert
        Assert.False(result.success);
        Assert.Equal("User hat keine Berechtigung", result.message);
    }

    [Fact]
    public async Task CloseReport_ReportNotFound_ReturnsFalseWithMessage()
    {
        // Arrange
        var closeReportDto = new CloseReportDto(Guid.NewGuid().ToString(), "Resolved", false, false);
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("Admin") });
        _reportSurveyRepositoryMock.Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<ReportSurvey, bool>>>()))
            .ReturnsAsync((ReportSurvey)null);

        // Act
        var result = await _ucReport.CloseReport(Jwt, closeReportDto);

        // Assert
        Assert.False(result.success);
        Assert.Equal("Meldung nicht gefunden", result.message);
    }

    [Fact]
    public async Task CloseReport_ValidRequest_ReturnsTrueWithMessage()
    {
        // Arrange
        var closeReportDto = new CloseReportDto(Guid.NewGuid().ToString(), "Resolved", false, false);
        var report = new ReportSurvey { Id = Guid.Parse(closeReportDto.ReportSurveyId) };
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt))
            .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("Admin") });
        _reportSurveyRepositoryMock.Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<ReportSurvey, bool>>>()))
            .ReturnsAsync(report);
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(Guid.NewGuid().ToString);

        // Act
        var result = await _ucReport.CloseReport(Jwt, closeReportDto);

        // Assert
        Assert.True(result.success);
        Assert.Equal("Meldung geschlossen", result.message);
    }
}