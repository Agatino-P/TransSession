using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure.Database.Entities;

namespace Shared.Infrastructure.Database.EntityConfigurations;

public sealed class PocLogEntryConfiguration : IEntityTypeConfiguration<PocLogEntry>
{
    public void Configure(EntityTypeBuilder<PocLogEntry> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.EntryType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);
    }
}