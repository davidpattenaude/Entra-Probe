namespace EntraProbe.App;

public sealed record ProfileOutputSelection(
    bool HasValue,
    string? Value,
    bool HasMissingValues,
    string MissingMessage)
{
    public static ProfileOutputSelection Present(string value) => new(true, value, false, string.Empty);

    public static ProfileOutputSelection Partial(string value, string missingMessage) => new(true, value, true, missingMessage);

    public static ProfileOutputSelection Missing(string missingMessage) => new(false, null, true, missingMessage);
}
