namespace Shared.Infrastructure.GateManager;

public interface IGateManager
{
    const string BeforeDoingWorkGate = "BeforeDoingWork";
    const string AfterDoingWorkGate = "AfterDoingWork";
    const string GateLoggingFileName = "GateLoggingFile.txt"; 
    
    // Called by your app/controller code
    Task GateReached(string name, CancellationToken cancellationToken);

    // Called by your test
    Task WaitUntilReached(string name, CancellationToken cancellationToken = default);

    // Called by your test
    void ReleaseGate(string name);
}