using System.Globalization;
using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Drawing;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Resume.Application;

public interface IResumePdfRenderer
{
    byte[] Render(ResumeDocument document);
}

public sealed class ResumePdfRenderer : IResumePdfRenderer
{
    private const string FontFamily = "Cairo Resume";
    private static readonly Color Accent = Color.FromHex("#1F4F70");
    private static readonly Color Ink = Color.FromHex("#2F3B4A");
    private static readonly Color Muted = Color.FromHex("#68717B");
    private static readonly Color Link = Color.FromHex("#1A73C8");
    private static readonly Color Rule = Color.FromHex("#727272");
    private static readonly object FontLock = new();
    private static bool _fontsRegistered;

    public byte[] Render(ResumeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        RegisterFonts();
        QuestPDF.Settings.License = LicenseType.Community;

        var arabic = document.Locale.Equals("ar", StringComparison.OrdinalIgnoreCase);
        var paper = document.Pdf.PaperSize.Equals("Letter", StringComparison.OrdinalIgnoreCase)
            ? PageSizes.Letter
            : PageSizes.A4;

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(paper);
                page.Margin(Math.Clamp((float)document.Pdf.MarginMm, 10f, 25f), Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily(FontFamily)
                    .FontSize(Math.Clamp((float)document.Pdf.FontSize, 9.5f, 12f))
                    .FontColor(Ink)
                    .LineHeight(arabic ? 1.42f : 1.25f));

                if (arabic)
                    page.ContentFromRightToLeft();
                else
                    page.ContentFromLeftToRight();

                page.Content().SemanticSpan(AccessibleText(document)).Column(column => Compose(column, document));
            });
        }).WithSettings(new DocumentSettings
        {
            CompressDocument = true,
            PDFUA_Conformance = PDFUA_Conformance.PDFUA_1,
            ContentDirection = arabic ? ContentDirection.RightToLeft : ContentDirection.LeftToRight
        }).WithMetadata(new DocumentMetadata
        {
            Title = $"{document.FullName} — {document.Headline}",
            Author = document.FullName,
            Subject = "Resume",
            Language = document.Locale
        }).GeneratePdf();
    }

    private static void Compose(ColumnDescriptor column, ResumeDocument document)
    {
        var arabic = document.Locale.Equals("ar", StringComparison.OrdinalIgnoreCase);
        var labels = Labels.For(arabic);

        column.Spacing(0);
        Header(column, document, arabic);

        IReadOnlyList<string> sectionKeys = document.Sections is { Count: > 0 } configuredSections
            ? configuredSections
            : ["summary", "skills", "experience", "projects", "education", "certifications", "languages"];
        var certificationsEnabled = sectionKeys.Contains("certifications", StringComparer.OrdinalIgnoreCase);
        var languagesEnabled = sectionKeys.Contains("languages", StringComparer.OrdinalIgnoreCase);
        var hasCoreSkills = document.Skills.Any(skill => IsCoreCategory(skill.Category, arabic));
        var technicalSkillsRendered = false;
        var credentialsRendered = false;

        foreach (var key in sectionKeys)
        {
            switch (key.ToLowerInvariant())
            {
                case "summary" when !string.IsNullOrWhiteSpace(document.Summary):
                    Section(column, labels.Summary, arabic, item => item.Text(document.Summary));
                    break;
                case "skills" when document.Skills.Count > 0:
                    RenderSkills(column, document, labels, arabic, includeTechnical: !hasCoreSkills);
                    technicalSkillsRendered = !hasCoreSkills;
                    break;
                case "experience" when document.Experiences.Count > 0:
                    Section(column, labels.Experience, arabic, item => RenderExperiences(item, document, arabic));
                    break;
                case "projects" when document.Projects.Count > 0:
                    Section(column, labels.Projects, arabic, item => RenderProjects(item, document, labels, arabic));
                    if (hasCoreSkills && !technicalSkillsRendered && document.Skills.Any(skill => !IsCoreCategory(skill.Category, arabic)))
                    {
                        RenderTechnicalSkills(column, document, labels, arabic);
                        technicalSkillsRendered = true;
                    }
                    break;
                case "education" when document.Educations.Count > 0:
                    Section(column, labels.Education, arabic, item => RenderEducation(item, document, labels, arabic));
                    break;
                case "certifications" when !credentialsRendered && document.Certifications.Count > 0:
                    if (languagesEnabled && document.Languages.Count > 0)
                        Section(column, labels.CertificationsAndLanguages, arabic, item => RenderCredentials(item, document, labels, arabic));
                    else
                        Section(column, labels.Certifications, arabic, item => RenderCertifications(item, document, arabic));
                    credentialsRendered = true;
                    break;
                case "languages" when !credentialsRendered && document.Languages.Count > 0:
                    if (certificationsEnabled && document.Certifications.Count > 0)
                        Section(column, labels.CertificationsAndLanguages, arabic, item => RenderCredentials(item, document, labels, arabic));
                    else
                        Section(column, labels.Languages, arabic, item => RenderLanguages(item, document, arabic));
                    credentialsRendered = true;
                    break;
            }
        }

        if (!technicalSkillsRendered && document.Skills.Any(skill => !IsCoreCategory(skill.Category, arabic)))
            RenderTechnicalSkills(column, document, labels, arabic);
    }

    private static void Header(ColumnDescriptor column, ResumeDocument document, bool arabic)
    {
        AlignForLocale(column.Item(), arabic)
            .Text(DisplayName(document.FullName, arabic))
            .FontSize(21)
            .Bold()
            .FontColor(Accent);

        AlignForLocale(column.Item().PaddingTop(1), arabic)
            .Text(document.Headline)
            .FontSize(11.5f)
            .Bold()
            .FontColor(Accent);

        var contactParts = new[]
        {
            (Value: document.Location, IsLeftToRight: false),
            (Value: document.Phone, IsLeftToRight: true),
            (Value: document.Email, IsLeftToRight: true)
        }
            .Where(part => !string.IsNullOrWhiteSpace(part.Value))
            .Select(part => (Value: part.Value!, part.IsLeftToRight))
            .ToList();
        if (contactParts.Count > 0)
        {
            var contact = AlignForLocale(column.Item().PaddingTop(2).ContentFromLeftToRight(), arabic);
            contact.Text(text =>
            {
                for (var index = 0; index < contactParts.Count; index++)
                {
                    if (index > 0) text.Span(" | ").FontColor(Muted);
                    var span = text.Span(contactParts[index].Value).FontSize(9f).FontColor(Muted);
                    if (contactParts[index].IsLeftToRight) span.DirectionFromLeftToRight();
                }
            });
        }

        if (document.Links.Count > 0)
        {
            var links = document.Links.Where(link => !string.IsNullOrWhiteSpace(link.Label) && !string.IsNullOrWhiteSpace(link.Url)).ToList();
            if (links.Count > 0)
            {
                var linksContainer = AlignForLocale(column.Item().PaddingTop(1).ContentFromLeftToRight(), arabic);
                linksContainer.Text(text =>
                {
                    for (var index = 0; index < links.Count; index++)
                    {
                        if (index > 0) text.Span(" | ").FontColor(Muted);
                        text.Hyperlink(links[index].Label, links[index].Url).FontSize(9f).Underline().FontColor(Link);
                    }
                });
            }
        }
    }

    private static void RenderSkills(ColumnDescriptor column, ResumeDocument document, Labels labels, bool arabic, bool includeTechnical)
    {
        var groups = document.Skills.GroupBy(skill => skill.Category).ToList();
        var coreGroups = groups.Where(group => IsCoreCategory(group.Key, arabic)).ToList();
        var technicalGroups = groups.Where(group => !IsCoreCategory(group.Key, arabic)).ToList();

        if (coreGroups.Count > 0)
        {
            Section(column, labels.CoreCompetencies, arabic, item => item.Text(text =>
            {
                var names = coreGroups.SelectMany(group => group.Select(skill => skill.Name));
                text.Span(string.Join(" | ", names));
            }));
        }

        if (includeTechnical && technicalGroups.Count > 0)
            RenderTechnicalSkills(column, technicalGroups, labels, arabic);
    }

    private static void RenderTechnicalSkills(ColumnDescriptor column, ResumeDocument document, Labels labels, bool arabic) =>
        RenderTechnicalSkills(column, document.Skills.GroupBy(skill => skill.Category).Where(group => !IsCoreCategory(group.Key, arabic)).ToList(), labels, arabic);

    private static void RenderTechnicalSkills(ColumnDescriptor column, IReadOnlyList<IGrouping<string, ResumeSkill>> technicalGroups, Labels labels, bool arabic)
    {
        if (technicalGroups.Count == 0) return;
        Section(column, labels.Skills, arabic, item => item.Column(skills =>
        {
            skills.Spacing(1);
            foreach (var group in technicalGroups)
            {
                AlignForLocale(skills.Item(), arabic).Text(text =>
                {
                    text.Span($"{group.Key}: ").Bold();
                    text.Span(string.Join(" | ", group.Select(skill => skill.Name)));
                });
            }
        }));
    }

    private static void RenderExperiences(IContainer container, ResumeDocument document, bool arabic)
    {
        container.Column(entries =>
        {
            entries.Spacing(2);
            foreach (var experience in document.Experiences)
            {
                var entry = entries.Item().PaddingTop(2).PreventPageBreak();
                entry.Row(row =>
                {
                    if (arabic)
                    {
                        row.AutoItem().PaddingRight(8).AlignLeft().ContentFromLeftToRight().Text(FormatDateRange(experience.StartDate, experience.EndDate, arabic)).FontSize(9.5f).Italic().FontColor(Muted);
                        row.RelativeItem().AlignRight().DefaultTextStyle(style => style.FontSize(10.8f)).Text(text => ExperienceTitle(text, experience, arabic));
                    }
                    else
                    {
                        row.RelativeItem().DefaultTextStyle(style => style.FontSize(10.8f)).Text(text => ExperienceTitle(text, experience, arabic));
                        row.AutoItem().PaddingLeft(8).AlignRight().Text(FormatDateRange(experience.StartDate, experience.EndDate, arabic)).FontSize(9.5f).Italic().FontColor(Muted);
                    }
                });

                if (!string.IsNullOrWhiteSpace(experience.Summary))
                    AlignForLocale(entries.Item(), arabic).Text(experience.Summary);
                foreach (var highlight in experience.Highlights)
                    Bullet(entries, highlight, arabic);
            }
        });
    }

    private static void ExperienceTitle(TextDescriptor text, ResumeExperience experience, bool arabic)
    {
        text.Span($"{experience.JobTitle} | {experience.Organization}").Bold().FontColor(Ink);
        if (!string.IsNullOrWhiteSpace(experience.Location))
            text.Span($" - {experience.Location}").Italic().FontColor(Muted);
    }

    private static void RenderProjects(IContainer container, ResumeDocument document, Labels labels, bool arabic)
    {
        container.Column(projects =>
        {
            projects.Spacing(2);
            foreach (var project in document.Projects)
            {
                var links = new List<(string Label, string Url)>();
                if (!string.IsNullOrWhiteSpace(project.DemoUrl)) links.Add((ProjectLinkLabel(project.DemoUrl, labels.Demo), project.DemoUrl));
                if (!string.IsNullOrWhiteSpace(project.RepositoryUrl)) links.Add((ProjectLinkLabel(project.RepositoryUrl, labels.Repository), project.RepositoryUrl));
                AlignForLocale(projects.Item().PaddingTop(2).PreventPageBreak(), arabic).Text(text =>
                {
                    text.Span(project.Name).Bold();
                    if (!string.IsNullOrWhiteSpace(project.Summary))
                    {
                        text.Span(" - ");
                        text.Span(project.Summary);
                    }
                    for (var index = 0; index < links.Count; index++)
                    {
                        text.Span(" | ").FontColor(Muted);
                        text.Hyperlink(links[index].Label, links[index].Url).FontSize(9f).Underline().FontColor(Link);
                    }
                });

                if (!string.IsNullOrWhiteSpace(project.Role))
                    AlignForLocale(projects.Item(), arabic).Text(project.Role).FontSize(9.5f).Italic().FontColor(Muted);

                foreach (var highlight in project.Highlights)
                    Bullet(projects, highlight, arabic);

            }
        });
    }

    private static void RenderEducation(IContainer container, ResumeDocument document, Labels labels, bool arabic)
    {
        container.Column(entries =>
        {
            entries.Spacing(2);
            foreach (var education in document.Educations)
            {
                AlignForLocale(entries.Item().PaddingTop(2).PreventPageBreak(), arabic).Text(text =>
                {
                    text.Span(EducationTitle(education, arabic)).Bold();
                    if (!string.IsNullOrWhiteSpace(education.Institution))
                        text.Span($" | {education.Institution}{(string.IsNullOrWhiteSpace(education.Location) ? "" : $", {education.Location}")}");
                    var date = EducationDate(education, labels, arabic);
                    if (!string.IsNullOrWhiteSpace(date)) text.Span($" | {date}");
                });
                if (!string.IsNullOrWhiteSpace(education.Notes))
                    AlignForLocale(entries.Item(), arabic).Text(education.Notes).FontSize(9.5f).FontColor(Muted);
            }
        });
    }

    private static void RenderCertifications(IContainer container, ResumeDocument document, bool arabic)
    {
        container.Column(entries =>
        {
            foreach (var certification in document.Certifications)
                AlignForLocale(entries.Item().PreventPageBreak(), arabic).Text(text => CertificationText(text, certification, arabic));
        });
    }

    private static void RenderLanguages(IContainer container, ResumeDocument document, bool arabic)
    {
        AlignForLocale(container, arabic).Text(text => LanguageText(text, document.Languages, arabic));
    }

    private static void RenderCredentials(IContainer container, ResumeDocument document, Labels labels, bool arabic)
    {
        container.Column(entries =>
        {
            entries.Spacing(1);
            foreach (var certification in document.Certifications)
                AlignForLocale(entries.Item().PreventPageBreak(), arabic).Text(text => CertificationText(text, certification, arabic));
            if (document.Languages.Count > 0)
                AlignForLocale(entries.Item().PreventPageBreak(), arabic).Text(text => LanguageText(text, document.Languages, arabic));
        });
    }

    private static void CertificationText(TextDescriptor text, ResumeCertification certification, bool arabic)
    {
        text.Span(certification.Name).Bold();
        if (!string.IsNullOrWhiteSpace(certification.Issuer)) text.Span($" | {certification.Issuer}").FontColor(Muted);
        if (!string.IsNullOrWhiteSpace(certification.IssuedOn)) text.Span($" | {FormatDate(certification.IssuedOn, arabic)}").FontColor(Muted);
    }

    private static void LanguageText(TextDescriptor text, IReadOnlyList<ResumeLanguage> languages, bool arabic)
    {
        for (var index = 0; index < languages.Count; index++)
        {
            if (index > 0) text.Span(" | ").FontColor(Muted);
            var language = languages[index];
            text.Span($"{LanguageName(language.LanguageCode, arabic)}: ").Bold();
            text.Span(language.Proficiency);
        }
    }

    private static void Section(ColumnDescriptor column, string title, bool arabic, Action<IContainer> content)
    {
        AlignForLocale(column.Item().PaddingTop(9).PaddingBottom(2).BorderBottom(0.75f).BorderColor(Rule), arabic)
            .Text(title)
            .FontSize(11.5f)
            .Bold()
            .FontColor(Accent);
        content(AlignForLocale(column.Item().PaddingTop(2), arabic));
    }

    private static void Bullet(ColumnDescriptor column, string value, bool arabic)
    {
        var item = arabic ? column.Item().PaddingRight(14) : column.Item().PaddingLeft(14);
        item.Row(row =>
        {
            if (arabic)
            {
                row.AutoItem().PaddingLeft(5).AlignRight().Text("• ").FontColor(Ink);
                row.RelativeItem().AlignRight().Text(value);
            }
            else
            {
                row.AutoItem().PaddingRight(5).Text("• ").FontColor(Ink);
                row.RelativeItem().Text(value);
            }
        });
    }

    private static IContainer AlignForLocale(IContainer container, bool arabic) => arabic ? container.AlignRight() : container.AlignLeft();

    private static string DisplayName(string value, bool arabic) => arabic ? value : value.ToUpperInvariant();

    private static string EducationTitle(ResumeEducation education, bool arabic)
    {
        if (string.IsNullOrWhiteSpace(education.FieldOfStudy)) return education.Degree;
        return arabic ? $"{education.Degree} في {education.FieldOfStudy}" : $"{education.Degree} in {education.FieldOfStudy}";
    }

    private static string? EducationDate(ResumeEducation education, Labels labels, bool arabic)
    {
        if (!string.IsNullOrWhiteSpace(education.EndDate)) return $"{labels.Graduated} {Year(education.EndDate, arabic)}";
        if (!string.IsNullOrWhiteSpace(education.StartDate)) return $"{FormatDate(education.StartDate, arabic)} - {labels.Present}";
        return null;
    }

    private static string FormatDateRange(string start, string? end, bool arabic) =>
        $"{FormatDate(start, arabic)} - {(!string.IsNullOrWhiteSpace(end) ? FormatDate(end, arabic) : Labels.For(arabic).Present)}";

    private static string FormatDate(string? value, bool arabic)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        if (!DateTime.TryParseExact(value, ["yyyy-MM-dd", "yyyy-MM"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return value;
        if (!arabic) return date.ToString("MMM yyyy", CultureInfo.InvariantCulture);
        var culture = new CultureInfo("ar", false);
        culture.DateTimeFormat.Calendar = new GregorianCalendar();
        return date.ToString("MMM yyyy", culture);
    }

    private static string Year(string? value, bool arabic)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        if (!DateTime.TryParseExact(value, ["yyyy-MM-dd", "yyyy-MM"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return value;
        return date.Year.ToString(arabic ? CultureInfo.GetCultureInfo("ar-SA") : CultureInfo.InvariantCulture);
    }

    private static string LanguageName(string code, bool arabic)
    {
        var normalized = code.Trim().ToLowerInvariant();
        return normalized switch
        {
            "ar" => arabic ? "العربية" : "Arabic",
            "en" => "English",
            "fr" => arabic ? "الفرنسية" : "French",
            _ => code
        };
    }

    private static string ProjectLinkLabel(string url, string fallback)
    {
        if (url.Contains("play.google.com", StringComparison.OrdinalIgnoreCase)) return "Google Play";
        if (url.Contains("apps.apple.com", StringComparison.OrdinalIgnoreCase)) return "App Store";
        if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase)) return "GitHub";
        return fallback;
    }

    private static bool IsCoreCategory(string category, bool arabic)
    {
        var value = category.Trim();
        return value.Contains("core", StringComparison.OrdinalIgnoreCase)
            || (!arabic && value.Contains("competenc", StringComparison.OrdinalIgnoreCase))
            || value.Contains("أساسي", StringComparison.OrdinalIgnoreCase)
            || value.Contains("جوهر", StringComparison.OrdinalIgnoreCase);
    }

    private static void RegisterFonts()
    {
        if (_fontsRegistered) return;
        lock (FontLock)
        {
            if (_fontsRegistered) return;
            var assembly = typeof(ResumePdfRenderer).Assembly;
            Register(assembly, "resume.Application.Assets.Fonts.Cairo-Regular.ttf");
            Register(assembly, "resume.Application.Assets.Fonts.Cairo-Bold.ttf");
            _fontsRegistered = true;
        }
    }

    private static string AccessibleText(ResumeDocument document)
    {
        var arabic = document.Locale.Equals("ar", StringComparison.OrdinalIgnoreCase);
        var labels = Labels.For(arabic);
        var lines = new List<string> { document.FullName, document.Headline };
        if (!string.IsNullOrWhiteSpace(document.Location)) lines.Add(document.Location);
        lines.AddRange(new[] { document.Phone, document.Email }.Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>());
        lines.AddRange(document.Links.Select(link => link.Label));

        if (!string.IsNullOrWhiteSpace(document.Summary))
        {
            lines.Add(labels.Summary);
            lines.Add(document.Summary);
        }
        if (document.Skills.Count > 0)
        {
            lines.Add(labels.Skills);
            lines.AddRange(document.Skills.GroupBy(skill => skill.Category).Select(group => $"{group.Key}: {string.Join(" | ", group.Select(skill => skill.Name))}"));
        }
        if (document.Experiences.Count > 0)
        {
            lines.Add(labels.Experience);
            foreach (var experience in document.Experiences)
            {
                lines.Add($"{experience.JobTitle} | {experience.Organization}");
                lines.AddRange(experience.Highlights.Select(highlight => $"• {highlight}"));
            }
        }
        if (document.Projects.Count > 0)
        {
            lines.Add(labels.Projects);
            lines.AddRange(document.Projects.Select(project => $"{project.Name} - {project.Summary}"));
        }
        if (document.Educations.Count > 0)
        {
            lines.Add(labels.Education);
            lines.AddRange(document.Educations.Select(education => $"{EducationTitle(education, arabic)} | {education.Institution}"));
        }
        if (document.Certifications.Count > 0)
        {
            lines.Add(labels.Certifications);
            lines.AddRange(document.Certifications.Select(certification => certification.Name));
        }
        if (document.Languages.Count > 0)
        {
            lines.Add(labels.Languages);
            lines.Add(string.Join(" | ", document.Languages.Select(language => $"{LanguageName(language.LanguageCode, arabic)}: {language.Proficiency}")));
        }
        return string.Join("\n", lines.Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static void Register(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded PDF font is missing: {resourceName}");
        FontManager.RegisterFontWithCustomName(FontFamily, stream);
    }

    private sealed record Labels(
        string Summary,
        string Skills,
        string Projects,
        string Experience,
        string Education,
        string Certifications,
        string Languages,
        string CertificationsAndLanguages,
        string CoreCompetencies,
        string Technologies,
        string Present,
        string Graduated,
        string Demo,
        string Repository)
    {
        public static Labels For(bool arabic) => arabic
            ? new("الملخص المهني", "المهارات التقنية", "التطبيقات المنشورة المختارة", "الخبرة العملية", "التعليم", "الشهادات", "اللغات", "الشهادات واللغات", "الكفاءات الأساسية", "التقنيات", "حتى الآن", "التخرج", "التجربة", "المستودع")
            : new("PROFESSIONAL SUMMARY", "TECHNICAL SKILLS", "SELECTED PUBLISHED APPLICATIONS", "PROFESSIONAL EXPERIENCE", "EDUCATION", "CERTIFICATIONS", "LANGUAGES", "CERTIFICATIONS & LANGUAGES", "CORE COMPETENCIES", "TECHNOLOGIES", "Present", "Graduated", "Live Demo", "Repository");
    }
}
