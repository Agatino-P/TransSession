using Shared.Infrastructure.GateManager;

namespace TransSession.Tests.WAFs;

public class FirstWaf : BaseWaf<First.Api.Program>
{
    public FirstWaf(
        string rabbitMqConnectionString,
        string msSqlConnectionString,
        string nginxBaseAddress,
        MultiGateManager multiGateManager)
        : base(
            rabbitMqConnectionString: rabbitMqConnectionString,
            msSqlConnectionString: msSqlConnectionString,
            nginxBaseAddress: nginxBaseAddress,
            multiGateManager: multiGateManager)
    {
    }
}