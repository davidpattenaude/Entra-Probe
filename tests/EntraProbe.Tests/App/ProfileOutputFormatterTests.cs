using EntraProbe.App;
using EntraProbe.Configuration;
using EntraProbe.Graph;

namespace EntraProbe.Tests.App;

[TestClass]
public sealed class ProfileOutputFormatterTests
{
    private readonly ProfileOutputFormatter _formatter = new();

    [TestMethod]
    public void Select_ReturnsMissingWhenBothRequestedAndBothValuesAreEmpty()
    {
        var selection = _formatter.Select(
            new EffectiveOptions("tenant", "client", new[] { OutputProperty.Department, OutputProperty.Dn }, false, false),
            new SignedInUserProfile(null, null));

        Assert.IsFalse(selection.HasValue);
        StringAssert.Contains(selection.MissingMessage, "Department was not present");
        StringAssert.Contains(selection.MissingMessage, "On-premises distinguished name was not present");
    }

    [TestMethod]
    public void Select_ReturnsLineSeparatedValueWhenOneRequestedFieldIsMissing()
    {
        var selection = _formatter.Select(
            new EffectiveOptions("tenant", "client", new[] { OutputProperty.Department, OutputProperty.Dn }, false, false),
            new SignedInUserProfile("Finance", null));

        Assert.IsTrue(selection.HasValue);
        Assert.AreEqual($"department=Finance{Environment.NewLine}dn=", selection.Value);
        Assert.IsTrue(selection.HasMissingValues);
    }

    [TestMethod]
    public void Select_ReturnsNamedValuesWhenMultiplePropertiesAreRequested()
    {
        var selection = _formatter.Select(
            new EffectiveOptions("tenant", "client", new[] { OutputProperty.Department, OutputProperty.Dn }, false, false),
            new SignedInUserProfile("Finance", "OU=Users,DC=contoso,DC=com"));

        Assert.IsTrue(selection.HasValue);
        Assert.AreEqual($"department=Finance{Environment.NewLine}dn=OU=Users,DC=contoso,DC=com", selection.Value);
        Assert.IsFalse(selection.HasMissingValues);
    }

    [TestMethod]
    public void Select_ReturnsExactDnValueWithoutAdditionalFormatting()
    {
        var selection = _formatter.Select(
            new EffectiveOptions("tenant", "client", new[] { OutputProperty.Dn }, false, false),
            new SignedInUserProfile("Finance", "OU=Users,DC=contoso,DC=com"));

        Assert.IsTrue(selection.HasValue);
        Assert.AreEqual("OU=Users,DC=contoso,DC=com", selection.Value);
    }
}
