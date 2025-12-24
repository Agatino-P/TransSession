namespace TransSession.Tests.WAFs;

public class FirstWaf : BaseWaf<First.Api.Program>
{
    // private readonly string _rabbitMqConnectionString;
    // private readonly string _msSqlConnectionString;

    public FirstWaf(string rabbitMqConnectionString, string msSqlConnectionString)
        : base(rabbitMqConnectionString, msSqlConnectionString)
    {
        // _rabbitMqConnectionString = rabbitMqConnectionString;
        // _msSqlConnectionString = msSqlConnectionString;
    }

    // protected override IHost CreateHost(IHostBuilder builder)
    // {
    //     builder.ConfigureHostConfiguration (cfg =>
    //     {
    //         Dictionary<string, string?> keyValuePairs = new Dictionary<string, string?>
    //         {
    //             ["NServiceBus:RabbitMqConnectionString"] = _rabbitMqConnectionString,
    //             ["NServiceBus:PersistenceConnectionString"] = _msSqlConnectionString,
    //         };
    //         cfg.AddInMemoryCollection(keyValuePairs);
    //     });
    //     return base.CreateHost(builder);
    // }
}