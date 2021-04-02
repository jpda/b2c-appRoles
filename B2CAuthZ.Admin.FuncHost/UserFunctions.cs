using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Linq;
using AzureFunctions.OidcAuthentication;
using B2CAuthZ.Admin;

namespace B2CAuthZ.Admin.FuncHost
{
    // all of the functions here pertain to user operations
    // the graph repositories here are filtered based on the user
    // preferably this would be some sort of filter or attribute
    // but for now, we rely on a factory to create the repositories for us
    // everything filtered based on user's membership

    public class UserFunctions
    {
        private readonly UserRepositoryFactory _repoFactory;
        private readonly IApiAuthentication _apiAuthentication;
        private const string ORGID_EXTENSION = "extension_OrgId";

        public UserFunctions(UserRepositoryFactory userRepoFactory, IApiAuthentication apiAuthentication)
        {
            _repoFactory = userRepoFactory;
            _apiAuthentication = apiAuthentication;
        }

        private async Task<IActionResult> RunFilteredRequest<T>(IHeaderDictionary headers, System.Func<IUserRepository, Task<T>> work)
        {
            var authResult = await _apiAuthentication.AuthenticateAsync(headers);
            if (authResult.Failed) return new UnauthorizedObjectResult(authResult.FailureReason);

            var orgId = authResult.User.Claims.SingleOrDefault(x => x.Type == ORGID_EXTENSION);
            if (orgId == null) return new UnauthorizedObjectResult(new { Message = "User is not a member of an organization" });

            var repo = _repoFactory.CreateForOrgId(authResult.User);
            return new OkObjectResult(await work(repo));
        }

        [FunctionName("GetUsers")]
        public async Task<IActionResult> GetUsers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequest req)
        {
            return await RunFilteredRequest(req.Headers, x => x.GetUsers());
        }

        // matches guid
        [FunctionName("GetUser")]
        public async Task<IActionResult> GetUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = @"users/{userId:regex([({{]?[a-fA-F0-9]{{8}}[-]?([a-fA-F0-9]{{4}}[-]?){{3}}[a-fA-F0-9]{{12}}[}})]?)}")] HttpRequest req, string userId)
        {
            return await RunFilteredRequest(req.Headers, x => x.GetUser(userId));
        }

        // todo: get by email
        //[FunctionName("GetUserByEmail")]
        public async Task<IActionResult> GetUserByEmail(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userEmail:regex((@)(.+)$)}")] HttpRequest req, string userEmail)
        {
            return new OkObjectResult(new { Method = "Email", userEmail });
            //return await RunFilteredRequest(req.Headers, x => x.GetUserBy(userEmail));
        }


        // this is extremely dangerous! do not use this method as it does not yet sanitize input!
        //[FunctionName("GetUserByWhatever")]
        public async Task<IActionResult> GetUserByWhatever(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userQuery}")] HttpRequest req, string userQuery)
        {
            // todo: figure out how to parse & deal with odata queries
            var authResult = await _apiAuthentication.AuthenticateAsync(req.Headers);
            if (authResult.Failed) return new ForbidResult(authenticationScheme: "Bearer");

            var orgId = authResult.User.Claims.SingleOrDefault(x => x.Type == ORGID_EXTENSION);
            if (orgId == null) return new UnauthorizedObjectResult(new { Message = "User is not a member of an organization" });

            var repo = _repoFactory.CreateForOrgId(authResult.User);
            // catchall for phonenumber + username (not email)
            return new OkObjectResult(new { Method = "Catchall", userQuery });
        }

        //users/{userObjectId}/appRoleAssignments?$filter=resourceId eq {servicePrincipalId}&$select=principalId,resourceId,appRoleId"
        [FunctionName("GetUserAppRoleAssignmentsByApplication")]
        public async Task<IActionResult> GetUserAppRoleAssignmentsByApplication(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{userQuery}/appRoleAssignments")] HttpRequest req, string userQuery)
        {
            return await RunFilteredRequest(req.Headers, x => x.GetUserAppRoleAssignments(userQuery));
        }
    }
}