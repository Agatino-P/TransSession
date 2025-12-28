namespace Shared.NServiceBus;

public class NServiceBusSettings
{
    public const string SectionName = "NServiceBus";

    public string EndPointName { get; set; } = "";
    public string RabbitMqConnectionString { get; set; } = "";
    public string PersistenceConnectionString { get; set; } = "";
    public string RabbitMqManagementApiUrl { get; set; } = "";
    public string RabbitMqManagementApiUser { get; set; } = "";
    public string RabbitMqManagementApiPassword { get; set; } = "";
}