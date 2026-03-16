namespace EntraProbe.Configuration;

public sealed record EffectiveOptions(
    string? TenantId,
    string? ClientId,
    IReadOnlyList<OutputProperty> Properties,
    bool Verbose,
    bool ShowHelp,
    string? RequestedPropertyValue = null,
    string? ParseError = null)
{
    public static EffectiveOptions FromRawOptions(RawOptions rawOptions)
    {
        var properties = OutputPropertyParser.TryParse(rawOptions.Property, out var parsedProperties)
            ? parsedProperties
            : [OutputProperty.Department];

        return new EffectiveOptions(
            rawOptions.TenantId,
            rawOptions.ClientId,
            properties,
            rawOptions.Verbose,
            rawOptions.ShowHelp,
            rawOptions.Property,
            rawOptions.ParseError);
    }
}
