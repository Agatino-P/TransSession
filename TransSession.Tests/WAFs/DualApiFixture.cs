namespace TransSession.Tests.WAFs;

public sealed class DualApiFixture : IAsyncLifetime
{
    public FirstWaf FirstWaf { get; } = new();
    public SecondWaf SecondWaf { get; } = new();


    public ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {

        await FirstWaf.DisposeAsync();
        await SecondWaf.DisposeAsync();
        await Task.CompletedTask;
    }
}