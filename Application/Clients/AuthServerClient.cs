using System.Text;
using System.Text.Json;
using Application.DTOs;
using Domain.Enums;

namespace Application.Clients;

public class AuthServerClient : IAuthServerClient
{

    private static readonly string UrlValidateToken =
        Environment.GetEnvironmentVariable("ConnectionStrings__AuthServer") + "/Auth/ValidateToken";

    private static readonly string UrlAuthUser =
        Environment.GetEnvironmentVariable("ConnectionStrings__AuthServer") + "/User";

    public async Task<bool> IsAuthenticated(string jwt)
    {
        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwt}");

        var response = await httpClient.GetAsync(UrlValidateToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<HttpResponseMessage> GetRoleForUser(string jwt)
    {
        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwt}");

        var response = await httpClient.GetAsync(UrlAuthUser + "/GetRoleForUser");

        return response;
    }

    public async Task<HttpResponseMessage> IsUserStruck(string jwt)
    {
        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwt}");

        var response = await httpClient.GetAsync(UrlAuthUser + "/IsUserStruck");

        return response;
    }

    public async Task<HttpResponseMessage> StrikeUser(string jwt, Guid userId)
    {
        using var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwt}");

        var response = await httpClient.PatchAsync(UrlAuthUser + $"/StrikeUser?strikedUserId={userId}", null);

        return response;
    }

    public async Task<HttpResponseMessage> ChangeRoleForUser(string jwt, string requestUserId, Role role)
    {
        using var httpClient = new HttpClient();
        var jsonContent = JsonSerializer.Serialize(new ChangeRoleDTO(requestUserId, role));
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwt}");

        return await httpClient.PostAsync(UrlAuthUser + "/ChangeRoleForUser", (content));
    }
}