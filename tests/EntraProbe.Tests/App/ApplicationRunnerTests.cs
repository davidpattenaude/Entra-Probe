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
        var console = new RecordingConsole();
        var options = TestOptions.Create();
        var runner = TestApplicationRunner.Create(
            console,
            formatter: new FakeProfileOutputFormatter { Selection = ProfileOutputSelection.Present("Finance") });
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.Success, exitCode);
        Assert.AreEqual("Finance", console.StandardOutput);
        Assert.AreEqual(string.Empty, console.StandardError);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsDepartmentMissingWhenGraphReturnsNull()
    {
        var console = new RecordingConsole();
        var options = TestOptions.Create(verbose: true);
        var runner = TestApplicationRunner.Create(
            console,
            formatter: new FakeProfileOutputFormatter
            {
                Selection = ProfileOutputSelection.Missing("Department was not present on the signed-in user's profile.")
            });
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
        var runner = TestApplicationRunner.Create(new RecordingConsole(), contextDetector: contextDetector);

        var exitCode = await runner.RunAsync(
            TestOptions.Create(),
            CancellationToken.None);

        Assert.AreEqual(ExitCode.UnsupportedExecutionContext, exitCode);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsAuthenticationFailureWhenAuthThrows()
    {
        var authService = new FakeAuthenticationService
        {
            ExceptionToThrow = new AuthenticationException("Authentication failed.")
        };
        var console = new RecordingConsole();
        var options = TestOptions.Create();
        var runner = TestApplicationRunner.Create(console, authService: authService);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.AuthenticationFailure, exitCode);
        StringAssert.Contains(console.StandardError, "Authentication failed.");
    }

    [TestMethod]
    public async Task RunAsync_ReturnsGraphFailureWhenGraphThrows()
    {
        var graphService = new FakeGraphProfileService
        {
            ExceptionToThrow = new GraphQueryException("Microsoft Graph query failed.")
        };
        var console = new RecordingConsole();
        var options = TestOptions.Create();
        var runner = TestApplicationRunner.Create(console, graphService: graphService);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.GraphFailure, exitCode);
        StringAssert.Contains(console.StandardError, "Microsoft Graph query failed.");
    }

    [TestMethod]
    public async Task RunAsync_ReturnsFriendlyErrorWhenNetworkIsUnavailable()
    {
        var authService = new FakeAuthenticationService
        {
            ExceptionToThrow = new AuthenticationException("Authentication failed: network unavailable.")
        };
        var console = new RecordingConsole();
        var options = TestOptions.Create();
        var runner = TestApplicationRunner.Create(console, authService: authService);
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.AuthenticationFailure, exitCode);
        StringAssert.Contains(console.StandardError, "network unavailable");
        Assert.AreEqual(string.Empty, console.StandardOutput);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsInvalidConfigurationWhenOptionsAreMissing()
    {
        var console = new RecordingConsole();
        var runner = TestApplicationRunner.Create(console);
        var exitCode = await runner.RunAsync(new EffectiveOptions(null, null, [OutputProperty.Department], false, false), CancellationToken.None);

        Assert.AreEqual(ExitCode.InvalidConfiguration, exitCode);
        StringAssert.Contains(console.StandardError, "Missing required tenant ID");
    }

    [TestMethod]
    public async Task RunAsync_ReturnsDnWhenExplicitlyRequested()
    {
        var console = new RecordingConsole();
        var options = TestOptions.Create(properties: [OutputProperty.Dn]);
        var runner = TestApplicationRunner.Create(
            console,
            formatter: new FakeProfileOutputFormatter { Selection = ProfileOutputSelection.Present("OU=Users,DC=contoso,DC=com") });
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.Success, exitCode);
        Assert.AreEqual("OU=Users,DC=contoso,DC=com", console.StandardOutput);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsDepartmentMissingWhenDnRequestedButUnavailable()
    {
        var console = new RecordingConsole();
        var options = TestOptions.Create(properties: [OutputProperty.Dn], verbose: true);
        var runner = TestApplicationRunner.Create(
            console,
            formatter: new FakeProfileOutputFormatter
            {
                Selection = ProfileOutputSelection.Missing("On-premises distinguished name was not present on the signed-in user's profile.")
            });
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.DepartmentMissing, exitCode);
        StringAssert.Contains(console.StandardError, "On-premises distinguished name was not present");
    }

    [TestMethod]
    public async Task RunAsync_WritesNamedValuesForMultiPropertyRequests()
    {
        var console = new RecordingConsole();
        var options = TestOptions.Create(properties: [OutputProperty.Department, OutputProperty.Dn]);
        var runner = TestApplicationRunner.Create(
            console,
            formatter: new FakeProfileOutputFormatter
            {
                Selection = ProfileOutputSelection.Present($"department=Finance{Environment.NewLine}dn=OU=Users,DC=contoso,DC=com")
            });
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.Success, exitCode);
        Assert.AreEqual($"department=Finance{Environment.NewLine}dn=OU=Users,DC=contoso,DC=com", console.StandardOutput);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsMissingExitCodeButPreservesPartialOutputForMultiPropertyRequests()
    {
        var console = new RecordingConsole();
        var options = TestOptions.Create(properties: [OutputProperty.Department, OutputProperty.Dn], verbose: true);
        var runner = TestApplicationRunner.Create(
            console,
            formatter: new FakeProfileOutputFormatter
            {
                Selection = ProfileOutputSelection.Partial($"department=Finance{Environment.NewLine}dn=", "On-premises distinguished name was not present on the signed-in user's profile.")
            });
        var exitCode = await runner.RunAsync(options, CancellationToken.None);

        Assert.AreEqual(ExitCode.DepartmentMissing, exitCode);
        Assert.AreEqual($"department=Finance{Environment.NewLine}dn=", console.StandardOutput);
        StringAssert.Contains(console.StandardError, "On-premises distinguished name was not present");
    }
}
