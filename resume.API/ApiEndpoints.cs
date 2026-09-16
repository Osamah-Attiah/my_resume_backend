using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resume.Application;
using Resume.Core;
using Resume.Infrastructure;

namespace Resume.Api;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapResumeApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        MapAuth(api);
        var admin = api.MapGroup("/admin").RequireAuthorization();
        MapOwner(admin);
        MapSkills(admin);
        MapProjects(admin);
        MapMedia(admin);
        MapProfiles(admin);
        MapSites(admin);
        MapSupportingContent(admin);
        MapPublishing(admin);
        MapInternal(api);
        return endpoints;
    }

    private static void MapAuth(RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth");
        auth.MapPost("/login", async (LoginRequest request, ResumeDbContext db, TokenService tokens, CancellationToken ct) =>
        {
            var email = request.Email.Trim().ToUpperInvariant();
            var user = await db.AdminUsers.SingleOrDefaultAsync(x => x.NormalizedEmail == email, ct);
            var generic = Results.Problem(statusCode: 401, title: "Email or password is incorrect", extensions: Problem("INVALID_CREDENTIALS"));
            if (user is null || !user.IsActive || user.LockoutEnd > DateTimeOffset.UtcNow) return generic;
            var result = new PasswordHasher<AdminUser>().VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                user.FailedAttempts++;
                if (user.FailedAttempts >= 5) user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
                await db.SaveChangesAsync(ct);
                return generic;
            }
            user.FailedAttempts = 0;
            user.LockoutEnd = null;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new AuthResponse(tokens.Create(user), DateTimeOffset.UtcNow.AddMinutes(30)));
        }).RequireRateLimiting("login").AllowAnonymous();
        auth.MapGet("/me", async (HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var user = await db.AdminUsers.AsNoTracking().SingleAsync(x => x.Id == http.User.AdminId(), ct);
            return Results.Ok(new { user.Id, user.PersonId, email = user.NormalizedEmail.ToLowerInvariant() });
        }).RequireAuthorization();
        auth.MapPost("/logout", async (HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var user = await db.AdminUsers.SingleAsync(x => x.Id == http.User.AdminId(), ct);
            user.SessionVersion++;
            await Audit(db, user.Id, "auth.logout", "AdminUser", user.Id, ct);
            return Results.NoContent();
        }).RequireAuthorization();
        auth.MapPut("/password", async (PasswordChangeRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (request.NewPassword.Length < 14) return Validation("PASSWORD_TOO_SHORT", "newPassword", "Use at least 14 characters.");
            var user = await db.AdminUsers.SingleAsync(x => x.Id == http.User.AdminId(), ct);
            var hasher = new PasswordHasher<AdminUser>();
            if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
                return Results.Problem(statusCode: 400, title: "Current password is incorrect", extensions: Problem("INVALID_CURRENT_PASSWORD"));
            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
            user.SessionVersion++;
            await Audit(db, user.Id, "auth.password_changed", "AdminUser", user.Id, ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static void MapOwner(RouteGroupBuilder admin)
    {
        admin.MapGet("/person", async (HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var person = await db.Persons.AsNoTracking().Include(x => x.Translations).Include(x => x.Links).ThenInclude(x => x.Translations).SingleAsync(x => x.Id == http.User.PersonId(), ct);
            return Results.Ok(PersonDto.From(person));
        });
        admin.MapPut("/person", async (PersonUpdate request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var person = await db.Persons.SingleAsync(x => x.Id == http.User.PersonId(), ct);
            if (person.Version != request.Version) return Conflict();

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.PersonTranslations.Where(x => x.PersonId == person.Id).ExecuteDeleteAsync(ct);

            person.Email = Clean(request.Email);
            person.Phone = Clean(request.Phone);
            person.DefaultLocale = request.DefaultLocale;
            person.UpdatedAt = DateTimeOffset.UtcNow;

            var translations = new[]
            {
                CreatePersonTranslation(person.Id, request.En, Locale.En),
                CreatePersonTranslation(person.Id, request.Ar, Locale.Ar)
            };
            person.Translations.AddRange(translations);
            db.PersonTranslations.AddRange(translations);

            await Audit(db, http.User.AdminId(), "person.updated", "Person", person.Id, ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(PersonDto.From(person));
        });
        admin.MapPost("/person/links", async (LinkUpsert request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!IsHttps(request.Url)) return Validation("HTTPS_REQUIRED", "url", "Only HTTPS links are accepted.");
            var link = new PersonLink { PersonId = http.User.PersonId(), Kind = request.Kind, Url = request.Url.Trim(), SortOrder = request.SortOrder };
            link.Translations.AddRange([new() { PersonLinkId = link.Id, Locale = Locale.En, Label = request.LabelEn.Trim() }, new() { PersonLinkId = link.Id, Locale = Locale.Ar, Label = request.LabelAr.Trim() }]);
            db.Add(link);
            await Audit(db, http.User.AdminId(), "person_link.created", "PersonLink", link.Id, ct);
            return Results.Created($"/api/v1/admin/person/links/{link.Id}", link);
        });
        admin.MapPut("/person/links/{id:guid}", async (Guid id, LinkUpsert request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!IsHttps(request.Url)) return Validation("HTTPS_REQUIRED", "url", "Only HTTPS links are accepted.");
            var link = await db.PersonLinks.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (link is null) return Results.NotFound();
            if (link.Version != request.Version) return Conflict();
            link.Kind = request.Kind; link.Url = request.Url.Trim(); link.SortOrder = request.SortOrder;
            UpsertLinkTranslation(link, Locale.En, request.LabelEn); UpsertLinkTranslation(link, Locale.Ar, request.LabelAr);
            await Audit(db, http.User.AdminId(), "person_link.updated", "PersonLink", link.Id, ct);
            return Results.Ok(link);
        });
        admin.MapDelete("/person/links/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var link = await db.PersonLinks.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (link is null) return Results.NotFound();
            db.Remove(link); await Audit(db, http.User.AdminId(), "person_link.deleted", "PersonLink", id, ct); return Results.NoContent();
        });
    }

    private static void MapSkills(RouteGroupBuilder admin)
    {
        admin.MapGet("/skills", async (HttpContext http, ResumeDbContext db, CancellationToken ct) => Results.Ok(await db.Skills.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == http.User.PersonId() && x.ArchivedAt == null).OrderBy(x => x.Category).ThenBy(x => x.CanonicalName).ToListAsync(ct)));
        admin.MapPost("/skills", async (SkillRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.CanonicalName)) return Validation("REQUIRED", "canonicalName", "Canonical name is required.");
            var category = Clean(request.CategoryEn) ?? Clean(request.Category) ?? "";
            var skill = new Skill { PersonId = http.User.PersonId(), CanonicalName = request.CanonicalName.Trim(), Category = category, CategoryEn = category, CategoryAr = Clean(request.CategoryAr) ?? category };
            skill.Translations.AddRange([new() { SkillId = skill.Id, Locale = Locale.En, DisplayName = request.DisplayNameEn.Trim() }, new() { SkillId = skill.Id, Locale = Locale.Ar, DisplayName = request.DisplayNameAr.Trim() }]);
            db.Add(skill); await Audit(db, http.User.AdminId(), "skill.created", "Skill", skill.Id, ct);
            return Results.Created($"/api/v1/admin/skills/{skill.Id}", skill);
        });
        admin.MapPut("/skills/{id:guid}", async (Guid id, SkillRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var skill = await db.Skills.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (skill is null) return Results.NotFound(); if (skill.Version != request.Version) return Conflict();
            var category = Clean(request.CategoryEn) ?? Clean(request.Category) ?? "";
            skill.CanonicalName = request.CanonicalName.Trim(); skill.Category = category; skill.CategoryEn = category; skill.CategoryAr = Clean(request.CategoryAr) ?? category;
            UpsertSkillTranslation(skill, Locale.En, request.DisplayNameEn); UpsertSkillTranslation(skill, Locale.Ar, request.DisplayNameAr);
            await Audit(db, http.User.AdminId(), "skill.updated", "Skill", skill.Id, ct); return Results.Ok(skill);
        });
        admin.MapDelete("/skills/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var skill = await db.Skills.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (skill is null) return Results.NotFound();
            skill.ArchivedAt = DateTimeOffset.UtcNow; await Audit(db, http.User.AdminId(), "skill.archived", "Skill", id, ct); return Results.NoContent();
        });
    }

    private static void MapProjects(RouteGroupBuilder admin)
    {
        admin.MapGet("/projects", async (HttpContext http, ResumeDbContext db, CancellationToken ct) => Results.Ok(await db.Projects.AsNoTracking().Include(x => x.Translations).Include(x => x.Highlights).ThenInclude(x => x.Translations).Include(x => x.Media).ThenInclude(x => x.Asset).ThenInclude(x => x!.Translations).Where(x => x.PersonId == http.User.PersonId() && x.ArchivedAt == null).OrderByDescending(x => x.UpdatedAt).ToListAsync(ct)));
        admin.MapGet("/projects/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.AsNoTracking().Include(x => x.Translations).Include(x => x.Highlights).ThenInclude(x => x.Translations).Include(x => x.Skills).Include(x => x.Media).ThenInclude(x => x.Asset).ThenInclude(x => x!.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            return project is null ? Results.NotFound() : Results.Ok(project);
        });
        admin.MapPost("/projects", async (ProjectRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var error = ValidateProject(request); if (error is not null) return error;
            var project = new Project { PersonId = http.User.PersonId() }; ApplyProject(project, request);
            db.Add(project); await Audit(db, http.User.AdminId(), "project.created", "Project", project.Id, ct);
            return Results.Created($"/api/v1/admin/projects/{project.Id}", project);
        });
        admin.MapPut("/projects/{id:guid}", async (Guid id, ProjectRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var error = ValidateProject(request); if (error is not null) return error;
            var project = await db.Projects.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (project is null) return Results.NotFound(); if (project.Version != request.Version) return Conflict();
            ApplyProject(project, request); await Audit(db, http.User.AdminId(), "project.updated", "Project", project.Id, ct); return Results.Ok(project);
        });
        admin.MapDelete("/projects/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (project is null) return Results.NotFound();
            project.ArchivedAt = DateTimeOffset.UtcNow; await Audit(db, http.User.AdminId(), "project.archived", "Project", id, ct); return Results.NoContent();
        });
        admin.MapPost("/projects/{id:guid}/highlights", async (Guid id, HighlightRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(request.TextEn) || string.IsNullOrWhiteSpace(request.TextAr)) return Validation("TRANSLATION_REQUIRED", "text", "Arabic and English highlight text are required.");
            var highlight = new ProjectHighlight { ProjectId = id, SortOrder = request.SortOrder };
            highlight.Translations.AddRange([new() { HighlightId = highlight.Id, Locale = Locale.En, Text = request.TextEn.Trim() }, new() { HighlightId = highlight.Id, Locale = Locale.Ar, Text = request.TextAr.Trim() }]);
            db.Add(highlight); await Audit(db, http.User.AdminId(), "project_highlight.created", "ProjectHighlight", highlight.Id, ct); return Results.Created($"/api/v1/admin/projects/{id}/highlights/{highlight.Id}", highlight);
        });
        admin.MapPut("/projects/{id:guid}/highlights/{highlightId:guid}", async (Guid id, Guid highlightId, HighlightRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.TextEn) || string.IsNullOrWhiteSpace(request.TextAr)) return Validation("TRANSLATION_REQUIRED", "text", "Arabic and English highlight text are required.");
            var highlight = await db.ProjectHighlights.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == highlightId && x.ProjectId == id && db.Projects.Any(p => p.Id == id && p.PersonId == http.User.PersonId()), ct);
            if (highlight is null) return Results.NotFound();
            if (highlight.Version != request.Version) return Conflict();
            highlight.SortOrder = request.SortOrder;
            UpsertProjectHighlightTranslation(highlight, Locale.En, request.TextEn);
            UpsertProjectHighlightTranslation(highlight, Locale.Ar, request.TextAr);
            await Audit(db, http.User.AdminId(), "project_highlight.updated", "ProjectHighlight", highlight.Id, ct); return Results.Ok(highlight);
        });
        admin.MapDelete("/projects/{id:guid}/highlights/{highlightId:guid}", async (Guid id, Guid highlightId, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var highlight = await db.ProjectHighlights.SingleOrDefaultAsync(x => x.Id == highlightId && x.ProjectId == id && db.Projects.Any(p => p.Id == id && p.PersonId == http.User.PersonId()), ct);
            if (highlight is null) return Results.NotFound();
            db.ProfileProjectHighlights.RemoveRange(db.ProfileProjectHighlights.Where(x => x.HighlightId == highlightId));
            db.ProjectHighlights.Remove(highlight);
            await Audit(db, http.User.AdminId(), "project_highlight.deleted", "ProjectHighlight", highlight.Id, ct); return Results.NoContent();
        });
        admin.MapGet("/projects/{id:guid}/media", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (project is null) return Results.NotFound();
            var items = await db.ProjectMedia.AsNoTracking().Include(x => x.Asset).ThenInclude(x => x!.Translations).Where(x => x.ProjectId == id).OrderBy(x => x.SortOrder).ToListAsync(ct);
            return Results.Ok(new { project.CoverAssetId, items });
        });
        admin.MapPut("/projects/{id:guid}/media", async (Guid id, ProjectMediaRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (project is null) return Results.NotFound();
            var assetIds = request.AssetIds.Distinct().ToList(); if (assetIds.Count != request.AssetIds.Count) return Validation("DUPLICATE_MEDIA", "assetIds", "Each image can be attached only once.");
            if (request.CoverAssetId is not null && !assetIds.Contains(request.CoverAssetId.Value)) return Validation("COVER_NOT_ATTACHED", "coverAssetId", "The cover must be one of the attached images.");
            if (await db.MediaAssets.CountAsync(x => x.PersonId == project.PersonId && x.ArchivedAt == null && x.Status == "Ready" && assetIds.Contains(x.Id), ct) != assetIds.Count) return Validation("MEDIA_OWNERSHIP_MISMATCH", "assetIds", "Every image must be an active asset owned by this profile owner.");
            var current = await db.ProjectMedia.Where(x => x.ProjectId == id).ToListAsync(ct); db.ProjectMedia.RemoveRange(current.Where(x => !assetIds.Contains(x.AssetId)));
            foreach (var pair in assetIds.Select((assetId, index) => new { assetId, index })) { var item = current.SingleOrDefault(x => x.AssetId == pair.assetId); if (item is null) db.ProjectMedia.Add(new ProjectMedia { ProjectId = id, AssetId = pair.assetId, SortOrder = pair.index }); else item.SortOrder = pair.index; }
            project.CoverAssetId = request.CoverAssetId;
            await Audit(db, http.User.AdminId(), "project.media_updated", "Project", id, ct); return Results.NoContent();
        });
    }

    private static void MapMedia(RouteGroupBuilder admin)
    {
        admin.MapGet("/media", async (HttpContext http, ResumeDbContext db, CancellationToken ct) => Results.Ok(await db.MediaAssets.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == http.User.PersonId() && x.ArchivedAt == null).OrderByDescending(x => x.CreatedAt).ToListAsync(ct)));
        admin.MapPost("/media", async (HttpRequest request, HttpContext http, ResumeDbContext db, IMediaStorage storage, CancellationToken ct) =>
        {
            if (!request.HasFormContentType) return Validation("MULTIPART_REQUIRED", "file", "Upload one image as multipart/form-data.");
            var form = await request.ReadFormAsync(ct); var file = form.Files.SingleOrDefault(); if (file is null || form.Files.Count != 1) return Validation("ONE_IMAGE_REQUIRED", "file", "Upload exactly one image.");
            if (file.Length is <= 0 or > 5_000_000) return Validation("IMAGE_SIZE", "file", "Image size must be between 1 byte and 5 MB.");
            var altAr = form["altAr"].ToString().Trim(); var altEn = form["altEn"].ToString().Trim(); if (string.IsNullOrWhiteSpace(altAr) || string.IsNullOrWhiteSpace(altEn)) return Validation("ALT_TRANSLATION_REQUIRED", "alt", "Arabic and English alt text are required.");
            await using var memory = new MemoryStream((int)file.Length); await file.CopyToAsync(memory, ct); var bytes = memory.ToArray();
            var info = ImageInfo(bytes); if (info is null) return Validation("UNSUPPORTED_IMAGE", "file", "Only valid PNG, JPEG, and WebP images are accepted.");
            if (info.Value.Width is < 1 or > 6000 || info.Value.Height is < 1 or > 6000) return Validation("IMAGE_DIMENSIONS", "file", "Image dimensions must not exceed 6000×6000 pixels.");
            var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            var existing = await db.MediaAssets.AsNoTracking().Include(x => x.Translations).SingleOrDefaultAsync(x => x.PersonId == http.User.PersonId() && x.Sha256 == sha && x.ArchivedAt == null, ct); if (existing is not null) return Results.Ok(existing);
            if (!storage.IsConfigured) return Results.Problem(statusCode: 503, title: "Cloudinary media storage is not configured", extensions: Problem("MEDIA_STORAGE_NOT_CONFIGURED"));
            var upload = await storage.UploadAsync(bytes, info.Value.MimeType, Guid.NewGuid().ToString("N"), ct);
            var asset = new MediaAsset { PersonId = http.User.PersonId(), Provider = "Cloudinary", StorageKey = upload.StorageKey, DeliveryUrl = upload.DeliveryUrl, MimeType = info.Value.MimeType, SizeBytes = upload.SizeBytes, Width = upload.Width, Height = upload.Height, Sha256 = sha, Status = "Ready" };
            asset.Translations.AddRange([new() { AssetId = asset.Id, Locale = Locale.Ar, AltText = altAr, Caption = Clean(form["captionAr"].ToString()) }, new() { AssetId = asset.Id, Locale = Locale.En, AltText = altEn, Caption = Clean(form["captionEn"].ToString()) }]);
            db.Add(asset); await Audit(db, http.User.AdminId(), "media.uploaded", "MediaAsset", asset.Id, ct); return Results.Created($"/api/v1/admin/media/{asset.Id}", asset);
        }).DisableAntiforgery();
        admin.MapPut("/media/{id:guid}", async (Guid id, MediaUpdateRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.AltAr) || string.IsNullOrWhiteSpace(request.AltEn)) return Validation("ALT_TRANSLATION_REQUIRED", "alt", "Arabic and English alt text are required.");
            var asset = await db.MediaAssets.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId() && x.ArchivedAt == null, ct); if (asset is null) return Results.NotFound(); if (asset.Version != request.Version) return Conflict();
            UpsertMediaTranslation(asset, Locale.Ar, request.AltAr, request.CaptionAr); UpsertMediaTranslation(asset, Locale.En, request.AltEn, request.CaptionEn); await Audit(db, http.User.AdminId(), "media.updated", "MediaAsset", id, ct); return Results.Ok(asset);
        });
        admin.MapDelete("/media/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId() && x.ArchivedAt == null, ct); if (asset is null) return Results.NotFound();
            if (await db.ProjectMedia.AnyAsync(x => x.AssetId == id, ct)) return Results.Problem(statusCode: 409, title: "Detach the image from projects before archiving it", extensions: Problem("MEDIA_IN_USE"));
            asset.ArchivedAt = DateTimeOffset.UtcNow; await Audit(db, http.User.AdminId(), "media.archived", "MediaAsset", id, ct); return Results.NoContent();
        });
    }

    private static void MapProfiles(RouteGroupBuilder admin)
    {
        admin.MapGet("/profiles", async (HttpContext http, ResumeDbContext db, CancellationToken ct) => Results.Ok(await db.ResumeProfiles.AsNoTracking().Include(x => x.Translations).Include(x => x.Sections).Include(x => x.PdfSettings).Where(x => x.PersonId == http.User.PersonId() && x.ArchivedAt == null).OrderBy(x => x.InternalName).ToListAsync(ct)));
        admin.MapGet("/profiles/{id:guid}/selection", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var personId = http.User.PersonId();
            if (!await db.ResumeProfiles.AsNoTracking().AnyAsync(x => x.Id == id && x.PersonId == personId, ct)) return Results.NotFound();
            var selectedProjects = await db.ProfileProjects.AsNoTracking().Include(x => x.Translations).Where(x => x.ProfileId == id).ToDictionaryAsync(x => x.ProjectId, ct);
            var selectedSkills = await db.ProfileSkills.AsNoTracking().Where(x => x.ProfileId == id).ToDictionaryAsync(x => x.SkillId, ct);
            var selectedExperiences = await db.ProfileExperiences.AsNoTracking().Where(x => x.ProfileId == id).ToDictionaryAsync(x => x.ExperienceId, ct);
            var selectedProjectIds = selectedProjects.Values.Select(x => x.Id).ToList();
            var selectedExperienceIds = selectedExperiences.Values.Select(x => x.Id).ToList();
            var selectedProjectHighlights = await db.ProfileProjectHighlights.AsNoTracking().Where(x => selectedProjectIds.Contains(x.ProfileProjectId)).ToListAsync(ct);
            var selectedExperienceHighlights = await db.ProfileExperienceHighlights.AsNoTracking().Where(x => selectedExperienceIds.Contains(x.ProfileExperienceId)).ToListAsync(ct);
            var selectedEducations = await db.ProfileEducations.AsNoTracking().Where(x => x.ProfileId == id).ToDictionaryAsync(x => x.EducationId, ct);
            var selectedCertifications = await db.ProfileCertifications.AsNoTracking().Where(x => x.ProfileId == id).ToDictionaryAsync(x => x.CertificationId, ct);
            var selectedLanguages = await db.ProfileLanguages.AsNoTracking().Where(x => x.ProfileId == id).ToDictionaryAsync(x => x.SpokenLanguageId, ct);
            var projects = await db.Projects.AsNoTracking().Include(x => x.Translations).Include(x => x.Highlights).ThenInclude(x => x.Translations).Where(x => x.PersonId == personId && x.ArchivedAt == null).OrderBy(x => x.Slug).ToListAsync(ct);
            var skills = await db.Skills.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == personId && x.ArchivedAt == null).OrderBy(x => x.Category).ThenBy(x => x.CanonicalName).ToListAsync(ct);
            var experiences = await db.Experiences.AsNoTracking().Include(x => x.Translations).Include(x => x.Highlights).ThenInclude(x => x.Translations).Where(x => x.PersonId == personId && x.ArchivedAt == null).OrderByDescending(x => x.StartDate).ToListAsync(ct);
            var educations = await db.Educations.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == personId && x.ArchivedAt == null).OrderByDescending(x => x.StartDate).ToListAsync(ct);
            var certifications = await db.Certifications.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == personId && x.ArchivedAt == null).OrderByDescending(x => x.IssuedOn).ToListAsync(ct);
            var languages = await db.SpokenLanguages.AsNoTracking().Where(x => x.PersonId == personId).OrderBy(x => x.LanguageCode).ToListAsync(ct);
            static object Item(Guid itemId, string labelAr, string labelEn, bool selected, bool webEnabled, bool pdfEnabled, int webOrder, int pdfOrder) => new { id = itemId, labelAr, labelEn, selected, webEnabled, pdfEnabled, webOrder, pdfOrder };
            return Results.Ok(new
            {
                projects = projects.Select(x =>
                {
                    var s = selectedProjects.GetValueOrDefault(x.Id);
                    return new
                    {
                        id = x.Id,
                        labelAr = x.Translations.FirstOrDefault(t => t.Locale == Locale.Ar)?.Name ?? x.Slug,
                        labelEn = x.Translations.FirstOrDefault(t => t.Locale == Locale.En)?.Name ?? x.Slug,
                        selected = s is not null,
                        webEnabled = s?.WebEnabled ?? false,
                        pdfEnabled = s?.PdfEnabled ?? false,
                        webOrder = s?.WebOrder ?? 0,
                        pdfOrder = s?.PdfOrder ?? 0,
                        summaryOverrideAr = s?.Translations.FirstOrDefault(t => t.Locale == Locale.Ar)?.SummaryOverride,
                        summaryOverrideEn = s?.Translations.FirstOrDefault(t => t.Locale == Locale.En)?.SummaryOverride,
                        highlights = x.Highlights.OrderBy(h => h.SortOrder).Select(h =>
                        {
                            var selected = s is null ? null : selectedProjectHighlights.SingleOrDefault(sh => sh.ProfileProjectId == s.Id && sh.HighlightId == h.Id);
                            return new { id = h.Id, labelAr = h.Translations.FirstOrDefault(t => t.Locale == Locale.Ar)?.Text ?? "", labelEn = h.Translations.FirstOrDefault(t => t.Locale == Locale.En)?.Text ?? "", selected = selected is not null, webEnabled = selected?.WebEnabled ?? false, pdfEnabled = selected?.PdfEnabled ?? false, sortOrder = selected?.SortOrder ?? h.SortOrder };
                        })
                    };
                }),
                skills = skills.Select(x => { var s = selectedSkills.GetValueOrDefault(x.Id); return Item(x.Id, x.Translations.FirstOrDefault(t => t.Locale == Locale.Ar)?.DisplayName ?? x.CanonicalName, x.Translations.FirstOrDefault(t => t.Locale == Locale.En)?.DisplayName ?? x.CanonicalName, s is not null, s?.WebEnabled ?? false, s?.PdfEnabled ?? false, s?.WebOrder ?? 0, s?.PdfOrder ?? 0); }),
                experiences = experiences.Select(x =>
                {
                    var s = selectedExperiences.GetValueOrDefault(x.Id);
                    return new
                    {
                        id = x.Id,
                        labelAr = x.Translations.FirstOrDefault(t => t.Locale == Locale.Ar)?.JobTitle ?? "خبرة",
                        labelEn = x.Translations.FirstOrDefault(t => t.Locale == Locale.En)?.JobTitle ?? "Experience",
                        selected = s is not null,
                        webEnabled = s?.WebEnabled ?? false,
                        pdfEnabled = s?.PdfEnabled ?? false,
                        webOrder = s?.WebOrder ?? 0,
                        pdfOrder = s?.PdfOrder ?? 0,
                        highlights = x.Highlights.OrderBy(h => h.SortOrder).Select(h =>
                        {
                            var selected = s is null ? null : selectedExperienceHighlights.SingleOrDefault(sh => sh.ProfileExperienceId == s.Id && sh.HighlightId == h.Id);
                            return new { id = h.Id, labelAr = h.Translations.FirstOrDefault(t => t.Locale == Locale.Ar)?.Text ?? "", labelEn = h.Translations.FirstOrDefault(t => t.Locale == Locale.En)?.Text ?? "", selected = selected is not null, webEnabled = selected?.WebEnabled ?? false, pdfEnabled = selected?.PdfEnabled ?? false, sortOrder = selected?.SortOrder ?? h.SortOrder };
                        })
                    };
                }),
                educations = educations.Select(x => { var s = selectedEducations.GetValueOrDefault(x.Id); return Item(x.Id, x.Translations.FirstOrDefault(t => t.Locale == Locale.Ar)?.Degree ?? "تعليم", x.Translations.FirstOrDefault(t => t.Locale == Locale.En)?.Degree ?? "Education", s is not null, s?.WebEnabled ?? false, s?.PdfEnabled ?? false, s?.WebOrder ?? 0, s?.PdfOrder ?? 0); }),
                certifications = certifications.Select(x => { var s = selectedCertifications.GetValueOrDefault(x.Id); return Item(x.Id, x.Translations.FirstOrDefault(t => t.Locale == Locale.Ar)?.Name ?? "شهادة", x.Translations.FirstOrDefault(t => t.Locale == Locale.En)?.Name ?? "Certification", s is not null, s?.WebEnabled ?? false, s?.PdfEnabled ?? false, s?.WebOrder ?? 0, s?.PdfOrder ?? 0); }),
                languages = languages.Select(x => { var s = selectedLanguages.GetValueOrDefault(x.Id); return Item(x.Id, x.LanguageCode, x.LanguageCode, s is not null, s?.WebEnabled ?? false, s?.PdfEnabled ?? false, s?.WebOrder ?? 0, s?.PdfOrder ?? 0); })
            });
        });
        admin.MapPost("/profiles", async (ProfileRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!DomainValidation.IsSlug(request.Slug)) return Validation("INVALID_SLUG", "slug", "Use lowercase Latin letters, numbers and hyphens.");
            var profile = new ResumeProfile { PersonId = http.User.PersonId(), InternalName = request.InternalName.Trim(), Slug = request.Slug, DefaultLocale = request.DefaultLocale };
            profile.Translations.AddRange([new() { ProfileId = profile.Id, Locale = Locale.En, Headline = request.HeadlineEn.Trim(), Summary = request.SummaryEn.Trim() }, new() { ProfileId = profile.Id, Locale = Locale.Ar, Headline = request.HeadlineAr.Trim(), Summary = request.SummaryAr.Trim() }]);
            profile.PdfSettings.AddRange([new() { ProfileId = profile.Id, Locale = Locale.En }, new() { ProfileId = profile.Id, Locale = Locale.Ar }]);
            db.Add(profile); await Audit(db, http.User.AdminId(), "profile.created", "ResumeProfile", profile.Id, ct); return Results.Created($"/api/v1/admin/profiles/{profile.Id}", profile);
        });
        admin.MapPut("/profiles/{id:guid}", async (Guid id, ProfileUpdateRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!DomainValidation.IsSlug(request.Slug)) return Validation("INVALID_SLUG", "slug", "Use lowercase Latin letters, numbers and hyphens.");
            var profile = await db.ResumeProfiles.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (profile is null) return Results.NotFound(); if (profile.Version != request.Version) return Conflict();
            profile.InternalName = request.InternalName.Trim(); profile.Slug = request.Slug; profile.DefaultLocale = request.DefaultLocale;
            UpsertProfileTranslation(profile, Locale.En, request.HeadlineEn, request.SummaryEn, request.SeoTitleEn, request.SeoDescriptionEn);
            UpsertProfileTranslation(profile, Locale.Ar, request.HeadlineAr, request.SummaryAr, request.SeoTitleAr, request.SeoDescriptionAr);
            await Audit(db, http.User.AdminId(), "profile.updated", "ResumeProfile", id, ct); return Results.Ok(profile);
        });
        admin.MapDelete("/profiles/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var profile = await db.ResumeProfiles.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (profile is null) return Results.NotFound();
            if (await db.SiteProfiles.AnyAsync(x => x.ProfileId == id, ct)) return Results.Problem(statusCode: 409, title: "Profile is attached to a site", extensions: Problem("PROFILE_IN_USE"));
            profile.ArchivedAt = DateTimeOffset.UtcNow; await Audit(db, http.User.AdminId(), "profile.archived", "ResumeProfile", id, ct); return Results.NoContent();
        });
        admin.MapPost("/profiles/{id:guid}/duplicate", async (Guid id, DuplicateProfileRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!DomainValidation.IsSlug(request.Slug)) return Validation("INVALID_SLUG", "slug", "Use lowercase Latin letters, numbers and hyphens.");
            var source = await db.ResumeProfiles.AsNoTracking().Include(x => x.Translations).Include(x => x.Sections).Include(x => x.PdfSettings).Include(x => x.Projects).ThenInclude(x => x.Translations).Include(x => x.Skills).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (source is null) return Results.NotFound();
            var copy = new ResumeProfile { PersonId = source.PersonId, InternalName = request.InternalName.Trim(), Slug = request.Slug, DefaultLocale = source.DefaultLocale };
            copy.Translations = source.Translations.Select(x => new ResumeProfileTranslation { ProfileId = copy.Id, Locale = x.Locale, Headline = x.Headline, Summary = x.Summary, SeoTitle = x.SeoTitle, SeoDescription = x.SeoDescription }).ToList();
            copy.Sections = source.Sections.Select(x => new ProfileSection { ProfileId = copy.Id, SectionKey = x.SectionKey, WebEnabled = x.WebEnabled, PdfEnabled = x.PdfEnabled, WebOrder = x.WebOrder, PdfOrder = x.PdfOrder }).ToList();
            copy.PdfSettings = source.PdfSettings.Select(x => new ProfilePdfSetting { ProfileId = copy.Id, Locale = x.Locale, TemplateKey = x.TemplateKey, PaperSize = x.PaperSize, FontSize = x.FontSize, MarginMm = x.MarginMm, TargetPages = x.TargetPages }).ToList();
            var projectCopies = source.Projects.ToDictionary(x => x.Id, x => new ProfileProject
            {
                ProfileId = copy.Id,
                ProjectId = x.ProjectId,
                WebEnabled = x.WebEnabled,
                PdfEnabled = x.PdfEnabled,
                WebOrder = x.WebOrder,
                PdfOrder = x.PdfOrder,
                Translations = x.Translations.Select(t => new ProfileProjectTranslation { Locale = t.Locale, SummaryOverride = t.SummaryOverride }).ToList()
            });
            copy.Projects = projectCopies.Values.ToList();
            copy.Skills = source.Skills.Select(x => new ProfileSkill { ProfileId = copy.Id, SkillId = x.SkillId, WebEnabled = x.WebEnabled, PdfEnabled = x.PdfEnabled, WebOrder = x.WebOrder, PdfOrder = x.PdfOrder }).ToList();
            var sourceProjectIds = source.Projects.Select(x => x.Id).ToList();
            var sourceHighlights = await db.ProfileProjectHighlights.AsNoTracking().Where(x => sourceProjectIds.Contains(x.ProfileProjectId)).ToListAsync(ct);
            db.ProfileProjectHighlights.AddRange(sourceHighlights.Select(x => new ProfileProjectHighlight { ProfileProjectId = projectCopies[x.ProfileProjectId].Id, HighlightId = x.HighlightId, WebEnabled = x.WebEnabled, PdfEnabled = x.PdfEnabled, SortOrder = x.SortOrder }));
            var sourceExperiences = await db.ProfileExperiences.AsNoTracking().Where(x => x.ProfileId == id).ToListAsync(ct);
            var experienceCopies = sourceExperiences.ToDictionary(x => x.Id, x => new ProfileExperience { ProfileId = copy.Id, ExperienceId = x.ExperienceId, WebEnabled = x.WebEnabled, PdfEnabled = x.PdfEnabled, WebOrder = x.WebOrder, PdfOrder = x.PdfOrder });
            db.ProfileExperiences.AddRange(experienceCopies.Values);
            var sourceExperienceIds = sourceExperiences.Select(x => x.Id).ToList();
            var sourceExperienceHighlights = await db.ProfileExperienceHighlights.AsNoTracking().Where(x => sourceExperienceIds.Contains(x.ProfileExperienceId)).ToListAsync(ct);
            db.ProfileExperienceHighlights.AddRange(sourceExperienceHighlights.Select(x => new ProfileExperienceHighlight { ProfileExperienceId = experienceCopies[x.ProfileExperienceId].Id, HighlightId = x.HighlightId, WebEnabled = x.WebEnabled, PdfEnabled = x.PdfEnabled, SortOrder = x.SortOrder }));
            db.ProfileEducations.AddRange((await db.ProfileEducations.AsNoTracking().Where(x => x.ProfileId == id).ToListAsync(ct)).Select(x => new ProfileEducation { ProfileId = copy.Id, EducationId = x.EducationId, WebEnabled = x.WebEnabled, PdfEnabled = x.PdfEnabled, WebOrder = x.WebOrder, PdfOrder = x.PdfOrder }));
            db.ProfileCertifications.AddRange((await db.ProfileCertifications.AsNoTracking().Where(x => x.ProfileId == id).ToListAsync(ct)).Select(x => new ProfileCertification { ProfileId = copy.Id, CertificationId = x.CertificationId, WebEnabled = x.WebEnabled, PdfEnabled = x.PdfEnabled, WebOrder = x.WebOrder, PdfOrder = x.PdfOrder }));
            db.ProfileLanguages.AddRange((await db.ProfileLanguages.AsNoTracking().Where(x => x.ProfileId == id).ToListAsync(ct)).Select(x => new ProfileLanguage { ProfileId = copy.Id, SpokenLanguageId = x.SpokenLanguageId, WebEnabled = x.WebEnabled, PdfEnabled = x.PdfEnabled, WebOrder = x.WebOrder, PdfOrder = x.PdfOrder }));
            db.Add(copy); await Audit(db, http.User.AdminId(), "profile.duplicated", "ResumeProfile", copy.Id, ct); return Results.Created($"/api/v1/admin/profiles/{copy.Id}", copy);
        });
        admin.MapPut("/profiles/{id:guid}/selection", async (Guid id, ProfileSelectionRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var profile = await db.ResumeProfiles.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (profile is null) return Results.NotFound();
            var projectIds = request.Projects.Select(x => x.Id).ToList(); var skillIds = request.Skills.Select(x => x.Id).ToList();
            if (await db.Projects.CountAsync(x => x.PersonId == profile.PersonId && projectIds.Contains(x.Id), ct) != projectIds.Count || await db.Skills.CountAsync(x => x.PersonId == profile.PersonId && skillIds.Contains(x.Id), ct) != skillIds.Count)
                return Results.Problem(statusCode: 400, title: "A selected item is not owned by this person", extensions: Problem("OWNERSHIP_MISMATCH"));
            foreach (var item in request.Projects)
            {
                var highlightIds = (item.Highlights ?? []).Select(x => x.Id).Distinct().ToList();
                if (highlightIds.Count > 0 && await db.ProjectHighlights.CountAsync(x => x.ProjectId == item.Id && highlightIds.Contains(x.Id), ct) != highlightIds.Count)
                    return Results.Problem(statusCode: 400, title: "A selected highlight does not belong to its project", extensions: Problem("HIGHLIGHT_OWNERSHIP_MISMATCH"));
            }
            var existingProjects = await db.ProfileProjects.Include(x => x.Translations).Where(x => x.ProfileId == id).ToListAsync(ct);
            var requestedProjectIds = request.Projects.Select(x => x.Id).ToHashSet();
            db.ProfileProjects.RemoveRange(existingProjects.Where(x => !requestedProjectIds.Contains(x.ProjectId)));
            foreach (var item in request.Projects)
            {
                var selection = existingProjects.SingleOrDefault(x => x.ProjectId == item.Id);
                if (selection is null)
                {
                    selection = new ProfileProject { ProfileId = id, ProjectId = item.Id };
                    db.ProfileProjects.Add(selection);
                }
                selection.WebEnabled = item.WebEnabled;
                selection.PdfEnabled = item.PdfEnabled;
                selection.WebOrder = item.WebOrder;
                selection.PdfOrder = item.PdfOrder;
                UpsertProfileProjectTranslation(db, selection, Locale.En, item.SummaryOverrideEn);
                UpsertProfileProjectTranslation(db, selection, Locale.Ar, item.SummaryOverrideAr);
                var existingHighlights = await db.ProfileProjectHighlights.Where(x => x.ProfileProjectId == selection.Id).ToListAsync(ct);
                var requestedHighlights = item.Highlights ?? [];
                var requestedHighlightIds = requestedHighlights.Select(x => x.Id).ToHashSet();
                db.ProfileProjectHighlights.RemoveRange(existingHighlights.Where(x => !requestedHighlightIds.Contains(x.HighlightId)));
                foreach (var highlight in requestedHighlights)
                {
                    var selected = existingHighlights.SingleOrDefault(x => x.HighlightId == highlight.Id);
                    if (selected is null)
                    {
                        selected = new ProfileProjectHighlight { ProfileProjectId = selection.Id, HighlightId = highlight.Id };
                        db.ProfileProjectHighlights.Add(selected);
                    }
                    selected.WebEnabled = highlight.WebEnabled; selected.PdfEnabled = highlight.PdfEnabled; selected.SortOrder = highlight.SortOrder;
                }
            }
            var existingSkills = await db.ProfileSkills.Where(x => x.ProfileId == id).ToListAsync(ct);
            var requestedSkillIds = request.Skills.Select(x => x.Id).ToHashSet();
            db.ProfileSkills.RemoveRange(existingSkills.Where(x => !requestedSkillIds.Contains(x.SkillId)));
            foreach (var item in request.Skills)
            {
                var selection = existingSkills.SingleOrDefault(x => x.SkillId == item.Id);
                if (selection is null) { selection = new ProfileSkill { ProfileId = id, SkillId = item.Id }; db.ProfileSkills.Add(selection); }
                selection.WebEnabled = item.WebEnabled; selection.PdfEnabled = item.PdfEnabled; selection.WebOrder = item.WebOrder; selection.PdfOrder = item.PdfOrder;
            }
            await Audit(db, http.User.AdminId(), "profile.selection_updated", "ResumeProfile", id, ct); return Results.NoContent();
        });
        admin.MapPut("/profiles/{id:guid}/selection/supporting", async (Guid id, SupportingSelectionRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var profile = await db.ResumeProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (profile is null) return Results.NotFound();
            if (!await OwnsAll(db.Experiences, profile.PersonId, request.Experiences.Select(x => x.Id), ct) || !await OwnsAll(db.Educations, profile.PersonId, request.Educations.Select(x => x.Id), ct) || !await OwnsAll(db.Certifications, profile.PersonId, request.Certifications.Select(x => x.Id), ct) || !await OwnsAll(db.SpokenLanguages, profile.PersonId, request.Languages.Select(x => x.Id), ct)) return Results.Problem(statusCode: 400, title: "Selected content ownership mismatch", extensions: Problem("OWNERSHIP_MISMATCH"));
            foreach (var item in request.Experiences)
            {
                var highlightIds = (item.Highlights ?? []).Select(x => x.Id).Distinct().ToList();
                if (highlightIds.Count > 0 && await db.ExperienceHighlights.CountAsync(x => x.ExperienceId == item.Id && highlightIds.Contains(x.Id), ct) != highlightIds.Count)
                    return Results.Problem(statusCode: 400, title: "A selected highlight does not belong to its experience", extensions: Problem("HIGHLIGHT_OWNERSHIP_MISMATCH"));
            }
            var existingExperiences = await db.ProfileExperiences.Where(x => x.ProfileId == id).ToListAsync(ct);
            var requestedExperienceIds = request.Experiences.Select(x => x.Id).ToHashSet();
            db.ProfileExperiences.RemoveRange(existingExperiences.Where(x => !requestedExperienceIds.Contains(x.ExperienceId)));
            foreach (var item in request.Experiences)
            {
                var selection = existingExperiences.SingleOrDefault(x => x.ExperienceId == item.Id);
                if (selection is null)
                {
                    selection = new ProfileExperience { ProfileId = id, ExperienceId = item.Id };
                    db.ProfileExperiences.Add(selection);
                }
                selection.WebEnabled = item.WebEnabled; selection.PdfEnabled = item.PdfEnabled; selection.WebOrder = item.WebOrder; selection.PdfOrder = item.PdfOrder;
                var existingHighlights = await db.ProfileExperienceHighlights.Where(x => x.ProfileExperienceId == selection.Id).ToListAsync(ct);
                var requestedHighlights = item.Highlights ?? [];
                var requestedHighlightIds = requestedHighlights.Select(x => x.Id).ToHashSet();
                db.ProfileExperienceHighlights.RemoveRange(existingHighlights.Where(x => !requestedHighlightIds.Contains(x.HighlightId)));
                foreach (var highlight in requestedHighlights)
                {
                    var selected = existingHighlights.SingleOrDefault(x => x.HighlightId == highlight.Id);
                    if (selected is null)
                    {
                        selected = new ProfileExperienceHighlight { ProfileExperienceId = selection.Id, HighlightId = highlight.Id };
                        db.ProfileExperienceHighlights.Add(selected);
                    }
                    selected.WebEnabled = highlight.WebEnabled; selected.PdfEnabled = highlight.PdfEnabled; selected.SortOrder = highlight.SortOrder;
                }
            }
            var existingEducations = await db.ProfileEducations.Where(x => x.ProfileId == id).ToListAsync(ct);
            var requestedEducationIds = request.Educations.Select(x => x.Id).ToHashSet();
            db.ProfileEducations.RemoveRange(existingEducations.Where(x => !requestedEducationIds.Contains(x.EducationId)));
            foreach (var item in request.Educations) { var selection = existingEducations.SingleOrDefault(x => x.EducationId == item.Id); if (selection is null) { selection = new ProfileEducation { ProfileId = id, EducationId = item.Id }; db.ProfileEducations.Add(selection); } selection.WebEnabled = item.WebEnabled; selection.PdfEnabled = item.PdfEnabled; selection.WebOrder = item.WebOrder; selection.PdfOrder = item.PdfOrder; }
            var existingCertifications = await db.ProfileCertifications.Where(x => x.ProfileId == id).ToListAsync(ct);
            var requestedCertificationIds = request.Certifications.Select(x => x.Id).ToHashSet();
            db.ProfileCertifications.RemoveRange(existingCertifications.Where(x => !requestedCertificationIds.Contains(x.CertificationId)));
            foreach (var item in request.Certifications) { var selection = existingCertifications.SingleOrDefault(x => x.CertificationId == item.Id); if (selection is null) { selection = new ProfileCertification { ProfileId = id, CertificationId = item.Id }; db.ProfileCertifications.Add(selection); } selection.WebEnabled = item.WebEnabled; selection.PdfEnabled = item.PdfEnabled; selection.WebOrder = item.WebOrder; selection.PdfOrder = item.PdfOrder; }
            var existingLanguages = await db.ProfileLanguages.Where(x => x.ProfileId == id).ToListAsync(ct);
            var requestedLanguageIds = request.Languages.Select(x => x.Id).ToHashSet();
            db.ProfileLanguages.RemoveRange(existingLanguages.Where(x => !requestedLanguageIds.Contains(x.SpokenLanguageId)));
            foreach (var item in request.Languages) { var selection = existingLanguages.SingleOrDefault(x => x.SpokenLanguageId == item.Id); if (selection is null) { selection = new ProfileLanguage { ProfileId = id, SpokenLanguageId = item.Id }; db.ProfileLanguages.Add(selection); } selection.WebEnabled = item.WebEnabled; selection.PdfEnabled = item.PdfEnabled; selection.WebOrder = item.WebOrder; selection.PdfOrder = item.PdfOrder; }
            await Audit(db, http.User.AdminId(), "profile.supporting_selection_updated", "ResumeProfile", id, ct); return Results.NoContent();
        });
        admin.MapPut("/profiles/{id:guid}/sections", async (Guid id, IReadOnlyList<ProfileSectionRequest> request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.ResumeProfiles.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound();
            var allowed = new HashSet<string>(["summary", "skills", "projects", "experience", "education", "certifications", "languages"]); if (request.Any(x => !allowed.Contains(x.SectionKey)) || request.Select(x => x.SectionKey).Distinct().Count() != request.Count) return Validation("INVALID_SECTION", "sections", "Section keys must be supported and unique.");
            db.ProfileSections.RemoveRange(db.ProfileSections.Where(x => x.ProfileId == id)); db.ProfileSections.AddRange(request.Select(x => new ProfileSection { ProfileId = id, SectionKey = x.SectionKey, WebEnabled = x.WebEnabled, PdfEnabled = x.PdfEnabled, WebOrder = x.WebOrder, PdfOrder = x.PdfOrder }));
            await Audit(db, http.User.AdminId(), "profile.sections_updated", "ResumeProfile", id, ct); return Results.NoContent();
        });
        admin.MapPut("/profiles/{id:guid}/pdf/{locale}", async (Guid id, string locale, PdfSettingRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!ParseLocale(locale, out var parsed)) return Validation("INVALID_LOCALE", "locale", "Use ar or en."); if (request.FontSize is < 9.5m or > 12m || request.MarginMm is < 10m or > 25m || request.TargetPages is < 1 or > 2) return Validation("INVALID_PDF_SETTINGS", "pdf", "Use font 9.5–12, margins 10–25mm, and one or two target pages.");
            if (!await db.ResumeProfiles.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound();
            var setting = await db.ProfilePdfSettings.SingleOrDefaultAsync(x => x.ProfileId == id && x.Locale == parsed, ct); if (setting is null) { setting = new ProfilePdfSetting { ProfileId = id, Locale = parsed }; db.Add(setting); } else if (setting.Version != request.Version) return Conflict();
            setting.TemplateKey = request.TemplateKey.Trim(); setting.PaperSize = request.PaperSize; setting.FontSize = request.FontSize; setting.MarginMm = request.MarginMm; setting.TargetPages = request.TargetPages;
            await Audit(db, http.User.AdminId(), "profile.pdf_settings_updated", "ResumeProfile", id, ct); return Results.Ok(setting);
        });
        admin.MapGet("/profiles/{id:guid}/contact", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.ResumeProfiles.AsNoTracking().AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound();
            var setting = await db.ProfileContactSettings.AsNoTracking().SingleOrDefaultAsync(x => x.ProfileId == id, ct);
            return Results.Ok(setting ?? new ProfileContactSetting { ProfileId = id });
        });
        admin.MapPut("/profiles/{id:guid}/contact", async (Guid id, ProfileContactRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.ResumeProfiles.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound();
            var setting = await db.ProfileContactSettings.SingleOrDefaultAsync(x => x.ProfileId == id, ct);
            if (setting is null) { setting = new ProfileContactSetting { ProfileId = id }; db.Add(setting); }
            setting.WebEmail = request.WebEmail; setting.PdfEmail = request.PdfEmail; setting.WebPhone = request.WebPhone; setting.PdfPhone = request.PdfPhone; setting.ShowLocation = request.ShowLocation; setting.ShowPhotoWeb = request.ShowPhotoWeb;
            await Audit(db, http.User.AdminId(), "profile.contact_updated", "ResumeProfile", id, ct); return Results.Ok(setting);
        });
        admin.MapGet("/profiles/{id:guid}/preview", async (Guid id, string locale, string target, HttpContext http, SnapshotService snapshots, CancellationToken ct) =>
        {
            if (!ParseLocale(locale, out var parsed)) return Validation("INVALID_LOCALE", "locale", "Use ar or en.");
            var document = await snapshots.BuildResumeAsync(http.User.PersonId(), id, parsed, string.Equals(target, "pdf", StringComparison.OrdinalIgnoreCase), ct);
            return document is null ? Results.NotFound() : Results.Ok(document);
        });
        admin.MapGet("/profiles/{id:guid}/pdf", async (Guid id, string locale, HttpContext http, SnapshotService snapshots, IResumePdfRenderer renderer, CancellationToken ct) =>
        {
            if (!ParseLocale(locale, out var parsed)) return Validation("INVALID_LOCALE", "locale", "Use ar or en.");
            var document = await snapshots.BuildResumeAsync(http.User.PersonId(), id, parsed, true, ct);
            if (document is null) return Results.NotFound();
            var pdf = renderer.Render(document);
            var suffix = parsed == Locale.Ar ? "ar" : "en";
            return Results.File(pdf, "application/pdf", $"resume-{suffix}.pdf", enableRangeProcessing: false);
        });
        admin.MapPost("/profiles/{id:guid}/validate", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var profile = await db.ResumeProfiles.AsNoTracking().Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (profile is null) return Results.NotFound();
            var errors = new List<ValidationIssue>();
            foreach (var locale in new[] { Locale.En, Locale.Ar }) if (profile.Translations.All(x => x.Locale != locale || string.IsNullOrWhiteSpace(x.Headline) || string.IsNullOrWhiteSpace(x.Summary))) errors.Add(new("TRANSLATION_REQUIRED", $"translations.{locale.ToString().ToLowerInvariant()}", "Headline and summary are required."));
            return Results.Ok(new PublicationValidation(errors.Count == 0, errors, []));
        });
    }

    private static void MapSites(RouteGroupBuilder admin)
    {
        admin.MapGet("/sites", async (HttpContext http, ResumeDbContext db, CancellationToken ct) => Results.Ok(await db.Sites.AsNoTracking().Include(x => x.Translations).Include(x => x.Locales).Include(x => x.Profiles).Where(x => x.PersonId == http.User.PersonId() && x.ArchivedAt == null).ToListAsync(ct)));
        admin.MapPost("/sites", async (SiteRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!DomainValidation.IsSlug(request.Slug)) return Validation("INVALID_SLUG", "slug", "Use lowercase Latin letters, numbers and hyphens.");
            if (request.BaseUrl is not null && !IsHttps(request.BaseUrl)) return Validation("HTTPS_REQUIRED", "baseUrl", "Production base URL must use HTTPS.");
            var site = new Site { PersonId = http.User.PersonId(), Name = request.Name.Trim(), Slug = request.Slug, BaseUrl = Clean(request.BaseUrl), DefaultLocale = request.DefaultLocale, IsPrimaryIdentitySite = request.IsPrimaryIdentitySite };
            site.Locales = request.Locales.Distinct().Select(x => new SiteLocale { SiteId = site.Id, Locale = x }).ToList();
            db.Add(site); await Audit(db, http.User.AdminId(), "site.created", "Site", site.Id, ct); return Results.Created($"/api/v1/admin/sites/{site.Id}", site);
        });
        admin.MapPut("/sites/{id:guid}", async (Guid id, SiteUpdateRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!DomainValidation.IsSlug(request.Slug)) return Validation("INVALID_SLUG", "slug", "Use lowercase Latin letters, numbers and hyphens.");
            if (!IsHttpsOrEmpty(request.BaseUrl)) return Validation("HTTPS_REQUIRED", "baseUrl", "Production base URL must use HTTPS.");
            if (!string.IsNullOrWhiteSpace(request.DeploymentTargetKey) && !IsDeploymentTarget(request.DeploymentTargetKey)) return Validation("INVALID_DEPLOYMENT_TARGET", "deploymentTargetKey", "Use at most 58 lowercase Latin letters, numbers and hyphens.");
            if (!string.IsNullOrWhiteSpace(request.SearchVerificationToken) && !IsSearchVerificationToken(request.SearchVerificationToken)) return Validation("INVALID_SEARCH_VERIFICATION", "searchVerificationToken", "Enter only the Google verification token, not an HTML tag.");
            var site = await db.Sites.Include(x => x.Locales).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (site is null) return Results.NotFound(); if (site.Version != request.Version) return Conflict();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            site.Name = request.Name.Trim(); site.Slug = request.Slug; site.BaseUrl = Clean(request.BaseUrl); site.DefaultLocale = request.DefaultLocale; site.IsPrimaryIdentitySite = request.IsPrimaryIdentitySite; site.DeploymentTargetKey = Clean(request.DeploymentTargetKey); site.SearchVerificationToken = Clean(request.SearchVerificationToken);
            var wantedLocales = request.Locales.Distinct().ToHashSet(); db.SiteLocales.RemoveRange(site.Locales.Where(x => !wantedLocales.Contains(x.Locale))); db.SiteLocales.AddRange(wantedLocales.Where(locale => site.Locales.All(x => x.Locale != locale)).Select(locale => new SiteLocale { SiteId = id, Locale = locale }));
            await db.SiteTranslations.Where(x => x.SiteId == id).ExecuteDeleteAsync(ct);
            var translations = new[]
            {
                new SiteTranslation { SiteId = id, Locale = Locale.En, Title = request.En.Title.Trim(), Description = request.En.Description.Trim() },
                new SiteTranslation { SiteId = id, Locale = Locale.Ar, Title = request.Ar.Title.Trim(), Description = request.Ar.Description.Trim() }
            };
            site.Translations = translations.ToList(); db.SiteTranslations.AddRange(translations);
            await Audit(db, http.User.AdminId(), "site.updated", "Site", id, ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(site);
        });
        admin.MapDelete("/sites/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var site = await db.Sites.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (site is null) return Results.NotFound(); site.ArchivedAt = DateTimeOffset.UtcNow; await Audit(db, http.User.AdminId(), "site.archived", "Site", id, ct); return Results.NoContent();
        });
        admin.MapPut("/sites/{id:guid}/profiles", async (Guid id, SiteProfilesRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var site = await db.Sites.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (site is null) return Results.NotFound();
            if (request.Profiles.Count == 0) return Validation("SITE_PROFILE_REQUIRED", "profiles", "Attach at least one profile to the site.");
            if (request.Profiles.Count(x => x.IsDefault) != 1) return Validation("ONE_DEFAULT_REQUIRED", "profiles", "Exactly one default profile is required.");
            if (request.Profiles.Select(x => x.ProfileId).Distinct().Count() != request.Profiles.Count) return Validation("DUPLICATE_PROFILE", "profiles", "A profile can be attached only once.");
            if (request.Profiles.Any(x => !DomainValidation.IsSlug(x.PathSlug))) return Validation("INVALID_SLUG", "profiles.pathSlug", "Use lowercase Latin letters, numbers and hyphens.");
            if (request.Profiles.Select(x => x.PathSlug).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Profiles.Count) return Validation("DUPLICATE_PATH", "profiles.pathSlug", "Each profile path must be unique within the site.");
            var profileIds = request.Profiles.Select(x => x.ProfileId).ToList(); if (await db.ResumeProfiles.CountAsync(x => x.PersonId == site.PersonId && profileIds.Contains(x.Id), ct) != profileIds.Count) return Results.Problem(statusCode: 400, title: "Profile ownership mismatch", extensions: Problem("OWNERSHIP_MISMATCH"));
            db.SiteProfiles.RemoveRange(db.SiteProfiles.Where(x => x.SiteId == id)); db.SiteProfiles.AddRange(request.Profiles.Select(x => new SiteProfile { SiteId = id, ProfileId = x.ProfileId, PathSlug = x.PathSlug, IsDefault = x.IsDefault, Indexable = x.Indexable, IsListed = x.IsListed, SortOrder = x.SortOrder }));
            await Audit(db, http.User.AdminId(), "site.profiles_updated", "Site", id, ct); return Results.NoContent();
        });
        admin.MapGet("/sites/{id:guid}/seo", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.Sites.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound();
            return Results.Ok(new { pages = await db.SeoPageSettings.AsNoTracking().Where(x => x.SiteId == id).ToListAsync(ct), redirects = await db.SiteRedirects.AsNoTracking().Where(x => x.SiteId == id).ToListAsync(ct) });
        });
        admin.MapPost("/sites/{id:guid}/redirects", async (Guid id, RedirectRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.Sites.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound();
            if (!SafePath(request.SourcePath) || !SafePath(request.TargetPath) || request.SourcePath == request.TargetPath || request.StatusCode is not (301 or 308)) return Validation("INVALID_REDIRECT", "sourcePath", "Use distinct internal paths and status 301 or 308.");
            var redirect = new SiteRedirect { SiteId = id, SourcePath = request.SourcePath, TargetPath = request.TargetPath, StatusCode = request.StatusCode }; db.Add(redirect); await Audit(db, http.User.AdminId(), "redirect.created", "SiteRedirect", redirect.Id, ct); return Results.Created($"/api/v1/admin/sites/{id}/redirects/{redirect.Id}", redirect);
        });
        admin.MapPut("/sites/{id:guid}/redirects/{redirectId:guid}", async (Guid id, Guid redirectId, RedirectUpdateRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.Sites.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound(); if (!SafePath(request.SourcePath) || !SafePath(request.TargetPath) || request.SourcePath == request.TargetPath || request.StatusCode is not (301 or 308)) return Validation("INVALID_REDIRECT", "sourcePath", "Use distinct internal paths and status 301 or 308.");
            var redirect = await db.SiteRedirects.SingleOrDefaultAsync(x => x.Id == redirectId && x.SiteId == id, ct); if (redirect is null) return Results.NotFound(); if (redirect.Version != request.Version) return Conflict(); redirect.SourcePath = request.SourcePath; redirect.TargetPath = request.TargetPath; redirect.StatusCode = request.StatusCode; redirect.IsActive = request.IsActive;
            await Audit(db, http.User.AdminId(), "redirect.updated", "SiteRedirect", redirectId, ct); return Results.Ok(redirect);
        });
        admin.MapDelete("/sites/{id:guid}/redirects/{redirectId:guid}", async (Guid id, Guid redirectId, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.Sites.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound(); var redirect = await db.SiteRedirects.SingleOrDefaultAsync(x => x.Id == redirectId && x.SiteId == id, ct); if (redirect is null) return Results.NotFound(); db.Remove(redirect); await Audit(db, http.User.AdminId(), "redirect.deleted", "SiteRedirect", redirectId, ct); return Results.NoContent();
        });
        admin.MapPut("/sites/{id:guid}/seo/pages", async (Guid id, SeoPageRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var personId = await db.Sites.Where(x => x.Id == id && x.PersonId == http.User.PersonId()).Select(x => (Guid?)x.PersonId).SingleOrDefaultAsync(ct);
            if (personId is null) return Results.NotFound(); if (!IsHttpsOrEmpty(request.CanonicalOverrideUrl)) return Validation("HTTPS_REQUIRED", "canonicalOverrideUrl", "Canonical overrides must use HTTPS.");
            if (request.PageKind is not ("Home" or "Profile" or "Project")) return Validation("INVALID_PAGE_KIND", "pageKind", "Use Home, Profile, or Project.");
            if (request.PageKind == "Home" && (request.ProfileId is not null || request.ProjectId is not null) || request.PageKind == "Profile" && (request.ProfileId is null || request.ProjectId is not null) || request.PageKind == "Project" && (request.ProfileId is null || request.ProjectId is null)) return Validation("INVALID_SEO_TARGET", "pageKind", "The selected page kind requires a matching profile/project target.");
            if (request.ProfileId is not null && !await db.SiteProfiles.AnyAsync(x => x.SiteId == id && x.ProfileId == request.ProfileId, ct)) return Validation("PROFILE_NOT_ON_SITE", "profileId", "The profile must be attached to this site.");
            if (request.ProjectId is not null && !await db.ProfileProjects.AnyAsync(x => x.ProfileId == request.ProfileId && x.ProjectId == request.ProjectId && x.WebEnabled, ct)) return Validation("PROJECT_NOT_ON_PROFILE", "projectId", "The project must be selected for this profile's website.");
            if (request.OgAssetId is not null && !await db.MediaAssets.AnyAsync(x => x.Id == request.OgAssetId && x.PersonId == personId && x.ArchivedAt == null && x.Status == "Ready" && x.DeliveryUrl != null && x.Width != null && x.Height != null, ct)) return Validation("OG_MEDIA_INVALID", "ogAssetId", "Choose a ready image owned by this profile owner.");
            SeoPageSetting? page = null; if (request.Id is not null) page = await db.SeoPageSettings.SingleOrDefaultAsync(x => x.Id == request.Id && x.SiteId == id, ct); if (request.Id is not null && page is null) return Results.NotFound(); if (page is not null && page.Version != request.Version) return Conflict();
            if (page is null && await db.SeoPageSettings.AnyAsync(x => x.SiteId == id && x.Locale == request.Locale && x.PageKind == request.PageKind && x.ProfileId == request.ProfileId && x.ProjectId == request.ProjectId, ct)) return Results.Problem(statusCode: 409, title: "SEO settings already exist for this page", extensions: Problem("SEO_PAGE_EXISTS"));
            page ??= new SeoPageSetting { SiteId = id }; page.Locale = request.Locale; page.PageKind = request.PageKind.Trim(); page.ProfileId = request.ProfileId; page.ProjectId = request.ProjectId; page.TitleOverride = Clean(request.TitleOverride); page.DescriptionOverride = Clean(request.DescriptionOverride); page.CanonicalOverrideUrl = Clean(request.CanonicalOverrideUrl); page.OgAssetId = request.OgAssetId; page.IndexOverride = request.IndexOverride; if (db.Entry(page).State == EntityState.Detached) db.Add(page);
            await Audit(db, http.User.AdminId(), "seo_page.upserted", "SeoPageSetting", page.Id, ct); return Results.Ok(page);
        });
    }

    private static void MapSupportingContent(RouteGroupBuilder admin)
    {
        admin.MapGet("/experiences", async (HttpContext http, ResumeDbContext db, CancellationToken ct) => Results.Ok(await db.Experiences.AsNoTracking().Include(x => x.Translations).Include(x => x.Highlights).ThenInclude(x => x.Translations).Where(x => x.PersonId == http.User.PersonId() && x.ArchivedAt == null).OrderByDescending(x => x.StartDate).ToListAsync(ct)));
        admin.MapPost("/experiences", async (ExperienceRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var error = ValidateExperience(request); if (error is not null) return error;
            var item = new Experience { PersonId = http.User.PersonId() }; ApplyExperience(item, request); db.Add(item);
            await Audit(db, http.User.AdminId(), "experience.created", "Experience", item.Id, ct); return Results.Created($"/api/v1/admin/experiences/{item.Id}", item);
        });
        admin.MapPut("/experiences/{id:guid}", async (Guid id, ExperienceRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var error = ValidateExperience(request); if (error is not null) return error;
            var item = await db.Experiences.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (item is null) return Results.NotFound(); if (item.Version != request.Version) return Conflict(); ApplyExperience(item, request);
            await Audit(db, http.User.AdminId(), "experience.updated", "Experience", item.Id, ct); return Results.Ok(item);
        });
        admin.MapDelete("/experiences/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) => await ArchiveOwned(db.Experiences, id, http, db, "experience.archived", ct));
        admin.MapPost("/experiences/{id:guid}/highlights", async (Guid id, HighlightRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.Experiences.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId() && x.ArchivedAt == null, ct)) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(request.TextEn) || string.IsNullOrWhiteSpace(request.TextAr)) return Validation("TRANSLATION_REQUIRED", "text", "Arabic and English highlight text are required.");
            var highlight = new ExperienceHighlight { ExperienceId = id, SortOrder = request.SortOrder };
            highlight.Translations.AddRange([new() { HighlightId = highlight.Id, Locale = Locale.En, Text = request.TextEn.Trim() }, new() { HighlightId = highlight.Id, Locale = Locale.Ar, Text = request.TextAr.Trim() }]);
            db.Add(highlight); await Audit(db, http.User.AdminId(), "experience_highlight.created", "ExperienceHighlight", highlight.Id, ct); return Results.Created($"/api/v1/admin/experiences/{id}/highlights/{highlight.Id}", highlight);
        });
        admin.MapPut("/experiences/{id:guid}/highlights/{highlightId:guid}", async (Guid id, Guid highlightId, HighlightRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.TextEn) || string.IsNullOrWhiteSpace(request.TextAr)) return Validation("TRANSLATION_REQUIRED", "text", "Arabic and English highlight text are required.");
            var highlight = await db.ExperienceHighlights.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == highlightId && x.ExperienceId == id && db.Experiences.Any(e => e.Id == id && e.PersonId == http.User.PersonId()), ct);
            if (highlight is null) return Results.NotFound();
            if (highlight.Version != request.Version) return Conflict();
            highlight.SortOrder = request.SortOrder;
            UpsertExperienceHighlightTranslation(highlight, Locale.En, request.TextEn);
            UpsertExperienceHighlightTranslation(highlight, Locale.Ar, request.TextAr);
            await Audit(db, http.User.AdminId(), "experience_highlight.updated", "ExperienceHighlight", highlight.Id, ct); return Results.Ok(highlight);
        });
        admin.MapDelete("/experiences/{id:guid}/highlights/{highlightId:guid}", async (Guid id, Guid highlightId, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var highlight = await db.ExperienceHighlights.SingleOrDefaultAsync(x => x.Id == highlightId && x.ExperienceId == id && db.Experiences.Any(e => e.Id == id && e.PersonId == http.User.PersonId()), ct);
            if (highlight is null) return Results.NotFound();
            db.ProfileExperienceHighlights.RemoveRange(db.ProfileExperienceHighlights.Where(x => x.HighlightId == highlightId));
            db.ExperienceHighlights.Remove(highlight);
            await Audit(db, http.User.AdminId(), "experience_highlight.deleted", "ExperienceHighlight", highlight.Id, ct); return Results.NoContent();
        });
        admin.MapGet("/educations", async (HttpContext http, ResumeDbContext db, CancellationToken ct) => Results.Ok(await db.Educations.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == http.User.PersonId() && x.ArchivedAt == null).ToListAsync(ct)));
        admin.MapPost("/educations", async (EducationRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var error = ValidateEducation(request); if (error is not null) return error;
            var item = new Education { PersonId = http.User.PersonId() }; ApplyEducation(item, request); db.Add(item);
            await Audit(db, http.User.AdminId(), "education.created", "Education", item.Id, ct); return Results.Created($"/api/v1/admin/educations/{item.Id}", item);
        });
        admin.MapPut("/educations/{id:guid}", async (Guid id, EducationRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var error = ValidateEducation(request); if (error is not null) return error;
            var item = await db.Educations.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (item is null) return Results.NotFound(); if (item.Version != request.Version) return Conflict(); ApplyEducation(item, request);
            await Audit(db, http.User.AdminId(), "education.updated", "Education", item.Id, ct); return Results.Ok(item);
        });
        admin.MapDelete("/educations/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) => await ArchiveOwned(db.Educations, id, http, db, "education.archived", ct));
        admin.MapGet("/certifications", async (HttpContext http, ResumeDbContext db, CancellationToken ct) => Results.Ok(await db.Certifications.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == http.User.PersonId() && x.ArchivedAt == null).ToListAsync(ct)));
        admin.MapPost("/certifications", async (CertificationRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var error = ValidateCertification(request); if (error is not null) return error;
            var item = new Certification { PersonId = http.User.PersonId() }; ApplyCertification(item, request); db.Add(item);
            await Audit(db, http.User.AdminId(), "certification.created", "Certification", item.Id, ct); return Results.Created($"/api/v1/admin/certifications/{item.Id}", item);
        });
        admin.MapPut("/certifications/{id:guid}", async (Guid id, CertificationRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var error = ValidateCertification(request); if (error is not null) return error;
            var item = await db.Certifications.Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (item is null) return Results.NotFound(); if (item.Version != request.Version) return Conflict(); ApplyCertification(item, request);
            await Audit(db, http.User.AdminId(), "certification.updated", "Certification", item.Id, ct); return Results.Ok(item);
        });
        admin.MapDelete("/certifications/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) => await ArchiveOwned(db.Certifications, id, http, db, "certification.archived", ct));
        admin.MapGet("/languages", async (HttpContext http, ResumeDbContext db, CancellationToken ct) => Results.Ok(await db.SpokenLanguages.AsNoTracking().Where(x => x.PersonId == http.User.PersonId()).ToListAsync(ct)));
        admin.MapPost("/languages", async (LanguageRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) => { var error = ValidateLanguage(request); if (error is not null) return error; var fallback = Clean(request.Proficiency) ?? Clean(request.ProficiencyEn) ?? Clean(request.ProficiencyAr) ?? ""; var item = new SpokenLanguage { PersonId = http.User.PersonId(), LanguageCode = request.LanguageCode.Trim().ToLowerInvariant(), Proficiency = fallback, ProficiencyEn = Clean(request.ProficiencyEn) ?? fallback, ProficiencyAr = Clean(request.ProficiencyAr) ?? fallback }; db.Add(item); await Audit(db, http.User.AdminId(), "language.created", "SpokenLanguage", item.Id, ct); return Results.Created($"/api/v1/admin/languages/{item.Id}", item); });
        admin.MapPut("/languages/{id:guid}", async (Guid id, LanguageRequest request, HttpContext http, ResumeDbContext db, CancellationToken ct) => { var error = ValidateLanguage(request); if (error is not null) return error; var item = await db.SpokenLanguages.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (item is null) return Results.NotFound(); if (item.Version != request.Version) return Conflict(); var fallback = Clean(request.Proficiency) ?? Clean(request.ProficiencyEn) ?? Clean(request.ProficiencyAr) ?? ""; item.LanguageCode = request.LanguageCode.Trim().ToLowerInvariant(); item.Proficiency = fallback; item.ProficiencyEn = Clean(request.ProficiencyEn) ?? fallback; item.ProficiencyAr = Clean(request.ProficiencyAr) ?? fallback; await Audit(db, http.User.AdminId(), "language.updated", "SpokenLanguage", item.Id, ct); return Results.Ok(item); });
        admin.MapDelete("/languages/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) => { var item = await db.SpokenLanguages.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (item is null) return Results.NotFound(); db.Remove(item); await Audit(db, http.User.AdminId(), "language.deleted", "SpokenLanguage", id, ct); return Results.NoContent(); });
    }

    private static void MapPublishing(RouteGroupBuilder admin)
    {
        admin.MapPost("/profiles/{id:guid}/pdf-exports", async (Guid id, PdfExportRequest request, HttpContext http, ResumeDbContext db, SnapshotService snapshots, PublicationDispatcher dispatcher, CancellationToken ct) =>
        {
            var profile = await db.ResumeProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId() && x.ArchivedAt == null, ct); if (profile is null) return Results.NotFound();
            if (!dispatcher.IsConfigured(PublicationPurpose.PrivatePdfExport)) return Results.Problem(statusCode: 503, title: "Private PDF export is not configured", extensions: Problem("PRIVATE_EXPORT_NOT_CONFIGURED"));
            var locales = request.Locales.Distinct().ToList();
            if (locales.Count is < 1 or > 2) return Validation("INVALID_LOCALES", "locales", "Select Arabic, English, or both.");
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100) return Validation("INVALID_IDEMPOTENCY_KEY", "idempotencyKey", "Provide a non-empty key up to 100 characters.");
            var existing = await db.Publications.AsNoTracking().Include(x => x.Attempts).SingleOrDefaultAsync(x => x.PersonId == profile.PersonId && x.IdempotencyKey == request.IdempotencyKey, ct);
            if (existing is not null) { var currentAttempt = existing.Attempts.OrderByDescending(x => x.AttemptNumber).First(); return Results.Accepted($"/api/v1/admin/publications/{existing.Id}", new { publicationId = existing.Id, attemptId = currentAttempt.Id, state = existing.State, dispatchMode = "existing" }); }
            if (await db.Publications.AnyAsync(x => x.ProfileId == id && x.Purpose == PublicationPurpose.PrivatePdfExport && (x.State == PublicationState.Queued || x.State == PublicationState.Building || x.State == PublicationState.Validating || x.State == PublicationState.Deploying), ct)) return Results.Problem(statusCode: 409, title: "A private PDF export is already active", extensions: Problem("EXPORT_ACTIVE"));
            var documents = new List<object>();
            foreach (var locale in locales)
            {
                var document = await snapshots.BuildResumeAsync(profile.PersonId, id, locale, true, ct);
                if (document is null) return Validation("TRANSLATION_REQUIRED", $"profile.{locale.ToString().ToLowerInvariant()}", "The requested language is incomplete.");
                documents.Add(new { profileId = id, locale = locale.ToString().ToLowerInvariant(), slug = profile.Slug, isDefault = true, indexable = false, isListed = false, document, pdfDocument = document, seo = new { title = document.Headline, description = document.Summary }, projectKinds = new Dictionary<string, string>() });
            }
            var payload = new { schemaVersion = 1, purpose = "privatePdfExport", baseUrl = "https://private.invalid", lastModified = DateOnly.FromDateTime(DateTime.UtcNow), profiles = documents, redirects = Array.Empty<object>(), generatedFrom = "immutable-private-export" };
            var revision = (await db.Publications.Where(x => x.ProfileId == id).MaxAsync(x => (long?)x.Revision, ct) ?? 0) + 1;
            var publication = new Publication { PersonId = profile.PersonId, ProfileId = id, Purpose = PublicationPurpose.PrivatePdfExport, Revision = revision, Snapshot = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)), SnapshotHash = SnapshotService.Hash(payload), RequestedBy = http.User.AdminId(), IdempotencyKey = request.IdempotencyKey };
            var attempt = new PublicationAttempt { PublicationId = publication.Id, AttemptNumber = 1, LeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(45) }; publication.Attempts.Add(attempt); db.Add(publication);
            await Audit(db, http.User.AdminId(), "pdf_export.queued", "Publication", publication.Id, ct);
            var dispatch = await dispatcher.DispatchAsync(publication.Id, attempt.Id, ct, PublicationPurpose.PrivatePdfExport);
            if (dispatch.Configured && !dispatch.Succeeded) { await ApplyPublicationResult(publication, attempt, new AttemptResult(PublicationState.Failed, "WORKFLOW_DISPATCH_FAILED", dispatch.Error), db, ct); await Audit(db, http.User.AdminId(), "pdf_export.dispatch_failed", "Publication", publication.Id, ct); return Results.Problem(statusCode: 502, title: "Private export workflow dispatch failed", extensions: new Dictionary<string, object?> { ["code"] = "WORKFLOW_DISPATCH_FAILED", ["publicationId"] = publication.Id, ["attemptId"] = attempt.Id }); }
            return Results.Accepted($"/api/v1/admin/publications/{publication.Id}", new { publicationId = publication.Id, attemptId = attempt.Id, state = publication.State, dispatchMode = dispatch.Configured ? "github-private" : "unconfigured" });
        });
        admin.MapGet("/profiles/{id:guid}/pdf-exports", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.ResumeProfiles.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound();
            return Results.Ok(await db.Publications.AsNoTracking().Where(x => x.ProfileId == id && x.Purpose == PublicationPurpose.PrivatePdfExport).OrderByDescending(x => x.Revision).Take(20).Select(x => new { x.Id, x.Revision, x.State, x.RequestedAt, x.CompletedAt, artifacts = x.Artifacts.Where(a => a.Kind == ArtifactKind.Pdf).Select(a => new { a.Id, a.Locale, a.SizeBytes, a.PageCount, a.CreatedAt }), errorSummary = x.Attempts.OrderByDescending(a => a.AttemptNumber).Select(a => a.ErrorSummary).FirstOrDefault() }).ToListAsync(ct));
        });
        admin.MapGet("/publications/{id:guid}/artifacts/{artifactId:guid}/download", async (Guid id, Guid artifactId, HttpContext http, ResumeDbContext db, PrivateArtifactDownloader downloader, CancellationToken ct) =>
        {
            var artifact = await db.PublicationArtifacts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == artifactId && x.PublicationId == id && x.Visibility == ArtifactVisibility.Private && x.Kind == ArtifactKind.Pdf && db.Publications.Any(p => p.Id == x.PublicationId && p.PersonId == http.User.PersonId() && p.Purpose == PublicationPurpose.PrivatePdfExport), ct);
            if (artifact is null) return Results.NotFound(); if (artifact.CreatedAt < DateTimeOffset.UtcNow.AddDays(-7)) return Results.Problem(statusCode: 410, title: "Private PDF artifact expired; generate it again", extensions: Problem("ARTIFACT_EXPIRED")); if (artifact.Locale is null) return Results.NotFound();
            var result = await downloader.DownloadAsync(artifact.PathOrArtifactId, artifact.Locale.Value, artifact.Sha256, ct);
            if (!result.Configured) return Results.Problem(statusCode: 503, title: "Private export repository is not configured", extensions: Problem("PRIVATE_EXPORT_NOT_CONFIGURED"));
            if (result.Error == "EXPIRED") return Results.Problem(statusCode: 410, title: "Private PDF artifact expired; generate it again", extensions: Problem("ARTIFACT_EXPIRED"));
            if (result.Content is null) return Results.Problem(statusCode: 502, title: result.Error ?? "Private artifact download failed", extensions: Problem("ARTIFACT_DOWNLOAD_FAILED"));
            return Results.File(result.Content, "application/pdf", $"resume-{artifact.Locale.Value.ToString().ToLowerInvariant()}.pdf", enableRangeProcessing: false);
        });
        admin.MapGet("/sites/{id:guid}/publications", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            if (!await db.Sites.AnyAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct)) return Results.NotFound();
            var items = await db.Publications.AsNoTracking().Where(x => x.SiteId == id && x.PersonId == http.User.PersonId())
                .OrderByDescending(x => x.Revision).Take(20)
                .Select(x => new { x.Id, x.Revision, x.State, x.Purpose, x.RequestedAt, x.CompletedAt, x.SnapshotHash, attemptId = x.Attempts.OrderByDescending(a => a.AttemptNumber).Select(a => a.Id).FirstOrDefault(), attemptNumber = x.Attempts.Max(a => a.AttemptNumber), errorSummary = x.Attempts.OrderByDescending(a => a.AttemptNumber).Select(a => a.ErrorSummary).FirstOrDefault() })
                .ToListAsync(ct);
            return Results.Ok(items);
        });
        admin.MapPost("/sites/{id:guid}/publish", async (Guid id, PublishRequest request, HttpContext http, ResumeDbContext db, SnapshotService snapshots, PublicationDispatcher dispatcher, CancellationToken ct) =>
        {
            var site = await db.Sites.AsNoTracking().Include(x => x.Profiles).Include(x => x.Locales).Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (site is null) return Results.NotFound();
            var existing = await db.Publications.AsNoTracking().Include(x => x.Attempts).SingleOrDefaultAsync(x => x.PersonId == site.PersonId && x.IdempotencyKey == request.IdempotencyKey, ct);
            if (existing is not null) { var currentAttempt = existing.Attempts.OrderByDescending(x => x.AttemptNumber).First(); return Results.Accepted($"/api/v1/admin/publications/{existing.Id}", new { publicationId = existing.Id, attemptId = currentAttempt.Id, statusUrl = $"/api/v1/admin/publications/{existing.Id}", state = existing.State, dispatchMode = "existing" }); }
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100) return Validation("INVALID_IDEMPOTENCY_KEY", "idempotencyKey", "Provide a non-empty key up to 100 characters.");
            if (string.IsNullOrWhiteSpace(site.DeploymentTargetKey)) return Validation("DEPLOYMENT_NOT_CONFIGURED", "deploymentTargetKey", "Configure the site's deployment target before publishing.");
            if (!IsDeploymentTarget(site.DeploymentTargetKey)) return Validation("INVALID_DEPLOYMENT_TARGET", "deploymentTargetKey", "Use at most 58 lowercase Latin letters, numbers and hyphens.");
            if (!IsHttpsOrEmpty(site.BaseUrl) || string.IsNullOrWhiteSpace(site.BaseUrl)) return Validation("BASE_URL_REQUIRED", "baseUrl", "A production HTTPS base URL is required before publishing.");
            if (site.Locales.Count == 0 || site.Profiles.Count == 0 || site.Profiles.Count(x => x.IsDefault) != 1) return Validation("SITE_CONFIGURATION_INCOMPLETE", "site", "At least one locale, one profile, and exactly one default profile are required.");
            if (await db.Publications.AnyAsync(x => x.SiteId == id && (x.State == PublicationState.Queued || x.State == PublicationState.Building || x.State == PublicationState.Validating || x.State == PublicationState.Deploying), ct)) return Results.Problem(statusCode: 409, title: "A publish is already active", extensions: Problem("PUBLISH_ACTIVE"));
            var publicProfiles = new List<object>();
            foreach (var siteProfile in site.Profiles.OrderBy(x => x.SortOrder))
            foreach (var locale in site.Locales.Select(x => x.Locale))
            {
                var document = await snapshots.BuildResumeAsync(site.PersonId, siteProfile.ProfileId, locale, false, ct);
                var pdfDocument = await snapshots.BuildResumeAsync(site.PersonId, siteProfile.ProfileId, locale, true, ct);
                if (document is null || pdfDocument is null) continue;
                var translation = await db.ResumeProfileTranslations.AsNoTracking().SingleOrDefaultAsync(x => x.ProfileId == siteProfile.ProfileId && x.Locale == locale, ct);
                var projectSlugs = document.Projects.Select(x => x.Slug).ToList();
                var projectRecords = await db.Projects.AsNoTracking().Where(x => x.PersonId == site.PersonId && projectSlugs.Contains(x.Slug)).Select(x => new { x.Id, x.Slug, x.Kind }).ToListAsync(ct);
                var projectKinds = projectRecords.ToDictionary(x => x.Slug, x => x.Kind.ToString());
                var profileSeo = await db.SeoPageSettings.AsNoTracking().SingleOrDefaultAsync(x => x.SiteId == site.Id && x.Locale == locale && x.PageKind == "Profile" && x.ProfileId == siteProfile.ProfileId && x.ProjectId == null, ct);
                var homeSeo = siteProfile.IsDefault ? await db.SeoPageSettings.AsNoTracking().SingleOrDefaultAsync(x => x.SiteId == site.Id && x.Locale == locale && x.PageKind == "Home" && x.ProfileId == null && x.ProjectId == null, ct) : null;
                var pageSeo = homeSeo ?? profileSeo;
                var siteTranslation = site.Translations.SingleOrDefault(x => x.Locale == locale);
                var fallbackTitle = siteProfile.IsDefault ? siteTranslation?.Title : translation?.SeoTitle;
                var fallbackDescription = siteProfile.IsDefault ? siteTranslation?.Description : translation?.SeoDescription;
                var indexable = siteProfile.Indexable && pageSeo?.IndexOverride is not false;
                var pageOgImage = await PublicOgImage(pageSeo, locale, site.PersonId, db, ct);
                var projectIds = projectRecords.Select(x => x.Id).ToList();
                var projectSettings = await db.SeoPageSettings.AsNoTracking().Where(x => x.SiteId == site.Id && x.Locale == locale && x.PageKind == "Project" && x.ProfileId == siteProfile.ProfileId && x.ProjectId != null && projectIds.Contains(x.ProjectId.Value)).ToListAsync(ct);
                var projectSeo = new Dictionary<string, object>();
                foreach (var projectRecord in projectRecords)
                {
                    var setting = projectSettings.SingleOrDefault(x => x.ProjectId == projectRecord.Id);
                    var ogImage = await PublicOgImage(setting, locale, site.PersonId, db, ct);
                    projectSeo[projectRecord.Slug] = new { title = setting?.TitleOverride, description = setting?.DescriptionOverride, canonical = setting?.CanonicalOverrideUrl, ogImage, indexable = indexable && setting?.IndexOverride is not false };
                }
                publicProfiles.Add(new { profileId = siteProfile.ProfileId, locale = locale.ToString().ToLowerInvariant(), slug = siteProfile.PathSlug, siteProfile.IsDefault, indexable, siteProfile.IsListed, document, pdfDocument, seo = new { title = pageSeo?.TitleOverride ?? fallbackTitle ?? $"{document.FullName} — {document.Headline}", description = pageSeo?.DescriptionOverride ?? fallbackDescription ?? document.Summary, canonical = pageSeo?.CanonicalOverrideUrl, ogImage = pageOgImage }, projectSeo, projectKinds });
            }
            if (publicProfiles.Count != site.Profiles.Count * site.Locales.Count) return Validation("TRANSLATION_REQUIRED", "profiles", "Every published profile must be complete in every enabled site locale.");
            var redirects = await db.SiteRedirects.AsNoTracking().Where(x => x.SiteId == id).Select(x => new { source = x.SourcePath, target = x.TargetPath, status = x.StatusCode }).ToListAsync(ct);
            var payloadCore = new { schemaVersion = 1, baseUrl = site.BaseUrl, site = new { site.Id, site.Name, site.Slug, site.BaseUrl, site.ThemeKey, site.DeploymentTargetKey, site.SearchVerificationToken }, profiles = publicProfiles, redirects, generatedFrom = "immutable-publication" };
            var previousSnapshot = await db.Publications.AsNoTracking().Where(x => x.SiteId == site.Id && x.Purpose == PublicationPurpose.SitePublish).OrderByDescending(x => x.Revision).Select(x => x.Snapshot).FirstOrDefaultAsync(ct);
            var lastModified = SnapshotLastModified.Resolve(previousSnapshot, payloadCore, DateOnly.FromDateTime(DateTime.UtcNow));
            var payload = new { schemaVersion = payloadCore.schemaVersion, baseUrl = payloadCore.baseUrl, lastModified, site = payloadCore.site, profiles = payloadCore.profiles, redirects = payloadCore.redirects, generatedFrom = payloadCore.generatedFrom };
            var revision = (await db.Publications.Where(x => x.SiteId == id).MaxAsync(x => (long?)x.Revision, ct) ?? 0) + 1;
            var publication = new Publication { PersonId = site.PersonId, SiteId = id, Purpose = PublicationPurpose.SitePublish, Revision = revision, Snapshot = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)), SnapshotHash = SnapshotService.Hash(payload), RequestedBy = http.User.AdminId(), IdempotencyKey = request.IdempotencyKey };
            var attempt = new PublicationAttempt { PublicationId = publication.Id, AttemptNumber = 1, LeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(45) };
            publication.Attempts.Add(attempt); db.Add(publication); await Audit(db, http.User.AdminId(), "publication.queued", "Publication", publication.Id, ct);
            var dispatch = await DispatchAndRecordFailure(publication, attempt, http.User.AdminId(), db, dispatcher, ct);
            if (dispatch.Configured && !dispatch.Succeeded) return Results.Problem(statusCode: 502, title: "GitHub workflow dispatch failed", extensions: new Dictionary<string, object?> { ["code"] = "WORKFLOW_DISPATCH_FAILED", ["publicationId"] = publication.Id, ["attemptId"] = attempt.Id, ["detail"] = dispatch.Error });
            return Results.Accepted($"/api/v1/admin/publications/{publication.Id}", new { publicationId = publication.Id, attemptId = attempt.Id, statusUrl = $"/api/v1/admin/publications/{publication.Id}", state = publication.State, dispatchMode = dispatch.Configured ? "github" : "manual" });
        });
        admin.MapGet("/publications/{id:guid}", async (Guid id, HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var item = await db.Publications.AsNoTracking().Include(x => x.Attempts).Include(x => x.Artifacts).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); return item is null ? Results.NotFound() : Results.Ok(item);
        });
        admin.MapPost("/publications/{id:guid}/retry", async (Guid id, HttpContext http, ResumeDbContext db, PublicationDispatcher dispatcher, CancellationToken ct) =>
        {
            var publication = await db.Publications.Include(x => x.Attempts).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (publication is null) return Results.NotFound();
            if (publication.State is not (PublicationState.Failed or PublicationState.Cancelled)) return Results.Problem(statusCode: 409, title: "Only failed or cancelled publications can be retried", extensions: Problem("RETRY_NOT_ALLOWED"));
            if (publication.Purpose == PublicationPurpose.PrivatePdfExport && !dispatcher.IsConfigured(PublicationPurpose.PrivatePdfExport)) return Results.Problem(statusCode: 503, title: "Private PDF export is not configured", extensions: Problem("PRIVATE_EXPORT_NOT_CONFIGURED"));
            var attempt = new PublicationAttempt { PublicationId = publication.Id, AttemptNumber = publication.Attempts.Max(x => x.AttemptNumber) + 1, LeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(45) };
            db.PublicationAttempts.Add(attempt); publication.State = PublicationState.Queued; publication.CompletedAt = null;
            await Audit(db, http.User.AdminId(), "publication.retried", "Publication", publication.Id, ct);
            var dispatch = await DispatchAndRecordFailure(publication, attempt, http.User.AdminId(), db, dispatcher, ct);
            if (dispatch.Configured && !dispatch.Succeeded) return Results.Problem(statusCode: 502, title: "GitHub workflow dispatch failed", extensions: new Dictionary<string, object?> { ["code"] = "WORKFLOW_DISPATCH_FAILED", ["publicationId"] = publication.Id, ["attemptId"] = attempt.Id, ["detail"] = dispatch.Error });
            return Results.Accepted($"/api/v1/admin/publications/{publication.Id}", new { publicationId = publication.Id, attemptId = attempt.Id, state = publication.State, dispatchMode = dispatch.Configured ? "github" : "manual" });
        });
        admin.MapPost("/publications/{id:guid}/reconcile", async (Guid id, HttpContext http, ResumeDbContext db, PublicationReconciler reconciler, CancellationToken ct) =>
        {
            var publication = await db.Publications.Include(x => x.Attempts).Include(x => x.Artifacts).SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (publication is null) return Results.NotFound();
            var attempt = publication.Attempts.OrderByDescending(x => x.AttemptNumber).First();
            var active = publication.State is PublicationState.Queued or PublicationState.Building or PublicationState.Validating or PublicationState.Deploying;
            var reconciliation = active ? await reconciler.ReconcileAsync(publication, attempt, ct) : new WorkflowReconciliation(false, false, null, null);
            if (active && reconciliation.Completed && reconciliation.Result is not null)
            {
                var invalid = await ValidateAttemptResult(publication, reconciliation.Result, db, ct);
                if (invalid is not null) return Results.Problem(statusCode: 502, title: "Workflow evidence failed publication validation", extensions: Problem("RECONCILE_EVIDENCE_INVALID"));
                await ApplyPublicationResult(publication, attempt, reconciliation.Result, db, ct);
                await Audit(db, http.User.AdminId(), "publication.reconciled", "Publication", publication.Id, ct);
            }
            else if (active && attempt.LeaseExpiresAt is not null && attempt.LeaseExpiresAt <= DateTimeOffset.UtcNow)
            {
                var failed = new AttemptResult(PublicationState.Failed, "ATTEMPT_LEASE_EXPIRED", "No final workflow callback arrived before the attempt lease expired.");
                await ApplyPublicationResult(publication, attempt, failed, db, ct);
                await Audit(db, http.User.AdminId(), "publication.reconciled_failed", "Publication", publication.Id, ct);
            }
            return Results.Ok(new { publicationId = publication.Id, attemptId = attempt.Id, state = publication.State, attempt.LeaseExpiresAt, attempt.WorkflowRunId, reconciliation.Configured, reconciliation.Error, reconciledAt = DateTimeOffset.UtcNow });
        });
        admin.MapPost("/sites/{id:guid}/rollback", async (Guid id, RollbackRequest request, HttpContext http, ResumeDbContext db, PublicationDispatcher dispatcher, CancellationToken ct) =>
        {
            var site = await db.Sites.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct);
            if (site is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 100) return Validation("INVALID_IDEMPOTENCY_KEY", "idempotencyKey", "Provide a non-empty key up to 100 characters.");
            var existing = await db.Publications.AsNoTracking().Include(x => x.Attempts).SingleOrDefaultAsync(x => x.PersonId == site.PersonId && x.IdempotencyKey == request.IdempotencyKey, ct);
            if (existing is not null) { var currentAttempt = existing.Attempts.OrderByDescending(x => x.AttemptNumber).First(); return Results.Accepted($"/api/v1/admin/publications/{existing.Id}", new { publicationId = existing.Id, attemptId = currentAttempt.Id, state = existing.State }); }
            if (await db.Publications.AnyAsync(x => x.SiteId == id && (x.State == PublicationState.Queued || x.State == PublicationState.Building || x.State == PublicationState.Validating || x.State == PublicationState.Deploying), ct)) return Results.Problem(statusCode: 409, title: "A publish is already active", extensions: Problem("PUBLISH_ACTIVE"));
            var source = await db.Publications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.SourcePublicationId && x.SiteId == id && x.PersonId == site.PersonId && x.Purpose == PublicationPurpose.SitePublish && x.State == PublicationState.Succeeded, ct);
            if (source is null) return Results.Problem(statusCode: 400, title: "Rollback source must be a successful publication of this site", extensions: Problem("INVALID_ROLLBACK_SOURCE"));
            var revision = (await db.Publications.Where(x => x.SiteId == id).MaxAsync(x => (long?)x.Revision, ct) ?? 0) + 1;
            var publication = new Publication { PersonId = site.PersonId, SiteId = id, Purpose = PublicationPurpose.SitePublish, Revision = revision, SchemaVersion = source.SchemaVersion, Snapshot = source.Snapshot, SnapshotHash = source.SnapshotHash, RequestedBy = http.User.AdminId(), IdempotencyKey = request.IdempotencyKey };
            var attempt = new PublicationAttempt { PublicationId = publication.Id, AttemptNumber = 1, LeaseExpiresAt = DateTimeOffset.UtcNow.AddMinutes(45) };
            publication.Attempts.Add(attempt); db.Add(publication); await Audit(db, http.User.AdminId(), "publication.rollback_queued", "Publication", publication.Id, ct);
            var dispatch = await DispatchAndRecordFailure(publication, attempt, http.User.AdminId(), db, dispatcher, ct);
            if (dispatch.Configured && !dispatch.Succeeded) return Results.Problem(statusCode: 502, title: "GitHub workflow dispatch failed", extensions: new Dictionary<string, object?> { ["code"] = "WORKFLOW_DISPATCH_FAILED", ["publicationId"] = publication.Id, ["attemptId"] = attempt.Id, ["detail"] = dispatch.Error });
            return Results.Accepted($"/api/v1/admin/publications/{publication.Id}", new { publicationId = publication.Id, attemptId = attempt.Id, sourcePublicationId = source.Id, state = publication.State, dispatchMode = dispatch.Configured ? "github" : "manual" });
        });
        admin.MapGet("/data-export", async (HttpContext http, ResumeDbContext db, CancellationToken ct) =>
        {
            var personId = http.User.PersonId();
            return Results.Ok(new
            {
                schemaVersion = 1,
                exportedAt = DateTimeOffset.UtcNow,
                person = await db.Persons.AsNoTracking().Include(x => x.Translations).Include(x => x.Links).ThenInclude(x => x.Translations).SingleAsync(x => x.Id == personId, ct),
                skills = await db.Skills.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == personId).ToListAsync(ct),
                projects = await db.Projects.AsNoTracking().Include(x => x.Translations).Include(x => x.Highlights).ThenInclude(x => x.Translations).Where(x => x.PersonId == personId).ToListAsync(ct),
                experiences = await db.Experiences.AsNoTracking().Include(x => x.Translations).Include(x => x.Highlights).ThenInclude(x => x.Translations).Where(x => x.PersonId == personId).ToListAsync(ct),
                educations = await db.Educations.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == personId).ToListAsync(ct),
                certifications = await db.Certifications.AsNoTracking().Include(x => x.Translations).Where(x => x.PersonId == personId).ToListAsync(ct),
                languages = await db.SpokenLanguages.AsNoTracking().Where(x => x.PersonId == personId).ToListAsync(ct),
                profiles = await db.ResumeProfiles.AsNoTracking().Include(x => x.Translations).Include(x => x.Sections).Include(x => x.PdfSettings).Where(x => x.PersonId == personId).ToListAsync(ct),
                profileContacts = await db.ProfileContactSettings.AsNoTracking().Where(x => db.ResumeProfiles.Any(p => p.Id == x.ProfileId && p.PersonId == personId)).ToListAsync(ct),
                profileProjects = await db.ProfileProjects.AsNoTracking().Where(x => db.ResumeProfiles.Any(p => p.Id == x.ProfileId && p.PersonId == personId)).ToListAsync(ct),
                profileSkills = await db.ProfileSkills.AsNoTracking().Where(x => db.ResumeProfiles.Any(p => p.Id == x.ProfileId && p.PersonId == personId)).ToListAsync(ct),
                sites = await db.Sites.AsNoTracking().Include(x => x.Translations).Include(x => x.Locales).Include(x => x.Profiles).Where(x => x.PersonId == personId).ToListAsync(ct)
            });
        });
    }

    private static void MapInternal(RouteGroupBuilder api)
    {
        var internalApi = api.MapGroup("/internal").AddEndpointFilter(async (context, next) =>
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var expected = configuration["Publishing:CallbackSecret"];
            var actual = context.HttpContext.Request.Headers["X-Publishing-Secret"].ToString();
            if (string.IsNullOrWhiteSpace(expected) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(expected), System.Text.Encoding.UTF8.GetBytes(actual))) return Results.Unauthorized();
            return await next(context);
        });
        internalApi.MapGet("/publications/{id:guid}/snapshot", async (Guid id, ResumeDbContext db, CancellationToken ct) =>
        {
            var publication = await db.Publications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); return publication is null ? Results.NotFound() : Results.Text(publication.Snapshot, "application/json");
        });
        internalApi.MapPost("/publications/{id:guid}/attempts/{attemptId:guid}/progress", async (Guid id, Guid attemptId, AttemptProgress request, ResumeDbContext db, CancellationToken ct) =>
        {
            if (request.State is not (PublicationState.Building or PublicationState.Validating or PublicationState.Deploying) || !long.TryParse(request.WorkflowRunId, out _)) return Results.BadRequest();
            var publication = await db.Publications.Include(x => x.Attempts).SingleOrDefaultAsync(x => x.Id == id, ct); if (publication is null) return Results.NotFound();
            var attempt = publication.Attempts.SingleOrDefault(x => x.Id == attemptId); if (attempt is null || attempt.AttemptNumber != publication.Attempts.Max(x => x.AttemptNumber) || publication.State is PublicationState.Succeeded or PublicationState.Failed or PublicationState.Cancelled) return Results.Conflict();
            attempt.WorkflowRunId = request.WorkflowRunId; attempt.StartedAt ??= DateTimeOffset.UtcNow; attempt.State = request.State; publication.State = request.State;
            await db.SaveChangesAsync(ct); return Results.NoContent();
        });
        internalApi.MapPost("/publications/{id:guid}/attempts/{attemptId:guid}/result", async (Guid id, Guid attemptId, AttemptResult request, ResumeDbContext db, CancellationToken ct) =>
        {
            if (request.State is not (PublicationState.Succeeded or PublicationState.Failed or PublicationState.Cancelled)) return Results.BadRequest();
            var publication = await db.Publications.Include(x => x.Attempts).Include(x => x.Artifacts).SingleOrDefaultAsync(x => x.Id == id, ct); if (publication is null) return Results.NotFound();
            var attempt = publication.Attempts.SingleOrDefault(x => x.Id == attemptId); if (attempt is null || attempt.AttemptNumber != publication.Attempts.Max(x => x.AttemptNumber)) return Results.Conflict();
            var invalid = await ValidateAttemptResult(publication, request, db, ct); if (invalid is not null) return invalid;
            await ApplyPublicationResult(publication, attempt, request, db, ct);
            await db.SaveChangesAsync(ct); return Results.NoContent();
        });
    }

    private static IResult? ValidateProject(ProjectRequest request)
    {
        if (!DomainValidation.IsSlug(request.Slug)) return Validation("INVALID_SLUG", "slug", "Use lowercase Latin letters, numbers and hyphens.");
        if (request.IsOngoing && request.EndDate is not null) return Validation("ONGOING_END_DATE", "endDate", "An ongoing project cannot have an end date.");
        if (request.StartDate is not null && request.EndDate < request.StartDate) return Validation("DATE_ORDER", "endDate", "End date cannot be before start date.");
        if (!IsHttpsOrEmpty(request.RepositoryUrl) || !IsHttpsOrEmpty(request.DemoUrl)) return Validation("HTTPS_REQUIRED", "url", "Project links must use HTTPS.");
        return null;
    }

    private static IResult? ValidateExperience(ExperienceRequest request)
    {
        if (request.IsCurrent && request.EndDate is not null) return Validation("ONGOING_END_DATE", "endDate", "A current role cannot have an end date.");
        if (!DomainValidation.IsValidDateRange(request.StartDate, request.EndDate, request.IsCurrent)) return Validation("DATE_ORDER", "endDate", "End date cannot be before start date.");
        if (!IsHttpsOrEmpty(request.OrganizationUrl)) return Validation("HTTPS_REQUIRED", "organizationUrl", "Organization URL must use HTTPS.");
        if (string.IsNullOrWhiteSpace(request.En.Organization) || string.IsNullOrWhiteSpace(request.En.JobTitle) || string.IsNullOrWhiteSpace(request.Ar.Organization) || string.IsNullOrWhiteSpace(request.Ar.JobTitle)) return Validation("TRANSLATION_REQUIRED", "translations", "Organization and job title are required in Arabic and English.");
        return null;
    }

    private static IResult? ValidateEducation(EducationRequest request)
    {
        if (request.IsCurrent && request.EndDate is not null) return Validation("ONGOING_END_DATE", "endDate", "Current education cannot have an end date.");
        if (!DomainValidation.IsValidDateRange(request.StartDate, request.EndDate, request.IsCurrent)) return Validation("DATE_ORDER", "endDate", "End date cannot be before start date.");
        if (string.IsNullOrWhiteSpace(request.En.Institution) || string.IsNullOrWhiteSpace(request.En.Degree) || string.IsNullOrWhiteSpace(request.Ar.Institution) || string.IsNullOrWhiteSpace(request.Ar.Degree)) return Validation("TRANSLATION_REQUIRED", "translations", "Institution and degree are required in Arabic and English.");
        return null;
    }

    private static IResult? ValidateCertification(CertificationRequest request)
    {
        if (request.ExpiresOn < request.IssuedOn) return Validation("DATE_ORDER", "expiresOn", "Expiry cannot be before issue date.");
        if (!IsHttpsOrEmpty(request.CredentialUrl)) return Validation("HTTPS_REQUIRED", "credentialUrl", "Credential URL must use HTTPS.");
        if (string.IsNullOrWhiteSpace(request.En.Name) || string.IsNullOrWhiteSpace(request.En.Issuer) || string.IsNullOrWhiteSpace(request.Ar.Name) || string.IsNullOrWhiteSpace(request.Ar.Issuer)) return Validation("TRANSLATION_REQUIRED", "translations", "Name and issuer are required in Arabic and English.");
        return null;
    }

    private static IResult? ValidateLanguage(LanguageRequest request)
    {
        if (request.LanguageCode.Trim().Length is < 2 or > 12) return Validation("INVALID_LANGUAGE_CODE", "languageCode", "Use a BCP 47 language code such as ar or en.");
        if (string.IsNullOrWhiteSpace(request.Proficiency) && string.IsNullOrWhiteSpace(request.ProficiencyEn) && string.IsNullOrWhiteSpace(request.ProficiencyAr)) return Validation("REQUIRED", "proficiency", "At least one proficiency label is required.");
        return null;
    }

    private static void ApplyProject(Project project, ProjectRequest request)
    {
        project.Slug = request.Slug; project.Kind = request.Kind; project.StartDate = request.StartDate; project.EndDate = request.EndDate; project.IsOngoing = request.IsOngoing; project.RepositoryUrl = Clean(request.RepositoryUrl); project.DemoUrl = Clean(request.DemoUrl);
        UpsertProjectTranslation(project, Locale.En, request.En); UpsertProjectTranslation(project, Locale.Ar, request.Ar);
    }
    private static void ApplyExperience(Experience item, ExperienceRequest request)
    {
        item.EmploymentType = request.EmploymentType.Trim(); item.StartDate = request.StartDate; item.EndDate = request.EndDate; item.IsCurrent = request.IsCurrent; item.OrganizationUrl = Clean(request.OrganizationUrl);
        UpsertExperienceTranslation(item, Locale.En, request.En); UpsertExperienceTranslation(item, Locale.Ar, request.Ar);
    }
    private static void ApplyEducation(Education item, EducationRequest request)
    {
        item.StartDate = request.StartDate; item.EndDate = request.EndDate; item.IsCurrent = request.IsCurrent;
        UpsertEducationTranslation(item, Locale.En, request.En); UpsertEducationTranslation(item, Locale.Ar, request.Ar);
    }
    private static void ApplyCertification(Certification item, CertificationRequest request)
    {
        item.IssuedOn = request.IssuedOn; item.ExpiresOn = request.ExpiresOn; item.CredentialId = Clean(request.CredentialId); item.CredentialUrl = Clean(request.CredentialUrl);
        UpsertCertificationTranslation(item, Locale.En, request.En); UpsertCertificationTranslation(item, Locale.Ar, request.Ar);
    }
    private static PersonTranslation CreatePersonTranslation(Guid personId, PersonTranslationInput input, Locale locale) => new()
    {
        PersonId = personId,
        Locale = locale,
        FullName = input.FullName.Trim(),
        City = Clean(input.City),
        Country = Clean(input.Country),
        DefaultHeadline = Clean(input.Headline),
        DefaultSummary = Clean(input.Summary)
    };
    private static void UpsertLinkTranslation(PersonLink link, Locale locale, string value) { var t = link.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) link.Translations.Add(new PersonLinkTranslation { PersonLinkId = link.Id, Locale = locale, Label = value.Trim() }); else t.Label = value.Trim(); }
    private static void UpsertSkillTranslation(Skill skill, Locale locale, string value) { var t = skill.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) skill.Translations.Add(new SkillTranslation { SkillId = skill.Id, Locale = locale, DisplayName = value.Trim() }); else t.DisplayName = value.Trim(); }
    private static void UpsertProjectTranslation(Project project, Locale locale, ProjectTranslationInput input) { var t = project.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) project.Translations.Add(new ProjectTranslation { ProjectId = project.Id, Locale = locale, Name = input.Name.Trim(), Role = Clean(input.Role), Summary = input.Summary.Trim(), Description = Clean(input.Description) }); else { t.Name = input.Name.Trim(); t.Role = Clean(input.Role); t.Summary = input.Summary.Trim(); t.Description = Clean(input.Description); } }
    private static void UpsertProjectHighlightTranslation(ProjectHighlight highlight, Locale locale, string text) { var t = highlight.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) highlight.Translations.Add(new ProjectHighlightTranslation { HighlightId = highlight.Id, Locale = locale, Text = text.Trim() }); else t.Text = text.Trim(); }
    private static void UpsertExperienceHighlightTranslation(ExperienceHighlight highlight, Locale locale, string text) { var t = highlight.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) highlight.Translations.Add(new ExperienceHighlightTranslation { HighlightId = highlight.Id, Locale = locale, Text = text.Trim() }); else t.Text = text.Trim(); }
    private static void UpsertExperienceTranslation(Experience item, Locale locale, ExperienceTranslationInput input) { var t = item.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) item.Translations.Add(new ExperienceTranslation { ExperienceId = item.Id, Locale = locale, Organization = input.Organization.Trim(), JobTitle = input.JobTitle.Trim(), Location = Clean(input.Location), Summary = Clean(input.Summary) }); else { t.Organization = input.Organization.Trim(); t.JobTitle = input.JobTitle.Trim(); t.Location = Clean(input.Location); t.Summary = Clean(input.Summary); } }
    private static void UpsertEducationTranslation(Education item, Locale locale, EducationTranslationInput input) { var t = item.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) item.Translations.Add(new EducationTranslation { EducationId = item.Id, Locale = locale, Institution = input.Institution.Trim(), Degree = input.Degree.Trim(), FieldOfStudy = Clean(input.FieldOfStudy), Location = Clean(input.Location), Notes = Clean(input.Notes) }); else { t.Institution = input.Institution.Trim(); t.Degree = input.Degree.Trim(); t.FieldOfStudy = Clean(input.FieldOfStudy); t.Location = Clean(input.Location); t.Notes = Clean(input.Notes); } }
    private static void UpsertCertificationTranslation(Certification item, Locale locale, CertificationTranslationInput input) { var t = item.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) item.Translations.Add(new CertificationTranslation { CertificationId = item.Id, Locale = locale, Name = input.Name.Trim(), Issuer = input.Issuer.Trim() }); else { t.Name = input.Name.Trim(); t.Issuer = input.Issuer.Trim(); } }
    private static void UpsertProfileTranslation(ResumeProfile profile, Locale locale, string headline, string summary, string? seoTitle, string? seoDescription) { var t = profile.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) profile.Translations.Add(new ResumeProfileTranslation { ProfileId = profile.Id, Locale = locale, Headline = headline.Trim(), Summary = summary.Trim(), SeoTitle = Clean(seoTitle), SeoDescription = Clean(seoDescription) }); else { t.Headline = headline.Trim(); t.Summary = summary.Trim(); t.SeoTitle = Clean(seoTitle); t.SeoDescription = Clean(seoDescription); } }
    private static void UpsertProfileProjectTranslation(ResumeDbContext db, ProfileProject project, Locale locale, string? summaryOverride)
    {
        var t = project.Translations.SingleOrDefault(x => x.Locale == locale);
        if (t is null)
        {
            t = new ProfileProjectTranslation { ProfileProjectId = project.Id, Locale = locale, SummaryOverride = Clean(summaryOverride) };
            project.Translations.Add(t);
            db.Entry(t).State = EntityState.Added;
        }
        else t.SummaryOverride = Clean(summaryOverride);
    }
    private static void UpsertSiteTranslation(Site site, Locale locale, SiteTranslationInput input) { var t = site.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) site.Translations.Add(new SiteTranslation { SiteId = site.Id, Locale = locale, Title = input.Title.Trim(), Description = input.Description.Trim() }); else { t.Title = input.Title.Trim(); t.Description = input.Description.Trim(); } }
    private static void UpsertMediaTranslation(MediaAsset asset, Locale locale, string alt, string? caption) { var t = asset.Translations.SingleOrDefault(x => x.Locale == locale); if (t is null) asset.Translations.Add(new MediaAssetTranslation { AssetId = asset.Id, Locale = locale, AltText = alt.Trim(), Caption = Clean(caption) }); else { t.AltText = alt.Trim(); t.Caption = Clean(caption); } }
    private static async Task<object?> PublicOgImage(SeoPageSetting? setting, Locale locale, Guid personId, ResumeDbContext db, CancellationToken ct)
    {
        if (setting?.OgAssetId is null) return null;
        var asset = await db.MediaAssets.AsNoTracking().Include(x => x.Translations).SingleOrDefaultAsync(x => x.Id == setting.OgAssetId && x.PersonId == personId && x.ArchivedAt == null && x.Status == "Ready", ct);
        if (asset?.DeliveryUrl is null || asset.Width is null || asset.Height is null) return null;
        var translation = asset.Translations.SingleOrDefault(x => x.Locale == locale);
        if (translation is null || string.IsNullOrWhiteSpace(translation.AltText)) return null;
        return new { src = asset.DeliveryUrl, width = asset.Width.Value, height = asset.Height.Value, alt = translation.AltText };
    }
    private static async Task<bool> OwnsAll<T>(DbSet<T> set, Guid personId, IEnumerable<Guid> requestedIds, CancellationToken ct) where T : OwnedEntity
    {
        var ids = requestedIds.Distinct().ToList();
        return ids.Count == 0 || await set.CountAsync(x => x.PersonId == personId && ids.Contains(x.Id), ct) == ids.Count;
    }
    private static async Task<IResult> ArchiveOwned<T>(DbSet<T> set, Guid id, HttpContext http, ResumeDbContext db, string action, CancellationToken ct) where T : OwnedEntity
    {
        var item = await set.SingleOrDefaultAsync(x => x.Id == id && x.PersonId == http.User.PersonId(), ct); if (item is null) return Results.NotFound();
        switch (item) { case Experience value: value.ArchivedAt = DateTimeOffset.UtcNow; break; case Education value: value.ArchivedAt = DateTimeOffset.UtcNow; break; case Certification value: value.ArchivedAt = DateTimeOffset.UtcNow; break; default: return Results.BadRequest(); }
        await Audit(db, http.User.AdminId(), action, typeof(T).Name, id, ct); return Results.NoContent();
    }
    private static async Task ApplyPublicationResult(Publication publication, PublicationAttempt attempt, AttemptResult request, ResumeDbContext db, CancellationToken ct)
    {
        attempt.State = request.State; attempt.ErrorCode = Clean(request.ErrorCode); attempt.ErrorSummary = Clean(request.ErrorSummary); attempt.FinishedAt = DateTimeOffset.UtcNow; publication.State = request.State; publication.CompletedAt = DateTimeOffset.UtcNow;
        if (request.State != PublicationState.Succeeded) return;
        foreach (var result in request.Artifacts ?? [])
        {
            var artifact = publication.Artifacts.SingleOrDefault(x => x.Kind == result.Kind && x.ProfileId == result.ProfileId && x.Locale == result.Locale);
            if (artifact is null) { artifact = new PublicationArtifact { PublicationId = publication.Id, Kind = result.Kind, ProfileId = result.ProfileId, Locale = result.Locale }; publication.Artifacts.Add(artifact); db.Entry(artifact).State = EntityState.Added; }
            artifact.TemplateVersion = result.TemplateVersion.Trim(); artifact.PathOrArtifactId = result.PathOrArtifactId.Trim(); artifact.Sha256 = result.Sha256.ToLowerInvariant(); artifact.SizeBytes = result.SizeBytes; artifact.PageCount = result.PageCount; artifact.QaReport = result.QaReport.ValueKind == System.Text.Json.JsonValueKind.Undefined ? "{}" : result.QaReport.GetRawText(); artifact.Visibility = result.Visibility;
        }
        if (publication.Purpose == PublicationPurpose.SitePublish)
        {
            var site = await db.Sites.SingleAsync(x => x.Id == publication.SiteId, ct); site.CurrentPublicationId = publication.Id;
            if (request.Deployment is not null)
            {
                var deployment = await db.SiteDeployments.SingleOrDefaultAsync(x => x.AttemptId == attempt.Id, ct);
                deployment ??= new SiteDeployment { SiteId = site.Id, PublicationId = publication.Id, AttemptId = attempt.Id };
                deployment.ProviderDeploymentId = request.Deployment.ProviderDeploymentId.Trim(); deployment.DeploymentUrl = request.Deployment.DeploymentUrl.Trim(); deployment.State = request.Deployment.State; deployment.DeployedAt = request.Deployment.DeployedAt ?? DateTimeOffset.UtcNow;
                if (db.Entry(deployment).State == EntityState.Detached) db.Add(deployment);
            }
        }
    }
    private static async Task<IResult?> ValidateAttemptResult(Publication publication, AttemptResult request, ResumeDbContext db, CancellationToken ct)
    {
        if (request.State != PublicationState.Succeeded) return null;
        if (publication.SiteId is not null && await db.Publications.Where(x => x.SiteId == publication.SiteId && x.Purpose == PublicationPurpose.SitePublish).MaxAsync(x => x.Revision, ct) != publication.Revision)
            return Results.Problem(statusCode: 409, title: "A newer publication already exists for this site", extensions: Problem("STALE_PUBLICATION_RESULT"));
        if (request.Artifacts is null || request.Artifacts.Count == 0) return Validation("ARTIFACTS_REQUIRED", "artifacts", "A successful publication must report its generated artifacts.");
        if (request.Artifacts.Any(x => x.SizeBytes <= 0 || x.SizeBytes > 5_000_000 || !IsSha256(x.Sha256) || string.IsNullOrWhiteSpace(x.PathOrArtifactId))) return Validation("INVALID_ARTIFACT", "artifacts", "Artifact path, SHA-256, and a size up to 5 MB are required.");
        if (publication.Purpose == PublicationPurpose.SitePublish && request.Deployment is null) return Validation("DEPLOYMENT_REQUIRED", "deployment", "A successful site publication must include deployment evidence.");
        if (request.Deployment is not null && (!IsHttps(request.Deployment.DeploymentUrl) || string.IsNullOrWhiteSpace(request.Deployment.ProviderDeploymentId))) return Validation("INVALID_DEPLOYMENT", "deployment", "Deployment ID and HTTPS URL are required.");
        var artifactProfileIds = request.Artifacts.Where(x => x.ProfileId is not null).Select(x => x.ProfileId!.Value).Distinct().ToList();
        if (publication.SiteId is not null && artifactProfileIds.Count > 0 && await db.SiteProfiles.CountAsync(x => x.SiteId == publication.SiteId && artifactProfileIds.Contains(x.ProfileId), ct) != artifactProfileIds.Count) return Validation("ARTIFACT_PROFILE_MISMATCH", "artifacts.profileId", "Every artifact profile must belong to the published site.");
        if (publication.ProfileId is not null && artifactProfileIds.Any(x => x != publication.ProfileId)) return Validation("ARTIFACT_PROFILE_MISMATCH", "artifacts.profileId", "Artifact profile does not match the private export.");
        return null;
    }
    private static async Task<DispatchResult> DispatchAndRecordFailure(Publication publication, PublicationAttempt attempt, Guid actorId, ResumeDbContext db, PublicationDispatcher dispatcher, CancellationToken ct)
    {
        var result = await dispatcher.DispatchAsync(publication.Id, attempt.Id, ct, publication.Purpose);
        if (!result.Configured || result.Succeeded) return result;
        await ApplyPublicationResult(publication, attempt, new AttemptResult(PublicationState.Failed, "WORKFLOW_DISPATCH_FAILED", result.Error), db, ct);
        await Audit(db, actorId, "publication.dispatch_failed", "Publication", publication.Id, ct);
        return result;
    }
    private static async Task Audit(ResumeDbContext db, Guid actor, string action, string type, Guid id, CancellationToken ct) { db.AuditEvents.Add(new AuditEvent { ActorId = actor, Action = action, EntityType = type, EntityId = id }); await db.SaveChangesAsync(ct); }
    private static Dictionary<string, object?> Problem(string code) => new() { ["code"] = code };
    private static IResult Conflict() => Results.Problem(statusCode: 409, title: "The record changed since it was loaded", extensions: Problem("VERSION_CONFLICT"));
    private static IResult Validation(string code, string path, string message) => Results.Problem(statusCode: 400, title: "Validation failed", extensions: new Dictionary<string, object?> { ["code"] = code, ["errors"] = new[] { new { path, message } } });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsHttps(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    private static bool IsHttpsOrEmpty(string? value) => string.IsNullOrWhiteSpace(value) || IsHttps(value);
    private static bool IsDeploymentTarget(string value) => value.Length <= 58 && DomainValidation.IsSlug(value);
    private static bool IsSearchVerificationToken(string value) => value.Length <= 200 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    private static bool IsSha256(string value) => value.Length == 64 && value.All(char.IsAsciiHexDigit);
    private static (string MimeType, int Width, int Height)? ImageInfo(byte[] data)
    {
        if (data.Length >= 24 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16, 4)), System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20, 4)));
        if (data.Length >= 30 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            var kind = System.Text.Encoding.ASCII.GetString(data, 12, 4);
            if (kind == "VP8X") return ("image/webp", 1 + data[24] + (data[25] << 8) + (data[26] << 16), 1 + data[27] + (data[28] << 8) + (data[29] << 16));
            if (kind == "VP8 " && data.Length >= 30 && data.AsSpan(23, 3).SequenceEqual(new byte[] { 0x9d, 0x01, 0x2a })) return ("image/webp", System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(26, 2)) & 0x3fff, System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(28, 2)) & 0x3fff);
            if (kind == "VP8L" && data.Length >= 25 && data[20] == 0x2f) { var bits = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(21, 4)); return ("image/webp", (int)(bits & 0x3fff) + 1, (int)((bits >> 14) & 0x3fff) + 1); }
        }
        if (data.Length >= 4 && data[0] == 0xff && data[1] == 0xd8)
        {
            var offset = 2;
            while (offset + 8 < data.Length) { if (data[offset++] != 0xff) continue; var marker = data[offset++]; if (marker is 0xd8 or 0xd9) continue; if (offset + 2 > data.Length) break; var length = System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2)); if (length < 2 || offset + length > data.Length) break; if (marker is >= 0xc0 and <= 0xc3 or >= 0xc5 and <= 0xc7 or >= 0xc9 and <= 0xcb or >= 0xcd and <= 0xcf) return ("image/jpeg", System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 5, 2)), System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 3, 2))); offset += length; }
        }
        return null;
    }
    private static bool SafePath(string value) => value.StartsWith('/') && !value.Contains("//", StringComparison.Ordinal) && !value.Contains('\r') && !value.Contains('\n') && !value.Contains('?') && !value.Contains('#');
    private static bool ParseLocale(string value, out Locale locale) { locale = value.Equals("ar", StringComparison.OrdinalIgnoreCase) ? Locale.Ar : Locale.En; return value.Equals("ar", StringComparison.OrdinalIgnoreCase) || value.Equals("en", StringComparison.OrdinalIgnoreCase); }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt);
