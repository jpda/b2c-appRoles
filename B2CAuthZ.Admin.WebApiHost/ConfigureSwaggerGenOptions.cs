using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Resource;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace B2CAuthZ.Admin.WebApiHost
{

    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Policy names map to scopes
            var requiredScopes = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<RequiredScopeAttribute>()
                .SelectMany(attr => attr.AcceptedScope)
                .Distinct();

            if (requiredScopes.Any())
            {
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

                var oAuthScheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                };

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [ oAuthScheme ] = requiredScopes.ToList()
                    }
                };
            }
        }
    }

    public class ConfigureSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;
        private readonly SwaggerUIClientAuthOptions _swaggerUIClientAuthOptions;
        public ConfigureSwaggerGenOptions(IOptions<SwaggerUIClientAuthOptions> authOptions, IApiVersionDescriptionProvider provider)
        {
            _provider = provider;
            _swaggerUIClientAuthOptions = authOptions.Value;
        }

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateVersionInfo(description));
            }

            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(_swaggerUIClientAuthOptions.AuthorizationUrl),
                        TokenUrl = new Uri(_swaggerUIClientAuthOptions.TokenUrl),
                        Scopes = _swaggerUIClientAuthOptions.Scopes == null ? new Dictionary<string, string>() { } : _swaggerUIClientAuthOptions.Scopes.Split(' ').ToDictionary(x => x, x => "")
                    }
                },
                Description = "Use your b2x.studio account to access this API"
            });
            options.OperationFilter<SecurityRequirementsOperationFilter>();
        }

        // see this excellent post: https://christian-schou.dk/how-to-use-api-versioning-in-net-core-web-api/
        private static OpenApiInfo CreateVersionInfo(ApiVersionDescription desc)
        {
            var info = new OpenApiInfo
            {
                Title = "B2X Organization & Authorization Administration",
                Version = desc.ApiVersion.ToString("FF"),
                Description = "Administrative API for managing organizations, memberships & applications.",
                Contact = new OpenApiContact
                {
                    Name = "github: jpda",
                    Url = new Uri("https://github.com/jpda/b2c-approles")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://github.com/jpda/b2c-appRoles/blob/main/LICENSE")
                }
            };
            if (desc.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }
            return info;
        }
    }
}