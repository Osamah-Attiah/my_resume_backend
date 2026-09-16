using System.Text;
using Microsoft.EntityFrameworkCore;
using Resume.Core;

namespace Resume.Infrastructure;

public sealed class ResumeDbContext(DbContextOptions<ResumeDbContext> options) : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<PersonTranslation> PersonTranslations => Set<PersonTranslation>();
    public DbSet<PersonLink> PersonLinks => Set<PersonLink>();
    public DbSet<PersonLinkTranslation> PersonLinkTranslations => Set<PersonLinkTranslation>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillTranslation> SkillTranslations => Set<SkillTranslation>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTranslation> ProjectTranslations => Set<ProjectTranslation>();
    public DbSet<ProjectHighlight> ProjectHighlights => Set<ProjectHighlight>();
    public DbSet<ProjectHighlightTranslation> ProjectHighlightTranslations => Set<ProjectHighlightTranslation>();
    public DbSet<ProjectSkill> ProjectSkills => Set<ProjectSkill>();
    public DbSet<ProjectMedia> ProjectMedia => Set<ProjectMedia>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<ExperienceTranslation> ExperienceTranslations => Set<ExperienceTranslation>();
    public DbSet<ExperienceHighlight> ExperienceHighlights => Set<ExperienceHighlight>();
    public DbSet<ExperienceHighlightTranslation> ExperienceHighlightTranslations => Set<ExperienceHighlightTranslation>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<EducationTranslation> EducationTranslations => Set<EducationTranslation>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<CertificationTranslation> CertificationTranslations => Set<CertificationTranslation>();
    public DbSet<SpokenLanguage> SpokenLanguages => Set<SpokenLanguage>();
    public DbSet<ResumeProfile> ResumeProfiles => Set<ResumeProfile>();
    public DbSet<ResumeProfileTranslation> ResumeProfileTranslations => Set<ResumeProfileTranslation>();
    public DbSet<ProfileSection> ProfileSections => Set<ProfileSection>();
    public DbSet<ProfileContactSetting> ProfileContactSettings => Set<ProfileContactSetting>();
    public DbSet<ProfileProject> ProfileProjects => Set<ProfileProject>();
    public DbSet<ProfileProjectTranslation> ProfileProjectTranslations => Set<ProfileProjectTranslation>();
    public DbSet<ProfileProjectHighlight> ProfileProjectHighlights => Set<ProfileProjectHighlight>();
    public DbSet<ProfileSkill> ProfileSkills => Set<ProfileSkill>();
    public DbSet<ProfileExperience> ProfileExperiences => Set<ProfileExperience>();
    public DbSet<ProfileExperienceHighlight> ProfileExperienceHighlights => Set<ProfileExperienceHighlight>();
    public DbSet<ProfileEducation> ProfileEducations => Set<ProfileEducation>();
    public DbSet<ProfileCertification> ProfileCertifications => Set<ProfileCertification>();
    public DbSet<ProfileLanguage> ProfileLanguages => Set<ProfileLanguage>();
    public DbSet<ProfileLink> ProfileLinks => Set<ProfileLink>();
    public DbSet<ProfilePdfSetting> ProfilePdfSettings => Set<ProfilePdfSetting>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SiteLocale> SiteLocales => Set<SiteLocale>();
    public DbSet<SiteTranslation> SiteTranslations => Set<SiteTranslation>();
    public DbSet<SiteProfile> SiteProfiles => Set<SiteProfile>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaAssetTranslation> MediaAssetTranslations => Set<MediaAssetTranslation>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<PublicationAttempt> PublicationAttempts => Set<PublicationAttempt>();
    public DbSet<PublicationArtifact> PublicationArtifacts => Set<PublicationArtifact>();
    public DbSet<SiteDeployment> SiteDeployments => Set<SiteDeployment>();
    public DbSet<SeoPageSetting> SeoPageSettings => Set<SeoPageSetting>();
    public DbSet<SiteRedirect> SiteRedirects => Set<SiteRedirect>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        ConfigurePerson(b);
        ConfigureContent(b);
        ConfigureProfiles(b);
        ConfigureSitesAndPublishing(b);
        ConfigureLengths(b);

        foreach (var entity in b.Model.GetEntityTypes())
        {
            entity.SetTableName(Snake(entity.GetTableName() ?? entity.ClrType.Name));
            foreach (var property in entity.GetProperties()) property.SetColumnName(Snake(property.Name));
            foreach (var key in entity.GetKeys()) key.SetName(Snake(key.GetName() ?? $"pk_{entity.GetTableName()}"));
            foreach (var index in entity.GetIndexes()) index.SetDatabaseName(Snake(index.GetDatabaseName() ?? $"ix_{entity.GetTableName()}"));
            foreach (var foreignKey in entity.GetForeignKeys()) foreignKey.SetConstraintName(Snake(foreignKey.GetConstraintName() ?? $"fk_{entity.GetTableName()}"));
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<MutableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.Version = 1;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.Version++;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigurePerson(ModelBuilder b)
    {
        b.Entity<Person>().Property(x => x.DefaultLocale).HasConversion<string>();
        b.Entity<PersonTranslation>().Property(x => x.Locale).HasConversion<string>();
        b.Entity<PersonTranslation>().HasIndex(x => new { x.PersonId, x.Locale }).IsUnique();
        b.Entity<PersonTranslation>().HasOne<Person>().WithMany(x => x.Translations).HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PersonLink>().Property(x => x.Kind).HasConversion<string>();
        b.Entity<PersonLink>().HasOne<Person>().WithMany(x => x.Links).HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PersonLinkTranslation>().Property(x => x.Locale).HasConversion<string>();
        b.Entity<PersonLinkTranslation>().HasIndex(x => new { x.PersonLinkId, x.Locale }).IsUnique();
        b.Entity<PersonLinkTranslation>().HasOne<PersonLink>().WithMany(x => x.Translations).HasForeignKey(x => x.PersonLinkId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AdminUser>().HasIndex(x => x.NormalizedEmail).IsUnique();
        b.Entity<AdminUser>().HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Person>().HasIndex(x => x.ArchivedAt).HasFilter("archived_at IS NULL").IsUnique();
    }

    private static void ConfigureContent(ModelBuilder b)
    {
        b.Entity<Skill>().HasIndex(x => new { x.PersonId, x.CanonicalName }).IsUnique();
        Own<Skill, SkillTranslation>(b, x => x.SkillId, x => x.Translations, x => x.Locale);
        b.Entity<Project>().Property(x => x.Kind).HasConversion<string>();
        b.Entity<Project>().HasIndex(x => new { x.PersonId, x.Slug }).IsUnique();
        b.Entity<Project>().HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.CoverAssetId).OnDelete(DeleteBehavior.SetNull);
        Own<Project, ProjectTranslation>(b, x => x.ProjectId, x => x.Translations, x => x.Locale);
        b.Entity<ProjectHighlight>().HasOne<Project>().WithMany(x => x.Highlights).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        Own<ProjectHighlight, ProjectHighlightTranslation>(b, x => x.HighlightId, x => x.Translations, x => x.Locale);
        b.Entity<ProjectSkill>().HasKey(x => new { x.ProjectId, x.SkillId });
        b.Entity<ProjectSkill>().HasOne<Project>().WithMany(x => x.Skills).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProjectSkill>().HasOne<Skill>().WithMany().HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProjectMedia>().HasIndex(x => new { x.ProjectId, x.AssetId }).IsUnique();
        b.Entity<ProjectMedia>().HasOne<Project>().WithMany(x => x.Media).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProjectMedia>().HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        Own<Experience, ExperienceTranslation>(b, x => x.ExperienceId, x => x.Translations, x => x.Locale);
        b.Entity<ExperienceHighlight>().HasOne<Experience>().WithMany(x => x.Highlights).HasForeignKey(x => x.ExperienceId).OnDelete(DeleteBehavior.Cascade);
        Own<ExperienceHighlight, ExperienceHighlightTranslation>(b, x => x.HighlightId, x => x.Translations, x => x.Locale);
        Own<Education, EducationTranslation>(b, x => x.EducationId, x => x.Translations, x => x.Locale);
        Own<Certification, CertificationTranslation>(b, x => x.CertificationId, x => x.Translations, x => x.Locale);
        b.Entity<SpokenLanguage>().HasIndex(x => new { x.PersonId, x.LanguageCode }).IsUnique();
        foreach (var owned in new[] { typeof(Skill), typeof(Project), typeof(Experience), typeof(Education), typeof(Certification), typeof(SpokenLanguage) })
            b.Entity(owned).HasOne(typeof(Person)).WithMany().HasForeignKey(nameof(OwnedEntity.PersonId)).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureProfiles(ModelBuilder b)
    {
        b.Entity<ResumeProfile>().Property(x => x.DefaultLocale).HasConversion<string>();
        b.Entity<ResumeProfile>().HasIndex(x => new { x.PersonId, x.Slug }).IsUnique();
        b.Entity<ResumeProfile>().HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        Own<ResumeProfile, ResumeProfileTranslation>(b, x => x.ProfileId, x => x.Translations, x => x.Locale);
        b.Entity<ProfileSection>().HasIndex(x => new { x.ProfileId, x.SectionKey }).IsUnique();
        b.Entity<ProfileSection>().HasOne<ResumeProfile>().WithMany(x => x.Sections).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileContactSetting>().HasKey(x => x.ProfileId);
        b.Entity<ProfileContactSetting>().HasOne<ResumeProfile>().WithOne().HasForeignKey<ProfileContactSetting>(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileProject>().HasIndex(x => new { x.ProfileId, x.ProjectId }).IsUnique();
        b.Entity<ProfileProject>().HasOne<ResumeProfile>().WithMany(x => x.Projects).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileProject>().HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        Own<ProfileProject, ProfileProjectTranslation>(b, x => x.ProfileProjectId, x => x.Translations, x => x.Locale);
        b.Entity<ProfileProjectHighlight>().HasKey(x => new { x.ProfileProjectId, x.HighlightId });
        b.Entity<ProfileProjectHighlight>().HasOne<ProfileProject>().WithMany().HasForeignKey(x => x.ProfileProjectId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileProjectHighlight>().HasOne<ProjectHighlight>().WithMany().HasForeignKey(x => x.HighlightId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProfileSkill>().HasKey(x => new { x.ProfileId, x.SkillId });
        b.Entity<ProfileSkill>().HasOne<ResumeProfile>().WithMany(x => x.Skills).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileSkill>().HasOne<Skill>().WithMany().HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProfileExperience>().HasIndex(x => new { x.ProfileId, x.ExperienceId }).IsUnique();
        b.Entity<ProfileExperience>().HasOne<ResumeProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileExperience>().HasOne<Experience>().WithMany().HasForeignKey(x => x.ExperienceId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProfileExperienceHighlight>().HasKey(x => new { x.ProfileExperienceId, x.HighlightId });
        b.Entity<ProfileExperienceHighlight>().HasOne<ProfileExperience>().WithMany().HasForeignKey(x => x.ProfileExperienceId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileExperienceHighlight>().HasOne<ExperienceHighlight>().WithMany().HasForeignKey(x => x.HighlightId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProfileEducation>().HasKey(x => new { x.ProfileId, x.EducationId });
        b.Entity<ProfileEducation>().HasOne<ResumeProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileEducation>().HasOne<Education>().WithMany().HasForeignKey(x => x.EducationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProfileCertification>().HasKey(x => new { x.ProfileId, x.CertificationId });
        b.Entity<ProfileCertification>().HasOne<ResumeProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileCertification>().HasOne<Certification>().WithMany().HasForeignKey(x => x.CertificationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProfileLanguage>().HasKey(x => new { x.ProfileId, x.SpokenLanguageId });
        b.Entity<ProfileLanguage>().HasOne<ResumeProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileLanguage>().HasOne<SpokenLanguage>().WithMany().HasForeignKey(x => x.SpokenLanguageId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProfileLink>().HasKey(x => new { x.ProfileId, x.PersonLinkId });
        b.Entity<ProfileLink>().HasOne<ResumeProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ProfileLink>().HasOne<PersonLink>().WithMany().HasForeignKey(x => x.PersonLinkId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProfilePdfSetting>().Property(x => x.Locale).HasConversion<string>();
        b.Entity<ProfilePdfSetting>().Property(x => x.TemplateKey).HasDefaultValue("senior-mobile-engineer-v1");
        b.Entity<ProfilePdfSetting>().Property(x => x.PaperSize).HasDefaultValue("Letter");
        b.Entity<ProfilePdfSetting>().Property(x => x.FontSize).HasDefaultValue(9.5m);
        b.Entity<ProfilePdfSetting>().Property(x => x.MarginMm).HasDefaultValue(16.5m);
        b.Entity<ProfilePdfSetting>().Property(x => x.TargetPages).HasDefaultValue(2);
        b.Entity<ProfilePdfSetting>().HasIndex(x => new { x.ProfileId, x.Locale }).IsUnique();
        b.Entity<ProfilePdfSetting>().HasOne<ResumeProfile>().WithMany(x => x.PdfSettings).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureSitesAndPublishing(ModelBuilder b)
    {
        b.Entity<Site>().Property(x => x.DefaultLocale).HasConversion<string>();
        b.Entity<Site>().HasIndex(x => new { x.PersonId, x.Slug }).IsUnique();
        b.Entity<Site>().HasIndex(x => x.PersonId).HasFilter("is_primary_identity_site = true").IsUnique();
        b.Entity<Site>().HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SiteLocale>().HasKey(x => new { x.SiteId, x.Locale });
        b.Entity<SiteLocale>().Property(x => x.Locale).HasConversion<string>();
        b.Entity<SiteLocale>().HasOne<Site>().WithMany(x => x.Locales).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        Own<Site, SiteTranslation>(b, x => x.SiteId, x => x.Translations, x => x.Locale);
        b.Entity<SiteProfile>().HasIndex(x => new { x.SiteId, x.PathSlug }).IsUnique();
        b.Entity<SiteProfile>().HasIndex(x => new { x.SiteId, x.ProfileId }).IsUnique();
        b.Entity<SiteProfile>().HasIndex(x => x.SiteId).HasFilter("is_default = true").IsUnique();
        b.Entity<SiteProfile>().HasOne<Site>().WithMany(x => x.Profiles).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SiteProfile>().HasOne<ResumeProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Restrict);
        Own<MediaAsset, MediaAssetTranslation>(b, x => x.AssetId, x => x.Translations, x => x.Locale);
        b.Entity<MediaAsset>().HasIndex(x => new { x.PersonId, x.Sha256 }).IsUnique();
        b.Entity<MediaAsset>().HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Publication>().Property(x => x.Purpose).HasConversion<string>();
        b.Entity<Publication>().Property(x => x.State).HasConversion<string>();
        b.Entity<Publication>().Property(x => x.Snapshot).HasColumnType("jsonb");
        b.Entity<Publication>().HasIndex(x => new { x.PersonId, x.IdempotencyKey }).IsUnique();
        b.Entity<Publication>().HasIndex(x => new { x.SiteId, x.Revision }).IsUnique();
        b.Entity<Publication>().HasIndex(x => new { x.ProfileId, x.Revision }).IsUnique();
        b.Entity<Publication>().ToTable(t => t.HasCheckConstraint("ck_publication_target", "(site_id IS NULL) <> (profile_id IS NULL)"));
        b.Entity<Publication>().HasOne<Person>().WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Publication>().HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Publication>().HasOne<ResumeProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Site>().HasOne<Publication>().WithMany().HasForeignKey(x => x.CurrentPublicationId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<PublicationAttempt>().Property(x => x.State).HasConversion<string>();
        b.Entity<PublicationAttempt>().HasIndex(x => new { x.PublicationId, x.AttemptNumber }).IsUnique();
        b.Entity<PublicationAttempt>().HasOne<Publication>().WithMany(x => x.Attempts).HasForeignKey(x => x.PublicationId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PublicationArtifact>().Property(x => x.Kind).HasConversion<string>();
        b.Entity<PublicationArtifact>().Property(x => x.Visibility).HasConversion<string>();
        b.Entity<PublicationArtifact>().Property(x => x.Locale).HasConversion<string>();
        b.Entity<PublicationArtifact>().Property(x => x.QaReport).HasColumnType("jsonb");
        b.Entity<PublicationArtifact>().HasOne<Publication>().WithMany(x => x.Artifacts).HasForeignKey(x => x.PublicationId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<PublicationArtifact>().HasOne<ResumeProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PublicationArtifact>().HasIndex(x => new { x.PublicationId, x.ProfileId, x.Locale }).IsUnique().HasFilter("kind = 'Pdf'");
        b.Entity<PublicationArtifact>().HasIndex(x => x.PublicationId).IsUnique().HasFilter("kind = 'Manifest'");
        b.Entity<SiteDeployment>().HasIndex(x => x.AttemptId).IsUnique();
        b.Entity<SiteDeployment>().HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SiteDeployment>().HasOne<Publication>().WithMany().HasForeignKey(x => x.PublicationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SiteDeployment>().HasOne<PublicationAttempt>().WithMany().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SeoPageSetting>().Property(x => x.Locale).HasConversion<string>();
        b.Entity<SeoPageSetting>().HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SeoPageSetting>().HasOne<ResumeProfile>().WithMany().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SeoPageSetting>().HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SeoPageSetting>().HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.OgAssetId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<SiteRedirect>().HasIndex(x => new { x.SiteId, x.SourcePath }).IsUnique();
        b.Entity<SiteRedirect>().HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureLengths(ModelBuilder b)
    {
        foreach (var entity in b.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(string)))
        {
            var name = property.Name;
            property.SetMaxLength(name.Contains("Description", StringComparison.OrdinalIgnoreCase) ? 10_000 :
                name.Contains("Summary", StringComparison.OrdinalIgnoreCase) ? 2_000 :
                name is "Snapshot" or "QaReport" or "Metadata" or "PasswordHash" ? null : 1_000);
        }
        b.Entity<ProjectTranslation>().Property(x => x.Name).HasMaxLength(200);
        b.Entity<ResumeProfileTranslation>().Property(x => x.Headline).HasMaxLength(200);
        b.Entity<PersonTranslation>().Property(x => x.FullName).HasMaxLength(200);
        b.Entity<ProjectHighlightTranslation>().Property(x => x.Text).HasMaxLength(1000);
        b.Entity<ExperienceHighlightTranslation>().Property(x => x.Text).HasMaxLength(1000);
    }

    private static void Own<TParent, TTranslation>(ModelBuilder b,
        System.Linq.Expressions.Expression<Func<TTranslation, Guid>> fk,
        System.Linq.Expressions.Expression<Func<TParent, IEnumerable<TTranslation>?>> navigation,
        System.Linq.Expressions.Expression<Func<TTranslation, Locale>> locale)
        where TParent : class where TTranslation : class
    {
        var foreignKeyName = ((System.Linq.Expressions.MemberExpression)fk.Body).Member.Name;
        var localeName = ((System.Linq.Expressions.MemberExpression)locale.Body).Member.Name;
        b.Entity<TTranslation>().Property(locale).HasConversion<string>();
        b.Entity<TTranslation>().HasIndex(foreignKeyName, localeName).IsUnique();
        b.Entity<TTranslation>().HasOne<TParent>().WithMany(navigation).HasForeignKey(foreignKeyName).OnDelete(DeleteBehavior.Cascade);
    }

    private static string Snake(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && i > 0) result.Append('_');
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
