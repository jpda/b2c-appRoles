using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;

public class AzureAdAdminConfiguration
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string TenantName { get; set; }
    public string Authority { get; set; }
    public string Scopes { get; set; }
}

public class ClientCredentialProvider {
    
}

// msal token provider for graph - note that we access graph here as the application directly, not as users
// because of this, we do not go through the B2C authority, we use the normal AAD authority
// remember - this is a privileged account for calling graph - this API **must** ensure all calls are valid

public class MsalTokenProvider : IAuthenticationProvider
{
    private readonly AzureAdAdminConfiguration _config;
    public readonly IConfidentialClientApplication _client;

    public MsalTokenProvider(IOptions<AzureAdAdminConfiguration> opts)
    {
        _config = opts.Value;

        _client = ConfidentialClientApplicationBuilder
                .Create(_config.ClientId)
                .WithClientSecret(_config.ClientSecret)
                .WithAuthority(_config.Authority ?? $"https://login.microsoftonline.com/{_config.TenantName}/v2.0")
                .Build();
        // todo: add oob token cache
    }

    public async Task AuthenticateRequestAsync(HttpRequestMessage request)
    {
        var token = await _client.AcquireTokenForClient(_config.Scopes.Split(',')).ExecuteAsync();
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
    }
}