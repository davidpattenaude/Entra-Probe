namespace EntraProbe.Graph;

public sealed class GraphQueryException : Exception
{
    public GraphQueryException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
