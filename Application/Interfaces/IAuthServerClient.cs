using Domain.Enums;

namespace Application.Clients;

public interface IAuthServerClient
{
    Task<bool> IsAuthenticated(string jwt);
    Task<HttpResponseMessage> GetRoleForUser(string jwt);
    Task<HttpResponseMessage> IsUserStruck(string jwt);
    Task<HttpResponseMessage> StrikeUser(string jwt, Guid userId);
    Task<HttpResponseMessage> ChangeRoleForUser(string jwt, string requestUserId, Role role);
}