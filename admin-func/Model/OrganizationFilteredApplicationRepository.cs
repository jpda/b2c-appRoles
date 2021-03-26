using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Linq;

namespace admin_func
{
    // apps & service principals are tightly related but not the same. we want to greatly trim down on graph calls
    // this means we're going to say 'applications' but what we really mean is 'service principals' - 
    // a service principal is essentially an 'instance' of an application - but since these are single-tenant apps,
    // the service principal & application will largely have the same properties
    public class ApplicationRepositoryFactory
    {
        private readonly GraphServiceClient _graphClient;
        public ApplicationRepositoryFactory(GraphServiceClient client)
        {
            _graphClient = client;
        }
        public IApplicationRepository CreateForOrgId(string orgId)
        {
            return new OrganizationFilteredApplicationRepository(_graphClient, orgId);
        }
    }

    public class OrganizationFilteredApplicationRepository : FilteredRepository, IApplicationRepository
    {
        // todo: move to config and document, document, document
        private Guid ADMIN_APPROLE_ID = Guid.Parse("b3e0dfe9-f58d-4c35-97d3-a4efc954ba6e");
        public OrganizationFilteredApplicationRepository(GraphServiceClient client, string orgId) : base(client, orgId) { }

        // apps the user is allowed to administer _only_
        public async Task<IEnumerable<UserApplication>> GetApplications(string userId)
        {
            // get the user's app roles
            var assignments = await _graphClient.Users[userId]
                .AppRoleAssignments
                .Request()
                .Select("appRoleId,principalId,resourceId,resourceDisplayName")
                .GetAsync();

            // filter to those they can administer
            var apps = assignments.Where(y => y.AppRoleId == ADMIN_APPROLE_ID);
            return apps.Select(x => new UserApplication()
            {
                ServicePrincipalId = x.ResourceId?.ToString(),
                DisplayName = x.ResourceDisplayName,
                //UserAssignedAppRoles = assignments?.Where(x=>ToList()
            });
        }

        public async Task<UserApplication> GetApplication(string userId, string resourceId)
        {
            // get the user's app roles
            var assignments = await _graphClient.Users[userId]
                .AppRoleAssignments
                .Request()
                .Select("appRoleId,principalId,resourceId,resourceDisplayName")
                .GetAsync();

            // filter to those they can administer
            var app = assignments.SingleOrDefault(y => y.AppRoleId == ADMIN_APPROLE_ID && y.ResourceId == Guid.Parse(resourceId));
            if (app == null) return new UserApplication();

            return new UserApplication()
            {
                ServicePrincipalId = app.ResourceId?.ToString(),
                DisplayName = app.ResourceDisplayName,
                UserAssignedAppRoles = assignments?.ToList()
            };
        }

        public async Task<IEnumerable<AppRole>> GetAppRolesByServicePrincipal(string userId, string resourceId)
        {
            // get the user's app roles
            var assignments = await _graphClient.Users[userId]
                .AppRoleAssignments
                .Request()
                .Select("appRoleId,principalId,resourceId,resourceDisplayName")
                .GetAsync();

            // filter to those they can administer
            var app = assignments.SingleOrDefault(y => y.AppRoleId == ADMIN_APPROLE_ID && y.ResourceId == Guid.Parse(resourceId));
            if (app == null) return new List<AppRole>(); // todo: figure out the best way to say unauthorized here

            var appId = await _graphClient.ServicePrincipals[app.ResourceId.ToString()].Request().Select("appId").GetAsync();
            if (appId == null) return new List<AppRole>();

            var appRoles = await _graphClient.Applications.Request().Filter($"appId eq {appId.AppId}").Select("appRoles").GetAsync();
            return appRoles.SingleOrDefault().AppRoles;
        }

        public async Task<AppRoleAssignment> AssignAppRole(string userId, string principalId, string resourceId, string appRoleId)
        {
            // get the user's app roles
            var assignments = await _graphClient.Users[userId]
                .AppRoleAssignments
                .Request()
                .Select("appRoleId,principalId,resourceId,resourceDisplayName")
                .GetAsync();

            // filter to those they can administer
            var app = assignments.SingleOrDefault(y => y.AppRoleId == ADMIN_APPROLE_ID && y.ResourceId == Guid.Parse(resourceId));
            if (app == null) return null;

            // check if user is part of organization
            // get the user's app roles
            var user = await _graphClient.Users[principalId]
              .Request()
              .Select(userFieldSelection)
              .GetAsync();

            string callerOrgId = string.Empty;
            bool userOrgMatch = false;

            if (!user.AdditionalData.Any()) return null;
            if (user.AdditionalData.ContainsKey(orgIdExtension))
            {
                var orgData = user.AdditionalData[orgIdExtension].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    callerOrgId = orgData;
                }
            }

            var principal = await _graphClient.Users[principalId]
              .Request()
              .Select(userFieldSelection)
              .GetAsync();

            // todo: wrap this in ServiceResult or similar
            if (!principal.AdditionalData.Any()) return null;
            if (principal.AdditionalData.ContainsKey(orgIdExtension))
            {
                var orgData = principal.AdditionalData[orgIdExtension].ToString();
                userOrgMatch = string.Equals(orgData, callerOrgId, StringComparison.OrdinalIgnoreCase);
            }

            if (userOrgMatch) // assign only if match
            {
                var assignment = new AppRoleAssignment()
                {
                    ResourceId = app.ResourceId,
                    PrincipalId = Guid.Parse(user.Id),
                    AppRoleId = Guid.Parse(appRoleId)
                };
                var resource = await _graphClient.ServicePrincipals[app.ResourceId.ToString()].AppRoleAssignedTo.Request().AddAsync(assignment);
                return assignment; // todo: don't return this
            }
            return null;
        }

        // todo: this will have to be paged & searchable- too much potential to be too large
        public async Task<IEnumerable<AppRoleAssignment>> GetAppRoleAssignmentsByServicePrincipal(string userId, string servicePrincipalId)
        {
            // get user org

            var assignments = await _graphClient.ServicePrincipals[servicePrincipalId]
                .AppRoleAssignedTo
                .Request()
                .Select("appRoleId,principalId,resourceId,resourceDisplayName")
                .GetAsync();

            // get the user's app roles
            // filter to those they can administer
            var app = assignments.SingleOrDefault(y => y.AppRoleId == ADMIN_APPROLE_ID && y.ResourceId == Guid.Parse(servicePrincipalId));
            if (app == null) return null;

            // get users in org --- eeeeeeeek
            var filter = new QueryOption("$filter", $"{orgIdExtension} eq '{_orgId}'");
            var users = await _graphClient.Users
                    .Request(new List<QueryOption>() { filter })
                    .Select(userFieldSelection)
                    .GetAsync()
                    ;
            // filter assignments to users in org
            var userIdList = assignments.Select(x => x.PrincipalId.ToString()).Intersect(users.Select(x => x.Id));

            //assignments.Intersect()
            // todo: resolve role values
            return assignments.Where(x => users.Any(u => u.Id == x.PrincipalId.ToString()));
        }
    }
}