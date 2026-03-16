namespace EntraProbe.Configuration;

public static class OutputPropertyParser
{
    public static bool TryParse(string? value, out OutputProperty[] properties)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            properties = [OutputProperty.Department];
            return false;
        }

        var parsedProperties = new List<OutputProperty>();
        var tokens = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (!TryParseSingle(token, out var property))
            {
                properties = [OutputProperty.Department];
                return false;
            }

            if (!parsedProperties.Contains(property))
            {
                parsedProperties.Add(property);
            }
        }

        if (parsedProperties.Count == 0)
        {
            properties = [OutputProperty.Department];
            return false;
        }

        properties = [.. parsedProperties];
        return true;
    }

    private static bool TryParseSingle(string value, out OutputProperty property)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "department":
                property = OutputProperty.Department;
                return true;
            case "dn":
            case "distinguishedname":
            case "onpremisesdistinguishedname":
            case "on-premises-distinguished-name":
                property = OutputProperty.Dn;
                return true;
            default:
                property = OutputProperty.Department;
                return false;
        }
    }
}
