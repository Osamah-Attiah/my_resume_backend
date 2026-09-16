using System.Text.Json;
using Resume.Application;

namespace Resume.Tests;

public sealed class SnapshotLastModifiedTests
{
    private static readonly DateOnly Today = new(2026, 9, 13);

    [Fact]
    public void Keeps_the_previous_date_when_public_content_is_unchanged()
    {
        var core = new { schemaVersion = 1, baseUrl = "https://example.invalid", profiles = new[] { "demo" } };
        var previous = JsonSerializer.Serialize(new { schemaVersion = 1, baseUrl = "https://example.invalid", lastModified = new DateOnly(2025, 2, 3), profiles = new[] { "demo" } }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(new DateOnly(2025, 2, 3), SnapshotLastModified.Resolve(previous, core, Today));
    }

    [Fact]
    public void Uses_today_when_public_content_changes_or_the_previous_snapshot_is_invalid()
    {
        var core = new { schemaVersion = 1, baseUrl = "https://example.invalid", profiles = new[] { "changed" } };
        var previous = JsonSerializer.Serialize(new { schemaVersion = 1, baseUrl = "https://example.invalid", lastModified = new DateOnly(2025, 2, 3), profiles = new[] { "demo" } }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(Today, SnapshotLastModified.Resolve(previous, core, Today));
        Assert.Equal(Today, SnapshotLastModified.Resolve("not-json", core, Today));
    }
}
