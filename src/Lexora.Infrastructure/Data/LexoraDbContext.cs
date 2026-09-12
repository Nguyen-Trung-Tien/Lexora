using Lexora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Data;

public class LexoraDbContext : DbContext
{
    public LexoraDbContext(DbContextOptions<LexoraDbContext> options) : base(options)
    {
    }

    public DbSet<Word> Words => Set<Word>();
    public DbSet<WordMeaning> WordMeanings => Set<WordMeaning>();
    public DbSet<ExampleSentence> ExampleSentences => Set<ExampleSentence>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<WordTopic> WordTopics => Set<WordTopic>();
    public DbSet<ExamTag> ExamTags => Set<ExamTag>();
    public DbSet<WordExamTag> WordExamTags => Set<WordExamTag>();
    public DbSet<ReviewCard> ReviewCards => Set<ReviewCard>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<StudyLog> StudyLogs => Set<StudyLog>();
    public DbSet<GrammarRule> GrammarRules => Set<GrammarRule>();
    public DbSet<GrammarExample> GrammarExamples => Set<GrammarExample>();
    public DbSet<UserStreak> UserStreaks => Set<UserStreak>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Word configuration
        modelBuilder.Entity<Word>(b =>
        {
            b.HasKey(w => w.Id);
            b.Property(w => w.Term).IsRequired().HasMaxLength(150);
            b.Property(w => w.NormalizedTerm).IsRequired().HasMaxLength(150);
            b.HasIndex(w => w.NormalizedTerm);
            b.HasIndex(w => w.Level);
            b.HasIndex(w => w.PrimaryPartOfSpeech);

            b.HasMany(w => w.Meanings)
                .WithOne(m => m.Word)
                .HasForeignKey(m => m.WordId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(w => w.ReviewCard)
                .WithOne(rc => rc.Word)
                .HasForeignKey<ReviewCard>(rc => rc.WordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // WordMeaning configuration
        modelBuilder.Entity<WordMeaning>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.DefinitionVi).IsRequired().HasMaxLength(500);

            b.HasMany(m => m.Examples)
                .WithOne(e => e.WordMeaning)
                .HasForeignKey(e => e.WordMeaningId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExampleSentence configuration
        modelBuilder.Entity<ExampleSentence>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.SentenceEn).IsRequired().HasMaxLength(1000);
            b.Property(e => e.SentenceVi).IsRequired().HasMaxLength(1000);
        });

        // Topic and WordTopic configuration
        modelBuilder.Entity<WordTopic>(b =>
        {
            b.HasKey(wt => new { wt.WordId, wt.TopicId });

            b.HasOne(wt => wt.Word)
                .WithMany(w => w.WordTopics)
                .HasForeignKey(wt => wt.WordId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(wt => wt.Topic)
                .WithMany(t => t.WordTopics)
                .HasForeignKey(wt => wt.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExamTag and WordExamTag configuration
        modelBuilder.Entity<WordExamTag>(b =>
        {
            b.HasKey(wet => new { wet.WordId, wet.ExamTagId });

            b.HasOne(wet => wet.Word)
                .WithMany(w => w.WordExamTags)
                .HasForeignKey(wet => wet.WordId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(wet => wet.ExamTag)
                .WithMany(et => et.WordExamTags)
                .HasForeignKey(wet => wet.ExamTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ReviewCard configuration
        modelBuilder.Entity<ReviewCard>(b =>
        {
            b.HasKey(rc => rc.Id);
            b.HasIndex(rc => rc.NextReviewAt);
            b.HasIndex(rc => rc.Status);

            b.HasMany(rc => rc.StudyLogs)
                .WithOne(sl => sl.ReviewCard)
                .HasForeignKey(sl => sl.ReviewCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StudySession & StudyLog
        modelBuilder.Entity<StudySession>(b =>
        {
            b.HasKey(ss => ss.Id);
            b.HasMany(ss => ss.StudyLogs)
                .WithOne(sl => sl.StudySession)
                .HasForeignKey(sl => sl.StudySessionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StudyLog>(b =>
        {
            b.HasKey(sl => sl.Id);
            b.HasIndex(sl => sl.CreatedAt);
        });

        // GrammarRule & GrammarExample
        modelBuilder.Entity<GrammarRule>(b =>
        {
            b.HasKey(gr => gr.Id);
            b.Property(gr => gr.Title).IsRequired().HasMaxLength(200);
            b.Property(gr => gr.Structure).IsRequired().HasMaxLength(500);
            b.Property(gr => gr.ExplanationVi).IsRequired();

            b.HasMany(gr => gr.Examples)
                .WithOne(ge => ge.GrammarRule)
                .HasForeignKey(ge => ge.GrammarRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GrammarExample>(b =>
        {
            b.HasKey(ge => ge.Id);
            b.Property(ge => ge.SentenceEn).IsRequired().HasMaxLength(1000);
            b.Property(ge => ge.SentenceVi).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<UserStreak>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.LastActiveDate);
        });
    }
}
