namespace Shared.Infrastructure.Database.Entities;

public enum LogEntryType
{
    Undefined = 0,
    CommandSent,
    CommandReceived,
    EventSent,
    EventReceived,
    EntryAdded
}