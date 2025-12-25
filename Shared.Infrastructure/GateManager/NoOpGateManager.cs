namespace Shared.Infrastructure.GateManager;

public sealed class NoOpGateManager : IGateManager
{
    public Task GateReached(string name, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task WaitUntilReached(string name, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "WaitUntilReached is test-only. Replace IGateManager with MultiGateManager in tests.");
    }

    public void ReleaseGate(string name)
    {
        throw new NotSupportedException(
            "ReleaseGate is test-only. Replace IGateManager with MultiGateManager in tests.");
    }
}
