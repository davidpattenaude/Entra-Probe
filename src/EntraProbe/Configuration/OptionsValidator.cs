namespace EntraProbe.Configuration;

public static class OptionsValidator
{
    public static OptionsValidationResult Validate(EffectiveOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ParseError))
        {
            return OptionsValidationResult.Invalid(options.ParseError);
        }

        if (string.IsNullOrWhiteSpace(options.TenantId))
        {
            return OptionsValidationResult.Invalid("Missing required tenant ID. Provide --tenant-id, ENTRA_TENANT_ID, or appsettings.json.");
        }

        if (!Guid.TryParse(options.TenantId, out _))
        {
            return OptionsValidationResult.Invalid("Tenant ID must be a valid GUID.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            return OptionsValidationResult.Invalid("Missing required client ID. Provide --client-id, ENTRA_CLIENT_ID, or appsettings.json.");
        }

        if (!Guid.TryParse(options.ClientId, out _))
        {
            return OptionsValidationResult.Invalid("Client ID must be a valid GUID.");
        }

        if (options.Properties.Count == 0)
        {
            return OptionsValidationResult.Invalid("Property must include at least one value.");
        }

        if (!string.IsNullOrWhiteSpace(options.RequestedPropertyValue)
            && !OutputPropertyParser.TryParse(options.RequestedPropertyValue, out _))
        {
            return OptionsValidationResult.Invalid("Property must be `department`, `dn`, or a comma-separated list such as `department,dn`.");
        }

        return OptionsValidationResult.Valid();
    }
}