public sealed record PasswordChangeRequest(string CurrentPassword, string NewPassword);
public sealed record PersonTranslationInput(string FullName, string? City, string? Country, string? Headline, string? Summary);
public sealed record PersonUpdate(long Version, string? Email, string? Phone, Locale DefaultLocale, PersonTranslationInput En, PersonTranslationInput Ar);
public sealed record PersonDto(Guid Id, long Version, string? Email, string? Phone, Locale DefaultLocale, IReadOnlyList<PersonTranslation> Translations, IReadOnlyList<PersonLink> Links) { public static PersonDto From(Person value) => new(value.Id, value.Version, value.Email, value.Phone, value.DefaultLocale, value.Translations, value.Links); }
public sealed record LinkUpsert(long Version, LinkKind Kind, string Url, int SortOrder, string LabelEn, string LabelAr);
public sealed record SkillRequest(long Version, string CanonicalName, string Category, string DisplayNameEn, string DisplayNameAr, string? CategoryEn = null, string? CategoryAr = null);
public sealed record ProjectTranslationInput(string Name, string? Role, string Summary, string? Description);
public sealed record ProjectRequest(long Version, string Slug, ProjectKind Kind, DateOnly? StartDate, DateOnly? EndDate, bool IsOngoing, string? RepositoryUrl, string? DemoUrl, ProjectTranslationInput En, ProjectTranslationInput Ar);
public sealed record ProjectMediaRequest(Guid? CoverAssetId, IReadOnlyList<Guid> AssetIds);
public sealed record MediaUpdateRequest(long Version, string AltAr, string AltEn, string? CaptionAr, string? CaptionEn);
public sealed record HighlightRequest(long Version, int SortOrder, string TextEn, string TextAr);
public sealed record ExperienceTranslationInput(string Organization, string JobTitle, string? Location, string? Summary);
public sealed record ExperienceRequest(long Version, string EmploymentType, DateOnly StartDate, DateOnly? EndDate, bool IsCurrent, string? OrganizationUrl, ExperienceTranslationInput En, ExperienceTranslationInput Ar);
public sealed record EducationTranslationInput(string Institution, string Degree, string? FieldOfStudy, string? Location, string? Notes);
public sealed record EducationRequest(long Version, DateOnly? StartDate, DateOnly? EndDate, bool IsCurrent, EducationTranslationInput En, EducationTranslationInput Ar);
public sealed record CertificationTranslationInput(string Name, string Issuer);
public sealed record CertificationRequest(long Version, DateOnly? IssuedOn, DateOnly? ExpiresOn, string? CredentialId, string? CredentialUrl, CertificationTranslationInput En, CertificationTranslationInput Ar);
public sealed record ProfileRequest(string InternalName, string Slug, Locale DefaultLocale, string HeadlineEn, string SummaryEn, string HeadlineAr, string SummaryAr);
public sealed record ProfileUpdateRequest(long Version, string InternalName, string Slug, Locale DefaultLocale, string HeadlineEn, string SummaryEn, string HeadlineAr, string SummaryAr, string? SeoTitleEn, string? SeoDescriptionEn, string? SeoTitleAr, string? SeoDescriptionAr);
public sealed record DuplicateProfileRequest(string InternalName, string Slug);
public sealed record SelectionItem(Guid Id, bool WebEnabled, bool PdfEnabled, int WebOrder, int PdfOrder);
public sealed record HighlightSelectionItem(Guid Id, bool WebEnabled, bool PdfEnabled, int SortOrder);
public sealed record ProjectSelectionItem(Guid Id, bool WebEnabled, bool PdfEnabled, int WebOrder, int PdfOrder, string? SummaryOverrideEn, string? SummaryOverrideAr, IReadOnlyList<HighlightSelectionItem>? Highlights);
public sealed record ExperienceSelectionItem(Guid Id, bool WebEnabled, bool PdfEnabled, int WebOrder, int PdfOrder, IReadOnlyList<HighlightSelectionItem>? Highlights);
public sealed record ProfileSelectionRequest(IReadOnlyList<ProjectSelectionItem> Projects, IReadOnlyList<SelectionItem> Skills);
public sealed record SupportingSelectionRequest(IReadOnlyList<ExperienceSelectionItem> Experiences, IReadOnlyList<SelectionItem> Educations, IReadOnlyList<SelectionItem> Certifications, IReadOnlyList<SelectionItem> Languages);
public sealed record ProfileSectionRequest(string SectionKey, bool WebEnabled, bool PdfEnabled, int WebOrder, int PdfOrder);
public sealed record PdfSettingRequest(long Version, string TemplateKey, string PaperSize, decimal FontSize, decimal MarginMm, int TargetPages);
public sealed record ProfileContactRequest(bool WebEmail, bool PdfEmail, bool WebPhone, bool PdfPhone, bool ShowLocation, bool ShowPhotoWeb);
public sealed record SiteRequest(string Name, string Slug, string? BaseUrl, Locale DefaultLocale, bool IsPrimaryIdentitySite, IReadOnlyList<Locale> Locales);
public sealed record SiteTranslationInput(string Title, string Description);
public sealed record SiteUpdateRequest(long Version, string Name, string Slug, string? BaseUrl, Locale DefaultLocale, bool IsPrimaryIdentitySite, string? DeploymentTargetKey, string? SearchVerificationToken, IReadOnlyList<Locale> Locales, SiteTranslationInput En, SiteTranslationInput Ar);
public sealed record SiteProfileInput(Guid ProfileId, string PathSlug, bool IsDefault, bool Indexable, bool IsListed, int SortOrder);
public sealed record SiteProfilesRequest(IReadOnlyList<SiteProfileInput> Profiles);
public sealed record RedirectRequest(string SourcePath, string TargetPath, int StatusCode);
public sealed record RedirectUpdateRequest(long Version, string SourcePath, string TargetPath, int StatusCode, bool IsActive);
public sealed record SeoPageRequest(Guid? Id, long Version, Locale Locale, string PageKind, Guid? ProfileId, Guid? ProjectId, string? TitleOverride, string? DescriptionOverride, string? CanonicalOverrideUrl, Guid? OgAssetId, bool? IndexOverride);
public sealed record LanguageRequest(long Version, string LanguageCode, string Proficiency, string? ProficiencyEn = null, string? ProficiencyAr = null);
public sealed record PublishRequest(string IdempotencyKey);
public sealed record RollbackRequest(Guid SourcePublicationId, string IdempotencyKey);
public sealed record PdfExportRequest(IReadOnlyList<Locale> Locales, string IdempotencyKey);
public sealed record AttemptProgress(PublicationState State, string WorkflowRunId);
public sealed record ArtifactResult(Guid? ProfileId, Locale? Locale, ArtifactKind Kind, string TemplateVersion, string PathOrArtifactId, string Sha256, long SizeBytes, int? PageCount, System.Text.Json.JsonElement QaReport, ArtifactVisibility Visibility);
public sealed record DeploymentResult(string ProviderDeploymentId, string DeploymentUrl, PublicationState State, DateTimeOffset? DeployedAt);
public sealed record AttemptResult(PublicationState State, string? ErrorCode = null, string? ErrorSummary = null, IReadOnlyList<ArtifactResult>? Artifacts = null, DeploymentResult? Deployment = null);
