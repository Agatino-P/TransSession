namespace Shared.Infrastructure.NServiceBus;

public class NServiceBusSettings
{
    const string NServiceBusSectionName = "NServiceBus";

    public string EndPointName { get; set; } = "";
    public string RabbitMqConnectionString { get; set; } = "";
    public string PersistenceConnectionString { get; set; } = "";
}

