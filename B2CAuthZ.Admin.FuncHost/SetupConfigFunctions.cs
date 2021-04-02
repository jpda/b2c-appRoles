using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Linq;
using AzureFunctions.OidcAuthentication;
using System.Collections.Generic;
using Microsoft.Graph;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace B2CAuthZ.Admin.FuncHost
{
    // HERE BE DRAGONS
    // THIS IS FOR LOCAL DEV ONLY
    // THESE FUNCTIONS WILL CONFIGURE YOUR DIRECTORY
    // THEY ARE INCREDIBLY PRIVILEGED
    // DO NOT PUBLISH THESE TO ANY INTERNET ENDPOINT
    public class SetupConfigFunctions
    {
        // private readonly UserRepositoryFactory _repoFactory;
        // private readonly IApiAuthentication apiAuthentication;
        private const string ORGID_EXTENSION = "extension_OrgId";
        private readonly GraphServiceClient _graphClient;
        private readonly AzureAdAdminConfiguration _aadConfig;

        public SetupConfigFunctions(GraphServiceClient graph, IOptions<AzureAdAdminConfiguration> options)
        {
            _graphClient = graph;
            _aadConfig = options.Value;
        }

        // if you use this to configure your attributes, make sure you also add them in the portal under 'User Attributes' if you need to include them in 
        // application claims for portal User Flows - sadly, the API for creating them doesn't also create them for portal flows.
        // the IDs will remain the same, adding them in the portal uses the same underlying attribute but makes it available for portal flows.
        [FunctionName("ConfigureUserAttributes")]
        public async Task<IActionResult> ConfigureUserAttributes([HttpTrigger(AuthorizationLevel.Function, "get", Route = "config/attributes")] HttpRequest req)
        {
            try
            {
                var b2cExtensionsAppQuery = await _graphClient.Applications.Request().Select("id").Filter("startswith(displayName, 'b2c-extensions-app')").GetAsync();
                if (b2cExtensionsAppQuery.Count != 1) return new BadRequestObjectResult(new { Message = "missing b2c-ext app" });

                var extAppRequest = _graphClient.Applications[b2cExtensionsAppQuery.Single().Id].ExtensionProperties.Request();
                var extApp = await extAppRequest.GetAsync();
                var orgIdExt = await extAppRequest.AddAsync(new ExtensionProperty()
                {
                    Name = "OrgId",
                    DataType = "string",
                    TargetObjects = new[] { "User" }
                });

                await extAppRequest.AddAsync(new ExtensionProperty()
                {
                    Name = "OrgRole",
                    DataType = "string",
                    TargetObjects = new[] { "User" }
                });
                return new OkObjectResult(await extAppRequest.GetAsync());
            }
            catch (System.Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }

        private async Task<string> FindB2CExtensionsAppId()
        {
            var b2cExtensionsAppQuery = await _graphClient.Applications.Request().Select("appId").Filter("startswith(displayName, 'b2c-extensions-app')").GetAsync();
            return b2cExtensionsAppQuery.Single().AppId;
        }

        [FunctionName("ConfigureAdminAndTestApps")]
        public async Task<IActionResult> ConfigureAdminAndTestApps([HttpTrigger(AuthorizationLevel.Function, "get", Route = "config/apps")] HttpRequest req, ILogger log)
        {
            var apps = new List<Application>();
            var adminApiAppDefinition = new Application()
            {
                DisplayName = "b2c-authz-admin-api",
                SignInAudience = "AzureADandPersonalMicrosoftAccount"
            };

            var adminApiAppRequest = await _graphClient.Applications.Request().AddAsync(adminApiAppDefinition);
            log.LogInformation("App added, creating scopes");
            var adminApiApp = new Application
            {
                IdentifierUris = new[] { $"https://{_aadConfig.TenantName}/authz-admin-api" },
                Api = new ApiApplication()
                {
                    Oauth2PermissionScopes = new List<PermissionScope>() {
                        new PermissionScope() { Id = Guid.NewGuid(), Type = "Admin", Value = "access_api", AdminConsentDescription = "Access the API", AdminConsentDisplayName = "Access API", IsEnabled = true }
                    },
                    RequestedAccessTokenVersion = 2
                }
            };
            await _graphClient.Applications[adminApiAppRequest.Id].Request().UpdateAsync(adminApiApp);
            apps.Add(adminApiApp);

            var testAppWithRolesDef = new Application()
            {
                DisplayName = "b2c-authz-sample-app",
                SignInAudience = "AzureADandPersonalMicrosoftAccount"
            };

            var testAppWithRolesRequest = await _graphClient.Applications.Request().AddAsync(testAppWithRolesDef);
            var testAppWithRoles = new Application()
            {
                AppRoles = new List<AppRole>()
                {
                    new AppRole() { // common app admin role - this needs to be on every app that needs external management
                        AllowedMemberTypes = new[] {"User"},
                        Description = "Allows user to manage their organization's users for this application's roles",
                        DisplayName = "Application organization administrator",
                        Id = Guid.Parse("b3e0dfe9-f58d-4c35-97d3-a4efc954ba6e"),
                        Value = "ApplicationOrgAdministrator"
                    },
                    new AppRole() {
                        AllowedMemberTypes = new[] {"User"},
                        Description = "Some application role",
                        DisplayName = "Reader",
                        Id = Guid.NewGuid(),
                        Value = "Reader"
                    }
                }
            };
            await _graphClient.Applications[testAppWithRolesRequest.Id].Request().UpdateAsync(testAppWithRoles);
            apps.Add(testAppWithRoles);
            return new OkObjectResult(apps);
        }


        [FunctionName("ConfigureTestUsers")]
        public async Task<IActionResult> ConfigureTestUsers([HttpTrigger(AuthorizationLevel.Function, "get", Route = "config/testUsers")] HttpRequest req, ILogger log)
        {
            var extId = (await FindB2CExtensionsAppId()).Replace("-", string.Empty);
            log.LogInformation($"extid: {extId}");
            var a = new List<User>();
            var org123Admin = new User()
            {
                DisplayName = "Carl of Duty (org123 admin)",
                Identities = new List<ObjectIdentity>()
                {
                    new ObjectIdentity
                    {
                        SignInType = "emailAddress",
                        Issuer = $"{_aadConfig.TenantName}",
                        IssuerAssignedId = "b2cadmin@org123.com"
                    }
                },
                PasswordProfile = new PasswordProfile
                {
                    Password = "SuperSecretSquirrel1",
                    ForceChangePasswordNextSignIn = false
                },
                PasswordPolicies = "DisablePasswordExpiration",
                AdditionalData = new Dictionary<string, object>()
                {
                    {$"extension_{extId}_OrgId", "123"},
                    {$"extension_{extId}_OrgRole", "Admin"}
                }
            };

            var org123User = new User()
            {
                DisplayName = "Joe Smith (org123 user)",
                Identities = new List<ObjectIdentity>()
                {
                    new ObjectIdentity
                    {
                        SignInType = "emailAddress",
                        Issuer = $"{_aadConfig.TenantName}",
                        IssuerAssignedId = "b2cuser@org123.com"
                    }
                },
                PasswordProfile = new PasswordProfile
                {
                    Password = "SuperSecretSquirrel1",
                    ForceChangePasswordNextSignIn = false
                },
                PasswordPolicies = "DisablePasswordExpiration",
                AdditionalData = new Dictionary<string, object>()
                {
                    {$"extension_{extId}_OrgId", "123"},
                    {$"extension_{extId}_OrgRole", "Member"}
                }
            };

            var org456Admin = new User()
            {
                DisplayName = "Mark Plain (org456 admin)",
                Identities = new List<ObjectIdentity>()
                {
                    new ObjectIdentity
                    {
                        SignInType = "emailAddress",
                        Issuer = $"{_aadConfig.TenantName}",
                        IssuerAssignedId = "b2cadmin@org456.com"
                    }
                },
                PasswordProfile = new PasswordProfile
                {
                    Password = "SuperSecretSquirrel1",
                    ForceChangePasswordNextSignIn = false
                },
                PasswordPolicies = "DisablePasswordExpiration",
                AdditionalData = new Dictionary<string, object>()
                {
                    {$"extension_{extId}_OrgId", "456"},
                    {$"extension_{extId}_OrgRole", "Admin"}
                }
            };

            var admin123Id = await _graphClient.Users.Request().AddAsync(org123Admin);
            var user123Id = await _graphClient.Users.Request().AddAsync(org123User);
            var admin456Id = await _graphClient.Users.Request().AddAsync(org456Admin);
            a.Add(admin123Id);
            a.Add(user123Id);
            a.Add(admin456Id);

            // var admin123 = await _graphClient.Users[admin123Id.Id].Request().UpdateAsync(new User()
            // {
            //     AdditionalData = new Dictionary<string, object>()
            //     {
            //         {$"extension_{extId}_OrgId", "123"},
            //         {$"extension_{extId}_OrgRole", "Admin"}
            //     }
            // });

            return new OkObjectResult(a);
        }
    }
}