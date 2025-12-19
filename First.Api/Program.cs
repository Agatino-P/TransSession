using First.Api.Configuration;
using NServiceBus.TransactionalSession;
using Second.Contracts.NServiceBus.Commands;

namespace First.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        NServiceBusSettings nServiceBusSettings=builder.Configuration.GetSection("NServiceBus").Get<NServiceBusSettings>()!;
        
        var endpointConfiguration = NBusExtensions.CreateEndpoint(nServiceBusSettings);

        builder.UseNServiceBus(endpointConfiguration);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}



public static class NBusExtensions
{
    public static EndpointConfiguration CreateEndpoint(NServiceBusSettings nServiceBusSettings)
    {
    
        EndpointConfiguration endpointConfiguration = new EndpointConfiguration(nServiceBusSettings.EndPointName);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();
        
        var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
        transport.ConnectionString(nServiceBusSettings.RabbitConnectionString);
        transport.UseConventionalRoutingTopology(QueueType.Quorum);
        
        RoutingSettings<RabbitMQTransport> routing=transport.Routing()!;
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