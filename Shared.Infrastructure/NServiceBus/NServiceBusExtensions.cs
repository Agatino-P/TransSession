using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using NServiceBus.TransactionalSession;
using Shared.Infrastructure.Contracts;
using Shared.Infrastructure.Contracts.Commands;

namespace Shared.Infrastructure.NServiceBus;

public static class NServiceBusExtensions
{

    public static WebApplicationBuilder SharedConfigureNServiceBus(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Host.UseNServiceBus(hostContext =>
        {
            NServiceBusSettings settings = hostContext.Configuration
                .GetSection("NServiceBus")
                .Get<NServiceBusSettings>()!;

            EndpointConfiguration endpointConfiguration = NServiceBusExtensions.CreateEndpoint(settings);
            return endpointConfiguration;
        });
        
        return  webApplicationBuilder;
    }


    public static EndpointConfiguration CreateEndpoint(NServiceBusSettings nServiceBusSettings)
    {
        EndpointConfiguration endpointConfiguration = new EndpointConfiguration(nServiceBusSettings.EndPointName);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();

        TransportExtensions<RabbitMQTransport>? transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
        transport.ConnectionString(nServiceBusSettings.RabbitMqConnectionString);
        transport.UseConventionalRoutingTopology(QueueType.Quorum);

        RoutingSettings<RabbitMQTransport> routing = transport.Routing()!;
        routing.RouteToEndpoint(typeof(SecondApiCommand), SecondApiCommand.Endpoint);

        PersistenceExtensions<SqlPersistence> persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();
        persistence.ConnectionBuilder(() =>
            new Microsoft.Data.SqlClient.SqlConnection(
                nServiceBusSettings.PersistenceConnectionString));

        endpointConfiguration.EnableOutbox();
        persistence.EnableTransactionalSession();

        ConventionsBuilder? conventions = endpointConfiguration.Conventions();
        conventions.DefiningCommandsAs(MessageTypes.IsCommand);
        conventions.DefiningEventsAs(MessageTypes.IsEvent);

        return endpointConfiguration;
    }
}