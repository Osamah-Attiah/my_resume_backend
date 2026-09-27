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
}
