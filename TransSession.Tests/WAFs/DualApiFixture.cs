using Microsoft.Data.SqlClient;
using Testcontainers.RabbitMq;
using Testcontainers.MsSql;

namespace TransSession.Tests.WAFs;

public sealed class DualApiFixture : IAsyncLifetime
{
    public HttpClient FirstWafClient{ get; private set; } = null!;
    public HttpClient SecondWafClient{ get; private set; } = null!;

    private FirstWaf _firstWaf = null!;
    private SecondWaf _secondWaf= null!;
    
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("SaPassword123")
        .WithPortBinding(1433, 1433)
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management")
        .WithPortBinding(5672, 5672) // AMQP
        .WithPortBinding(15672, 15672) // Management UI
        .WithUsername("admin")
        .WithPassword("admin")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _sqlContainer.StartAsync(),
            _rabbitMqContainer.StartAsync()
        );

        string sqlConnectionString = await ConfigureSqlServer();

        _firstWaf = new FirstWaf(_rabbitMqContainer.GetConnectionString(), sqlConnectionString);
        _secondWaf = new SecondWaf(_rabbitMqContainer.GetConnectionString(), sqlConnectionString);
        
        FirstWafClient = _firstWaf.CreateClient();
        FirstWafClient.Timeout = Timeout.InfiniteTimeSpan;
        
        SecondWafClient= _secondWaf.CreateClient();
        SecondWafClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    private async Task<string> ConfigureSqlServer()
    {
        var masterConnectionString = _sqlContainer.GetConnectionString();
        await using (var sqlConnection = new SqlConnection(masterConnectionString))
        {
            await sqlConnection.OpenAsync();
            await using var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "IF DB_ID(N'poc') IS NULL CREATE DATABASE [poc];";
            await sqlCommand.ExecuteNonQueryAsync();
        }

        SqlConnectionStringBuilder sqlConnectionStringBuilder =
            new SqlConnectionStringBuilder(masterConnectionString) { InitialCatalog = "poc" };
        string sqlConnectionString = sqlConnectionStringBuilder.ToString();
        
        return sqlConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        await _firstWaf.DisposeAsync();
        await _secondWaf.DisposeAsync();
        await Task.CompletedTask;
    }
}