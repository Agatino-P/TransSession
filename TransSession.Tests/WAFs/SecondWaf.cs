using Shared.Infrastructure.GateManager;

namespace TransSession.Tests.WAFs;

public class SecondWaf : BaseWaf<Second.Api.Program>
{
    public SecondWaf
    (
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