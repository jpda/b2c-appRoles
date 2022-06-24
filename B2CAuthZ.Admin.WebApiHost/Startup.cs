using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.Graph;
using Azure.Identity;
using System;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace B2CAuthZ.Admin.WebApiHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetry();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(Configuration.GetSection("ApiAuthorization"));

            // services.AddHttpContextAccessor(); // should already be here due to microsoft.identity.web above
            // services.AddOptions<AzureAdAdminConfiguration>()
            //     .Configure<IConfiguration>((opt, config) =>
            //     {
            //         config.GetSection("AzureAdDirectoryAdmin").Bind(opt);
            //     });

            services.AddSingleton<GraphServiceClient>(sc =>
            {
                return new GraphServiceClient(new Azure.Identity.ClientSecretCredential(
                    Configuration.GetValue<string>("B2CGraphPrivilegedCredentials:TenantId"),
                    Configuration.GetValue<string>("B2CGraphPrivilegedCredentials:ClientId"),
                    Configuration.GetValue<string>("B2CGraphPrivilegedCredentials:ClientSecret")));
            });

            services.AddOptions<SwaggerUIClientAuthOptions>().Configure<IConfiguration>((opt, config) =>
            {
                config.GetSection("SwaggerUIClientAuthentication").Bind(opt);
            });

            services.AddOptions<OrganizationOptions>().Configure<IConfiguration>((opt, config) =>
            {
                config.GetSection("OrganizationOptions").Bind(opt);
            });

            // these are very important to stay scoped - as they are configured _per request_ with the orgId
            services.AddScoped<IUserRepository, OrganizationFilteredUserRepository>();
            services.AddScoped<IApplicationRepository, OrganizationFilteredApplicationRepository>();

            services.AddControllers().AddJsonOptions(x =>
            {
                // used to keep graph objects small
                x.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                x.JsonSerializerOptions.MaxDepth = 5;
            });

            services.AddEndpointsApiExplorer();

            services.AddApiVersioning();
            // see 
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerGenOptions>();
            services.AddSwaggerGen();

            services.AddCors(options =>
            {
                options.AddPolicy(name: "DevCorsPolicy",
                    builder =>
                    {
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                        builder.AllowAnyOrigin();
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseCors("DevCorsPolicy");
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("swagger/v1.0/swagger.json", "B2X Organization & Authorization Administration v1.0");
                    c.RoutePrefix = string.Empty;
                    c.DefaultModelExpandDepth(1);
                    c.OAuthClientId(Configuration.GetValue<string>("SwaggerUIClientAuth:ClientId"));
                    c.OAuthScopes();//Configuration.GetValue<string>("SwaggerUIClientAuth:ClientId"));
                    //c.OAuth2RedirectUrl(Configuration.GetValue<string>());
                    //c.OAuthClientSecret(Configuration.GetValue<string>("AzureAdB2CSwaggerUIClient:ClientId"));
                    c.OAuthUsePkce();

                });
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
