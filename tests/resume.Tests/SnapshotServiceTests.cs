using Microsoft.EntityFrameworkCore;
using Resume.Api;
using Resume.Core;
using Resume.Infrastructure;

namespace Resume.Tests;

public sealed class SnapshotServiceTests
{
    [Fact]
    public async Task Two_profiles_share_a_project_but_keep_independent_targeted_copy()
    {
        var options = new DbContextOptionsBuilder<ResumeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ResumeDbContext(options);
        var person = new Person { Translations = [new() { Locale = Locale.En, FullName = "TEST PERSON" }, new() { Locale = Locale.Ar, FullName = "شخص للاختبار" }] };
        var flutter = new ResumeProfile { PersonId = person.Id, Slug = "flutter-test", InternalName = "FLUTTER TEST", Translations = [new() { Locale = Locale.En, Headline = "Flutter Engineer", Summary = "Test" }] };
        var fullStack = new ResumeProfile { PersonId = person.Id, Slug = "full-stack-test", InternalName = "FULL STACK TEST", Translations = [new() { Locale = Locale.En, Headline = "Software Engineer | Flutter & .NET Backend", Summary = "Test" }] };
        var project = new Project { PersonId = person.Id, Slug = "shared-project", Translations = [new() { Locale = Locale.En, Name = "SHARED PROJECT", Summary = "Shared source fact" }] };
        var flutterSelection = new ProfileProject { ProfileId = flutter.Id, ProjectId = project.Id, Translations = [new() { Locale = Locale.En, SummaryOverride = "Flutter-focused copy" }] };
        var fullStackSelection = new ProfileProject { ProfileId = fullStack.Id, ProjectId = project.Id, Translations = [new() { Locale = Locale.En, SummaryOverride = "Backend-focused copy" }] };
        db.AddRange(person, flutter, fullStack, project, flutterSelection, fullStackSelection);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new SnapshotService(db);
        var flutterDocument = await service.BuildResumeAsync(person.Id, flutter.Id, Locale.En, false, TestContext.Current.CancellationToken);
        var fullStackDocument = await service.BuildResumeAsync(person.Id, fullStack.Id, Locale.En, false, TestContext.Current.CancellationToken);

        Assert.Equal(project.Slug, Assert.Single(flutterDocument!.Projects).Slug);
        Assert.Equal(project.Slug, Assert.Single(fullStackDocument!.Projects).Slug);
        Assert.Equal("Flutter-focused copy", flutterDocument.Projects[0].Summary);
        Assert.Equal("Backend-focused copy", fullStackDocument.Projects[0].Summary);
    }

