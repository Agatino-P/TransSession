using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TransSession.Tests.WAFs;

public abstract class BaseWaf<T> : WebApplicationFactory<T>
    where T : class
{
    private readonly string _rabbitMqConnectionString;
    private readonly string _msSqlConnectionString;

    public BaseWaf(
        string rabbitMqConnectionString,
        string msSqlConnectionString
        )
    {
        _rabbitMqConnectionString = rabbitMqConnectionString;
        _msSqlConnectionString = msSqlConnectionString;
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

}