using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.GateManager;

namespace TransSession.Tests.WAFs;

public class FirstWaf : BaseWaf<First.Api.Program>
{
    public FirstWaf(string rabbitMqConnectionString, string msSqlConnectionString,  MultiGateManager multiGateManager)
        : base(rabbitMqConnectionString, msSqlConnectionString, multiGateManager)
    {
    }
}