namespace Resume.Core;

public enum Locale { Ar, En }
public enum LinkKind { LinkedIn, GitHub, Website, Other }
public enum ProjectKind { Personal, OpenSource, Freelance, Employment }
public enum PublicationPurpose { SitePublish, PrivatePdfExport }
public enum PublicationState { Queued, Building, Validating, Deploying, Succeeded, Failed, Cancelled }
public enum ArtifactKind { Pdf, Manifest }
public enum ArtifactVisibility { Public, Private }

public abstract class MutableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public long Version { get; set; } = 1;
}

public abstract class OwnedEntity : MutableEntity
{
    public Guid PersonId { get; set; }
}

public sealed class Person : MutableEntity
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? PhotoAssetId { get; set; }
    public Locale DefaultLocale { get; set; } = Locale.En;
    public DateTimeOffset? ArchivedAt { get; set; }
    public List<PersonTranslation> Translations { get; set; } = [];
    public List<PersonLink> Links { get; set; } = [];
}

public sealed class PersonTranslation : MutableEntity
{
    public Guid PersonId { get; set; }
    public Locale Locale { get; set; }
    public string FullName { get; set; } = "";
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? DefaultHeadline { get; set; }
    public string? DefaultSummary { get; set; }
}

public sealed class PersonLink : MutableEntity
{
    public Guid PersonId { get; set; }
    public LinkKind Kind { get; set; }
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
    public List<PersonLinkTranslation> Translations { get; set; } = [];
}

public sealed class PersonLinkTranslation : MutableEntity
{
    public Guid PersonLinkId { get; set; }
    public Locale Locale { get; set; }
    public string Label { get; set; } = "";
}

