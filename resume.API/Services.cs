using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO.Compression;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resume.Application;
using Resume.Core;
using Resume.Infrastructure;

namespace Resume.Api;

public sealed record DispatchResult(bool Configured, bool Succeeded, string? Error);

public sealed class PublicationDispatcher(HttpClient client, IConfiguration configuration)
{
    public bool IsConfigured(PublicationPurpose purpose)
    {
        var owner = configuration["GitHub:Owner"] ?? configuration["Publishing:GitHubOwner"];
        var repository = purpose == PublicationPurpose.PrivatePdfExport
            ? configuration["GitHub:PrivateExportRepository"]
            : configuration["GitHub:PublishRepository"] ?? configuration["Publishing:GitHubRepository"];
        var token = configuration["GitHub:Token"] ?? configuration["Publishing:GitHubToken"];
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repository) || string.IsNullOrWhiteSpace(token)) return false;
        if (purpose != PublicationPurpose.PrivatePdfExport) return true;
        var productRepository = configuration["GitHub:ProductRepository"];
        var productRef = configuration["GitHub:ProductRef"];
        return !string.IsNullOrWhiteSpace(productRepository) && IsPinnedGitRef(productRef);
    }

    public async Task<DispatchResult> DispatchAsync(Guid publicationId, Guid attemptId, CancellationToken ct, PublicationPurpose purpose = PublicationPurpose.SitePublish)
    {
        var owner = configuration["GitHub:Owner"] ?? configuration["Publishing:GitHubOwner"];
        var repository = purpose == PublicationPurpose.PrivatePdfExport
            ? configuration["GitHub:PrivateExportRepository"]
            : configuration["GitHub:PublishRepository"] ?? configuration["Publishing:GitHubRepository"];
        var token = configuration["GitHub:Token"] ?? configuration["Publishing:GitHubToken"];
        if (!IsConfigured(purpose)) return new(false, false, null);
        var workflow = purpose == PublicationPurpose.PrivatePdfExport ? configuration["GitHub:PrivateExportWorkflow"] ?? "export-private-pdf.yml" : configuration["GitHub:PublishWorkflow"] ?? configuration["Publishing:GitHubWorkflow"] ?? "publish-site.yml";
        var gitRef = purpose == PublicationPurpose.PrivatePdfExport ? configuration["GitHub:PrivateExportRef"] ?? "main" : configuration["GitHub:PublishRef"] ?? configuration["Publishing:GitHubRef"] ?? "main";
        var uri = $"https://api.github.com/repos/{Uri.EscapeDataString(owner!)}/{Uri.EscapeDataString(repository!)}/actions/workflows/{Uri.EscapeDataString(workflow)}/dispatches";
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = purpose == PublicationPurpose.PrivatePdfExport
                ? JsonContent.Create(new { @ref = gitRef, inputs = new { publication_id = publicationId.ToString(), attempt_id = attemptId.ToString(), product_repository = configuration["GitHub:ProductRepository"]!, product_ref = configuration["GitHub:ProductRef"]! } })
                : JsonContent.Create(new { @ref = gitRef, inputs = new { publication_id = publicationId.ToString(), attempt_id = attemptId.ToString() } })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("terra-resume-publisher/1.0");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return new(true, true, null);
            var body = await response.Content.ReadAsStringAsync(ct);
            return new(true, false, $"GitHub returned {(int)response.StatusCode}: {body[..Math.Min(body.Length, 300)]}");
        }
        catch (HttpRequestException exception)
        {
            return new(true, false, exception.Message);
        }
    }

    private static bool IsPinnedGitRef(string? value) => value is not null && value.Length is 40 or 64 && value.All(char.IsAsciiHexDigit);
}

public sealed record WorkflowReconciliation(bool Configured, bool Completed, AttemptResult? Result, string? Error);

