using Lexora.Domain.Entities.MemoryTips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lexora.Infrastructure.Data.Configurations;

public class MemoryTipConfiguration : IEntityTypeConfiguration<MemoryTip>
{
    public void Configure(EntityTypeBuilder<MemoryTip> builder)
    {
        builder.ToTable("MemoryTips");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(t => t.MnemonicHook)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(t => t.Explanation)
            .IsRequired()
            .HasMaxLength(3000);

        builder.Property(t => t.Example)
            .HasMaxLength(2000);

        builder.Property(t => t.CommonTrap)
            .HasMaxLength(2000);

        builder.Property(t => t.Category)
            .IsRequired();

        builder.Property(t => t.Technique)
            .IsRequired();

        builder.Property(t => t.IsPinned)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.Tags)
            .HasMaxLength(500);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        // Indexes for lightning-fast filtered search and sorting
        builder.HasIndex(t => t.Category);
        builder.HasIndex(t => t.Technique);
        builder.HasIndex(t => t.IsPinned);
        builder.HasIndex(t => t.ExamTarget);
        builder.HasIndex(t => t.LinkedWordId);
        builder.HasIndex(t => t.LinkedGrammarRuleId);
        builder.HasIndex(t => t.CreatedAt);

        // Composite index for common filtering + pinned ordering + creation date
        builder.HasIndex(t => new { t.Category, t.IsPinned, t.CreatedAt });
    }
}
