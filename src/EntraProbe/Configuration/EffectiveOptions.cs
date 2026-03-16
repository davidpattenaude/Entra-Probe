namespace EntraProbe.Configuration;

public sealed record EffectiveOptions(
    string? TenantId,
    string? ClientId,
    IReadOnlyList<OutputProperty> Properties,
    bool Verbose,
    bool ShowHelp,
    string? RequestedPropertyValue = null,
    string? ParseError = null);
