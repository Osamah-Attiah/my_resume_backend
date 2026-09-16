using Resume.Application;

namespace Resume.Tests;

public sealed class DomainValidationTests
{
    [Theory]
    [InlineData("flutter-engineer")]
    [InlineData("profile-2")]
    public void Valid_latin_slugs_are_accepted(string slug) => Assert.True(DomainValidation.IsSlug(slug));

    [Theory]
    [InlineData("Flutter Engineer")]
    [InlineData("سيرة")]
    [InlineData("../admin")]
    [InlineData("")]
    public void Unsafe_slugs_are_rejected(string slug) => Assert.False(DomainValidation.IsSlug(slug));

    [Fact]
    public void Current_date_ranges_require_an_empty_end_date() => Assert.False(DomainValidation.IsValidDateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 2, 1), true));

    [Fact]
    public void End_date_cannot_precede_start_date() => Assert.False(DomainValidation.IsValidDateRange(new DateOnly(2025, 2, 1), new DateOnly(2025, 1, 1), false));

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("http://example.com", false)]
    public void Only_https_urls_are_accepted(string value, bool expected) => Assert.Equal(expected, DomainValidation.IsHttpsUrl(value));
}

