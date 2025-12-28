using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Database.Entities;

namespace Shared.Database.EntityConfigurations;

public sealed class PocLogEntryConfiguration : IEntityTypeConfiguration<PocLogEntry>
{
    public void Configure(EntityTypeBuilder<PocLogEntry> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.EntryType)
            .HasConversion<string>()
            .HasMaxLength(50)  
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);
    }
}