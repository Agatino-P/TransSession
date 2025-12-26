using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using Shared.Infrastructure.Database.Repository;
using Shared.Infrastructure.NServiceBus;

namespace Shared.Infrastructure.Database;

public static class DatabaseExtensions
{
    public static WebApplicationBuilder SharedAddTransactionalSessionAwarePocDbContext(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddScoped<PocDbContext>(serviceProvider =>
        {
            var sqlStorageSession = serviceProvider.GetService<ISynchronizedStorageSession>() as ISqlStorageSession;

            if (sqlStorageSession?.Connection != null)
            {
                var pocDbContext = new PocDbContext(
                    new DbContextOptionsBuilder<PocDbContext>()
                        .UseSqlServer(sqlStorageSession.Connection)
                        .Options);

                pocDbContext.Database.UseTransaction(sqlStorageSession.Transaction);

                return pocDbContext;
            }

            NServiceBusSettings settings = webApplicationBuilder.Configuration
                .GetSection("NServiceBus")
                .Get<NServiceBusSettings>()!;
            
            return new PocDbContext(
                new DbContextOptionsBuilder<PocDbContext>()
                    .UseSqlServer(settings.PersistenceConnectionString)
                    .Options);
        });

        return webApplicationBuilder;
    }


    public static async Task EnsurePocDbCreatedAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        PocDbContext pocDbContext = scope.ServiceProvider.GetRequiredService<PocDbContext>();

        await pocDbContext.Database.EnsureCreatedAsync();
    }

    public static IServiceCollection SharedAddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPocLogEntryRepository, PocLogEntryRepository>();
        return services;
    }
}