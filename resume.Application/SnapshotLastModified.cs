using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;

namespace Resume.Application;

public static class SnapshotLastModified
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static DateOnly Resolve(string? previousSnapshot, object payloadCore, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(previousSnapshot)) return today;

        try
        {
            var previous = JsonNode.Parse(previousSnapshot)?.AsObject();
            var previousDateText = previous?["lastModified"]?.GetValue<string>();
            previous?.Remove("lastModified");
            var current = JsonSerializer.SerializeToNode(payloadCore, JsonOptions);
            if (previous is not null && current is not null && JsonNode.DeepEquals(Canonicalize(previous), Canonicalize(current)) && DateOnly.TryParseExact(previousDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var previousDate)) return previousDate;
        }
        catch (JsonException)
        {
            // A legacy or corrupt snapshot must never block a fresh publication.
        }

        return today;
    }

    private static JsonNode? Canonicalize(JsonNode? node) => node switch
    {
        JsonObject value => new JsonObject(value.OrderBy(x => x.Key).Select(x => KeyValuePair.Create(x.Key, Canonicalize(x.Value)))),
        JsonArray value => new JsonArray(value.Select(Canonicalize).ToArray()),
        null => null,
        _ => node.DeepClone()
    };
}
