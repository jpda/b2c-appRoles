using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace B2CAuthZ.Admin.WebApiHost
{
    public interface IApplicationRepository
    {
        Task<AppRoleAssignment> AssignAppRole(Guid principalId, Guid resourceId, Guid appRoleId);
        Task<AppRoleAssignment> AssignAppRole(AppRoleAssignment request);
        Task<UserApplication> GetResource(Guid resourceId);
        Task<IEnumerable<UserApplication>> GetResources();
        Task<IEnumerable<AppRole>> GetAppRolesByResource(Guid resourceId);
        Task<IEnumerable<AppRoleAssignment>> GetAppRoleAssignmentsByResource(Guid resourceId);
    }
}