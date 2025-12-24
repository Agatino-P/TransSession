using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Database.Repository;
using Shared.Infrastructure.NServiceBus;

namespace Shared.Infrastructure.Database;

public static class DatabaseExtensions
{
    public static WebApplicationBuilder SharedAddDbContext(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddDbContext<PocDbContext>((sp, options) =>
        {
            var settings = webApplicationBuilder.Configuration
                .GetSection("NServiceBus")
                .Get<NServiceBusSettings>()!;

            options.UseSqlServer(settings.PersistenceConnectionString);
        });

        return webApplicationBuilder;
    }

    public static async Task EnsurePocDbCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var pocDbContext = scope.ServiceProvider.GetRequiredService<PocDbContext>();

        await pocDbContext.Database.EnsureCreatedAsync();
    }

    public static IServiceCollection SharedAddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPocLogEntryRepository, PocLogEntryRepository>();
        return services;
    }
}