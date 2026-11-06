namespace BpfBuilder.Infrastructure.Persistence.Configurations;

internal sealed class ActivityImportConfiguration : IEntityTypeConfiguration<ActivityImport>
{
    private const int TextLength = ActivityWorkbookParser.TextMaxLength;

    private const int CodeLength = 40;

    public void Configure(EntityTypeBuilder<ActivityImport> builder)
    {
        builder.ToTable("ActivityImports");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.FileName).HasMaxLength(ActivityImport.FileNameMaxLength);
        builder.HasIndex(i => i.FiscalYearId).IsUnique();
        builder.HasOne<FiscalYear>().WithMany().HasForeignKey(i => i.FiscalYearId).OnDelete(DeleteBehavior.Cascade);

        builder.OwnsMany(i => i.Sessions, s =>
        {
            Rows(s, "ImportedSessions");
            s.Property(x => x.Code).HasMaxLength(TextLength);
            s.Property(x => x.Title).HasMaxLength(TextLength);
            s.Property(x => x.PlannedHours).HasPrecision(14, 2);
            s.Property(x => x.DeliveryMode).HasMaxLength(CodeLength);
            s.Property(x => x.DistanceLearning).HasMaxLength(CodeLength);
            s.Property(x => x.TrainingGoal).HasMaxLength(CodeLength);
            s.Property(x => x.RncpLevel).HasMaxLength(CodeLength);
            s.Property(x => x.NsfCode).HasMaxLength(CodeLength);
        });

        builder.OwnsMany(i => i.Learners, l =>
        {
            Rows(l, "ImportedLearners");
            l.Property(x => x.LearnerCode).HasMaxLength(TextLength);
            l.Property(x => x.SessionCode).HasMaxLength(TextLength);
            l.Property(x => x.Category).HasMaxLength(CodeLength);
            l.Property(x => x.HoursAttended).HasPrecision(14, 2);
        });

        builder.OwnsMany(i => i.Trainers, t =>
        {
            Rows(t, "ImportedTrainers");
            t.Property(x => x.TrainerCode).HasMaxLength(TextLength);
            t.Property(x => x.SessionCode).HasMaxLength(TextLength);
            t.Property(x => x.Type).HasMaxLength(CodeLength);
            t.Property(x => x.HoursDelivered).HasPrecision(14, 2);
        });

        builder.OwnsMany(i => i.Revenue, r =>
        {
            Rows(r, "ImportedRevenue");
            r.Property(x => x.FormLine).HasMaxLength(CodeLength);
            r.Property(x => x.Amount).HasPrecision(14, 2);
            r.Property(x => x.SessionCode).HasMaxLength(TextLength);
            r.Property(x => x.Label).HasMaxLength(TextLength);
        });

        builder.OwnsMany(i => i.Costs, c =>
        {
            Rows(c, "ImportedCosts");
            c.Property(x => x.Category).HasMaxLength(CodeLength);
            c.Property(x => x.Amount).HasPrecision(14, 2);
            c.Property(x => x.Label).HasMaxLength(TextLength);
        });
    }

    private static void Rows<TRow>(OwnedNavigationBuilder<ActivityImport, TRow> rows, string table)
        where TRow : class
    {
        rows.ToTable(table);
        rows.WithOwner().HasForeignKey("ActivityImportId");
        rows.Property<int>("Id");
        rows.HasKey("Id");
    }
}
