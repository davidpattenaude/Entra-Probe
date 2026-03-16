namespace EntraProbe.Identity;

public sealed class AuthenticationException : Exception
{
    public AuthenticationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
