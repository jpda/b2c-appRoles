using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Linq;

namespace admin_func
{
    // the factory is here to give us org-scoped repositories - don't really like this
    // will probably revise in the future - but it is here for now
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

        public IApplicationRepository Create()
        {
            return new GlobalApplicationRepository(_graphClient);
        }
    }

    // strictly for global administrators - people who can see everything in the directory, no org filtering
    public class GlobalApplicationRepository : IApplicationRepository
    {
        private readonly GraphServiceClient _graphClient;
        public GlobalApplicationRepository(GraphServiceClient client)
        {
            _graphClient = client;
        }

        public Task<AppRoleAssignment> AssignAppRole(string userId, string principalId, string resourceId, string appRoleId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<AppRoleAssignment>> GetAppRoleAssignmentsByResource(string userId, string resourceId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<AppRole>> GetAppRolesByResource(string userId, string resourceId)
        {
            throw new NotImplementedException();
        }

        public Task<UserApplication> GetResource(string userId, string resourceId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserApplication>> GetResources(string userId)
        {
            throw new NotImplementedException();
        }
    }

    // this repository is filtered based on the user
    public class OrganizationFilteredApplicationRepository : FilteredRepository, IApplicationRepository
    {
        // todo: move to config and document, document, document
        private Guid ADMIN_APPROLE_ID = Guid.Parse("b3e0dfe9-f58d-4c35-97d3-a4efc954ba6e");
        public OrganizationFilteredApplicationRepository(GraphServiceClient client, string orgId) : base(client, orgId) { }

        // apps the user is allowed to administer _only_, based on membership in the ADMIN_APPROLE_ID role
        // this returns resourceId, which is technically the service principal id, not the application id itself
        // because of this, while it is called Applications, it's really returning service principal ids
        // this is OK and helps cut down on calls, since AppRoleAssignment is done via the resourceId (aka the ServicePrincipalId)
        public async Task<IEnumerable<UserApplication>> GetResources(string userId)
        {
            // get the user's app roles
            var assignments = await _graphClient.Users[userId]
                .AppRoleAssignments
                // graph does not yet supporting filtering on appRoleId in assignments, in talks to get this field added
                // that filter will trim this call considerably
                .Request()
                .Select("appRoleId,principalId,resourceId,resourceDisplayName")
                .GetAsync();

            // filter to those they can administer
            var apps = assignments.Where(y => y.AppRoleId == ADMIN_APPROLE_ID);
            return apps.Select(x => new UserApplication()
            {
                ResourceId = x.ResourceId?.ToString(),
                DisplayName = x.ResourceDisplayName,
                //UserAssignedAppRoles = assignments?.Where(x=>ToList()
            });
        }

        public async Task<UserApplication> GetResource(string userId, string resourceId)
        {
            // get the user's app roles
            var assignments = await _graphClient.Users[userId]
                .AppRoleAssignments
                // graph does not yet supporting filtering on appRoleId in assignments, in talks to get this field added
                // that filter will trim this call considerably
                .Request()
                .Select("appRoleId,principalId,resourceId,resourceDisplayName")
                .GetAsync();

            // filter to those they can administer
            var app = assignments.SingleOrDefault(y => y.AppRoleId == ADMIN_APPROLE_ID && y.ResourceId == Guid.Parse(resourceId));
            if (app == null) return new UserApplication();

            return new UserApplication()
            {
                ResourceId = app.ResourceId?.ToString(),
                DisplayName = app.ResourceDisplayName,
                UserAssignedAppRoles = assignments?.ToList()
            };
        }

        public async Task<IEnumerable<AppRole>> GetAppRolesByResource(string userId, string resourceId)
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

        // todo: this will have to be paged & searchable - too much potential to be too large
        // todo: cache this
        public async Task<IEnumerable<AppRoleAssignment>> GetAppRoleAssignmentsByResource(string userId, string resourceId)
        {
            // get user org

            var assignments = await _graphClient.ServicePrincipals[resourceId]
                .AppRoleAssignedTo
                .Request()
                .Select("appRoleId,principalId,resourceId,resourceDisplayName")
                .GetAsync();

            // get the user's app roles
            // filter to those they can administer
            var app = assignments.SingleOrDefault(y => y.AppRoleId == ADMIN_APPROLE_ID && y.ResourceId == Guid.Parse(resourceId));
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