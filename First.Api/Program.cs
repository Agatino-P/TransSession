using First.Contracts.NServiceBus;
using NServiceBus.TransactionalSession;

namespace TransSession.First.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var endpointConfiguration = NBusExtensions.CreateEndpoint("First.Api");

        builder.UseNServiceBus(endpointConfiguration);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
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
    public static EndpointConfiguration CreateEndpoint(string endpointName)
    {
        EndpointConfiguration endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();
        var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
        transport.ConnectionString("host=localhost;username=guest;password=guest");
        transport.UseConventionalRoutingTopology(QueueType.Quorum);
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.SqlDialect<SqlDialect.MsSqlServer>();

        persistence.ConnectionBuilder(() =>
            new Microsoft.Data.SqlClient.SqlConnection(
                "Server=localhost;Database=Nsb;User Id=sa;Password=SaPassword123;TrustServerCertificate=True"));

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