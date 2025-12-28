using Shared.Database.Entities;

namespace Shared.Database.Repository;

public interface IPocLogEntryRepository
{
    Task AddEntry(LogEntryType logEntryType, string description);
}