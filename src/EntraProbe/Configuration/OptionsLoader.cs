using Microsoft.Extensions.Configuration;

namespace EntraProbe.Configuration;

public static class OptionsLoader
{
    public static EffectiveOptions Load(string[] args)
    {
        var appSettings = LoadAppSettings();
        var environment = ReadEnvironment();
        return Load(args, appSettings, environment);
    }

    public static EffectiveOptions Load(
        string[] args,
        RawOptions? appSettings,
        IReadOnlyDictionary<string, string?>? environment)
    {
        var defaults = appSettings ?? new RawOptions();
        var commandLine = ParseArguments(args);
        var environmentValues = environment ?? EmptyEnvironment;

        // Keep wrapper behavior predictable: the most explicit call-site input wins.
        var merged = new RawOptions
        {
            TenantId = FirstNonEmpty(commandLine.TenantId, GetEnvironment(environmentValues, "ENTRA_TENANT_ID"), defaults.TenantId),
            ClientId = FirstNonEmpty(commandLine.ClientId, GetEnvironment(environmentValues, "ENTRA_CLIENT_ID"), defaults.ClientId),
            Property = FirstNonEmpty(commandLine.Property, GetEnvironment(environmentValues, "ENTRA_PROPERTY"), defaults.Property),
            Verbose = commandLine.Verbose || (GetBooleanEnvironment(environmentValues, "ENTRA_VERBOSE") ?? defaults.Verbose),
            ShowHelp = commandLine.ShowHelp || (GetBooleanEnvironment(environmentValues, "ENTRA_HELP") ?? defaults.ShowHelp),
            ParseError = commandLine.ParseError
        };

        return merged.ToEffectiveOptions();
    }

    private static readonly IReadOnlyDictionary<string, string?> EmptyEnvironment =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    private static RawOptions LoadAppSettings()
    {
        // Use the executable directory rather than the current working directory so wrappers can launch the tool from anywhere.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        return configuration.GetSection("Entra").Get<RawOptions>() ?? new RawOptions();
    }

    private static RawOptions ParseArguments(string[] args)
    {
        string? tenantId = null;
        string? clientId = null;
        string? property = null;
        var verbose = false;
        var showHelp = false;
        string? parseError = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--tenant-id":
                    if (!TryReadOptionValue(args, ref i, "--tenant-id", out tenantId, out parseError))
                    {
                        return new RawOptions
                        {
                            TenantId = tenantId,
                            ClientId = clientId,
                            Property = property,
                            Verbose = verbose,
                            ShowHelp = showHelp,
                            ParseError = parseError
                        };
                    }

                    break;
                case "--client-id":
                    if (!TryReadOptionValue(args, ref i, "--client-id", out clientId, out parseError))
                    {
                        return new RawOptions
                        {
                            TenantId = tenantId,
                            ClientId = clientId,
                            Property = property,
                            Verbose = verbose,
                            ShowHelp = showHelp,
                            ParseError = parseError
                        };
                    }

                    break;
                case "--property":
                    if (!TryReadOptionValue(args, ref i, "--property", out property, out parseError))
                    {
                        return new RawOptions
                        {
                            TenantId = tenantId,
                            ClientId = clientId,
                            Property = property,
                            Verbose = verbose,
                            ShowHelp = showHelp,
                            ParseError = parseError
                        };
                    }

                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--help":
                case "-h":
                case "/?":
                    showHelp = true;
                    break;
                default:
                    parseError = $"Unknown argument: {args[i]}";
                    return new RawOptions
                    {
                        TenantId = tenantId,
                        ClientId = clientId,
                        Property = property,
                        Verbose = verbose,
                        ShowHelp = showHelp,
                        ParseError = parseError
                    };
            }
        }

        return new RawOptions
        {
            TenantId = tenantId,
            ClientId = clientId,
            Property = property,
            Verbose = verbose,
            ShowHelp = showHelp,
            ParseError = parseError
        };
    }

    private static bool TryReadOptionValue(
        string[] args,
        ref int index,
        string optionName,
        out string? value,
        out string? parseError)
    {
        value = null;
        parseError = null;

        if (index + 1 >= args.Length || LooksLikeSwitch(args[index + 1]))
        {
            parseError = $"Missing value for {optionName}.";
            return false;
        }

        value = args[++index];
        return true;
    }

    private static bool LooksLikeSwitch(string value)
    {
        return value.StartsWith("-", StringComparison.Ordinal) || value.StartsWith("/", StringComparison.Ordinal);
    }

    private static Dictionary<string, string?> ReadEnvironment()
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ENTRA_TENANT_ID"] = Environment.GetEnvironmentVariable("ENTRA_TENANT_ID"),
            ["ENTRA_CLIENT_ID"] = Environment.GetEnvironmentVariable("ENTRA_CLIENT_ID"),
            ["ENTRA_PROPERTY"] = Environment.GetEnvironmentVariable("ENTRA_PROPERTY"),
            ["ENTRA_VERBOSE"] = Environment.GetEnvironmentVariable("ENTRA_VERBOSE"),
            ["ENTRA_HELP"] = Environment.GetEnvironmentVariable("ENTRA_HELP")
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? GetEnvironment(IReadOnlyDictionary<string, string?> environment, string key)
    {
        return environment.TryGetValue(key, out var value) ? value : null;
    }

    private static bool? GetBooleanEnvironment(IReadOnlyDictionary<string, string?> environment, string key)
    {
        if (!environment.TryGetValue(key, out var value))
        {
            return null;
        }

        return bool.TryParse(value, out var parsed) ? parsed : null;
    }
}