public sealed class PublicationReconciler(HttpClient client, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<WorkflowReconciliation> ReconcileAsync(Publication publication, PublicationAttempt attempt, CancellationToken ct)
    {
        var owner = configuration["GitHub:Owner"] ?? configuration["Publishing:GitHubOwner"];
        var repository = publication.Purpose == PublicationPurpose.PrivatePdfExport
            ? configuration["GitHub:PrivateExportRepository"]
            : configuration["GitHub:PublishRepository"] ?? configuration["Publishing:GitHubRepository"];
        var token = configuration["GitHub:Token"] ?? configuration["Publishing:GitHubToken"];
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repository) || string.IsNullOrWhiteSpace(token) || !long.TryParse(attempt.WorkflowRunId, out var runId)) return new(false, false, null, null);

        try
        {
            using var runRequest = GitHubRequest(HttpMethod.Get, $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/actions/runs/{runId}", token);
            using var runResponse = await client.SendAsync(runRequest, ct);
            if (!runResponse.IsSuccessStatusCode) return new(true, false, null, $"GitHub run lookup returned {(int)runResponse.StatusCode}.");
            using var runJson = JsonDocument.Parse(await runResponse.Content.ReadAsStringAsync(ct));
            var status = runJson.RootElement.GetProperty("status").GetString();
            var conclusion = runJson.RootElement.TryGetProperty("conclusion", out var conclusionNode) ? conclusionNode.GetString() : null;
            if (!string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase)) return new(true, false, null, null);
            if (!string.Equals(conclusion, "success", StringComparison.OrdinalIgnoreCase)) return new(true, true, new AttemptResult(PublicationState.Failed, "WORKFLOW_FAILED", $"GitHub Actions completed with conclusion '{conclusion ?? "unknown"}'."), null);

            var evidenceName = publication.Purpose == PublicationPurpose.PrivatePdfExport ? $"private-resume-evidence-{publication.Id}" : $"public-site-evidence-{publication.Id}";
            using var artifactsRequest = GitHubRequest(HttpMethod.Get, $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/actions/runs/{runId}/artifacts?per_page=100", token);
            using var artifactsResponse = await client.SendAsync(artifactsRequest, ct);
            if (!artifactsResponse.IsSuccessStatusCode) return new(true, false, null, $"GitHub artifact lookup returned {(int)artifactsResponse.StatusCode}.");
            using var artifactsJson = JsonDocument.Parse(await artifactsResponse.Content.ReadAsStringAsync(ct));
            var evidence = artifactsJson.RootElement.GetProperty("artifacts").EnumerateArray().SingleOrDefault(x => string.Equals(x.GetProperty("name").GetString(), evidenceName, StringComparison.Ordinal));
            if (evidence.ValueKind == JsonValueKind.Undefined || evidence.GetProperty("expired").GetBoolean()) return new(true, false, null, "Workflow evidence is missing or expired.");
            var archiveUrl = evidence.GetProperty("archive_download_url").GetString();
            if (string.IsNullOrWhiteSpace(archiveUrl)) return new(true, false, null, "Workflow evidence has no download URL.");
            using var downloadRequest = GitHubRequest(HttpMethod.Get, archiveUrl, token);
            using var downloadResponse = await client.SendAsync(downloadRequest, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!downloadResponse.IsSuccessStatusCode || downloadResponse.Content.Headers.ContentLength is > 6_000_000) return new(true, false, null, "Workflow evidence could not be downloaded safely.");
            var bytes = await downloadResponse.Content.ReadAsByteArrayAsync(ct);
            if (bytes.Length > 6_000_000) return new(true, false, null, "Workflow evidence exceeds the 6 MB safety limit.");
            using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            var resultName = publication.Purpose == PublicationPurpose.PrivatePdfExport ? "private-export-result.json" : "publication-result.json";
            var entry = archive.Entries.SingleOrDefault(x => string.Equals(Path.GetFileName(x.FullName), resultName, StringComparison.Ordinal));
            if (entry is null || entry.Length is <= 0 or > 1_000_000) return new(true, false, null, "Workflow result evidence is missing or invalid.");
            await using var stream = entry.Open();
            var result = await JsonSerializer.DeserializeAsync<AttemptResult>(stream, JsonOptions, ct);
            return result?.State == PublicationState.Succeeded ? new(true, true, result, null) : new(true, false, null, "Workflow result evidence is not a successful result.");
        }
        catch (HttpRequestException exception)
        {
            return new(true, false, null, exception.Message);
        }
        catch (JsonException exception)
        {
            return new(true, false, null, $"Workflow evidence is invalid: {exception.Message}");
        }
        catch (InvalidDataException exception)
        {
            return new(true, false, null, $"Workflow evidence archive is invalid: {exception.Message}");
        }
    }

    private static HttpRequestMessage GitHubRequest(HttpMethod method, string uri, string token)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("terra-resume-reconciler/1.0");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }
}

public sealed record PrivateArtifactDownload(bool Configured, byte[]? Content, string? Error);

