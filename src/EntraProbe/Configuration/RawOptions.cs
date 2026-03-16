namespace EntraProbe.Configuration;

public sealed record RawOptions
{
    public string? TenantId { get; init; }

    public string? ClientId { get; init; }

    public string? Property { get; init; }

    public bool Verbose { get; init; }

    public bool ShowHelp { get; init; }

    public string? ParseError { get; init; }
}
