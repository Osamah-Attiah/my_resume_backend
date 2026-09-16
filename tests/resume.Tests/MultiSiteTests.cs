using Microsoft.EntityFrameworkCore;
using Resume.Core;
using Resume.Infrastructure;

namespace Resume.Tests;

public sealed class MultiSiteTests
{
    [Fact]
    public async Task Two_sites_share_source_profiles_but_keep_independent_publications()
    {
        var options = new DbContextOptionsBuilder<ResumeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ResumeDbContext(options);
        var person = new Person();
        var profile = new ResumeProfile { PersonId = person.Id, InternalName = "SHARED TEST", Slug = "shared-test" };
        var firstSite = new Site { PersonId = person.Id, Name = "FIRST TEST SITE", Slug = "first-test" };
        var secondSite = new Site { PersonId = person.Id, Name = "SECOND TEST SITE", Slug = "second-test" };
        firstSite.Profiles.Add(new SiteProfile { SiteId = firstSite.Id, ProfileId = profile.Id, PathSlug = "shared", IsDefault = true });
        secondSite.Profiles.Add(new SiteProfile { SiteId = secondSite.Id, ProfileId = profile.Id, PathSlug = "shared", IsDefault = true });
        var firstPublication = new Publication { PersonId = person.Id, SiteId = firstSite.Id, Purpose = PublicationPurpose.SitePublish, Revision = 1, IdempotencyKey = "first-test", SnapshotHash = new string('a', 64) };
        var secondPublication = new Publication { PersonId = person.Id, SiteId = secondSite.Id, Purpose = PublicationPurpose.SitePublish, Revision = 1, IdempotencyKey = "second-test", SnapshotHash = new string('b', 64) };
        firstSite.CurrentPublicationId = firstPublication.Id;
        secondSite.CurrentPublicationId = secondPublication.Id;
        db.AddRange(person, profile, firstSite, secondSite, firstPublication, secondPublication);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var saved = await db.Sites.AsNoTracking().Include(x => x.Profiles).OrderBy(x => x.Slug).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, saved.Count);
        Assert.All(saved, site => Assert.Equal(profile.Id, Assert.Single(site.Profiles).ProfileId));
        Assert.NotEqual(saved[0].CurrentPublicationId, saved[1].CurrentPublicationId);
        Assert.Equal(1, await db.Publications.CountAsync(x => x.SiteId == firstSite.Id, TestContext.Current.CancellationToken));
        Assert.Equal(1, await db.Publications.CountAsync(x => x.SiteId == secondSite.Id, TestContext.Current.CancellationToken));
    }
}
