namespace Shared.Infrastructure.Database;

public class SqlServerSettings
{
    public const string Section = "SqlServer";

    public string ConnectionString { get; set; } = "";
}

