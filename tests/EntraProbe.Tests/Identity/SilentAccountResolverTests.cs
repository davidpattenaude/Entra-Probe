using EntraProbe.Execution;
using EntraProbe.Identity;
using Microsoft.Identity.Client;

namespace EntraProbe.Tests.Identity;

[TestClass]
public sealed class SilentAccountResolverTests
{
    [TestMethod]
    public void Resolve_ReturnsOperatingSystemAccountOnWindows()
    {
        var resolver = new SilentAccountResolver();
        var accounts = new IAccount[]
        {
            new TestAccount("alice@contoso.com"),
            new TestAccount("bob@contoso.com")
        };

        var result = resolver.Resolve(RuntimePlatform.Windows, accounts);

        Assert.AreSame(PublicClientApplication.OperatingSystemAccount, result);
    }

    [TestMethod]
    public void Resolve_ReturnsSingleCachedAccountOnMacOs()
    {
        var resolver = new SilentAccountResolver();
        var account = new TestAccount("alice@contoso.com");

        var result = resolver.Resolve(RuntimePlatform.MacOS, [account]);

        Assert.AreSame(account, result);
    }

    [TestMethod]
    public void Resolve_ThrowsForAmbiguousMacOsAccountCache()
    {
        var resolver = new SilentAccountResolver();
        var accounts = new IAccount[]
        {
            new TestAccount("alice@contoso.com"),
            new TestAccount("bob@contoso.com")
        };

        var exception = Assert.ThrowsException<AuthenticationException>(() => resolver.Resolve(RuntimePlatform.MacOS, accounts));

        Assert.AreEqual("Authentication failed: multiple cached signed-in accounts are available and the correct macOS account could not be selected silently.", exception.Message);
    }

    private sealed class TestAccount : IAccount
    {
        public TestAccount(string username)
        {
            Username = username;
            Environment = "login.microsoftonline.com";
            HomeAccountId = new AccountId("home-account-id", "tenant-id", "object-id");
        }

        public string Username { get; }

        public string Environment { get; }

        public AccountId HomeAccountId { get; }
    }
}
