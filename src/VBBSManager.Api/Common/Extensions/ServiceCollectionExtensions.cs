using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using VBBSManager.Api.Features.Alerts.List;
using VBBSManager.Api.Features.Alerts.MarkRead;
using VBBSManager.Api.Features.Auth.Login;
using VBBSManager.Api.Features.Auth.RefreshToken;
using VBBSManager.Api.Features.Creatives.List;
using VBBSManager.Api.Features.Financial.DRE;
using VBBSManager.Api.Features.Financial.Overview;
using VBBSManager.Api.Features.Webhooks.Brevo;
using VBBSManager.Api.Features.Webhooks.Hotmart;
using VBBSManager.Infrastructure.ExternalClients.Hotmart;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Postgres")));

        return services;
    }

    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var secret = config["Jwt:Secret"]!;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
            });

        return services;
    }

    public static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration config)
    {
        services.AddHangfire(hf => hf
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(config.GetConnectionString("Postgres"))));

        services.AddHangfireServer();

        return services;
    }

    public static IServiceCollection AddExternalClients(this IServiceCollection services)
    {
        services.AddHttpClient<IHotmartClient, HotmartClient>()
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

        return services;
    }

    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IFinancialOverviewService, FinancialOverviewService>();
        services.AddScoped<IDreService, DreService>();
        services.AddScoped<ICreativesListService, CreativesListService>();
        services.AddScoped<IAlertsListService, AlertsListService>();
        services.AddScoped<IMarkAlertReadService, MarkAlertReadService>();
        services.AddScoped<IHotmartWebhookService, HotmartWebhookService>();
        services.AddScoped<IBrevoWebhookService, BrevoWebhookService>();

        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "VBBS Manager API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "JWT Bearer token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    []
                }
            });
        });

        return services;
    }
}
