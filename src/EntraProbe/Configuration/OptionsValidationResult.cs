namespace EntraProbe.Configuration;

public sealed record OptionsValidationResult(bool IsValid, string? ErrorMessage)
{
    public static OptionsValidationResult Valid() => new(true, null);

    public static OptionsValidationResult Invalid(string message) => new(false, message);
}
