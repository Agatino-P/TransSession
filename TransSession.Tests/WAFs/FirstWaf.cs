using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.GateManager;

namespace TransSession.Tests.WAFs;

public class FirstWaf : BaseWaf<First.Api.Program>
{

    public MultiGateManager GateManager { get; } = new MultiGateManager();
    
    public FirstWaf(string rabbitMqConnectionString, string msSqlConnectionString)
        : base(rabbitMqConnectionString, msSqlConnectionString)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder webHostBuilder)
    {
        webHostBuilder.ConfigureServices(services =>
        {
            ServiceDescriptor? existingIGateManagerServiceDescriptor =
                services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(IGateManager));

            if (existingIGateManagerServiceDescriptor is not null)
            {
                services.Remove(existingIGateManagerServiceDescriptor);
            }

            services.AddSingleton<IGateManager>(GateManager);
        });
        base.ConfigureWebHost(webHostBuilder);
    }
}