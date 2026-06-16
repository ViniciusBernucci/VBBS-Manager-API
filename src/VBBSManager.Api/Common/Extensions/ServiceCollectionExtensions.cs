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
using VBBSManager.Api.Features.Financial.CashFlow.CreateTransaction;
using VBBSManager.Api.Features.Financial.CashFlow.DeleteTransaction;
using VBBSManager.Api.Features.Financial.CashFlow.GetCashFlow;
using VBBSManager.Api.Features.Financial.CashFlow.SetConfig;
using VBBSManager.Api.Features.Financial.CashFlow.UpdateTransaction;
using VBBSManager.Api.Features.Financial.FixedExpenses.List;
using VBBSManager.Api.Features.Financial.FixedExpenses.MonthlyStatus;
using VBBSManager.Api.Features.Financial.FixedExpenses.Pay;
using VBBSManager.Api.Features.Financial.FixedExpenses.Save;
using VBBSManager.Api.Features.Financial.DailySales;
using VBBSManager.Api.Features.Financial.DRE;
using VBBSManager.Api.Features.Financial.Overview;
using VBBSManager.Api.Features.Planning.Financial.Get;
using VBBSManager.Api.Features.Planning.Financial.Update;
using VBBSManager.Api.Features.Planning.Goals.Get;
using VBBSManager.Api.Features.Planning.Goals.Update;
using VBBSManager.Api.Features.Traffic.Overview;
using VBBSManager.Api.Features.Traffic.Sync;
using VBBSManager.Api.Features.Webhooks.Brevo;
using VBBSManager.Api.Features.Webhooks.Hotmart;
using VBBSManager.Infrastructure.ExternalClients.Hotmart;
using VBBSManager.Infrastructure.ExternalClients.Meta;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }

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

    public static IServiceCollection AddExternalClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HotmartSettings>(options =>
        {
            options.ClientId = configuration["HOTMART_CLIENT_ID"] ?? string.Empty;
            options.ClientSecret = configuration["HOTMART_CLIENT_SECRET"] ?? string.Empty;
        });

        services.AddHttpClient<IHotmartAuthClient, HotmartAuthClient>(client =>
            client.BaseAddress = new Uri("https://api-sec-vlc.hotmart.com"));

        services.AddHttpClient<IHotmartClient, HotmartClient>(client =>
            client.BaseAddress = new Uri("https://developers.hotmart.com/payments/api/v1/"))
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

        services.AddScoped<IHotmartSalesService, HotmartSalesService>();

        // Meta Ads
        services.Configure<MetaSettings>(options =>
        {
            options.AccessToken = configuration["FACEBOOK_TOKEN"] ?? string.Empty;
            options.AdAccountId = configuration["FACEBOOK_AD_ACCOUNT_ID"] ?? string.Empty;
        });

        var metaApiVersion = configuration["FACEBOOK_API_VERSION"] ?? "v25.0";
        services.AddHttpClient<IMetaAdsClient, MetaAdsClient>(client =>
        {
            client.BaseAddress = new Uri($"https://graph.facebook.com/{metaApiVersion}/");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddTransientHttpErrorPolicy(p =>
            p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

        services.AddScoped<IMetaAdsMonthSyncService, MetaAdsMonthSyncService>();

        return services;
    }

    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        services.AddScoped<IGetDailySalesService, GetDailySalesService>();
        services.AddScoped<ISyncDailySalesService, SyncDailySalesService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IFinancialOverviewService, FinancialOverviewService>();
        services.AddScoped<IDreService, DreService>();
        services.AddScoped<ICreativesListService, CreativesListService>();
        services.AddScoped<IAlertsListService, AlertsListService>();
        services.AddScoped<IMarkAlertReadService, MarkAlertReadService>();
        services.AddScoped<IHotmartWebhookService, HotmartWebhookService>();
        services.AddScoped<IBrevoWebhookService, BrevoWebhookService>();
        services.AddScoped<IGetPlanningGoalsService, GetPlanningGoalsService>();
        services.AddScoped<IUpdatePlanningGoalsService, UpdatePlanningGoalsService>();
        services.AddScoped<IGetFinancialConfigService, GetFinancialConfigService>();
        services.AddScoped<IUpdateFinancialConfigService, UpdateFinancialConfigService>();
        services.AddScoped<IGetCashFlowService, GetCashFlowService>();
        services.AddScoped<ISetCashFlowConfigService, SetCashFlowConfigService>();
        services.AddScoped<ICreateTransactionService, CreateTransactionService>();
        services.AddScoped<IUpdateTransactionService, UpdateTransactionService>();
        services.AddScoped<IDeleteTransactionService, DeleteTransactionService>();
        services.AddScoped<IListFixedExpensesService, ListFixedExpensesService>();
        services.AddScoped<ISaveFixedExpenseService, SaveFixedExpenseService>();
        services.AddScoped<IPayFixedExpenseService, PayFixedExpenseService>();
        services.AddScoped<IGetMonthlyStatusService, GetMonthlyStatusService>();
        services.AddScoped<ITrafficSyncService, TrafficSyncService>();
        services.AddScoped<ITrafficOverviewService, TrafficOverviewService>();

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
