using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Application.Clients;
using Application.Interfaces;
using Application.UseCases;
using Application.Utils;
using Domain.Entities;
using Domain.Enums;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.UseCases;

[TestSubject(typeof(UcUser))]
public class UcUserTest
{
    private const string Jwt = "invalid_jwt";
    private readonly Mock<ILogger<UcUser>> _loggerMock = new();
    private readonly Mock<IAuthServerClient> _authServerClientMock = new();
    private readonly Mock<IJwtUtil> _jwtUtilMock = new();
    private readonly Mock<IRequestChangeRoleRepository> _requestChangeRoleRepositoryMock = new();
    private readonly UcUser _ucUser;

    public UcUserTest()
    {
        _ucUser = new UcUser(_loggerMock.Object, _requestChangeRoleRepositoryMock.Object,
            _jwtUtilMock.Object, _authServerClientMock.Object);
    }

    [Fact]
    public async Task RequestRoleChange_InvalidJwt_ReturnsFalseWithErrorMessage()
    {
        var role = "Moderator";
        var reason = "Need more permissions";
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Throws(new Exception("Invalid JWT"));
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK }));
        var result = await _ucUser.RequestRoleChange(Jwt, role, reason);

        Assert.False(result.success);
        Assert.Equal("Fehler beim parsen des JWT", result.message);
    }

    [Fact]
    public async Task RequestRoleChange_UserHasOpenRequest_ReturnsFalseWithMessage()
    {
        var role = "Moderator";
        var reason = "Need more permissions";
        var userId = Guid.NewGuid().ToString();
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FilterAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(new List<RequestChangeRole> { new() });
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK }));
        var result = await _ucUser.RequestRoleChange(Jwt, role, reason);

        Assert.False(result.success);
        Assert.Equal("Es gibt bereits eine offene Anfrage", result.message);
    }

    [Fact]
    public async Task RequestRoleChange_GettingCurrentRoleForUser_ReturnsFalseWithMessage()
    {
        var role = "Moderator";
        var reason = "Need more permissions";
        var userId = Guid.NewGuid().ToString();
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FilterAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(new List<RequestChangeRole>());
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK }));
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage
                { Content = new StringContent("User"), StatusCode = HttpStatusCode.InternalServerError }));

        var result = await _ucUser.RequestRoleChange(Jwt, role, reason);

        Assert.False(result.success);
        Assert.Equal("User", result.message);
    }

    [Fact]
    public async Task RequestRoleChange_CannotParseNewRole_ReturnsFalseWithMessage()
    {
        var role = "UnknownRole";
        var reason = "Need more permissions";
        var userId = Guid.NewGuid().ToString();
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FilterAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(new List<RequestChangeRole>());
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK }));
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage
                { Content = new StringContent("User"), StatusCode = HttpStatusCode.OK }));

        var result = await _ucUser.RequestRoleChange(Jwt, role, reason);

        Assert.False(result.success);
        Assert.Equal("Fehler beim parsen der Rolle", result.message);
    }

    [Fact]
    public async Task RequestRoleChange_CannotParseCurrentRole_ReturnsFalseWithMessage()
    {
        var role = "Moderator";
        var reason = "Need more permissions";
        var userId = Guid.NewGuid().ToString();
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FilterAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(new List<RequestChangeRole>());
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK }));
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage { Content = new StringContent("Unknown"), StatusCode = HttpStatusCode.OK }));

        var result = await _ucUser.RequestRoleChange(Jwt, role, reason);

        Assert.False(result.success);
        Assert.Equal("Fehler beim parsen der Rolle", result.message);
    }

    [Fact]
    public async Task RequestRoleChange_UserRoleIsMoreThanOneStepUp_ReturnsFalseWithMessage()
    {
        var role = "Admin";
        var reason = "Need more permissions";
        var userId = Guid.NewGuid().ToString();
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FilterAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(new List<RequestChangeRole>());
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK }));
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage
                { Content = new StringContent("User"), StatusCode = HttpStatusCode.OK }));

        var result = await _ucUser.RequestRoleChange(Jwt, role, reason);

        Assert.False(result.success);
        Assert.Equal("User hat keine Berechtigung", result.message);
    }

    [Fact]
    public async Task RequestRoleChange_ModeratorRoleIsMoreThanOneStepUp_ReturnsFalseWithMessage()
    {
        var role = "Operator";
        var reason = "Need more permissions";
        var userId = Guid.NewGuid().ToString();
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FilterAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(new List<RequestChangeRole>());
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK }));
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage
                { Content = new StringContent("Moderator"), StatusCode = HttpStatusCode.OK }));

        var result = await _ucUser.RequestRoleChange(Jwt, role, reason);

        Assert.False(result.success);
        Assert.Equal("User hat keine Berechtigung", result.message);
    }

    [Fact]
    public async Task RequestRoleChange_ValidRequest_ReturnsTrueWithMessage()
    {
        var role = "Moderator";
        var reason = "Need more permissions";
        var userId = Guid.NewGuid().ToString();
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FilterAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(new List<RequestChangeRole>());
        _authServerClientMock.Setup(client => client.IsUserStruck(Jwt)).Returns(Task.FromResult(
            new HttpResponseMessage { Content = new StringContent("false"), StatusCode = HttpStatusCode.OK }));
        _authServerClientMock.Setup(client => client.GetRoleForUser(Jwt))
            .ReturnsAsync(new HttpResponseMessage
                { Content = new StringContent("User"), StatusCode = HttpStatusCode.OK });
        _requestChangeRoleRepositoryMock.Setup(repo => repo.Add(It.IsAny<RequestChangeRole>()));
        _requestChangeRoleRepositoryMock.Setup(repo => repo.SaveChangesAsync());

        var result = await _ucUser.RequestRoleChange(Jwt, role, reason);

        Assert.True(result.success);
        Assert.Equal("Rollenänderungsanfrage erfolgreich erstellt", result.message);
    }

    [Fact]
    public void GetAllOpenRequests_UserHasNoPermission_ReturnsFalseWithMessage()
    {
        var response = Task.FromResult(new HttpResponseMessage
            { StatusCode = HttpStatusCode.OK, Content = new StringContent("User") });

        var result = _ucUser.GetAllOpenRequests(response);

        Assert.False(result.success);
        Assert.Equal("User hat keine Berechtigung", result.message);
    }

    [Fact]
    public void GetAllOpenRequests_ValidRequest_ReturnsTrueWithRequests()
    {
        var response = Task.FromResult(new HttpResponseMessage
        {
            Content = new StringContent("Admin"),
            StatusCode = HttpStatusCode.OK
        });
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FilterAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(new List<RequestChangeRole> { new() });

        var result = _ucUser.GetAllOpenRequests(response);

        Assert.True(result.success);
        Assert.NotEmpty(result.message);
    }

    [Fact]
    public async Task CloseRequest_InvalidJwt_ReturnsFalseWithErrorMessage()
    {
        var requestUserId = Guid.NewGuid().ToString();
        var isApproved = true;
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Throws(new Exception("Invalid JWT"));

        var result =
            await _ucUser.CloseRequest(Jwt, Task.FromResult(new HttpResponseMessage()), requestUserId, isApproved);

        Assert.False(result.success);
        Assert.Equal("Fehler beim parsen des JWT", result.message);
    }

    [Fact]
    public async Task CloseRequest_NoPermission_ReturnsFalseWithErrorMessage()
    {
        var requestUserId = Guid.NewGuid().ToString();
        var isApproved = true;
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(Guid.NewGuid().ToString());

        var result =
            await _ucUser.CloseRequest(Jwt,
                Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest }), requestUserId,
                isApproved);

        Assert.False(result.success);
        Assert.Equal("User hat keine Berechtigung", result.message);
    }

    [Fact]
    public async Task CloseRequest_RequestNotFound_ReturnsFalseWithMessage()
    {
        var requestUserId = Guid.NewGuid().ToString();
        var isApproved = true;
        var userId = Guid.NewGuid().ToString();
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync((RequestChangeRole)null);

        var result =
            await _ucUser.CloseRequest(Jwt,
                Task.FromResult(new HttpResponseMessage
                    { StatusCode = HttpStatusCode.OK, Content = new StringContent("Moderator") }), requestUserId,
                isApproved);

        Assert.False(result.success);
        Assert.Equal("Anfrage nicht gefunden", result.message);
    }

    [Fact]
    public async Task CloseRequest_ValidRequest_ReturnsTrueWithMessage()
    {
        var requestUserId = Guid.NewGuid().ToString();
        var isApproved = true;
        var userId = Guid.NewGuid().ToString();
        var requestChangeRole = new RequestChangeRole();
        _authServerClientMock.Setup(client => client.ChangeRoleForUser(Jwt, requestUserId, It.IsAny<Role>())).Returns(
            Task.FromResult(
                new HttpResponseMessage
                    { Content = new StringContent("Rolle geändert"), StatusCode = HttpStatusCode.OK }));
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(requestChangeRole);
        _requestChangeRoleRepositoryMock.Setup(repo => repo.SaveChangesAsync());

        var result = await _ucUser.CloseRequest(Jwt, Task.FromResult(new HttpResponseMessage
        {
            Content = new StringContent("Admin"),
            StatusCode = HttpStatusCode.OK
        }), requestUserId, isApproved);

        Assert.True(result.success);
        Assert.Equal("Anfrage erfolgreich bearbeitet", result.message);
    }

    [Fact]
    public async Task CloseRequest_AuthServerError_ReturnsFalseWithMessage()
    {
        var requestUserId = Guid.NewGuid().ToString();
        var isApproved = true;
        var userId = Guid.NewGuid().ToString();
        var requestChangeRole = new RequestChangeRole();
        _authServerClientMock.Setup(client => client.ChangeRoleForUser(Jwt, requestUserId, It.IsAny<Role>())).Returns(
            Task.FromResult(
                new HttpResponseMessage
                {
                    Content = new StringContent("Fehler beim Ändern der Rolle"), StatusCode = HttpStatusCode.BadRequest
                }));
        _jwtUtilMock.Setup(util => util.ParseJwt(Jwt)).Returns(userId);
        _requestChangeRoleRepositoryMock
            .Setup(repo => repo.FindByAsync(It.IsAny<Expression<Func<RequestChangeRole, bool>>>()))
            .ReturnsAsync(requestChangeRole);
        _requestChangeRoleRepositoryMock.Setup(repo => repo.SaveChangesAsync());

        var result = await _ucUser.CloseRequest(Jwt, Task.FromResult(new HttpResponseMessage
        {
            Content = new StringContent("Admin"),
            StatusCode = HttpStatusCode.OK
        }), requestUserId, isApproved);

        Assert.False(result.success);
        Assert.Equal("Fehler beim Ändern der Rolle", result.message);
    }
}