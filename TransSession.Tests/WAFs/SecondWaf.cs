using Shared.Infrastructure.GateManager;

namespace TransSession.Tests.WAFs;

public class SecondWaf : BaseWaf<Second.Api.Program>
{
    public SecondWaf(string rabbitMqConnectionString, string msSqlConnectionString, MultiGateManager multiGateManager) 
        : base(rabbitMqConnectionString, msSqlConnectionString, multiGateManager)
    {
    }
}