using System.Text.RegularExpressions;
using Resume.Core;

namespace Resume.Application;

public sealed record ResumeDocument(
    string Locale,
    string Direction,
    string FullName,
    string? Location,
    string Headline,
    string Summary,
    string? Email,
    string? Phone,
    IReadOnlyList<ResumeLink> Links,
    IReadOnlyList<ResumeSkill> Skills,
    IReadOnlyList<ResumeProject> Projects,
    IReadOnlyList<ResumeExperience> Experiences,
    IReadOnlyList<ResumeEducation> Educations,
    IReadOnlyList<ResumeCertification> Certifications,
    IReadOnlyList<ResumeLanguage> Languages,
    IReadOnlyList<string> Sections,
    ResumePdfOptions Pdf);

public sealed record ResumeLink(string Kind, string Label, string Url);
public sealed record ResumeSkill(string Category, string Name);
public sealed record ResumeMedia(string Src, int Width, int Height, string Alt, string? Caption);
public sealed record ResumeProject(string Slug, string Name, string? Role, string Summary, string? Description, string? RepositoryUrl, string? DemoUrl, IReadOnlyList<string> Highlights, IReadOnlyList<string> Skills, ResumeMedia? Cover = null, IReadOnlyList<ResumeMedia>? Media = null);
public sealed record ResumeExperience(string Organization, string JobTitle, string? Location, string StartDate, string? EndDate, string? Summary, IReadOnlyList<string> Highlights);
public sealed record ResumeEducation(string Institution, string Degree, string? FieldOfStudy, string? Location, string? StartDate, string? EndDate, string? Notes);
public sealed record ResumeCertification(string Name, string Issuer, string? IssuedOn, string? ExpiresOn, string? CredentialUrl);
public sealed record ResumeLanguage(string LanguageCode, string Proficiency);
public sealed record ResumePdfOptions(string PaperSize, decimal FontSize, decimal MarginMm, int TargetPages);

public sealed record ValidationIssue(string Code, string Path, string Message);
public sealed record PublicationValidation(bool CanPublish, IReadOnlyList<ValidationIssue> Errors, IReadOnlyList<ValidationIssue> Warnings);

public static partial class DomainValidation
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    public static bool IsSlug(string value) => value.Length is > 0 and <= 100 && SlugPattern().IsMatch(value);
    public static bool IsHttpsUrl(string? value) => string.IsNullOrWhiteSpace(value) ||
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    public static bool IsValidDateRange(DateOnly? start, DateOnly? end, bool current) =>
        current ? end is null : start is null || end is null || end >= start;
    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
