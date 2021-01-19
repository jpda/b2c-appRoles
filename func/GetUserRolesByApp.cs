using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace func
{
    public static class GetUserRolesByApp
    {
        private static readonly HttpClient _client = new HttpClient()
        {
            BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
        };

        [FunctionName("GetUserRolesByApp")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {

            // get configuration 
            var cca = ConfidentialClientApplicationBuilder.Create("").WithClientSecret("").WithTenantId("").Build();
            // todo: error handling
            var graphToken = await cca.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphToken.AccessToken);

            // graph token
            // inbound stuff ==> user's object ID
            // inbound stuff ==> client_id of the application the user is accessing

            var userObjectId = "";
            var clientId = "";

            // https://graph.microsoft.com/v1.0/servicePrincipals?$filter=appId eq '05c77d87-2fcc-44fd-b385-ae8080afa879'&$select=id

            // translate client_id ==> service principal id

            var servicePrincipalObject = await _client.MakeGraphRequest<ServicePrincipalTranslationResponse>(
                $"servicePrincipals?$filter=appId eq '{clientId}&$select=id'");

            // var servicePrincipalTranslationQuery = $"servicePrincipals?$filter=appId eq '{clientId}&$select=id'";
            // var servicePrincipalTranslationRequest = await _client.GetAsync(servicePrincipalTranslationQuery);

            // 409 -- conflict, b2c-specific thing
            if (!servicePrincipalObject.success) return new ConflictResult();

            // var result = await System.Text.Json.JsonSerializer
            //     .DeserializeAsync<ServicePrincipalTranslationResponse>
            //     (await servicePrincipalTranslationRequest.Content.ReadAsStreamAsync());

            var servicePrincipalId = servicePrincipalObject.thing.Id;

            // /users/39a0e707-d275-4860-b534-5cd1c2d2dbe1/appRoleAssignments?$filter=resourceId eq fd076aa7-1423-4587-b0f1-e160f49f679f'
            var appRoleAssignmentList = await _client.MakeGraphRequest<AppRoleAssignmentResponse>(
                $"users/{userObjectId}/appRoleAssignments?$filter=resourceId eq {servicePrincipalId}&$select=principalId,resourceId,appRoleId";
            );
            // var appRoleAssignmentQuery = @$"users/{userObjectId}/appRoleAssignments?$filter=resourceId eq {servicePrincipalId}&$select=principalId,resourceId,appRoleId";
            // var appRoleAssignmentRequest = await _client.GetAsync(appRoleAssignmentQuery);

            if (!appRoleAssignmentList.success) return new ConflictResult();

            // var appRoleAssignmentResponse = await JsonSerializer
            //     .DeserializeAsync<AppRoleAssignmentResponse>(
            //         await appRoleAssignmentRequest.Content.ReadAsStreamAsync()
            //     );

            //todo: figure out how much to push into extension method (include conflict or not, messages for conflict, etc)

            var listOfAppRolesForUser = appRoleAssignmentList.thing.Assignments.Select(x => x.AppRoleId);

            var appRolesForApplication = await _client.MakeGraphRequest<Application>(
                $"servicePrincipal/{servicePrincipalId}/appRoles"
            );

            if (!appRolesForApplication.success) return new ConflictResult();

            var listOfAppRoleValuesUserIsAMemberOf = appRolesForApplication.thing.AppRoles.Where(x => x.Enabled && listOfAppRolesForUser.Contains(x.Id))
                        .Select(appRole => appRole.Value);

            return new OkObjectResult(listOfAppRoleValuesUserIsAMemberOf);

            // this is what B2C will get:

            //["role1", "role2"];




            //https://graph.microsoft.com/beta/applications/825d5509-8c13-4651-8528-51f1c6efb7d0/appRoles?$count=true
            //https://graph.microsoft.com/v1.0/servicePrincipals?$filter=appId eq '05c77d87-2fcc-44fd-b385-ae8080afa879'&$select=id
            //https://graph.microsoft.com/beta/trustFramework/policies/B2C_1A_AADCommonClaimsProvider/$value
            //https://graph.microsoft.com/v1.0/servicePrincipals?$filter=appId eq '05c77d87-2fcc-44fd-b385-ae8080afa879'&$select=id
            // https://graph.microsoft.com/v1.0/users/39a0e707-d275-4860-b534-5cd1c2d2dbe1/appRoleAssignments?$filter=resourceId eq fd076aa7-1423-4587-b0f1-e160f49f679f
            // get the user's appRoleAssignments for that service principal/app id
            // resolve the guid ==> value of the assigned appRoles
            //  return roles: [ roles the user has] ;

            return new OkObjectResult(responseMessage);
        }
    }

    public static class Extensions
    {
        public async static Task<(bool success, T thing)> MakeGraphRequest<T>(this HttpClient client, string query)
        {
            var request = await client.GetAsync(query);

            if (!request.IsSuccessStatusCode) return (false, default); // return something to indicate failure

            return (true, await JsonSerializer
                .DeserializeAsync<T>(
                    await request.Content.ReadAsStreamAsync()
                ));
        }
    }

    public class GraphHelper
    {
        private readonly HttpClient _client;
        public GraphHelper(HttpClient client)
        {
            _client = client;
        }

        public async Task<T> Query<T>(string query)
        {
            //var appRoleAssignmentQuery = @$"users/{userObjectId}/appRoleAssignments?$filter=resourceId eq {servicePrincipalId}&$select=principalId,resourceId,appRoleId";
            var request = await _client.GetAsync(query);

            if (!request.IsSuccessStatusCode) return new ConflictResult(); // return something to indicate failure

            return await JsonSerializer
                .DeserializeAsync<T>(
                    await request.Content.ReadAsStreamAsync()
                );
        }

    }

    public class Application
    {
        public List<AppRole> AppRoles { get; set; }
    }

    public class AppRole
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
        [JsonPropertyName("isEnabled")]
        public bool Enabled { get; set; }
    }

    public class ServicePrincipalTranslationResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class AppRoleAssignmentResponse
    {
        [JsonPropertyName("value")]
        public List<AppRoleAssignment> Assignments { get; set; }
    }

    public class AppRoleAssignment
    {
        [JsonPropertyName("resourceId")]
        public string ResourceId { get; set; }
        [JsonPropertyName("principalId")]
        public string PrincipalId { get; set; }
        [JsonPropertyName("appRoleId")]
        public string AppRoleId { get; set; }
    }
}

