using Application.Clients;
using Application.Interfaces;
using Application.UseCases;
using Application.Utils;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

public static class ConfigureService
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<UcSurvey, UcSurvey>();
        services.AddTransient<UcUser, UcUser>();
        services.AddTransient<UcReport, UcReport>();
        services.AddScoped<IAuthServerClient, AuthServerClient>();
        services.AddScoped<IKeyPairRepository, KeyPairRepository>();
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        services.AddScoped<IVoteRepository, VoteRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IOptionRepository, OptionRepository>();
        services.AddScoped<IRequestChangeRoleRepository, RequestChangeRoleRepository>();
        services.AddScoped<IReportSurveyRepository, ReportSurveyRepository>();
        services.AddScoped<IJwtUtil, JwtUtil>();
        services.AddScoped<CDbContext, CDbContext>();
        return services;
    }
}