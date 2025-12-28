using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shared.Database;
using Shared.Database.Repository;
using Shared.GateManager;
using Shared.NServiceBus;
using Shared.ToxiProxyTestContainer;
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

    private FirstWaf _firstWaf = null!;
    private SecondWaf _secondWaf = null!;

    private ToxiProxyEndpoint _msSqlProxy = null!;
    private ToxiProxyEndpoint _rabbitAmqpProxy = null!;
    private ToxiProxyEndpoint _rabbitAdminProxy = null!;

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
            .WithPortBinding(1433, true)
            .WithNetwork(_network)
            .WithNetworkAliases(_msSqlAlias)
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management")
            .WithUsername("admin")
            .WithPassword("admin")
            // .WithPortBinding(5672, 5672) // AMQP
            // .WithPortBinding(15672, 15672) // Management UI
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

        _msSqlProxy = await _toxiProxyContainer.CreateProxyAsync(
            name: "mssql-proxy",
            proxiedHost: _msSqlAlias,
            proxiedPort: 1433
        );

        string mssqlConfigurationConnectionString = getSqlConnectionString(
            originalConnectionString: _sqlContainer.GetConnectionString(),
            newHost: _sqlContainer.Hostname,
            newPort: _sqlContainer.GetMappedPublicPort(1433),
            newInitialCatalog: "master");
        await ConfigureSqlServer(mssqlConfigurationConnectionString);

        string fixtureDbContextConnectionString = getSqlConnectionString(
            originalConnectionString: _sqlContainer.GetConnectionString(),
            newHost: _sqlContainer.Hostname,
            newPort: _sqlContainer.GetMappedPublicPort(1433),
            newInitialCatalog: "poc");
        PocDbContext = createFixtureDbContext(fixtureDbContextConnectionString);
        PocLogEntryRepository = new PocLogEntryRepository(PocDbContext);

        _rabbitAmqpProxy = await _toxiProxyContainer.CreateProxyAsync(
            name: "rabbit-amqp-proxy",
            proxiedHost: _rabbitMqAlias,
            proxiedPort: 5672
        );

        _rabbitAdminProxy = await _toxiProxyContainer.CreateProxyAsync(
            name: "rabbit-admin-proxy",
            proxiedHost: _rabbitMqAlias,
            proxiedPort: 15672
        );

        NginxProxy = await _toxiProxyContainer.CreateProxyAsync(
            name: "nginx-proxy",
            proxiedHost: _nginxAlias,
            proxiedPort: 80
        );

        string proxiedMsSqlConnectionString = getSqlConnectionString(
            originalConnectionString: _sqlContainer.GetConnectionString(),
            newHost: _msSqlProxy.MappedHost,
            newPort: _msSqlProxy.MappedPort,
            newInitialCatalog: "poc");

        string proxiedRabbitAmqpConnectionString = new UriBuilder(_rabbitMqContainer.GetConnectionString())
            {
                Host = _rabbitAmqpProxy.MappedHost, Port = _rabbitAmqpProxy.MappedPort,
            }
            .ToString();

        string proxiedRabbitMqManagementUrl =
            new UriBuilder(Uri.UriSchemeHttp, _rabbitAdminProxy.MappedHost, _rabbitAdminProxy.MappedPort)
                .Uri.ToString()
                .TrimEnd('/');

        NServiceBusSettings nServiceBusSettingsForWaf = new NServiceBusSettings()
        {
            RabbitMqConnectionString = proxiedRabbitAmqpConnectionString,
            RabbitMqManagementApiUrl = proxiedRabbitMqManagementUrl,
            RabbitMqManagementApiUser = "admin",
            RabbitMqManagementApiPassword = "admin"
        };
        
        string proxiedNginxBaseAddress = $"http://{NginxProxy.MappedHost}:{NginxProxy.MappedPort}/";

        initializeWafClients(
            msSqlConnectionString: proxiedMsSqlConnectionString,
            nServiceBusSettings: nServiceBusSettingsForWaf,
            proxiedNginxBaseAddress);
    }

    private PocDbContext createFixtureDbContext(string sqlConnectionString)
    {
        DbContextOptions<PocDbContext> pocDbContextOptions = new DbContextOptionsBuilder<PocDbContext>()
            .UseSqlServer(sqlConnectionString)
            .Options;

        return new PocDbContext(pocDbContextOptions);
    }

    private void initializeWafClients(
        string msSqlConnectionString,
        NServiceBusSettings nServiceBusSettings,
        string nginxBaseAddress)
    {
        _firstWaf = new FirstWaf(
            msSqlConnectionString: msSqlConnectionString,
            nServiceBusSettings: nServiceBusSettings,
            nginxBaseAddress: nginxBaseAddress,
            MultiGateManager);
        _secondWaf = new SecondWaf(
            msSqlConnectionString: msSqlConnectionString,
            nServiceBusSettings: nServiceBusSettings,
            nginxBaseAddress: nginxBaseAddress,
            MultiGateManager);

        FirstWafClient = _firstWaf.CreateClient();

        SecondWafClient = _secondWaf.CreateClient();
    }

    private async Task ConfigureSqlServer(string connectionString)
    {
        await using (SqlConnection sqlConnection = new(connectionString))
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


    private string getSqlConnectionString(string originalConnectionString, string newHost, int newPort,
        string? newInitialCatalog = null)
    {
        SqlConnectionStringBuilder sqlConnectionStringBuilder =
            new(originalConnectionString)
            {
                DataSource = $"{newHost},{newPort}",
                InitialCatalog = string.IsNullOrWhiteSpace(newInitialCatalog) ? "" : newInitialCatalog
            };

        return sqlConnectionStringBuilder.ConnectionString;
    }
}