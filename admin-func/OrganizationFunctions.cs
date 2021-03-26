using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace admin_func
{
    // //these functions are for managing the organizations - TBD for now

    public class OrganizationFunctions
    {
        // these functions are limited by default to the user's scope, e.g., get _my_ organizations
        [FunctionName("GetOrganizations")]
        public async Task<IActionResult> GetOrganizations(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organization")] HttpRequest request)
        {
            // not a direct proxy, but will return the user's b2c 'organizations'
            return new OkObjectResult(await _repo.GetOrganizations("jdandison%2Borgid@gmail.com"));
        }

        [FunctionName("GetOrganization")]
        public async Task<IActionResult> GetOrganization(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "organization/{organizationId}")] HttpRequest request, string organizationId)
        {
            // returns a specific organization based on id
            // check user access
            // user "jdandison%2Borgid@gmail.com"
            return new OkObjectResult(await _repo.GetOrganization("jdandison%2Borgid@gmail.com", organizationId));
            //return new OkObjectResult(await _repo.GetOrganizations());
        }
    }
}