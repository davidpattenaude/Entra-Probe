using EntraProbe.Configuration;
using EntraProbe.ConsoleSupport;
using EntraProbe.Execution;
using EntraProbe.Graph;
using EntraProbe.Identity;

namespace EntraProbe.App;

public sealed class ApplicationRunner
{
    private readonly IExecutionContextDetector _executionContextDetector;
    private readonly IAuthenticationService _authenticationService;
    private readonly IGraphProfileService _graphProfileService;
    private readonly IProfileOutputFormatter _profileOutputFormatter;
    private readonly IConsoleWriter _console;

    public ApplicationRunner(
        IExecutionContextDetector executionContextDetector,
        IAuthenticationService authenticationService,
        IGraphProfileService graphProfileService,
        IProfileOutputFormatter profileOutputFormatter,
        IConsoleWriter console)
    {
        _executionContextDetector = executionContextDetector;
        _authenticationService = authenticationService;
        _graphProfileService = graphProfileService;
        _profileOutputFormatter = profileOutputFormatter;
        _console = console;
    }

    public async Task<ExitCode> RunAsync(EffectiveOptions options, CancellationToken cancellationToken)
    {
        if (options.ShowHelp)
        {
            WriteHelp();
            return ExitCode.Success;
        }

        var validation = OptionsValidator.Validate(options);
        if (!validation.IsValid)
        {
            _console.WriteError(validation.ErrorMessage!);
            return ExitCode.InvalidConfiguration;
        }

        var context = _executionContextDetector.Detect();
        if (!context.IsSupported)
        {
            _console.WriteError(context.Message ?? "This tool must run in an interactive user session.");
            return ExitCode.UnsupportedExecutionContext;
        }

        try
        {
            var authResult = await _authenticationService.AcquireAccessTokenAsync(options, context, cancellationToken).ConfigureAwait(false);
            var profile = await _graphProfileService.GetProfileAsync(authResult.AccessToken, cancellationToken).ConfigureAwait(false);
            var outputSelection = _profileOutputFormatter.Select(options, profile);

            if (!outputSelection.HasValue)
            {
                _console.WriteVerbose(options.Verbose, outputSelection.MissingMessage);
                return ExitCode.DepartmentMissing;
            }

            _console.WriteOutput(outputSelection.Value!);
            if (outputSelection.HasMissingValues)
            {
                _console.WriteVerbose(options.Verbose, outputSelection.MissingMessage);
                return ExitCode.DepartmentMissing;
            }

            return ExitCode.Success;
        }
        catch (AuthenticationException ex)
        {
            _console.WriteError(ex.Message);
            return ExitCode.AuthenticationFailure;
        }
        catch (GraphQueryException ex)
        {
            _console.WriteError(ex.Message);
            return ExitCode.GraphFailure;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _console.WriteError($"Unexpected failure: {ex.Message}");
            return ExitCode.UnexpectedError;
        }
    }

    private void WriteHelp()
    {
        const string helpText = """
Usage:
  EntraProbe.exe --tenant-id <tenant> --client-id <client> [--property department|dn|department,dn] [--verbose]

Configuration precedence:
  1. Command-line arguments
  2. Environment variables: ENTRA_TENANT_ID, ENTRA_CLIENT_ID, ENTRA_PROPERTY, ENTRA_VERBOSE, ENTRA_HELP
  3. appsettings.json

Notes:
  Specify multiple fields in order as a comma-separated list such as department,dn.
""";

        _console.WriteError(helpText);
    }
}
