using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.GateManager;

namespace TransSession.Tests.WAFs;

public abstract class BaseWaf<T> : WebApplicationFactory<T>
    where T : class
{
   
    private readonly string _rabbitMqConnectionString;
    private readonly string _msSqlConnectionString;
    protected readonly MultiGateManager MultiGateManager;

    public BaseWaf(
        string rabbitMqConnectionString,
        string msSqlConnectionString,
        MultiGateManager multiGateManager
        )
    {
        _rabbitMqConnectionString = rabbitMqConnectionString;
        _msSqlConnectionString = msSqlConnectionString;
        MultiGateManager = multiGateManager;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration (cfg =>
        {
            Dictionary<string, string?> keyValuePairs = new Dictionary<string, string?>
            {
                ["NServiceBus:RabbitMqConnectionString"] = _rabbitMqConnectionString,
                ["NServiceBus:PersistenceConnectionString"] = _msSqlConnectionString,
            };
            cfg.AddInMemoryCollection(keyValuePairs);
        });
        return base.CreateHost(builder);
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

            services.AddSingleton<IGateManager>(MultiGateManager);
        });
        base.ConfigureWebHost(webHostBuilder);
    }

}