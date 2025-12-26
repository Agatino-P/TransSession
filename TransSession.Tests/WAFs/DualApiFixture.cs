using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Database;
using Shared.Infrastructure.Database.Repository;
using Shared.Infrastructure.GateManager;
using Shared.Infrastructure.ToxiProxyTestContainer;
using Testcontainers.RabbitMq;
using Testcontainers.MsSql;

namespace TransSession.Tests.WAFs;

public sealed class DualApiFixture : IAsyncLifetime
{
    public HttpClient FirstWafClient { get; private set; } = null!;
    public HttpClient SecondWafClient { get; set; } = null!;
    public PocDbContext PocDbContext { get; private set; } = null!;
    public PocLogEntryRepository PocLogEntryRepository { get; private set; } = null!;

    public readonly MultiGateManager MultiGateManager = new MultiGateManager();

    public ToxiProxyEndpoint NginxProxy = null!;
    public async Task RestoreAllProxiesAsync() => await _toxiProxyContainer.RestoreAllAsync();
    
    
    private FirstWaf _firstWaf = null!;
    private SecondWaf _secondWaf = null!;

    private ToxiProxyEndpoint _msSqlProxy = null!;
    private ToxiProxyEndpoint _rabbitProxy = null!;
    
    private const string _rabbitMqAlias = "rabbitmq";
    private const string _msSqlAlias = "mssql";
    private const string _nginxAlias = "nginx";
    private const string _toxiproxyAlias = "toxiproxy";

    private readonly INetwork _network;
    private readonly MsSqlContainer _sqlContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly IContainer _nginxContainer;
    private readonly ToxiProxyContainer _toxiProxyContainer;

    public DualApiFixture()
    {
        _network = new NetworkBuilder().Build();

        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("SaPassword123")
            .WithPortBinding(1433, 1433)
            .WithNetwork(_network)
            .WithNetworkAliases(_msSqlAlias)
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management")
            .WithUsername("admin")
            .WithPassword("admin")
            .WithPortBinding(5672, 5672) // AMQP
            .WithPortBinding(15672, 15672) // Management UI
            .WithNetwork(_network)
            .WithNetworkAliases(_rabbitMqAlias)
            .Build();

        _nginxContainer = new ContainerBuilder()
            .WithImage("nginx:alpine")
            .WithNetwork(_network)
            .WithNetworkAliases(_nginxAlias)
            .Build();

        _toxiProxyContainer = new ToxiProxyContainer(
            network: _network,
            networkAlias: _toxiproxyAlias);
    }


    public async ValueTask InitializeAsync()
    {
        await _network.CreateAsync(); // if available in your version

        await Task.WhenAll(
            _sqlContainer.StartAsync(),
            _rabbitMqContainer.StartAsync(),
            _nginxContainer.StartAsync(),
            _toxiProxyContainer.StartAsync()
        );

        await ConfigureSqlServer();

        _msSqlProxy = await _toxiProxyContainer.CreateProxyAsync(
            name: "mssql-proxy",
            proxiedHost: _msSqlAlias,
            proxiedPort: 1433
        );

        SqlConnectionStringBuilder sqlConnectionStringBuilder =
            new(_sqlContainer.GetConnectionString())
            {
                DataSource = $"{_msSqlProxy.MappedHost},{_msSqlProxy.MappedPort}", InitialCatalog = "poc"
            };

        string sqlConnectionString = sqlConnectionStringBuilder.ConnectionString;

        initializeDbContext(sqlConnectionString);
        initializeRepository();

        _rabbitProxy = await _toxiProxyContainer.CreateProxyAsync(
            name: "rabbitmq-proxy",
            proxiedHost: _rabbitMqAlias,
            proxiedPort: 5672
        );

        UriBuilder rabbitMqUriBuilder = new UriBuilder(_rabbitMqContainer.GetConnectionString())
        {
            Host = _rabbitProxy.MappedHost, Port = _rabbitProxy.MappedPort,
        };

        string rabbitMqConnectionString = rabbitMqUriBuilder.ToString();

        NginxProxy = await _toxiProxyContainer.CreateProxyAsync(
            name: "nginx-proxy",
            proxiedHost: _nginxAlias,
            proxiedPort: 80
        );

        string toxiProxyBaseAddress = $"http://{NginxProxy.MappedHost}:{NginxProxy.MappedPort}/";

        initializeWafClients(sqlConnectionString, rabbitMqConnectionString, toxiProxyBaseAddress);
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

        PocDbContext = new PocDbContext(pocDbContextOptions);
    }

    private void initializeWafClients(
        string msSqlConnectionString,
        string rabbitMqConnectionString,
        string nginxBaseAddress)
    {
        _firstWaf = new FirstWaf(
            rabbitMqConnectionString: rabbitMqConnectionString,
            msSqlConnectionString: msSqlConnectionString,
            nginxBaseAddress: nginxBaseAddress,
            MultiGateManager);
        _secondWaf = new SecondWaf(
            rabbitMqConnectionString: rabbitMqConnectionString,
            msSqlConnectionString: msSqlConnectionString, 
            nginxBaseAddress: nginxBaseAddress,
            MultiGateManager);

        FirstWafClient = _firstWaf.CreateClient();

        SecondWafClient = _secondWaf.CreateClient();
    }

    private async Task ConfigureSqlServer()
    {
        string masterConnectionString = _sqlContainer.GetConnectionString()!;
        await using (SqlConnection sqlConnection = new SqlConnection(masterConnectionString))
        {
            await sqlConnection.OpenAsync();
            await using SqlCommand? sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "IF DB_ID(N'poc') IS NULL CREATE DATABASE [poc];";
            await sqlCommand.ExecuteNonQueryAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _firstWaf.DisposeAsync();
        await _secondWaf.DisposeAsync();
        await Task.CompletedTask;
    }
}