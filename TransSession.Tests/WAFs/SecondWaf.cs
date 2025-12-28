using Shared.GateManager;
using Shared.NServiceBus;

namespace TransSession.Tests.WAFs;

public class SecondWaf : BaseWaf<Second.Api.Program>
{
    public SecondWaf(
        string msSqlConnectionString,
        NServiceBusSettings nServiceBusSettings,
        string nginxBaseAddress,
        MultiGateManager multiGateManager)
        : base(
            msSqlConnectionString: msSqlConnectionString,
            nServiceBusSettings: nServiceBusSettings,            nginxBaseAddress: nginxBaseAddress,
            multiGateManager: multiGateManager)
    {
    }
}