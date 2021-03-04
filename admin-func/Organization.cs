using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace admin_func
{

    public class Organization
    {
        public string Id { get; set; }
        public IEnumerable<string> AllowedUserDomains { get; set; }
        public string DisplayName { get; set; }
    }

    public class User
    {
        public string Id { get; set; }
        public string Organization { get; set; }
    }

    public interface IOrganizationRepository
    {
        Task<Organization> GetOrganization(string organizationId);
        Task<IEnumerable<Organization>> GetOrganizations();
        Task<IEnumerable<User>> GetOrganizationUsers();
    }

    public class UserFunctions
    {
        public UserFunctions()
        {

        }

        // these functions are limited by default to the user's scope, e.g., get _my_ organizations
        [FunctionName("GetOrganizations")]
        public async Task<IActionResult> GetOrganizations([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organization")] HttpRequest request)
        {
            // not a direct proxy, but will return the user's b2c 'organizations'
            return new OkResult();
        }

        [FunctionName("GetOrganization")]
        public async Task<IActionResult> GetOrganization([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organization/{organizationId}")] HttpRequest request, string organizationId)
        {
            // not a direct proxy, but will return the user's b2c 'organizations'
            return new OkResult();
        }

        [FunctionName("GetOrganizationUsers")]
        public async Task<IActionResult> GetOrganizationUsers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequest request)
        {
            // get user within the org; know the user's org to filter the graph query
            // passthrough to Graph
            return new OkResult();
        }

        // matches guid
        [FunctionName("GetOrganizationUser")]
        public async Task<IActionResult> GetOrganizationUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = @"users/{userId:regex([({{]?[a-fA-F0-9]{{8}}[-]?([a-fA-F0-9]{{4}}[-]?){{3}}[a-fA-F0-9]{{12}}[}})]?)}")] HttpRequest request, string userId)
        {
            return new OkObjectResult(new { Message = "Guid", userId });
        }

        // matches email
        [FunctionName("GetOrganizationUserByEmail")]
        public async Task<IActionResult> GetOrganizationUserByEmail(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userEmail:regex((@)(.+)$)}")] HttpRequest request, string userEmail)
        {
            return new OkObjectResult(new { Method = "Email", userEmail });
        }

        //todo: figure out what to do with phonenumber and username
        // matches email
        [FunctionName("GetOrganizationUserByWhatever")]
        public async Task<IActionResult> GetOrganizationUserByWhatever(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userQuery}")] HttpRequest request, string userQuery)
        {
            // catchall for phonenumber + username (not email)
            return new OkObjectResult(new { Method = "Catchall", userQuery });
        }

        //users/{userObjectId}/appRoleAssignments?$filter=resourceId eq {servicePrincipalId}&$select=principalId,resourceId,appRoleId"
        [FunctionName("GetUserAppRoleAssignmentsByApplication")]
        public async Task<IActionResult> GetUserAppRoleAssignmentsByApplication(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userQuery}/appRoleAssignments")] HttpRequest request, string userQuery)
        {
            // todo: deal with any incoming odata clauses
            return new OkObjectResult(new { Method = "QueryByAppAndUser", userQuery, request.QueryString });
        }
    }

    public class ApplicationFunctions
    {
        // these functions are limited by default to the user's scope, e.g., get _my_ applications, where i am an administrator (e.g., have ApplicationAdministrator role)
        [FunctionName("GetApplications")]
        public static async Task<IActionResult> GetApplications([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications")] HttpRequest request)
        {
            return new OkResult();
        }

        [FunctionName("GetApplication")]
        public static async Task<IActionResult> GetApplication([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications/{applicationId}")] HttpRequest request, string applicationId)
        {
            return new OkResult();
        }

        // https://graph.microsoft.com/v1.0/applications/825d5509-8c13-4651-8528-51f1c6efb7d0/appRoles
        [FunctionName("GetApplicationRoles")]
        public static async Task<IActionResult> GetApplicationRoles([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "applications/{applicationId}/appRoles")] HttpRequest request, string applicationId)
        {
            return new OkResult();
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
        public static async Task<IActionResult> GetApplicationRoleAssignments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
                Route = "servicePrincipals/{servicePrincipalId}/appRoleAssignedTo")] HttpRequest request, string applicationId)
        {
            return new OkObjectResult(new { Method = "Email", applicationId });
        }

        // resolve app id --> service principal id
        // https://graph.microsoft.com/v1.0/servicePrincipals/fd076aa7-1423-4587-b0f1-e160f49f679f/appRoleAssignedTo

        [FunctionName("AddApplicationRoleAssignment")]
        public static async Task<IActionResult> AddApplicationRoleAssignment(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post",
                Route = "servicePrincipals/{servicePrincipalId}/appRoleAssignedTo")] HttpRequest request, string servicePrincipalId)
        {
            return new OkObjectResult(new { servicePrincipalId });
        }

        // going to start reusing some graph types here so we can possibly migrate to direct graph in the future
        [FunctionName("AddApplicationRole")]
        public async Task<IActionResult> AddApplicationRole(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch",
                Route = "applications/{applicationId}")] HttpRequest request, string applicationId, AppRole appRole)
        {
            return new OkResult();
        }
    }
}