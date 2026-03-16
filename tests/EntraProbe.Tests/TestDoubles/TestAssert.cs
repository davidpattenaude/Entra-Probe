namespace EntraProbe.Tests;

public static class TestAssert
{
    public static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name}.");
        return null!;
    }

    public static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name}.");
        return null!;
    }
}
