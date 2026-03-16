using EntraProbe.Configuration;
using EntraProbe.Graph;

namespace EntraProbe.App;

public sealed class ProfileOutputFormatter : IProfileOutputFormatter
{
    public ProfileOutputSelection Select(EffectiveOptions options, SignedInUserProfile profile)
    {
        if (options.Properties.Count == 1)
        {
            return SelectSingle(options.Properties[0], profile);
        }

        var values = new List<string>(options.Properties.Count);
        var missingMessages = new List<string>();

        foreach (var property in options.Properties)
        {
            var selection = SelectSingle(property, profile);
            var propertyName = GetPropertyName(property);

            if (selection.HasValue)
            {
                values.Add($"{propertyName}={selection.Value!}");
            }
            else
            {
                values.Add($"{propertyName}=");
                missingMessages.Add(selection.MissingMessage);
            }
        }

        if (missingMessages.Count == values.Count)
        {
            return ProfileOutputSelection.Missing(string.Join(" ", missingMessages));
        }

        // Emit self-describing key=value lines for multi-property requests so PowerShell can parse them later without guessing field order.
        var combinedValue = string.Join(Environment.NewLine, values);
        return missingMessages.Count > 0
            ? ProfileOutputSelection.Partial(combinedValue, string.Join(" ", missingMessages))
            : ProfileOutputSelection.Present(combinedValue);
    }

    private static string GetPropertyName(OutputProperty property)
    {
        return property switch
        {
            OutputProperty.Department => "department",
            OutputProperty.Dn => "dn",
            _ => "value"
        };
    }

    private static ProfileOutputSelection SelectSingle(OutputProperty property, SignedInUserProfile profile)
    {
        return property switch
        {
            OutputProperty.Department => !string.IsNullOrWhiteSpace(profile.Department)
                ? ProfileOutputSelection.Present(profile.Department!)
                : ProfileOutputSelection.Missing("Department was not present on the signed-in user's profile."),
            OutputProperty.Dn => !string.IsNullOrWhiteSpace(profile.OnPremisesDistinguishedName)
                ? ProfileOutputSelection.Present(profile.OnPremisesDistinguishedName!)
                : ProfileOutputSelection.Missing("On-premises distinguished name was not present on the signed-in user's profile."),
            _ => ProfileOutputSelection.Missing("The requested property was not present on the signed-in user's profile.")
        };
    }
}
