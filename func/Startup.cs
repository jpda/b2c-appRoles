using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;

[assembly: FunctionsStartup(typeof(func.Startup))]

namespace func
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // builder.Services.Add<Microsoft.Identity.Client.ConfidentialClientApplication>({

            // };
            // This is configuration from environment variables, settings.json etc.
            // var configuration = builder.GetContext().Configuration;

            // builder.Services.AddAuthentication(sharedOptions =>
            // {
            //     sharedOptions.DefaultScheme = "Bearer";
            //     sharedOptions.DefaultChallengeScheme = "Bearer";
            // })
            //     .AddMicrosoftIdentityWebApi(configuration)
            //         .EnableTokenAcquisitionToCallDownstreamApi()
            //         .AddInMemoryTokenCaches()
            //         .AddMicrosoftGraph();
        }
    }
}