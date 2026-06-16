using DotNetEnv;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VBBSManager.Api.Common.Extensions;
using VBBSManager.Api.Common.Middleware;
using VBBSManager.Infrastructure.Persistence;

Env.TraversePath().Load();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
            opts.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()));
    builder.Services.AddCorsPolicy(builder.Configuration);
    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddJwtAuth(builder.Configuration);
    builder.Services.AddHangfire(builder.Configuration);
    builder.Services.AddExternalClients(builder.Configuration);
    builder.Services.AddFeatureServices();
    builder.Services.AddSwagger();
    builder.Services.AddEndpointsApiExplorer();

    var app = builder.Build();

    // Aplica migrations pendentes automaticamente na inicialização
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseCors();
    app.UseAuthentication();
    app.UseMiddleware<TenantMiddleware>();
    app.UseAuthorization();
    app.UseHangfireDashboard("/hangfire");
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
