using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace B2CAuthZ.Admin
{
    // strictly for global administrators - people who can see everything in the directory, no org filtering
    public class GlobalApplicationRepository : IApplicationRepository
    {
        private readonly GraphServiceClient _graphClient;
        public GlobalApplicationRepository(GraphServiceClient client)
        {
            _graphClient = client;
        }

        public async Task<AppRoleAssignment> AssignAppRole(Guid principalId, Guid resourceId, Guid appRoleId)
        {

            var principal = await _graphClient.Users[principalId.ToString()]
                .Request()
                .GetAsync();

            var assignment = new AppRoleAssignment()
            {
                ResourceId = resourceId,
                PrincipalId = Guid.Parse(principal.Id),
                AppRoleId = appRoleId
            };
            return await _graphClient.ServicePrincipals[resourceId.ToString()].AppRoleAssignedTo.Request().AddAsync(assignment);
        }

        public Task<AppRoleAssignment> AssignAppRole(AppRoleAssignment request)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAppRoleAssignmentByResource(Guid resourceId, string appRoleAssignmentId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<AppRoleAssignment>> GetAppRoleAssignmentsByResource(Guid resourceId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<AppRole>> GetAppRolesByResource(Guid resourceId)
        {
            throw new NotImplementedException();
        }

        public Task<UserApplication> GetResource(Guid resourceId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserApplication>> GetResources()
        {
            throw new NotImplementedException();
        }
    }
}