    [Fact]
    public async Task Snapshot_applies_profile_project_summary_and_highlight_selection()
    {
        var options = new DbContextOptionsBuilder<ResumeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ResumeDbContext(options);
        var person = new Person { Translations = [new() { Locale = Locale.En, FullName = "TEST PERSON" }, new() { Locale = Locale.Ar, FullName = "شخص للاختبار" }] };
        var profile = new ResumeProfile { PersonId = person.Id, Slug = "targeted", InternalName = "TARGETED TEST", Translations = [new() { Locale = Locale.En, Headline = "Software Engineer | Flutter & .NET Backend", Summary = "Test summary" }, new() { Locale = Locale.Ar, Headline = "مهندس برمجيات", Summary = "ملخص اختباري" }] };
        var project = new Project { PersonId = person.Id, Slug = "test-project", Translations = [new() { Locale = Locale.En, Name = "TEST PROJECT", Summary = "Shared summary" }, new() { Locale = Locale.Ar, Name = "مشروع اختباري", Summary = "ملخص مشترك" }] };
        var included = new ProjectHighlight { ProjectId = project.Id, SortOrder = 1, Translations = [new() { Locale = Locale.En, Text = "Included result" }, new() { Locale = Locale.Ar, Text = "نتيجة مضمنة" }] };
        var excluded = new ProjectHighlight { ProjectId = project.Id, SortOrder = 2, Translations = [new() { Locale = Locale.En, Text = "Excluded result" }, new() { Locale = Locale.Ar, Text = "نتيجة مستبعدة" }] };
        project.Highlights.AddRange([included, excluded]);
        var selection = new ProfileProject { ProfileId = profile.Id, ProjectId = project.Id, Translations = [new() { Locale = Locale.En, SummaryOverride = "Targeted summary" }, new() { Locale = Locale.Ar, SummaryOverride = "ملخص مخصص" }] };
        db.AddRange(person, profile, project, selection, new ProfileProjectHighlight { ProfileProjectId = selection.Id, HighlightId = included.Id, WebEnabled = true, PdfEnabled = true, SortOrder = 0 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var document = await new SnapshotService(db).BuildResumeAsync(person.Id, profile.Id, Locale.En, false, TestContext.Current.CancellationToken);

        var result = Assert.Single(document!.Projects);
        Assert.Equal("Targeted summary", result.Summary);
        Assert.Equal(["Included result"], result.Highlights);
    }

    [Fact]
    public async Task Snapshot_does_not_leak_unselected_experience_highlights()
    {
        var options = new DbContextOptionsBuilder<ResumeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ResumeDbContext(options);
        var person = new Person { Translations = [new() { Locale = Locale.En, FullName = "TEST PERSON" }, new() { Locale = Locale.Ar, FullName = "شخص للاختبار" }] };
        var profile = new ResumeProfileBuilder(person.Id).Build();
        var experience = new Experience { PersonId = person.Id, StartDate = new DateOnly(2025, 1, 1), IsCurrent = true, Translations = [new() { Locale = Locale.En, Organization = "TEST ORG", JobTitle = "TEST ROLE" }, new() { Locale = Locale.Ar, Organization = "جهة اختبار", JobTitle = "دور اختباري" }] };
        var selectedHighlight = new ExperienceHighlight { ExperienceId = experience.Id, Translations = [new() { Locale = Locale.En, Text = "Selected fact" }, new() { Locale = Locale.Ar, Text = "حقيقة مختارة" }] };
        var hiddenHighlight = new ExperienceHighlight { ExperienceId = experience.Id, SortOrder = 1, Translations = [new() { Locale = Locale.En, Text = "Hidden fact" }, new() { Locale = Locale.Ar, Text = "حقيقة مخفية" }] };
        experience.Highlights.AddRange([selectedHighlight, hiddenHighlight]);
        var selection = new ProfileExperience { ProfileId = profile.Id, ExperienceId = experience.Id };
        db.AddRange(person, profile, experience, selection, new ProfileExperienceHighlight { ProfileExperienceId = selection.Id, HighlightId = selectedHighlight.Id });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var document = await new SnapshotService(db).BuildResumeAsync(person.Id, profile.Id, Locale.En, false, TestContext.Current.CancellationToken);

        Assert.Equal(["Selected fact"], Assert.Single(document!.Experiences).Highlights);
    }

    [Fact]
    public async Task Snapshot_contains_only_selected_supporting_content_and_applies_contact_policy()
    {
        var options = new DbContextOptionsBuilder<ResumeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ResumeDbContext(options);
        var person = new Person { Email = "test@example.invalid", Phone = "+000000", Translations = [new() { Locale = Locale.En, FullName = "TEST PERSON" }, new() { Locale = Locale.Ar, FullName = "شخص للاختبار" }] };
        var profile = new ResumeProfile { PersonId = person.Id, Slug = "test-profile", InternalName = "TEST PROFILE", Translations = [new() { Locale = Locale.En, Headline = "Software Engineer | Flutter & .NET Backend", Summary = "Test summary" }, new() { Locale = Locale.Ar, Headline = "مهندس برمجيات", Summary = "ملخص اختباري" }], PdfSettings = [new() { Locale = Locale.En }, new() { Locale = Locale.Ar }] };
        var experience = new Experience { PersonId = person.Id, StartDate = new DateOnly(2024, 1, 1), IsCurrent = true, Translations = [new() { Locale = Locale.En, Organization = "TEST ORG", JobTitle = "TEST ROLE" }, new() { Locale = Locale.Ar, Organization = "جهة اختبار", JobTitle = "دور اختباري" }] };
        var education = new Education { PersonId = person.Id, Translations = [new() { Locale = Locale.En, Institution = "TEST SCHOOL", Degree = "TEST DEGREE" }, new() { Locale = Locale.Ar, Institution = "جهة تعليم اختبارية", Degree = "درجة اختبارية" }] };
        var certification = new Certification { PersonId = person.Id, Translations = [new() { Locale = Locale.En, Name = "TEST CERTIFICATE", Issuer = "TEST ISSUER" }, new() { Locale = Locale.Ar, Name = "شهادة اختبارية", Issuer = "جهة اختبارية" }] };
        var language = new SpokenLanguage { PersonId = person.Id, LanguageCode = "en", Proficiency = "test-only" };
        db.AddRange(person, profile, experience, education, certification, language);
        db.AddRange(new ProfileExperience { ProfileId = profile.Id, ExperienceId = experience.Id }, new ProfileEducation { ProfileId = profile.Id, EducationId = education.Id }, new ProfileCertification { ProfileId = profile.Id, CertificationId = certification.Id }, new ProfileLanguage { ProfileId = profile.Id, SpokenLanguageId = language.Id }, new ProfileContactSetting { ProfileId = profile.Id, WebEmail = true, WebPhone = false, PdfEmail = false, PdfPhone = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new SnapshotService(db);
        var web = await service.BuildResumeAsync(person.Id, profile.Id, Locale.En, false, TestContext.Current.CancellationToken);
        var pdf = await service.BuildResumeAsync(person.Id, profile.Id, Locale.En, true, TestContext.Current.CancellationToken);

        Assert.NotNull(web); Assert.Single(web.Experiences); Assert.Single(web.Educations); Assert.Single(web.Certifications); Assert.Single(web.Languages);
        Assert.Equal("test@example.invalid", web.Email); Assert.Null(web.Phone);
        Assert.Null(pdf!.Email); Assert.Equal("+000000", pdf.Phone);
    }
}

file sealed class ResumeProfileBuilder
{
    private readonly Guid personId;
    public ResumeProfileBuilder(Guid personId) => this.personId = personId;
    public ResumeProfile Build() => new()
    {
        PersonId = personId,
        Slug = "experience-test",
        InternalName = "EXPERIENCE TEST",
        Translations = [new() { Locale = Locale.En, Headline = "Software Engineer | Flutter & .NET Backend", Summary = "Test summary" }, new() { Locale = Locale.Ar, Headline = "مهندس برمجيات", Summary = "ملخص اختباري" }]
    };
}
