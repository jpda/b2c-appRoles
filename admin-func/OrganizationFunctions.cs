using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using AzureFunctions.OidcAuthentication;
using System.Linq;
using System.Security.Claims;

namespace admin_func
{
    // //these functions are for managing the organizations - TBD for now

    public class OrganizationFunctions
    {
        private readonly IOrganizationRepository _repo;
        private readonly IApiAuthentication apiAuthentication;
        private const string ORGID_EXTENSION = "extension_OrgId";
        private readonly UserRepositoryFactory _userRepoFactory;

        public OrganizationFunctions(IOrganizationRepository repo, UserRepositoryFactory repoFactory)
        {
            _repo = repo;
            _userRepoFactory = repoFactory;
        }

        private async Task<IActionResult> RunFilteredRequest<T>(IHeaderDictionary headers, System.Func<IUserRepository, string, Task<T>> work)
        {
            var authResult = await this.apiAuthentication.AuthenticateAsync(headers);
            if (authResult.Failed) return new UnauthorizedObjectResult(authResult.FailureReason);

            var orgId = authResult.User.Claims.SingleOrDefault(x => x.Type == ORGID_EXTENSION);
            if (orgId == null) return new UnauthorizedObjectResult(new { Message = "User is not a member of an organization" });

            var repo = _userRepoFactory.CreateForOrgId(orgId.Value);
            var userId = authResult.User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value;
            return new OkObjectResult(await work(repo, userId));
        }

        // these are global admin functions - they do not filter results based on principal! 
        [FunctionName("GetOrganizations")]
        public async Task<IActionResult> GetOrganizations(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organization")] HttpRequest req)
        {
            // check for Role: ProviderAdministrator
            // not a direct proxy, but will return the user's b2c 'organizations'
            return new OkObjectResult(await _repo.GetOrganizations());
        }

        [FunctionName("GetOrganization")]
        public async Task<IActionResult> GetOrganization(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organization/{organizationId}")] HttpRequest req, string organizationId)
        {
            // check for Role: ProviderAdministrator
            // returns a specific organization based on id
            return new OkObjectResult(await _repo.GetOrganization(organizationId));
        }

        [FunctionName("SetUserOrganization")]
        public async Task<IActionResult> SetUserOrganization(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "organization/{organizationId}/members")] HttpRequest req, string organizationId)
        {
            var request = JsonSerializer.Deserialize<OrganizationMembership>(await new System.IO.StreamReader(req.Body).ReadToEndAsync());
            return await RunFilteredRequest(req.Headers, (repo, user) => repo.SetUserOrganization(request));
        }

        // [FunctionName("RemoveUserOrganization")]
        // public async Task<IActionResult> RemoveUserOrganization(
        //     [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "organization/{organizationId}/members")] HttpRequest req, string organizationId, OrganizationMembership membershipRequest)
        // {

        // }

    }
}