using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.IdentityModel.Logging;
using B2CAuthZ.Admin.FuncHost;
using AzureFunctions.OidcAuthentication;
using B2CAuthZ.Admin;

[assembly: FunctionsStartup(typeof(Startup))]

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        var context = builder.GetContext();

        builder.ConfigurationBuilder
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;

        builder.Services.AddLogging();
        builder.Services.AddOptions<AzureAdAdminConfiguration>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                configuration.GetSection("AzureAd").Bind(options);
            });
        builder.Services.AddOptions<OrganizationOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                configuration.GetSection("OrganizationOptions").Bind(options);
            });
        builder.Services.AddSingleton<IAuthenticationProvider, MsalTokenProvider>();
        // todo: you gotta be kidding me
        builder.Services.AddSingleton<string>("https://graph.microsoft.com/v1.0/");
        builder.Services.AddSingleton<GraphServiceClient>();
        builder.Services.AddSingleton<UserRepositoryFactory>();
        builder.Services.AddSingleton<ApplicationRepositoryFactory>();

        builder.Services.AddOidcApiAuthorization();
    }
}

public class AzureAdAdminConfiguration
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string TenantName { get; set; }
    public string Authority { get; set; }
    public string Scopes { get; set; }
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
    }

    public async Task AuthenticateRequestAsync(HttpRequestMessage request)
    {
        var token = await _client.AcquireTokenForClient(_config.Scopes.Split(',')).ExecuteAsync();
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
    }
}