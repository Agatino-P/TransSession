using Shared.Infrastructure.Database.Entities;

namespace Shared.Infrastructure.Database.Repository;

public class PocLogEntryRepository : IPocLogEntryRepository
{
    private readonly PocDbContext _pocDbContext;

    public PocLogEntryRepository(PocDbContext pocDbContext)
    {
        _pocDbContext = pocDbContext;
    }

    public async Task AddEntry(LogEntryType logEntryType, string description)
    {
        PocLogEntry pocLogEntry = new(logEntryType, description);
        _pocDbContext.LogEntries.Add(pocLogEntry);
        int actual= await _pocDbContext.SaveChangesAsync();
    }
}