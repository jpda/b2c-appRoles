using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Graph;

[assembly: FunctionsStartup(typeof(AzureAdB2CAppRoleShim.Startup))]

namespace B2CAuthZ.Runtime.FuncHost
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            var config = builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            config.GetSection("AzureAd");
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddOptions<MsalTokenProviderConfiguration>()
                .Configure<IConfiguration>((options, configuration) =>
                {
                    configuration.GetSection("AzureAd").Bind(options);
                });

            builder.Services.AddSingleton<IAuthenticationProvider, MsalTokenProvider>();
            // todo: you gotta be kidding me
            builder.Services.AddSingleton<string>("https://graph.microsoft.com/v1.0/");
            builder.Services.AddSingleton<GraphServiceClient>();
        }
    }

    public class MsalTokenProviderConfiguration
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantName { get; set; }
        public string Authority { get; set; }
        public string Scopes { get; set; }
    }

    public class MsalTokenProvider : Microsoft.Graph.IAuthenticationProvider
    {
        private readonly MsalTokenProviderConfiguration _config;
        public readonly IConfidentialClientApplication _client;

        public MsalTokenProvider(IOptions<MsalTokenProviderConfiguration> opts)
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
}