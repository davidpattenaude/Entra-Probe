namespace EntraProbe.Graph;

public interface IGraphProfileService
{
    Task<SignedInUserProfile> GetProfileAsync(string accessToken, CancellationToken cancellationToken);
}
