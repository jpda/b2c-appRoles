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

        public Task<AppRoleAssignment> AssignAppRole(Guid principalId, Guid resourceId, Guid appRoleId)
        {
            throw new NotImplementedException();
        }

        public Task<AppRoleAssignment> AssignAppRole(AppRoleAssignment request)
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