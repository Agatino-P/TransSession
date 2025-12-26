using Shared.Infrastructure.GateManager;
using Shared.Infrastructure.NServiceBus;

namespace TransSession.Tests.WAFs;

public class FirstWaf : BaseWaf<First.Api.Program>
{
    public FirstWaf(
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