using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace B2CAuthZ.Admin
{
    // this repository is filtered based on the calling user
    // only shows apps the user is allowed to administer, based on membership in the _adminAppRoleId role
    // this returns resourceId, which is technically the service principal id, not the application id itself
    // because of this, while it is called Applications, it's really returning service principal ids
    // this is OK and helps cut down on calls, since AppRoleAssignment is done via the resourceId (aka the ServicePrincipalId)
    public class OrganizationFilteredApplicationRepository : FilteredRepository, IApplicationRepository
    {
        private readonly Guid _adminAppRoleId;
        private IEnumerable<AppRoleAssignment> _userAdminAppRoleCache;

        public OrganizationFilteredApplicationRepository(
            GraphServiceClient client,
            IHttpContextAccessor httpContext,
            IOptions<OrganizationOptions> options
        ) : base(client, httpContext.HttpContext.User, options)
        {
            // working around a bit of graph type inconsistency
            _adminAppRoleId = Guid.Parse(_options.ApplicationOrgAdministratorRoleId);
        }

        private async Task<IEnumerable<AppRoleAssignment>> GetResourcesUserCanAdminister(bool refresh = false)
        {
            if (_userAdminAppRoleCache != null && !refresh)
            {
                return _userAdminAppRoleCache;
            }
            // get the user's app roles
            var assignments = await _graphClient.Users[_callingUserId]
                .AppRoleAssignments
                // graph does not yet supporting filtering on appRoleId in assignments, in talks to get this field added
                // that filter will trim this call considerably
                .Request()
                .Select(_options.AppRoleAssignmentFieldSelection)
                .GetAsync();

            // filter to those they can administer
            var apps = assignments.Where(y => y.AppRoleId == _adminAppRoleId);
            _userAdminAppRoleCache = apps;
            return apps;
        }

        private async Task<AppRoleAssignment> GetResourceUserCanAdminister(Guid resourceId)
        {
            var apps = await GetResourcesUserCanAdminister();
            return apps.SingleOrDefault(x => x.ResourceId == resourceId);
        }

        // this returns resourceId, which is technically the service principal id, not the application id itself
        // because of this, while it is called Applications, it's really returning service principal ids
        // this is OK and helps cut down on calls, since AppRoleAssignment is done via the resourceId (aka the ServicePrincipalId)
        public async Task<IEnumerable<UserApplication>> GetResources()
        {
            var apps = await GetResourcesUserCanAdminister();
            return apps.Select(x => new UserApplication()
            {
                ResourceId = x.ResourceId?.ToString(),
                DisplayName = x.ResourceDisplayName
            });
        }

        public async Task<UserApplication> GetResource(Guid resourceId)
        {
            var app = await GetResourceUserCanAdminister(resourceId);
            return new UserApplication()
            {
                ResourceId = app.ResourceId.ToString(),
                DisplayName = app.ResourceDisplayName
            };

            // not sure this is actually necessary - yet
            // return app.GroupBy(x => x.ResourceId).Select(appRoles => new { Id = appRoles.Key, Roles = appRoles }).Select(app => new UserApplication()
            // {
            //     ResourceId = app.Id.Value.ToString(),
            //     DisplayName = app.Roles.First().ResourceDisplayName,
            //     UserAssignedAppRoles = app.Roles
            // }).Single();
        }

        public async Task<IEnumerable<AppRole>> GetAppRolesByResource(Guid resourceId)
        {
            var app = await GetResource(resourceId);
            if (app == null) return new List<AppRole>();

            var appId = await _graphClient.ServicePrincipals[app.ResourceId.ToString()].Request().Select("appId").GetAsync();
            if (appId == null) return new List<AppRole>();

            var appRoles = await _graphClient.Applications.Request().Filter($"appId eq {appId.AppId}").Select("appRoles").GetAsync();
            return appRoles.SingleOrDefault().AppRoles;
        }

        public async Task<AppRoleAssignment> AssignAppRole(AppRoleAssignment request)
        {
            return await AssignAppRole(request.PrincipalId.Value, request.ResourceId.Value, request.AppRoleId.Value);
        }

        public async Task<AppRoleAssignment> AssignAppRole(Guid targetPrincipalId, Guid resourceId, Guid appRoleId)
        {
            var resource = await GetResourceUserCanAdminister(resourceId);
            if (resource == null) return null;

            var principal = await _graphClient.Users[targetPrincipalId.ToString()]
              .Request()
              .Select(_options.UserFieldSelection)
              .GetAsync();

            // todo: wrap this in ServiceResult or similar
            if (!principal.AdditionalData.Any()) return null;
            bool userOrgMatch = false;
            if (principal.AdditionalData.ContainsKey(_options.OrgIdExtensionName))
            {
                var orgData = principal.AdditionalData[_options.OrgIdExtensionName].ToString();
                userOrgMatch = string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase);
            }

            if (userOrgMatch) // assign only if match
            {
                var assignment = new AppRoleAssignment()
                {
                    ResourceId = resource.ResourceId,
                    PrincipalId = Guid.Parse(principal.Id),
                    AppRoleId = appRoleId
                };
                await _graphClient.ServicePrincipals[resource.ResourceId.ToString()].AppRoleAssignedTo.Request().AddAsync(assignment);
                return assignment;
            }
            return null;
        }

        // todo: this will have to be paged & searchable - too much potential to be too large
        // todo: cache this
        public async Task<IEnumerable<AppRoleAssignment>> GetAppRoleAssignmentsByResource(Guid resourceId)
        {
            var app = await GetResourceUserCanAdminister(resourceId);
            if (app == null) return new List<AppRoleAssignment>();

            var assignments = await _graphClient.ServicePrincipals[app.ResourceId.ToString()]
                .AppRoleAssignedTo
                .Request()
                .Select(_options.AppRoleAssignmentFieldSelection)
                .GetAsync();

            // get users in org --- eeeeeeeek
            var filter = new QueryOption("$filter", $"{_options.OrgIdExtensionName} eq '{_orgId}'");
            var users = await _graphClient.Users
                    .Request(new List<QueryOption>() { filter })
                    .Select(_options.UserFieldSelection)
                    .GetAsync()
                    ;
            // filter assignments to users in org
            var userIdList = assignments.Select(x => x.PrincipalId.ToString()).Intersect(users.Select(x => x.Id));

            // todo: resolve role values
            return assignments.Where(x => users.Any(u => u.Id == x.PrincipalId.ToString()));
        }
    }
}