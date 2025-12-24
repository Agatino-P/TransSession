namespace TransSession.Tests.WAFs;

public class SecondWaf : BaseWaf<Second.Api.Program>
{
    public SecondWaf(string rabbitMqConnectionString, string msSqlConnectionString) : base(rabbitMqConnectionString, msSqlConnectionString)
    {
    }
}