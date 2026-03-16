using EntraProbe.App;
using EntraProbe.Configuration;
using EntraProbe.ConsoleSupport;
using EntraProbe.Execution;
using EntraProbe.Graph;
using EntraProbe.Identity;

var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var effectiveOptions = OptionsLoader.Load(args);
var console = new SystemConsole();

IExecutionContextDetector executionContextDetector = new PlatformExecutionContextDetector();
IPublicClientApplicationFactory publicClientApplicationFactory = new RuntimePlatformPublicClientApplicationFactory();
ISilentAccountResolver silentAccountResolver = new SilentAccountResolver();
IAuthenticationService authenticationService = new MsalAuthenticationService(publicClientApplicationFactory, silentAccountResolver);
IGraphProfileService graphProfileService = new GraphProfileService();
IProfileOutputFormatter profileOutputFormatter = new ProfileOutputFormatter();
var runner = new ApplicationRunner(executionContextDetector, authenticationService, graphProfileService, profileOutputFormatter, console);

try
{
    var exitCode = await runner.RunAsync(effectiveOptions, cancellation.Token).ConfigureAwait(false);
    return (int)exitCode;
}
catch (OperationCanceledException)
{
    console.WriteError("Operation canceled.");
    return (int)ExitCode.UnexpectedError;
}
catch (Exception ex)
{
    console.WriteError($"Unexpected failure: {ex.Message}");
    return (int)ExitCode.UnexpectedError;
}
