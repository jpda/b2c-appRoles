using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Graph;
using System.Linq;
using AzureFunctions.OidcAuthentication;
using System.Security.Claims;
using System.Text.Json;

namespace admin_func
{
    public class ApplicationFunctions
    {
        //private readonly UserRepositoryFactory _repoFactory;
        private readonly ApplicationRepositoryFactory _appRepoFactory;
        private readonly IApiAuthentication apiAuthentication;
        private const string ORGID_EXTENSION = "extension_OrgId";

        public ApplicationFunctions(ApplicationRepositoryFactory appRepoFactory, IApiAuthentication apiAuthentication)
        {
            //_repoFactory = userRepoFactory;
            _appRepoFactory = appRepoFactory;
            this.apiAuthentication = apiAuthentication;
        }

        private async Task<IActionResult> RunFilteredRequest<T>(IHeaderDictionary headers, System.Func<IApplicationRepository, string, Task<T>> work)
        {
            var authResult = await this.apiAuthentication.AuthenticateAsync(headers);
            if (authResult.Failed) return new UnauthorizedObjectResult(authResult.FailureReason);

            var orgId = authResult.User.Claims.SingleOrDefault(x => x.Type == ORGID_EXTENSION);
            if (orgId == null) return new UnauthorizedObjectResult(new { Message = "User is not a member of an organization" });

            var repo = _appRepoFactory.CreateForOrgId(orgId.Value);
            var userId = authResult.User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value;
            return new OkObjectResult(await work(repo, userId));
        }

        // these functions are limited by default to the user's scope, e.g., get _my_ applications, where i am an administrator (e.g., have ApplicationAdministrator role)
        [FunctionName("GetApplications")]
        public async Task<IActionResult> GetApplications(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications")] HttpRequest req)
        {
            return await RunFilteredRequest(req.Headers, (repo, user) => repo.GetApplications(user));
        }

        [FunctionName("GetApplication")]
        public async Task<IActionResult> GetApplication(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "servicePrincipals/{servicePrincipalId}")] HttpRequest req, string servicePrincipalId)
        {
            return await RunFilteredRequest(req.Headers, (repo, user) => repo.GetApplication(user, servicePrincipalId));
        }

        // https://graph.microsoft.com/v1.0/applications/825d5509-8c13-4651-8528-51f1c6efb7d0/appRoles
        // note this isn't _really_ an application, it's a service principal. trying to reduce round trips
        [FunctionName("GetServicePrincipalRoles")]
        public async Task<IActionResult> GetServicePrincipalRoles(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "servicePrincipals/{servicePrincipalId}/appRoles")] HttpRequest req, string servicePrincipalId)
        {
            return await RunFilteredRequest(req.Headers, (repo, user) => repo.GetAppRolesByServicePrincipal(user, servicePrincipalId));
        }

        // $filter=appRoles/any(x:x/id eq '1f861d87-4256-4563-bec8-cda2e0b925a7')
        // [FunctionName("GetApplicationRole")]
        // public static async Task<IActionResult> GetApplicationRole(
        //     [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = @"applications/{applicationId}/appRoles")] HttpRequest request, string applicationId)
        // {
        //     return new OkObjectResult(new { applicationId, appRoleId });
        // }

        // resolve app id --> service principal id
        // https://graph.microsoft.com/v1.0/servicePrincipals/fd076aa7-1423-4587-b0f1-e160f49f679f/appRoleAssignedTo
        [FunctionName("GetApplicationRoleAssignments")]
        public async Task<IActionResult> GetApplicationRoleAssignments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = "servicePrincipals/{servicePrincipalId}/appRoleAssignedTo")] HttpRequest req, string servicePrincipalId)
        {
            return await RunFilteredRequest(req.Headers, (repo, user) => repo.GetAppRoleAssignmentsByServicePrincipal(user, servicePrincipalId));
        }

        // resolve app id --> service principal id
        // https://graph.microsoft.com/v1.0/servicePrincipals/fd076aa7-1423-4587-b0f1-e160f49f679f/appRoleAssignedTo

        [FunctionName("AddApplicationRoleAssignment")]
        public async Task<IActionResult> AddApplicationRoleAssignment(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post",
                Route = "servicePrincipals/{servicePrincipalId}/appRoleAssignedTo")] HttpRequest req, string servicePrincipalId)
        {
            var assignmentRequest = JsonSerializer.Deserialize<AppRoleAssignment>(await new System.IO.StreamReader(req.Body).ReadToEndAsync());
            return await RunFilteredRequest(req.Headers,
                (repo, user) => repo.AssignAppRole(user, servicePrincipalId, servicePrincipalId, assignmentRequest.AppRoleId.ToString()));
        }

        [FunctionName("AddApplicationRole")]
        public async Task<IActionResult> AddApplicationRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch",
                Route = "applications/{applicationId}")] HttpRequest req, string applicationId, AppRole appRole)
        {
            // todo
            return new OkResult();
        }
    }
}