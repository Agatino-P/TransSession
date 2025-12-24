using Shared.Infrastructure.Database.Entities;

namespace Shared.Infrastructure.Database.Repository;

public interface IPocLogEntryRepository
{
    Task AddEntry(LogEntryType logEntryType, string description);
}