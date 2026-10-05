using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Resume.Core;
using Resume.Infrastructure;

namespace Resume.Tests;

public sealed class PublicProjectApiTests
{
    [Fact]
    public async Task Public_projects_reflect_saved_links_and_archiving_without_republishing()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Development")
            .ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ResumeDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ResumeDbContext>>();
                services.AddDbContext<ResumeDbContext>(options => options.UseInMemoryDatabase(databaseName));
            }));
        var person = new Person { Translations = [new() { Locale = Locale.En, FullName = "Test Person" }] };
        var profile = new ResumeProfile { PersonId = person.Id, Slug = "work", InternalName = "Work", Translations = [new() { Locale = Locale.En, Headline = "Engineer", Summary = "Test" }] };
        var project = new Project { PersonId = person.Id, Slug = "fresh-project", Translations = [new() { Locale = Locale.En, Name = "Fresh Project", Summary = "Current summary" }] };
        var site = new Site { PersonId = person.Id, Slug = "test-site", BaseUrl = "https://portfolio.example.test", CurrentPublicationId = Guid.NewGuid() };
        site.Profiles.Add(new SiteProfile { SiteId = site.Id, ProfileId = profile.Id, PathSlug = "work", IsDefault = true });
        site.Locales.Add(new SiteLocale { SiteId = site.Id, Locale = Locale.En });
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ResumeDbContext>();
            db.AddRange(person, profile, project, site, new ProfileProject { ProfileId = profile.Id, ProjectId = project.Id });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var client = factory.CreateClient();
        var path = $"/api/v1/public/sites/{site.Id}/profiles/work/projects?locale=en";
        async Task<JsonDocument> GetProjects()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("Origin", "https://portfolio.example.test");
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        using (var first = await GetProjects())
        {
            var item = Assert.Single(first.RootElement.GetProperty("projects").EnumerateArray());
            Assert.Equal("Current summary", item.GetProperty("summary").GetString());
            Assert.Empty(item.GetProperty("links").EnumerateArray());
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ResumeDbContext>();
            db.ProjectLinks.Add(new ProjectLink { ProjectId = project.Id, Kind = "website", Url = "https://example.com/project", LabelEn = "Project site" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        using (var updated = await GetProjects())
        {
            var item = Assert.Single(updated.RootElement.GetProperty("projects").EnumerateArray());
            var link = Assert.Single(item.GetProperty("links").EnumerateArray());
            Assert.Equal("https://example.com/project", link.GetProperty("url").GetString());
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ResumeDbContext>();
            var saved = await db.Projects.SingleAsync(x => x.Id == project.Id, TestContext.Current.CancellationToken);
            saved.ArchivedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        using var archived = await GetProjects();
        Assert.Empty(archived.RootElement.GetProperty("projects").EnumerateArray());
    }

    [Fact]
    public async Task Public_content_reflects_experience_archiving_and_web_selection_without_republishing()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Development")
            .ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ResumeDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ResumeDbContext>>();
                services.AddDbContext<ResumeDbContext>(options => options.UseInMemoryDatabase(databaseName));
            }));
        var person = new Person { Translations = [new() { Locale = Locale.En, FullName = "Test Person" }, new() { Locale = Locale.Ar, FullName = "شخص للاختبار" }] };
        var profile = new ResumeProfile { PersonId = person.Id, Slug = "work", InternalName = "Work", Translations = [new() { Locale = Locale.En, Headline = "Engineer", Summary = "Test" }, new() { Locale = Locale.Ar, Headline = "مهندس", Summary = "اختبار" }] };
        var freelance = new Experience { PersonId = person.Id, StartDate = new DateOnly(2025, 1, 1), IsCurrent = true, Translations = [new() { Locale = Locale.En, Organization = "Freelance", JobTitle = "Developer" }, new() { Locale = Locale.Ar, Organization = "عمل مستقل", JobTitle = "مطور" }] };
        var company = new Experience { PersonId = person.Id, StartDate = new DateOnly(2024, 1, 1), IsCurrent = true, Translations = [new() { Locale = Locale.En, Organization = "Current company", JobTitle = "Engineer", Summary = "Current work" }, new() { Locale = Locale.Ar, Organization = "جهة العمل الحالية", JobTitle = "مهندس" }] };
        var pdfOnly = new Experience { PersonId = person.Id, StartDate = new DateOnly(2023, 1, 1), Translations = [new() { Locale = Locale.En, Organization = "PDF only", JobTitle = "Private role" }] };
        var unselected = new Experience { PersonId = person.Id, StartDate = new DateOnly(2022, 1, 1), Translations = [new() { Locale = Locale.En, Organization = "Unselected", JobTitle = "Private role" }] };
        var selection = new ProfileExperience { ProfileId = profile.Id, ExperienceId = company.Id };
        var site = new Site { PersonId = person.Id, Slug = "test-site", BaseUrl = "https://portfolio.example.test", CurrentPublicationId = Guid.NewGuid() };
        site.Profiles.Add(new SiteProfile { SiteId = site.Id, ProfileId = profile.Id, PathSlug = "work", IsDefault = true });
        site.Locales.AddRange([new SiteLocale { SiteId = site.Id, Locale = Locale.En }, new SiteLocale { SiteId = site.Id, Locale = Locale.Ar }]);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ResumeDbContext>();
            db.AddRange(person, profile, freelance, company, pdfOnly, unselected, site, selection,
                new ProfileExperience { ProfileId = profile.Id, ExperienceId = freelance.Id },
                new ProfileExperience { ProfileId = profile.Id, ExperienceId = pdfOnly.Id, WebEnabled = false, PdfEnabled = true });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var client = factory.CreateClient();
        async Task<JsonDocument> GetContent(string locale = "en")
        {
            using var response = await client.GetAsync($"/api/v1/public/sites/{site.Id}/profiles/work/projects?locale={locale}", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
        using (var first = await GetContent())
        {
            var names = first.RootElement.GetProperty("experiences").EnumerateArray().Select(x => x.GetProperty("organization").GetString()).ToArray();
            Assert.Equal(2, names.Length);
            Assert.Contains("Freelance", names);
            Assert.Contains("Current company", names);
            Assert.Empty(first.RootElement.GetProperty("projects").EnumerateArray());
        }
        using (var arabic = await GetContent("ar"))
            Assert.Contains(arabic.RootElement.GetProperty("experiences").EnumerateArray(), x => x.GetProperty("organization").GetString() == "عمل مستقل");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ResumeDbContext>();
            (await db.Experiences.SingleAsync(x => x.Id == freelance.Id, TestContext.Current.CancellationToken)).ArchivedAt = DateTimeOffset.UtcNow;
            (await db.ExperienceTranslations.SingleAsync(x => x.ExperienceId == company.Id && x.Locale == Locale.En, TestContext.Current.CancellationToken)).Summary = "Updated work";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        using (var updated = await GetContent())
        {
            var remaining = Assert.Single(updated.RootElement.GetProperty("experiences").EnumerateArray());
            Assert.Equal("Current company", remaining.GetProperty("organization").GetString());
            Assert.Equal("Updated work", remaining.GetProperty("summary").GetString());
        }
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ResumeDbContext>();
            (await db.ProfileExperiences.SingleAsync(x => x.Id == selection.Id, TestContext.Current.CancellationToken)).WebEnabled = false;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        using var empty = await GetContent();
        Assert.Empty(empty.RootElement.GetProperty("experiences").EnumerateArray());
    }
}
