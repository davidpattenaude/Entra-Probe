using EntraProbe.App;
using EntraProbe.Configuration;
using EntraProbe.Execution;
using EntraProbe.Graph;
using EntraProbe.Identity;

namespace EntraProbe.Tests.App;

[TestClass]
public sealed class ApplicationRunnerTests
{
    [TestMethod]
    public async Task RunAsync_WritesDepartmentAndReturnsSuccess()
    {
        var contextDetector = new FakeExecutionContextDetector();
        var authService = new FakeAuthenticationService();
        var graphService = new FakeGraphProfileService();
        var console = new RecordingConsole();
        var formatter = new FakeProfileOutputFormatter { Selection = ProfileOutputSelection.Present("Finance") };
        var options = new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Department }, false, false);

        var runner = new ApplicationRunner(contextDetector, authService, graphService, formatter, console);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.Success, exitCode);
        Assert.AreEqual("Finance", console.StandardOutput);
        Assert.AreEqual(string.Empty, console.StandardError);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsDepartmentMissingWhenGraphReturnsNull()
    {
        var contextDetector = new FakeExecutionContextDetector();
        var authService = new FakeAuthenticationService();
        var graphService = new FakeGraphProfileService();
        var console = new RecordingConsole();
        var formatter = new FakeProfileOutputFormatter
        {
            Selection = ProfileOutputSelection.Missing("Department was not present on the signed-in user's profile.")
        };
        var options = new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Department }, true, false);

        var runner = new ApplicationRunner(contextDetector, authService, graphService, formatter, console);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.DepartmentMissing, exitCode);
        Assert.AreEqual(string.Empty, console.StandardOutput);
        StringAssert.Contains(console.StandardError, "Department was not present");
    }

    [TestMethod]
    public async Task RunAsync_ReturnsUnsupportedExecutionContextWhenContextIsInvalid()
    {
        var contextDetector = new FakeExecutionContextDetector
        {
            Context = new ExecutionContextInfo(RuntimePlatform.Windows, false, true, false, false, "This tool must run in an interactive user session.")
        };
        var runner = new ApplicationRunner(
            contextDetector,
            new FakeAuthenticationService(),
            new FakeGraphProfileService(),
            new ProfileOutputFormatter(),
            new RecordingConsole());

        var exitCode = await runner.RunAsync(
            new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Department }, false, false),
            CancellationToken.None);

        Assert.AreEqual(ExitCode.UnsupportedExecutionContext, exitCode);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsAuthenticationFailureWhenAuthThrows()
    {
        var contextDetector = new FakeExecutionContextDetector();
        var authService = new FakeAuthenticationService
        {
            ExceptionToThrow = new AuthenticationException("Authentication failed.")
        };
        var console = new RecordingConsole();
        var options = new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Department }, false, false);

        var runner = new ApplicationRunner(contextDetector, authService, new FakeGraphProfileService(), new ProfileOutputFormatter(), console);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.AuthenticationFailure, exitCode);
        StringAssert.Contains(console.StandardError, "Authentication failed.");
    }

    [TestMethod]
    public async Task RunAsync_ReturnsGraphFailureWhenGraphThrows()
    {
        var contextDetector = new FakeExecutionContextDetector();
        var authService = new FakeAuthenticationService();
        var graphService = new FakeGraphProfileService
        {
            ExceptionToThrow = new GraphQueryException("Microsoft Graph query failed.")
        };
        var console = new RecordingConsole();
        var options = new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Department }, false, false);

        var runner = new ApplicationRunner(contextDetector, authService, graphService, new ProfileOutputFormatter(), console);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.GraphFailure, exitCode);
        StringAssert.Contains(console.StandardError, "Microsoft Graph query failed.");
    }

    [TestMethod]
    public async Task RunAsync_ReturnsFriendlyErrorWhenNetworkIsUnavailable()
    {
        var contextDetector = new FakeExecutionContextDetector();
        var authService = new FakeAuthenticationService
        {
            ExceptionToThrow = new AuthenticationException("Authentication failed: network unavailable.")
        };
        var console = new RecordingConsole();
        var options = new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Department }, false, false);

        var runner = new ApplicationRunner(contextDetector, authService, new FakeGraphProfileService(), new ProfileOutputFormatter(), console);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.AuthenticationFailure, exitCode);
        StringAssert.Contains(console.StandardError, "network unavailable");
        Assert.AreEqual(string.Empty, console.StandardOutput);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsInvalidConfigurationWhenOptionsAreMissing()
    {
        var console = new RecordingConsole();
        var runner = new ApplicationRunner(
            new FakeExecutionContextDetector(),
            new FakeAuthenticationService(),
            new FakeGraphProfileService(),
            new ProfileOutputFormatter(),
            console);

        var exitCode = await runner.RunAsync(new EffectiveOptions(null, null, new[] { OutputProperty.Department }, false, false), CancellationToken.None);

        Assert.AreEqual(ExitCode.InvalidConfiguration, exitCode);
        StringAssert.Contains(console.StandardError, "Missing required tenant ID");
    }

    [TestMethod]
    public async Task RunAsync_ReturnsDnWhenExplicitlyRequested()
    {
        var contextDetector = new FakeExecutionContextDetector();
        var authService = new FakeAuthenticationService();
        var graphService = new FakeGraphProfileService();
        var console = new RecordingConsole();
        var formatter = new FakeProfileOutputFormatter { Selection = ProfileOutputSelection.Present("OU=Users,DC=contoso,DC=com") };
        var options = new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Dn }, false, false);

        var runner = new ApplicationRunner(contextDetector, authService, graphService, formatter, console);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.Success, exitCode);
        Assert.AreEqual("OU=Users,DC=contoso,DC=com", console.StandardOutput);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsDepartmentMissingWhenDnRequestedButUnavailable()
    {
        var contextDetector = new FakeExecutionContextDetector();
        var authService = new FakeAuthenticationService();
        var graphService = new FakeGraphProfileService();
        var console = new RecordingConsole();
        var formatter = new FakeProfileOutputFormatter
        {
            Selection = ProfileOutputSelection.Missing("On-premises distinguished name was not present on the signed-in user's profile.")
        };
        var options = new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Dn }, true, false);

        var runner = new ApplicationRunner(contextDetector, authService, graphService, formatter, console);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.DepartmentMissing, exitCode);
        StringAssert.Contains(console.StandardError, "On-premises distinguished name was not present");
    }

    [TestMethod]
    public async Task RunAsync_WritesNamedValuesForMultiPropertyRequests()
    {
        var contextDetector = new FakeExecutionContextDetector();
        var authService = new FakeAuthenticationService();
        var graphService = new FakeGraphProfileService();
        var console = new RecordingConsole();
        var formatter = new FakeProfileOutputFormatter
        {
            Selection = ProfileOutputSelection.Present($"department=Finance{Environment.NewLine}dn=OU=Users,DC=contoso,DC=com")
        };
        var options = new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Department, OutputProperty.Dn }, false, false);

        var runner = new ApplicationRunner(contextDetector, authService, graphService, formatter, console);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.Success, exitCode);
        Assert.AreEqual($"department=Finance{Environment.NewLine}dn=OU=Users,DC=contoso,DC=com", console.StandardOutput);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsMissingExitCodeButPreservesPartialOutputForMultiPropertyRequests()
    {
        var contextDetector = new FakeExecutionContextDetector();
        var authService = new FakeAuthenticationService();
        var graphService = new FakeGraphProfileService();
        var console = new RecordingConsole();
        var formatter = new FakeProfileOutputFormatter
        {
            Selection = ProfileOutputSelection.Partial($"department=Finance{Environment.NewLine}dn=", "On-premises distinguished name was not present on the signed-in user's profile.")
        };
        var options = new EffectiveOptions(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), new[] { OutputProperty.Department, OutputProperty.Dn }, true, false);

        var runner = new ApplicationRunner(contextDetector, authService, graphService, formatter, console);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.DepartmentMissing, exitCode);
        Assert.AreEqual($"department=Finance{Environment.NewLine}dn=", console.StandardOutput);
        StringAssert.Contains(console.StandardError, "On-premises distinguished name was not present");
    }
}
