namespace Shared.Database.Entities;

public class PocLogEntry
{
    public Guid Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public LogEntryType EntryType { get; set; } = LogEntryType.Undefined;

    public string Description { get; set; } = string.Empty;

    public PocLogEntry(LogEntryType entryType,  string description)
    {
            
        Id= Guid.NewGuid();
        Timestamp = DateTimeOffset.UtcNow;
        EntryType = entryType;
        Description = description;
    }
}