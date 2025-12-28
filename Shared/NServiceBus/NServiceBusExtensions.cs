using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using NServiceBus.TransactionalSession;
using Shared.Contracts;
using Shared.Contracts.Commands;

namespace Shared.NServiceBus;

public static class NServiceBusExtensions
{
    public static WebApplicationBuilder SharedConfigureNServiceBus(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Host.UseNServiceBus(hostContext =>
        {
            NServiceBusSettings settings = hostContext.Configuration.GetNServiceBusSettings();

            EndpointConfiguration endpointConfiguration = CreateEndpoint(settings);
            return endpointConfiguration;
        });

        return webApplicationBuilder;
    }

    public static NServiceBusSettings GetNServiceBusSettings(this IConfiguration configuration) =>
        configuration.GetSection(NServiceBusSettings.SectionName).Get<NServiceBusSettings>()!;

    private static EndpointConfiguration CreateEndpoint(NServiceBusSettings nServiceBusSettings)
    {
        EndpointConfiguration endpointConfiguration = new EndpointConfiguration(nServiceBusSettings.EndPointName);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();

        TransportExtensions<RabbitMQTransport>? transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
        transport.ConnectionString(nServiceBusSettings.RabbitMqConnectionString);
        transport.UseConventionalRoutingTopology(QueueType.Quorum);

        transport.ManagementApiConfiguration(
            url: nServiceBusSettings.RabbitMqManagementApiUrl,
            userName: nServiceBusSettings.RabbitMqManagementApiUser,
            password: nServiceBusSettings.RabbitMqManagementApiPassword
            );
        
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

        // TODO: Remove this after testing Transactional Session rollback
        endpointConfiguration.Recoverability().Immediate(c => c.NumberOfRetries(0));
        endpointConfiguration.Recoverability().Delayed(c => c.NumberOfRetries(0));

        return endpointConfiguration;
    }
}