public sealed class PrivateArtifactDownloader(HttpClient client, IConfiguration configuration)
{
    public async Task<PrivateArtifactDownload> DownloadAsync(string artifactId, Locale locale, string expectedSha256, CancellationToken ct)
    {
        var owner = configuration["GitHub:Owner"] ?? configuration["Publishing:GitHubOwner"];
        var repository = configuration["GitHub:PrivateExportRepository"];
        var token = configuration["GitHub:Token"] ?? configuration["Publishing:GitHubToken"];
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repository) || string.IsNullOrWhiteSpace(token)) return new(false, null, null);
        if (!long.TryParse(artifactId, out _)) return new(true, null, "Stored GitHub artifact identifier is invalid.");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/actions/artifacts/{Uri.EscapeDataString(artifactId)}/zip");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json")); request.Headers.UserAgent.ParseAdd("terra-resume-private-download/1.0"); request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.Gone) return new(true, null, "EXPIRED");
        if (!response.IsSuccessStatusCode) return new(true, null, $"GitHub returned {(int)response.StatusCode}.");
        if (response.Content.Headers.ContentLength is > 6_000_000) return new(true, null, "Artifact archive exceeds the 6 MB safety limit.");
        var archiveBytes = await response.Content.ReadAsByteArrayAsync(ct); if (archiveBytes.Length > 6_000_000) return new(true, null, "Artifact archive exceeds the 6 MB safety limit.");
        using var archive = new ZipArchive(new MemoryStream(archiveBytes), ZipArchiveMode.Read);
        var expectedName = $"resume-{locale.ToString().ToLowerInvariant()}.pdf";
        var entry = archive.Entries.SingleOrDefault(x => string.Equals(Path.GetFileName(x.FullName), expectedName, StringComparison.OrdinalIgnoreCase));
        if (entry is null || entry.Length is <= 0 or > 5_000_000) return new(true, null, "Expected PDF is missing or exceeds the 5 MB safety limit.");
        await using var source = entry.Open(); await using var target = new MemoryStream((int)entry.Length); await source.CopyToAsync(target, ct); var pdf = target.ToArray();
        if (pdf.Length < 5 || !pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) return new(true, null, "Artifact content is not a PDF.");
        var actualHash = Convert.ToHexString(SHA256.HashData(pdf)).ToLowerInvariant();
        return !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actualHash), Encoding.ASCII.GetBytes(expectedSha256.ToLowerInvariant())) ? new(true, null, "Artifact SHA-256 does not match the publication record.") : new(true, pdf, null);
    }
}

public sealed record MediaUploadResult(string StorageKey, string DeliveryUrl, int Width, int Height, long SizeBytes);
public interface IMediaStorage
{
    bool IsConfigured { get; }
    Task<MediaUploadResult> UploadAsync(byte[] content, string mimeType, string publicId, CancellationToken ct);
}

public sealed class CloudinaryMediaStorage(HttpClient client, IConfiguration configuration) : IMediaStorage
{
    private string? CloudName => configuration["Cloudinary:CloudName"];
    private string? ApiKey => configuration["Cloudinary:ApiKey"];
    private string? ApiSecret => configuration["Cloudinary:ApiSecret"];
    public bool IsConfigured => !string.IsNullOrWhiteSpace(CloudName) && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ApiSecret);

    public async Task<MediaUploadResult> UploadAsync(byte[] content, string mimeType, string publicId, CancellationToken ct)
    {
        if (!IsConfigured) throw new InvalidOperationException("Cloudinary is not configured.");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        const string folder = "resume-media";
        var signatureSource = $"folder={folder}&public_id={publicId}&timestamp={timestamp}{ApiSecret}";
        var signature = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(signatureSource))).ToLowerInvariant();
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(ApiKey!), "api_key"); form.Add(new StringContent(timestamp), "timestamp"); form.Add(new StringContent(signature), "signature"); form.Add(new StringContent(publicId), "public_id"); form.Add(new StringContent(folder), "folder");
        var file = new ByteArrayContent(content); file.Headers.ContentType = new MediaTypeHeaderValue(mimeType); form.Add(file, "file", $"{publicId}.{Extension(mimeType)}");
        using var response = await client.PostAsync($"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(CloudName!)}/image/upload", form, ct);
        var json = await response.Content.ReadAsStringAsync(ct); if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Cloudinary upload failed with {(int)response.StatusCode}.");
        using var document = JsonDocument.Parse(json); var root = document.RootElement;
        var secureUrl = root.GetProperty("secure_url").GetString()!; var deliveryUrl = secureUrl.Replace("/upload/", "/upload/f_auto,q_auto/", StringComparison.Ordinal);
        return new(root.GetProperty("public_id").GetString()!, deliveryUrl, root.GetProperty("width").GetInt32(), root.GetProperty("height").GetInt32(), root.GetProperty("bytes").GetInt64());
    }

    private static string Extension(string mimeType) => mimeType switch { "image/png" => "png", "image/webp" => "webp", _ => "jpg" };
}

