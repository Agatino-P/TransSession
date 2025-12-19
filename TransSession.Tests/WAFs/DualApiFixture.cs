using Testcontainers.MsSql;

namespace TransSession.Tests.WAFs;

public sealed class DualApiFixture : IAsyncLifetime
{
    public FirstWaf FirstWaf { get; } = new();
    public SecondWaf SecondWaf { get; } = new();

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("SaPassword123")
        .Build();

    public string ConnectionString => _sqlContainer.GetConnectionString();
    public async ValueTask InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {

        await FirstWaf.DisposeAsync();
        await SecondWaf.DisposeAsync();
        await Task.CompletedTask;
    }
}