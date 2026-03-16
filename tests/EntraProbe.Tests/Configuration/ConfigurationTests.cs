using EntraProbe.Configuration;

namespace EntraProbe.Tests.Configuration;

[TestClass]
public sealed class ConfigurationTests : IDisposable
{
    [TestMethod]
    public void Load_RecognizesSupportedArguments()
    {
        var result = OptionsLoader.Load([
            "--tenant-id", "tenant",
            "--client-id", "client",
            "--property", "dn",
            "--verbose",
            "--help"
        ], new RawOptions(), new Dictionary<string, string?>());

        Assert.AreEqual("tenant", result.TenantId);
        Assert.AreEqual("client", result.ClientId);
        CollectionAssert.AreEqual(new[] { OutputProperty.Dn }, result.Properties.ToArray());
        Assert.IsTrue(result.Verbose);
        Assert.IsTrue(result.ShowHelp);
    }

    [TestMethod]
    public void Load_PrefersCommandLineOverEnvironmentAndSettings()
    {
        var environment = new Dictionary<string, string?>
        {
            ["ENTRA_TENANT_ID"] = "env-tenant",
            ["ENTRA_CLIENT_ID"] = "env-client"
        };

        var result = OptionsLoader.Load(
            ["--tenant-id", "cli-tenant", "--client-id", "cli-client", "--property", "dn"],
            new RawOptions { TenantId = "json-tenant", ClientId = "json-client", Property = "department" },
            environment);

        Assert.AreEqual("cli-tenant", result.TenantId);
        Assert.AreEqual("cli-client", result.ClientId);
        CollectionAssert.AreEqual(new[] { OutputProperty.Dn }, result.Properties.ToArray());
    }

    [TestMethod]
    public void Load_PrefersEnvironmentOverSettingsWhenCliMissing()
    {
        var environment = new Dictionary<string, string?>
        {
            ["ENTRA_TENANT_ID"] = "env-tenant",
            ["ENTRA_CLIENT_ID"] = "env-client",
            ["ENTRA_PROPERTY"] = "department,dn"
        };

        var result = OptionsLoader.Load(
            [],
            new RawOptions { TenantId = "json-tenant", ClientId = "json-client", Property = "department" },
            environment);

        Assert.AreEqual("env-tenant", result.TenantId);
        Assert.AreEqual("env-client", result.ClientId);
        CollectionAssert.AreEqual(new[] { OutputProperty.Department, OutputProperty.Dn }, result.Properties.ToArray());
    }

    [TestMethod]
    public void Validate_ReturnsInvalidForMalformedGuidValues()
    {
        var result = OptionsValidator.Validate(new EffectiveOptions("bad", "still-bad", new[] { OutputProperty.Department }, false, false));

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.ErrorMessage, "Tenant ID must be a valid GUID");
    }

    [TestMethod]
    public void Load_DefaultsToDepartmentWhenPropertyIsNotSpecified()
    {
        var result = OptionsLoader.Load(
            [],
            new RawOptions { TenantId = "json-tenant", ClientId = "json-client", Property = null },
            new Dictionary<string, string?>());

        CollectionAssert.AreEqual(new[] { OutputProperty.Department }, result.Properties.ToArray());
    }

    [TestMethod]
    public void Load_SupportsCommaSeparatedPropertyList()
    {
        var result = OptionsLoader.Load(
            ["--property", "department,dn"],
            new RawOptions { TenantId = "json-tenant", ClientId = "json-client", Property = null },
            new Dictionary<string, string?>());

        CollectionAssert.AreEqual(new[] { OutputProperty.Department, OutputProperty.Dn }, result.Properties.ToArray());
    }

    [TestMethod]
    public void Validate_ReturnsInvalidForUnsupportedPropertyValues()
    {
        var result = OptionsLoader.Load(
            ["--property", "unsupported"],
            new RawOptions { TenantId = Guid.NewGuid().ToString(), ClientId = Guid.NewGuid().ToString() },
            new Dictionary<string, string?>());

        var validation = OptionsValidator.Validate(result);

        Assert.IsFalse(validation.IsValid);
        Assert.AreEqual("Property must be `department`, `dn`, or a comma-separated list such as `department,dn`.", validation.ErrorMessage);
    }

    [TestMethod]
    public void Validate_ReturnsInvalidForMissingCliOptionValue()
    {
        var result = OptionsLoader.Load(
            ["--tenant-id"],
            new RawOptions { ClientId = Guid.NewGuid().ToString() },
            new Dictionary<string, string?>());

        var validation = OptionsValidator.Validate(result);

        Assert.IsFalse(validation.IsValid);
        Assert.AreEqual("Missing value for --tenant-id.", validation.ErrorMessage);
    }

    [TestMethod]
    public void Validate_ReturnsInvalidForUnknownCliArgument()
    {
        var result = OptionsLoader.Load(
            ["--tenant-id", Guid.NewGuid().ToString(), "--client-id", Guid.NewGuid().ToString(), "--unknown"],
            new RawOptions(),
            new Dictionary<string, string?>());

        var validation = OptionsValidator.Validate(result);

        Assert.IsFalse(validation.IsValid);
        Assert.AreEqual("Unknown argument: --unknown", validation.ErrorMessage);
    }

    [TestMethod]
    public void Load_PrefersBooleanEnvironmentFlagsOverSettings()
    {
        var result = OptionsLoader.Load(
            [],
            new RawOptions { Verbose = false, ShowHelp = false },
            new Dictionary<string, string?>
            {
                ["ENTRA_VERBOSE"] = "true",
                ["ENTRA_HELP"] = "true"
            });

        Assert.IsTrue(result.Verbose);
        Assert.IsTrue(result.ShowHelp);
    }

    [TestMethod]
    public void Load_AllowsBooleanEnvironmentFlagsToDisableSettingsDefaults()
    {
        var result = OptionsLoader.Load(
            [],
            new RawOptions { Verbose = true, ShowHelp = true },
            new Dictionary<string, string?>
            {
                ["ENTRA_VERBOSE"] = "false",
                ["ENTRA_HELP"] = "false"
            });

        Assert.IsFalse(result.Verbose);
        Assert.IsFalse(result.ShowHelp);
    }

    public void Dispose()
    {
    }
}