public sealed class TokenService(IConfiguration configuration)
{
    public string Create(AdminUser user)
    {
        var key = configuration["Auth:SigningKey"] ?? "development-signing-key-change-me-32-chars";
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Auth:Issuer"] ?? "resume-api",
            audience: configuration["Auth:Audience"] ?? "resume-admin",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("person_id", user.PersonId.ToString()),
                new Claim("session_version", user.SessionVersion.ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class DatabaseHealthCheck(ResumeDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database is unavailable.");
}

public static class SessionValidation
{
    public static JwtBearerEvents CreateEvents() => new()
    {
        OnTokenValidated = async context =>
        {
            var idValue = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var versionValue = context.Principal?.FindFirstValue("session_version");
            if (!Guid.TryParse(idValue, out var id) || !long.TryParse(versionValue, out var version))
            {
                context.Fail("Invalid session");
                return;
            }
            var db = context.HttpContext.RequestServices.GetRequiredService<ResumeDbContext>();
            var valid = await db.AdminUsers.AsNoTracking().AnyAsync(x => x.Id == id && x.IsActive && x.SessionVersion == version, context.HttpContext.RequestAborted);
            if (!valid) context.Fail("Session expired");
        }
    };
}

public static class ClaimsExtensions
{
    public static Guid AdminId(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static Guid PersonId(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue("person_id")!);
}

public sealed class BootstrapService(ResumeDbContext db, IConfiguration configuration)
{
    public async Task BootstrapOwnerAsync()
    {
        await db.Database.MigrateAsync();
        if (await db.AdminUsers.AnyAsync()) throw new InvalidOperationException("An owner already exists.");
        var email = Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_EMAIL")?.Trim();
        var password = Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 14)
            throw new InvalidOperationException("Set BOOTSTRAP_ADMIN_EMAIL and a BOOTSTRAP_ADMIN_PASSWORD of at least 14 characters.");
        var existingPeople = await db.Persons.ToListAsync();
        if (existingPeople.Count > 1) throw new InvalidOperationException("Cannot infer the owner because multiple people already exist.");
        var person = existingPeople.SingleOrDefault() ?? new Person { DefaultLocale = Locale.En };
        person.Email = email;
        var user = new AdminUser { PersonId = person.Id, NormalizedEmail = email.ToUpperInvariant() };
        user.PasswordHash = new PasswordHasher<AdminUser>().HashPassword(user, password);
        if (db.Entry(person).State == EntityState.Detached) db.Add(person);
        db.Add(user);
        await db.SaveChangesAsync();
        Console.WriteLine("Owner created. Remove the bootstrap environment variables now.");
    }

    public async Task SeedDemoAsync()
    {
        if (!string.Equals(configuration["DemoSeed:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Set DemoSeed__Enabled=true explicitly. Demo content must never be treated as personal data.");
        await db.Database.MigrateAsync();
        if (await db.Persons.AnyAsync()) throw new InvalidOperationException("Database is not empty.");

        var person = new Person { Email = "demo@example.invalid", DefaultLocale = Locale.En };
        person.Translations.AddRange([
            new PersonTranslation { PersonId = person.Id, Locale = Locale.En, FullName = "DEMO PROFILE", DefaultHeadline = "Software Engineer | Flutter & .NET Backend", DefaultSummary = "Clearly labeled sample content used only to verify the platform. Replace every field before publishing." },
            new PersonTranslation { PersonId = person.Id, Locale = Locale.Ar, FullName = "ملف تجريبي", DefaultHeadline = "مهندس برمجيات | Flutter وخدمات خلفية باستخدام .NET", DefaultSummary = "محتوى تجريبي معلّم بوضوح لاختبار المنصة فقط. استبدل جميع الحقول قبل النشر." }
        ]);
        var flutter = DemoSkill(person.Id, "Flutter", "Mobile", "تطبيقات الهاتف المحمول", "Flutter");
        var dotnet = DemoSkill(person.Id, ".NET", "Backend", "الخدمات الخلفية", ".NET");
        var postgres = DemoSkill(person.Id, "PostgreSQL", "Data", "البيانات", "PostgreSQL");
        var project = new Project { PersonId = person.Id, Slug = "sample-resume-platform", Kind = ProjectKind.Personal, IsOngoing = true };
        project.Translations.AddRange([
            new ProjectTranslation { ProjectId = project.Id, Locale = Locale.En, Name = "Sample Resume Platform", Role = "Demo project", Summary = "A fictional sample project for testing bilingual rendering and profile selection.", Description = "This is test data, not professional experience or a real client engagement." },
            new ProjectTranslation { ProjectId = project.Id, Locale = Locale.Ar, Name = "منصة سيرة تجريبية", Role = "مشروع تجريبي", Summary = "مشروع افتراضي لاختبار العرض ثنائي اللغة وانتقاء البروفايلات.", Description = "هذه بيانات اختبار وليست خبرة وظيفية أو عملاً لعميل حقيقي." }
        ]);
        var highlight = new ProjectHighlight { ProjectId = project.Id, SortOrder = 0 };
        highlight.Translations.AddRange([
            new ProjectHighlightTranslation { HighlightId = highlight.Id, Locale = Locale.En, Text = "Verifies Arabic and English layout without claiming a business outcome." },
            new ProjectHighlightTranslation { HighlightId = highlight.Id, Locale = Locale.Ar, Text = "يتحقق من تنسيق العربية والإنجليزية دون ادعاء نتيجة تجارية." }
        ]);
        project.Highlights.Add(highlight);
        project.Skills.AddRange([new ProjectSkill { ProjectId = project.Id, SkillId = flutter.Id }, new ProjectSkill { ProjectId = project.Id, SkillId = dotnet.Id, SortOrder = 1 }, new ProjectSkill { ProjectId = project.Id, SkillId = postgres.Id, SortOrder = 2 }]);

        var profile = new ResumeProfile { PersonId = person.Id, InternalName = "DEMO — Flutter + .NET", Slug = "demo-full-stack", DefaultLocale = Locale.En };
        profile.Translations.AddRange([
            new ResumeProfileTranslation { ProfileId = profile.Id, Locale = Locale.En, Headline = "Software Engineer | Flutter & .NET Backend", Summary = "Sample bilingual profile. It contains no real employment history or personal achievements." },
            new ResumeProfileTranslation { ProfileId = profile.Id, Locale = Locale.Ar, Headline = "مهندس برمجيات | Flutter وخدمات خلفية باستخدام .NET", Summary = "بروفايل تجريبي ثنائي اللغة، لا يحتوي خبرة وظيفية أو إنجازات شخصية حقيقية." }
        ]);
        profile.Sections.AddRange([Section(profile.Id, "projects", 0), Section(profile.Id, "skills", 1), Section(profile.Id, "education", 2), Section(profile.Id, "languages", 3)]);
        profile.PdfSettings.AddRange([Pdf(profile.Id, Locale.En), Pdf(profile.Id, Locale.Ar)]);
        var profileProject = new ProfileProject { ProfileId = profile.Id, ProjectId = project.Id };
        profile.Projects.Add(profileProject);
        db.ProfileProjectHighlights.Add(new ProfileProjectHighlight { ProfileProjectId = profileProject.Id, HighlightId = highlight.Id, SortOrder = 0 });
        profile.Skills.AddRange([new ProfileSkill { ProfileId = profile.Id, SkillId = flutter.Id }, new ProfileSkill { ProfileId = profile.Id, SkillId = dotnet.Id, WebOrder = 1, PdfOrder = 1 }, new ProfileSkill { ProfileId = profile.Id, SkillId = postgres.Id, WebOrder = 2, PdfOrder = 2 }]);
        var site = new Site { PersonId = person.Id, Name = "DEMO Identity Site", Slug = "demo", BaseUrl = "https://example.invalid", IsPrimaryIdentitySite = true };
        site.Locales.AddRange([new SiteLocale { SiteId = site.Id, Locale = Locale.En }, new SiteLocale { SiteId = site.Id, Locale = Locale.Ar }]);
        site.Translations.AddRange([
            new SiteTranslation { SiteId = site.Id, Locale = Locale.En, Title = "DEMO PROFILE — Software Engineer", Description = "Test-only portfolio content." },
            new SiteTranslation { SiteId = site.Id, Locale = Locale.Ar, Title = "ملف تجريبي — مهندس برمجيات", Description = "محتوى معرض أعمال للاختبار فقط." }
        ]);
        site.Profiles.Add(new SiteProfile { SiteId = site.Id, ProfileId = profile.Id, PathSlug = profile.Slug, IsDefault = true, Indexable = false });
        db.AddRange(person, flutter, dotnet, postgres, project, profile, site);
        await db.SaveChangesAsync();
    }

    private static Skill DemoSkill(Guid personId, string canonical, string categoryEn, string categoryAr, string display) => new()
    {
        PersonId = personId, CanonicalName = canonical, Category = categoryEn, CategoryEn = categoryEn, CategoryAr = categoryAr,
        Translations = [new() { Locale = Locale.En, DisplayName = display }, new() { Locale = Locale.Ar, DisplayName = display }]
    };
    private static ProfileSection Section(Guid profileId, string key, int order) => new() { ProfileId = profileId, SectionKey = key, WebOrder = order, PdfOrder = order };
    private static ProfilePdfSetting Pdf(Guid profileId, Locale locale) => new() { ProfileId = profileId, Locale = locale };
}

public sealed class SnapshotService(ResumeDbContext db)
{
    public async Task<ResumeDocument?> BuildResumeAsync(Guid personId, Guid profileId, Locale locale, bool forPdf, CancellationToken ct)
    {
        var person = await db.Persons.AsNoTracking().Include(x => x.Translations).Include(x => x.Links).ThenInclude(x => x.Translations).SingleOrDefaultAsync(x => x.Id == personId, ct);
        var profile = await db.ResumeProfiles.AsNoTracking().Include(x => x.Translations).Include(x => x.PdfSettings).Include(x => x.Sections).SingleOrDefaultAsync(x => x.Id == profileId && x.PersonId == personId, ct);
        if (person is null || profile is null) return null;
        var pt = person.Translations.SingleOrDefault(x => x.Locale == locale);
        var rt = profile.Translations.SingleOrDefault(x => x.Locale == locale);
        if (pt is null || rt is null) return null;
        var sectionKeys = new[] { "summary", "skills", "experience", "projects", "education", "certifications", "languages" };
        var enabledSections = sectionKeys.Select((key, index) => profile.Sections.SingleOrDefault(x => x.SectionKey == key) ?? new ProfileSection { SectionKey = key, WebEnabled = true, PdfEnabled = true, WebOrder = index, PdfOrder = index })
            .Where(x => forPdf ? x.PdfEnabled : x.WebEnabled).OrderBy(x => forPdf ? x.PdfOrder : x.WebOrder).Select(x => x.SectionKey).ToList();
        bool Enabled(string key) => enabledSections.Contains(key);
        var projectSelections = Enabled("projects") ? await db.ProfileProjects.AsNoTracking().Include(x => x.Translations).Where(x => x.ProfileId == profileId && (forPdf ? x.PdfEnabled : x.WebEnabled)).OrderBy(x => forPdf ? x.PdfOrder : x.WebOrder).ToListAsync(ct) : [];
        var selectedProjects = projectSelections.Select(x => x.ProjectId).ToList();
        var projects = await db.Projects.AsNoTracking().Include(x => x.Translations).Include(x => x.Highlights).ThenInclude(x => x.Translations).Include(x => x.Skills).Include(x => x.Media).ThenInclude(x => x.Asset).ThenInclude(x => x!.Translations).Where(x => selectedProjects.Contains(x.Id)).ToListAsync(ct);
        var profileProjectIds = projectSelections.Select(x => x.Id).ToList();
        var selectedProjectHighlights = await db.ProfileProjectHighlights.AsNoTracking().Where(x => profileProjectIds.Contains(x.ProfileProjectId) && (forPdf ? x.PdfEnabled : x.WebEnabled)).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var selectedSkills = Enabled("skills") ? await db.ProfileSkills.AsNoTracking().Where(x => x.ProfileId == profileId && (forPdf ? x.PdfEnabled : x.WebEnabled)).OrderBy(x => forPdf ? x.PdfOrder : x.WebOrder).Select(x => x.SkillId).ToListAsync(ct) : [];
        var skills = await db.Skills.AsNoTracking().Include(x => x.Translations).Where(x => selectedSkills.Contains(x.Id)).ToListAsync(ct);
        var experienceSelections = Enabled("experience") ? await db.ProfileExperiences.AsNoTracking().Where(x => x.ProfileId == profileId && (forPdf ? x.PdfEnabled : x.WebEnabled)).OrderBy(x => forPdf ? x.PdfOrder : x.WebOrder).ToListAsync(ct) : [];
        var selectedExperiences = experienceSelections.Select(x => x.ExperienceId).ToList();
        var experiences = await db.Experiences.AsNoTracking().Include(x => x.Translations).Include(x => x.Highlights).ThenInclude(x => x.Translations).Where(x => selectedExperiences.Contains(x.Id) && x.ArchivedAt == null).ToListAsync(ct);
        var profileExperienceIds = experienceSelections.Select(x => x.Id).ToList();
        var selectedExperienceHighlights = await db.ProfileExperienceHighlights.AsNoTracking().Where(x => profileExperienceIds.Contains(x.ProfileExperienceId) && (forPdf ? x.PdfEnabled : x.WebEnabled)).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var selectedEducations = Enabled("education") ? await db.ProfileEducations.AsNoTracking().Where(x => x.ProfileId == profileId && (forPdf ? x.PdfEnabled : x.WebEnabled)).OrderBy(x => forPdf ? x.PdfOrder : x.WebOrder).Select(x => x.EducationId).ToListAsync(ct) : [];
        var educations = await db.Educations.AsNoTracking().Include(x => x.Translations).Where(x => selectedEducations.Contains(x.Id) && x.ArchivedAt == null).ToListAsync(ct);
        var selectedCertifications = Enabled("certifications") ? await db.ProfileCertifications.AsNoTracking().Where(x => x.ProfileId == profileId && (forPdf ? x.PdfEnabled : x.WebEnabled)).OrderBy(x => forPdf ? x.PdfOrder : x.WebOrder).Select(x => x.CertificationId).ToListAsync(ct) : [];
        var certifications = await db.Certifications.AsNoTracking().Include(x => x.Translations).Where(x => selectedCertifications.Contains(x.Id) && x.ArchivedAt == null).ToListAsync(ct);
        var selectedLanguages = Enabled("languages") ? await db.ProfileLanguages.AsNoTracking().Where(x => x.ProfileId == profileId && (forPdf ? x.PdfEnabled : x.WebEnabled)).OrderBy(x => forPdf ? x.PdfOrder : x.WebOrder).Select(x => x.SpokenLanguageId).ToListAsync(ct) : [];
        var languages = await db.SpokenLanguages.AsNoTracking().Where(x => selectedLanguages.Contains(x.Id)).ToListAsync(ct);
        var skillMap = skills.ToDictionary(x => x.Id, x => x.Translations.SingleOrDefault(t => t.Locale == locale)?.DisplayName ?? x.CanonicalName);
        var orderedProjects = selectedProjects.Select(id => projects.Single(x => x.Id == id)).Select(p =>
        {
            var t = p.Translations.Single(x => x.Locale == locale);
            var selection = projectSelections.Single(x => x.ProjectId == p.Id);
            var targeted = selection.Translations.SingleOrDefault(x => x.Locale == locale);
            var highlightSelection = selectedProjectHighlights.Where(x => x.ProfileProjectId == selection.Id).ToList();
            var highlights = highlightSelection.Join(p.Highlights, x => x.HighlightId, x => x.Id, (_, highlight) => highlight);
            var mediaRecords = forPdf ? [] : p.Media.OrderBy(x => x.SortOrder).Where(x => x.Asset is { Status: "Ready", DeliveryUrl: not null, Width: not null, Height: not null }).Select(x => new { x.AssetId, Media = new ResumeMedia(x.Asset!.DeliveryUrl!, x.Asset.Width!.Value, x.Asset.Height!.Value, x.Asset.Translations.SingleOrDefault(value => value.Locale == locale)?.AltText ?? t.Name, x.Asset.Translations.SingleOrDefault(value => value.Locale == locale)?.Caption) }).ToList();
            var media = mediaRecords.Select(x => x.Media).ToList(); var cover = mediaRecords.FirstOrDefault(x => x.AssetId == p.CoverAssetId)?.Media;
            return new ResumeProject(p.Slug, t.Name, t.Role, targeted?.SummaryOverride ?? t.Summary, t.Description, p.RepositoryUrl, p.DemoUrl,
                highlights.Select(h => h.Translations.SingleOrDefault(x => x.Locale == locale)?.Text).Where(x => x is not null).Cast<string>().ToList(),
                p.Skills.OrderBy(x => x.SortOrder).Select(x => skillMap.GetValueOrDefault(x.SkillId)).Where(x => x is not null).Cast<string>().ToList(), cover, media);
        }).ToList();
        var orderedSkills = selectedSkills.Select(id => skills.Single(x => x.Id == id)).Select(s => new ResumeSkill(
            locale == Locale.Ar ? (string.IsNullOrWhiteSpace(s.CategoryAr) ? s.Category : s.CategoryAr) : (string.IsNullOrWhiteSpace(s.CategoryEn) ? s.Category : s.CategoryEn),
            skillMap[s.Id])).ToList();
        var orderedExperiences = selectedExperiences.Join(experiences, id => id, x => x.Id, (_, x) => x).Select(x =>
        {
            var t = x.Translations.SingleOrDefault(value => value.Locale == locale);
            var selection = experienceSelections.Single(value => value.ExperienceId == x.Id);
            var highlights = selectedExperienceHighlights.Where(value => value.ProfileExperienceId == selection.Id).Join(x.Highlights, value => value.HighlightId, value => value.Id, (_, highlight) => highlight);
            return t is null ? null : new ResumeExperience(t.Organization, t.JobTitle, t.Location, x.StartDate.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), x.IsCurrent ? null : Month(x.EndDate), t.Summary,
                highlights.Select(h => h.Translations.SingleOrDefault(value => value.Locale == locale)?.Text).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToList());
        }).Where(x => x is not null).Cast<ResumeExperience>().ToList();
        var orderedEducations = selectedEducations.Join(educations, id => id, x => x.Id, (_, x) => x).Select(x =>
        {
            var t = x.Translations.SingleOrDefault(value => value.Locale == locale);
            return t is null ? null : new ResumeEducation(t.Institution, t.Degree, t.FieldOfStudy, t.Location, Month(x.StartDate), x.IsCurrent ? null : Month(x.EndDate), t.Notes);
        }).Where(x => x is not null).Cast<ResumeEducation>().ToList();
        var orderedCertifications = selectedCertifications.Join(certifications, id => id, x => x.Id, (_, x) => x).Select(x =>
        {
            var t = x.Translations.SingleOrDefault(value => value.Locale == locale);
            return t is null ? null : new ResumeCertification(t.Name, t.Issuer, Day(x.IssuedOn), Day(x.ExpiresOn), x.CredentialUrl);
        }).Where(x => x is not null).Cast<ResumeCertification>().ToList();
        var orderedLanguages = selectedLanguages.Join(languages, id => id, x => x.Id, (_, x) => new ResumeLanguage(x.LanguageCode,
            locale == Locale.Ar ? (string.IsNullOrWhiteSpace(x.ProficiencyAr) ? x.Proficiency : x.ProficiencyAr) : (string.IsNullOrWhiteSpace(x.ProficiencyEn) ? x.Proficiency : x.ProficiencyEn))).ToList();
        var options = profile.PdfSettings.SingleOrDefault(x => x.Locale == locale) ?? new ProfilePdfSetting();
        var contact = await db.ProfileContactSettings.AsNoTracking().SingleOrDefaultAsync(x => x.ProfileId == profileId, ct) ?? new ProfileContactSetting { ProfileId = profileId };
        var selectedLinkIds = await db.ProfileLinks.AsNoTracking().Where(x => x.ProfileId == profileId && (forPdf ? x.PdfEnabled : x.WebEnabled)).OrderBy(x => x.SortOrder).Select(x => x.PersonLinkId).ToListAsync(ct);
        var links = selectedLinkIds.Count == 0 ? person.Links.OrderBy(x => x.SortOrder).ToList() : selectedLinkIds.Join(person.Links, id => id, x => x.Id, (_, x) => x).ToList();
        var location = string.Join(", ", new[] { pt.City, pt.Country }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return new ResumeDocument(locale == Locale.Ar ? "ar" : "en", locale == Locale.Ar ? "rtl" : "ltr", pt.FullName,
            string.IsNullOrWhiteSpace(location) ? null : location, rt.Headline, rt.Summary,
            (forPdf ? contact.PdfEmail : contact.WebEmail) ? person.Email : null,
            (forPdf ? contact.PdfPhone : contact.WebPhone) ? person.Phone : null,
            links.Select(x => new ResumeLink(x.Kind.ToString(), x.Translations.SingleOrDefault(t => t.Locale == locale)?.Label ?? x.Kind.ToString(), x.Url)).ToList(),
            orderedSkills, orderedProjects, orderedExperiences, orderedEducations, orderedCertifications, orderedLanguages, enabledSections, new ResumePdfOptions(options.PaperSize, options.FontSize, options.MarginMm, options.TargetPages));
    }

    private static string? Month(DateOnly? value) => value?.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
    private static string? Day(DateOnly? value) => value?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    public static string Hash(object snapshot)
    {
        var bytes = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(snapshot));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