public sealed class AdminUser : MutableEntity
{
    public Guid PersonId { get; set; }
    public string NormalizedEmail { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int FailedAttempts { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public long SessionVersion { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public sealed class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string Metadata { get; set; } = "{}";
}

public sealed class Skill : OwnedEntity
{
    public string CanonicalName { get; set; } = "";
    public string Category { get; set; } = "";
    public string CategoryEn { get; set; } = "";
    public string CategoryAr { get; set; } = "";
    public DateTimeOffset? ArchivedAt { get; set; }
    public List<SkillTranslation> Translations { get; set; } = [];
}

public sealed class SkillTranslation : MutableEntity
{
    public Guid SkillId { get; set; }
    public Locale Locale { get; set; }
    public string DisplayName { get; set; } = "";
}

public sealed class Project : OwnedEntity
{
    public string Slug { get; set; } = "";
    public ProjectKind Kind { get; set; } = ProjectKind.Personal;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsOngoing { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? DemoUrl { get; set; }
    public Guid? CoverAssetId { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public List<ProjectTranslation> Translations { get; set; } = [];
    public List<ProjectHighlight> Highlights { get; set; } = [];
    public List<ProjectSkill> Skills { get; set; } = [];
    public List<ProjectMedia> Media { get; set; } = [];
}

public sealed class ProjectTranslation : MutableEntity
{
    public Guid ProjectId { get; set; }
    public Locale Locale { get; set; }
    public string Name { get; set; } = "";
    public string? Role { get; set; }
    public string Summary { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class ProjectHighlight : MutableEntity
{
    public Guid ProjectId { get; set; }
    public int SortOrder { get; set; }
    public List<ProjectHighlightTranslation> Translations { get; set; } = [];
}

public sealed class ProjectHighlightTranslation : MutableEntity
{
    public Guid HighlightId { get; set; }
    public Locale Locale { get; set; }
    public string Text { get; set; } = "";
}

public sealed class ProjectSkill
{
    public Guid ProjectId { get; set; }
    public Guid SkillId { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ProjectMedia : MutableEntity
{
    public Guid ProjectId { get; set; }
    public Guid AssetId { get; set; }
    public int SortOrder { get; set; }
    public MediaAsset? Asset { get; set; }
}

public sealed class Experience : OwnedEntity
{
    public string EmploymentType { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? OrganizationUrl { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public List<ExperienceTranslation> Translations { get; set; } = [];
    public List<ExperienceHighlight> Highlights { get; set; } = [];
}

public sealed class ExperienceTranslation : MutableEntity
{
    public Guid ExperienceId { get; set; }
    public Locale Locale { get; set; }
    public string Organization { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string? Location { get; set; }
    public string? Summary { get; set; }
}

public sealed class ExperienceHighlight : MutableEntity
{
    public Guid ExperienceId { get; set; }
    public int SortOrder { get; set; }
    public List<ExperienceHighlightTranslation> Translations { get; set; } = [];
}

public sealed class ExperienceHighlightTranslation : MutableEntity
{
    public Guid HighlightId { get; set; }
    public Locale Locale { get; set; }
    public string Text { get; set; } = "";
}

public sealed class Education : OwnedEntity
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public List<EducationTranslation> Translations { get; set; } = [];
}

public sealed class EducationTranslation : MutableEntity
{
    public Guid EducationId { get; set; }
    public Locale Locale { get; set; }
    public string Institution { get; set; } = "";
    public string Degree { get; set; } = "";
    public string? FieldOfStudy { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public sealed class Certification : OwnedEntity
{
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? CredentialId { get; set; }
    public string? CredentialUrl { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public List<CertificationTranslation> Translations { get; set; } = [];
}

public sealed class CertificationTranslation : MutableEntity
{
    public Guid CertificationId { get; set; }
    public Locale Locale { get; set; }
    public string Name { get; set; } = "";
    public string Issuer { get; set; } = "";
}

public sealed class SpokenLanguage : OwnedEntity
{
    public string LanguageCode { get; set; } = "";
    public string Proficiency { get; set; } = "";
    public string ProficiencyEn { get; set; } = "";
    public string ProficiencyAr { get; set; } = "";
}

public sealed class ResumeProfile : OwnedEntity
{
    public string InternalName { get; set; } = "";
    public string Slug { get; set; } = "";
    public Locale DefaultLocale { get; set; } = Locale.En;
    public DateTimeOffset? ArchivedAt { get; set; }
    public List<ResumeProfileTranslation> Translations { get; set; } = [];
    public List<ProfileSection> Sections { get; set; } = [];
    public List<ProfileProject> Projects { get; set; } = [];
    public List<ProfileSkill> Skills { get; set; } = [];
    public List<ProfilePdfSetting> PdfSettings { get; set; } = [];
}

public sealed class ResumeProfileTranslation : MutableEntity
{
    public Guid ProfileId { get; set; }
    public Locale Locale { get; set; }
    public string Headline { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
}

public sealed class ProfileSection : MutableEntity
{
    public Guid ProfileId { get; set; }
    public string SectionKey { get; set; } = "";
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int WebOrder { get; set; }
    public int PdfOrder { get; set; }
}

public sealed class ProfileContactSetting
{
    public Guid ProfileId { get; set; }
    public bool WebEmail { get; set; } = true;
    public bool PdfEmail { get; set; } = true;
    public bool WebPhone { get; set; }
    public bool PdfPhone { get; set; }
    public bool ShowLocation { get; set; } = true;
    public bool ShowPhotoWeb { get; set; }
}

public sealed class ProfileProject : MutableEntity
{
    public Guid ProfileId { get; set; }
    public Guid ProjectId { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int WebOrder { get; set; }
    public int PdfOrder { get; set; }
    public List<ProfileProjectTranslation> Translations { get; set; } = [];
}

public sealed class ProfileProjectTranslation : MutableEntity
{
    public Guid ProfileProjectId { get; set; }
    public Locale Locale { get; set; }
    public string? SummaryOverride { get; set; }
}

public sealed class ProfileProjectHighlight
{
    public Guid ProfileProjectId { get; set; }
    public Guid HighlightId { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class ProfileSkill
{
    public Guid ProfileId { get; set; }
    public Guid SkillId { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int WebOrder { get; set; }
    public int PdfOrder { get; set; }
}

public sealed class ProfileExperience : MutableEntity
{
    public Guid ProfileId { get; set; }
    public Guid ExperienceId { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int WebOrder { get; set; }
    public int PdfOrder { get; set; }
}

public sealed class ProfileExperienceHighlight
{
    public Guid ProfileExperienceId { get; set; }
    public Guid HighlightId { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class ProfileEducation
{
    public Guid ProfileId { get; set; }
    public Guid EducationId { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int WebOrder { get; set; }
    public int PdfOrder { get; set; }
}

public sealed class ProfileCertification
{
    public Guid ProfileId { get; set; }
    public Guid CertificationId { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int WebOrder { get; set; }
    public int PdfOrder { get; set; }
}

public sealed class ProfileLanguage
{
    public Guid ProfileId { get; set; }
    public Guid SpokenLanguageId { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int WebOrder { get; set; }
    public int PdfOrder { get; set; }
}

public sealed class ProfileLink
{
    public Guid ProfileId { get; set; }
    public Guid PersonLinkId { get; set; }
    public bool WebEnabled { get; set; } = true;
    public bool PdfEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class ProfilePdfSetting : MutableEntity
{
    public Guid ProfileId { get; set; }
    public Locale Locale { get; set; }
    public string TemplateKey { get; set; } = "senior-mobile-engineer-v1";
    public string PaperSize { get; set; } = "Letter";
    public decimal FontSize { get; set; } = 9.5m;
    public decimal MarginMm { get; set; } = 16.5m;
    public int TargetPages { get; set; } = 2;
}

public sealed class Site : OwnedEntity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? BaseUrl { get; set; }
    public Locale DefaultLocale { get; set; } = Locale.En;
    public string ThemeKey { get; set; } = "editorial-v1";
    public string? DeploymentTargetKey { get; set; }
    public Guid? CurrentPublicationId { get; set; }
    public bool IsPrimaryIdentitySite { get; set; }
    public string? SearchVerificationToken { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public List<SiteLocale> Locales { get; set; } = [];
    public List<SiteTranslation> Translations { get; set; } = [];
    public List<SiteProfile> Profiles { get; set; } = [];
}

public sealed class SiteLocale { public Guid SiteId { get; set; } public Locale Locale { get; set; } }

public sealed class SiteTranslation : MutableEntity
{
    public Guid SiteId { get; set; }
    public Locale Locale { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class SiteProfile : MutableEntity
{
    public Guid SiteId { get; set; }
    public Guid ProfileId { get; set; }
    public string PathSlug { get; set; } = "";
    public bool IsDefault { get; set; }
    public bool Indexable { get; set; }
    public bool IsListed { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class MediaAsset : OwnedEntity
{
    public string Provider { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public string? DeliveryUrl { get; set; }
    public string MimeType { get; set; } = "";
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string Sha256 { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? ArchivedAt { get; set; }
    public List<MediaAssetTranslation> Translations { get; set; } = [];
}

public sealed class MediaAssetTranslation : MutableEntity
{
    public Guid AssetId { get; set; }
    public Locale Locale { get; set; }
    public string AltText { get; set; } = "";
    public string? Caption { get; set; }
}

public sealed class Publication : MutableEntity
{
    public Guid PersonId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? ProfileId { get; set; }
    public PublicationPurpose Purpose { get; set; }
    public long Revision { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public string Snapshot { get; set; } = "{}";
    public string SnapshotHash { get; set; } = "";
    public PublicationState State { get; set; } = PublicationState.Queued;
    public Guid RequestedBy { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public List<PublicationAttempt> Attempts { get; set; } = [];
    public List<PublicationArtifact> Artifacts { get; set; } = [];
}

public sealed class PublicationAttempt : MutableEntity
{
    public Guid PublicationId { get; set; }
    public int AttemptNumber { get; set; }
    public string? WorkflowRunId { get; set; }
    public PublicationState State { get; set; } = PublicationState.Queued;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorSummary { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
}

public sealed class PublicationArtifact : MutableEntity
{
    public Guid PublicationId { get; set; }
    public Guid? ProfileId { get; set; }
    public Locale? Locale { get; set; }
    public ArtifactKind Kind { get; set; }
    public string TemplateVersion { get; set; } = "";
    public string PathOrArtifactId { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long SizeBytes { get; set; }
    public int? PageCount { get; set; }
    public string QaReport { get; set; } = "{}";
    public ArtifactVisibility Visibility { get; set; }
}

public sealed class SeoPageSetting : MutableEntity
{
    public Guid SiteId { get; set; }
    public Locale Locale { get; set; }
    public string PageKind { get; set; } = "";
    public Guid? ProfileId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? TitleOverride { get; set; }
    public string? DescriptionOverride { get; set; }
    public string? CanonicalOverrideUrl { get; set; }
    public Guid? OgAssetId { get; set; }
    public bool? IndexOverride { get; set; }
}

public sealed class SiteRedirect : MutableEntity
{
    public Guid SiteId { get; set; }
    public string SourcePath { get; set; } = "";
    public string TargetPath { get; set; } = "";
    public int StatusCode { get; set; } = 301;
    public bool IsActive { get; set; } = true;
}

public sealed class SiteDeployment : MutableEntity
{
    public Guid SiteId { get; set; }
    public Guid PublicationId { get; set; }
    public Guid AttemptId { get; set; }
    public string ProviderDeploymentId { get; set; } = "";
    public string DeploymentUrl { get; set; } = "";
    public PublicationState State { get; set; }
    public DateTimeOffset? DeployedAt { get; set; }
}
