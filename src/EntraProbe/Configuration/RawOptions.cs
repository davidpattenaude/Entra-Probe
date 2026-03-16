namespace EntraProbe.Configuration;

public sealed record RawOptions
{
    public string? TenantId { get; init; }

    public string? ClientId { get; init; }

    public string? Property { get; init; }

    public bool Verbose { get; init; }

    public bool ShowHelp { get; init; }

    public string? ParseError { get; init; }

    public EffectiveOptions ToEffectiveOptions()
    {
        var properties = OutputPropertyParser.TryParse(Property, out var parsedProperties)
            ? parsedProperties
            : [OutputProperty.Department];

        return new EffectiveOptions(
            TenantId,
            ClientId,
            properties,
            Verbose,
            ShowHelp,
            Property,
            ParseError);
    }
}
