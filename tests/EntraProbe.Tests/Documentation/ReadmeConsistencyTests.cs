using EntraProbe.Identity;

namespace EntraProbe.Tests.Documentation;

[TestClass]
public sealed class ReadmeConsistencyTests
{
    [TestMethod]
    public void Readme_ContainsTheMacOsBrokerRedirectUriUsedByCode()
    {
        var readmePath = FindRepositoryFile("README.md");
        var readmeContent = File.ReadAllText(readmePath);

        StringAssert.Contains(readmeContent, BrokerRedirectUris.MacOsUnsignedBroker);
    }

    private static string FindRepositoryFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        Assert.Fail($"Could not find {fileName} from {AppContext.BaseDirectory}.");
        return string.Empty;
    }
}
