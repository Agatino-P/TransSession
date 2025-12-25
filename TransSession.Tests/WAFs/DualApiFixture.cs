using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Database;
using Shared.Infrastructure.Database.Repository;
using Shared.Infrastructure.GateManager;
using Testcontainers.RabbitMq;
using Testcontainers.MsSql;

namespace TransSession.Tests.WAFs;

public sealed class DualApiFixture : IAsyncLifetime
{
    public HttpClient FirstWafClient{ get; private set; } = null!;
    private HttpClient SecondWafClient{ get; set; } = null!;
    public PocDbContext PocDbContext { get; private set; } = null!;
    public PocLogEntryRepository PocLogEntryRepository { get; private set; } = null!;
    
    public MultiGateManager FirstWafGateManager=>_firstWaf.GateManager;

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
        
        initializeDbContext(sqlConnectionString);
        initializeRepository();
        
        initializeWafClients(sqlConnectionString);
    }

    private void initializeRepository()
    {
        PocLogEntryRepository = new PocLogEntryRepository(PocDbContext);
    }

    private void initializeDbContext(string sqlConnectionString)
    {
        DbContextOptions<PocDbContext> pocDbContextOptions = new DbContextOptionsBuilder<PocDbContext>()
            .UseSqlServer(sqlConnectionString) 
            .Options;

        PocDbContext=new PocDbContext(pocDbContextOptions);
    }

    private void initializeWafClients(string sqlConnectionString)
    {
        _firstWaf = new FirstWaf(_rabbitMqContainer.GetConnectionString(), sqlConnectionString);
        _secondWaf = new SecondWaf(_rabbitMqContainer.GetConnectionString(), sqlConnectionString);
        
        FirstWafClient = _firstWaf.CreateClient();
        FirstWafClient.Timeout=TimeSpan.FromSeconds(30);
        
        SecondWafClient= _secondWaf.CreateClient();
        SecondWafClient.Timeout = Timeout.InfiniteTimeSpan;
        
    }

    private async Task<string> ConfigureSqlServer()
    {
        string masterConnectionString = _sqlContainer.GetConnectionString()!;
        await using (SqlConnection sqlConnection = new SqlConnection(masterConnectionString))
        {
            await sqlConnection.OpenAsync();
            await using SqlCommand? sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "IF DB_ID(N'poc') IS NULL CREATE DATABASE [poc];";
            await sqlCommand.ExecuteNonQueryAsync();
        }

        SqlConnectionStringBuilder sqlConnectionStringBuilder =
            new (masterConnectionString) { InitialCatalog = "poc" };
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