using Microsoft.Extensions.Options;
using Microsoft.Graph;
using System.Linq;

namespace B2CAuthZ.Admin
{
    public abstract class FilteredRepository
    {
        protected readonly GraphServiceClient _graphClient;
        protected readonly string _orgId;
        protected readonly string _callingUserId;
        protected readonly OrganizationOptions _options;

        protected FilteredRepository(GraphServiceClient client, string orgId, IOptions<OrganizationOptions> options)
        {
            _graphClient = client;
            _orgId = orgId;
            _options = options.Value;
        }

        protected FilteredRepository(GraphServiceClient client, System.Security.Claims.ClaimsPrincipal principal, IOptions<OrganizationOptions> options)
        {
            _graphClient = client;
            _options = options.Value;
            var orgIdClaim = principal.Claims.Where(x => x.Type == _options.OrgIdClaimName);
            _orgId = orgIdClaim.Any() ? orgIdClaim.Single().Value : throw new System.UnauthorizedAccessException("User is not a member of any organizations");
            var userIdClaim = principal.Claims.Where(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            _callingUserId = userIdClaim.Any() ? userIdClaim.Single().Value : throw new System.UnauthorizedAccessException("User nameidentifier/subject is missing");
        }
    }
}