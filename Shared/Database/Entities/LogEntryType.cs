namespace Shared.Infrastructure.Database.Entities;

public enum LogEntryType
{
    Undefined = 0,
    RestCallReceived,
    CommandSent,
    CommandReceived,
    EventSent,
    EventReceived,
    EntryAdded,
}