using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using NServiceBus.TransactionalSession;
using Second.Contracts.NServiceBus.Commands;

namespace Shared.Infrastructure.NServiceBus;

public static class NServiceBusExtensions
{

    public static WebApplicationBuilder SharedConfigureNServiceBus(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Host.UseNServiceBus(hostContext =>
        {
            var settings = hostContext.Configuration
                .GetSection("NServiceBus")
                .Get<NServiceBusSettings>()!;

            var endpointConfiguration = NServiceBusExtensions.CreateEndpoint(settings);
            return endpointConfiguration;
        });
        
        return  webApplicationBuilder;
    }


    public static EndpointConfiguration CreateEndpoint(NServiceBusSettings nServiceBusSettings)
    {
        EndpointConfiguration endpointConfiguration = new EndpointConfiguration(nServiceBusSettings.EndPointName);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();

        var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
        transport.ConnectionString(nServiceBusSettings.RabbitMqConnectionString);
        transport.UseConventionalRoutingTopology(QueueType.Quorum);

        RoutingSettings<RabbitMQTransport> routing = transport.Routing()!;
        routing.RouteToEndpoint(typeof(SecondCommand), SecondCommand.Endpoint);

        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(() =>
            new Microsoft.Data.SqlClient.SqlConnection(
                nServiceBusSettings.PersistenceConnectionString));
        // (Optional but common)
        // persistence.Schema("dbo");

        // --- Consistency features ---
        endpointConfiguration.EnableOutbox();
        persistence.EnableTransactionalSession();

        var conventions = endpointConfiguration.Conventions();
        conventions.DefiningCommandsAs(MessageTypes.IsCommand);
        conventions.DefiningEventsAs(MessageTypes.IsEvent);

        return endpointConfiguration;
    }
}