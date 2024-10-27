using System.Text.Json;
using Application.Clients;
using Application.DTOs;
using Application.Interfaces;
using Application.Utils;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.UseCases;

public class UcUser(
    ILogger<UcUser> logger,
    IRequestChangeRoleRepository requestChangeRoleRepository,
    IJwtUtil jwtUtil,
    IAuthServerClient authServerClient
)
{
    public async Task<(bool success, string message)> RequestRoleChange(string jwt, string role, string reason)
    {
        logger.LogDebug("UCUser - RequestRoleChange");
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

        if (requestChangeRoleRepository.FilterAsync(x => x.UserId == Guid.Parse(userId) && x.IsApproved == null).Result
            .Any())
        {
            logger.LogWarning("User has already an open request");
            return (false, "Es gibt bereits eine offene Anfrage");
        }

        var roleForUser = authServerClient.GetRoleForUser(jwt);
        if (!roleForUser.Result.IsSuccessStatusCode)
        {
            logger.LogWarning("Error getting role for user");
            return (false, roleForUser.Result.Content.ReadAsStringAsync().Result);
        }

        Role requestedRoleEnum;
        Role roleUser;
        try
        {
            requestedRoleEnum = Enum.Parse<Role>(role);
            roleUser = Enum.Parse<Role>(roleForUser.Result.Content.ReadAsStringAsync().Result);
        }
        catch (Exception e)
        {
            logger.LogError("Error parsing role: {0a}", e.Message);
            return (false, "Fehler beim parsen der Rolle");
        }

        switch (roleUser)
        {
            case Role.User:
            {
                if (requestedRoleEnum != Role.Moderator) return (false, "User hat keine Berechtigung");
                break;
            }
            case Role.Moderator:
            {
                if (requestedRoleEnum != Role.Admin) return (false, "User hat keine Berechtigung");
                break;
            }
            default:
                return (false, "User hat keine Berechtigung");
        }

        var requestChangeRole = new RequestChangeRole(requestedRoleEnum, Guid.Parse(userId), reason);

        requestChangeRoleRepository.Add(requestChangeRole);
        await requestChangeRoleRepository.SaveChangesAsync();
        return (true, "Rollenänderungsanfrage erfolgreich erstellt");
    }

    public (bool success, string message) GetAllOpenRequests(
        Task<HttpResponseMessage> getRoleForUser)
    {
        var response = getRoleForUser.Result;
        if (!response.IsSuccessStatusCode) return (false, response.Content.ReadAsStringAsync().Result);

        var role = Enum.Parse<Role>(response.Content.ReadAsStringAsync().Result);

        IEnumerable<RequestChangeRole>? requests;
        switch (role)
        {
            case Role.Operator:
                requests = requestChangeRoleRepository.FilterAsync(x => x.IsApproved == null).Result;
                return (true, JsonSerializer.Serialize(BuildAllOpenRequestsMessage(requests)));
            case Role.Admin:
                requests = requestChangeRoleRepository
                    .FilterAsync(x => (x.IsApproved == null && x.Role == Role.Moderator) || x.Role == Role.User).Result;
                return (true, JsonSerializer.Serialize(BuildAllOpenRequestsMessage(requests)));
            case Role.Moderator:
                requests = requestChangeRoleRepository.FilterAsync(x => x.IsApproved == null && x.Role == Role.User)
                    .Result;
                return (true, JsonSerializer.Serialize(BuildAllOpenRequestsMessage(requests)));
            default:
                return (false, "User hat keine Berechtigung");
        }
    }

    private static List<OpenRequestChangeRoleDto> BuildAllOpenRequestsMessage(IEnumerable<RequestChangeRole> requests)
    {
        return requests.Select(request => new OpenRequestChangeRoleDto(request.Id.ToString(), request.CreatedAt,
            request.UserId.ToString(), request.Reason, request.Role.ToString())).ToList();
    }


    public async Task<(bool success, string message)> CloseRequest(string jwt, Task<HttpResponseMessage> getRoleForUser,
        string requestUserId, bool isApproved)
    {
        logger.LogDebug("UCUser - CloseRequest");
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

        var response = getRoleForUser.Result;
        if (!response.IsSuccessStatusCode) return (false, "User hat keine Berechtigung");

        var role = Enum.Parse<Role>(response.Content.ReadAsStringAsync().Result);

        var requestChangeRole = requestChangeRoleRepository
            .FindByAsync(x => x.UserId == Guid.Parse(requestUserId) && x.IsApproved == null).Result;
        if (requestChangeRole == null) return (false, "Anfrage nicht gefunden");
        switch (role)
        {
            case Role.Operator:
                if (isApproved)
                {
                    var authServerResponseMessage = await authServerClient.ChangeRoleForUser(jwt, requestUserId, requestChangeRole.Role);
                    if (!authServerResponseMessage.IsSuccessStatusCode)
                    {
                        logger.LogError("Error changing role for user");
                        return (false, authServerResponseMessage.Content.ReadAsStringAsync().Result);
                    }

                }
                requestChangeRole.IsApproved = isApproved;
                requestChangeRole.ApprovedBy = Guid.Parse(userId);
                await requestChangeRoleRepository.SaveChangesAsync();
                return (true, "Anfrage erfolgreich bearbeitet");
            case Role.Admin:
                if (requestChangeRole.Role is Role.Moderator or Role.User)
                {
                    if (isApproved)
                    {
                        var authServerResponseMessage = await authServerClient.ChangeRoleForUser(jwt, requestUserId, requestChangeRole.Role);
                        if (!authServerResponseMessage.IsSuccessStatusCode)
                        {
                            logger.LogError("Error changing role for user");
                            return (false, authServerResponseMessage.Content.ReadAsStringAsync().Result);
                        }

                    }
                    requestChangeRole.IsApproved = isApproved;
                    requestChangeRole.ApprovedBy = Guid.Parse(userId);
                    await requestChangeRoleRepository.SaveChangesAsync();
                    return (true, "Anfrage erfolgreich bearbeitet");
                }

                break;
            case Role.Moderator:
                if (requestChangeRole.Role is Role.User)
                {
                    if (isApproved)
                    {
                        var authServerResponseMessage = await authServerClient.ChangeRoleForUser(jwt, requestUserId, requestChangeRole.Role);
                        if (!authServerResponseMessage.IsSuccessStatusCode)
                        {
                            logger.LogError("Error changing role for user");
                            return (false, authServerResponseMessage.Content.ReadAsStringAsync().Result);
                        }

                    }
                    requestChangeRole.IsApproved = isApproved;
                    requestChangeRole.ApprovedBy = Guid.Parse(userId);
                    await requestChangeRoleRepository.SaveChangesAsync();
                    return (true, "Anfrage erfolgreich bearbeitet");
                }

                break;
        }

        return (false, "User hat keine Berechtigung");
    }
}