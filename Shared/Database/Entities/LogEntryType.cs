namespace Shared.Infrastructure.Database.Entities;

public enum LogEntryType
{
    Undefined = 0,
    TestStarted,
    TestWaitUntilReachedComplete,
    TestCompleted,
    RestCallReceived,
    RestCallCompleted,
    CommandSent,
    CommandReceived,
    EventSent,
    EventReceived,
    AppGateReached,
    EntryAdded,
}