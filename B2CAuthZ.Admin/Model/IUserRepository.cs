using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace B2CAuthZ.Admin
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetUsers();
        Task<User> GetUser(string userId);
        Task<User> FindUserBySignInName(string userSignInName);
        Task<IEnumerable<OrganizationUser>> GetOrganizationUsers();
        Task<OrganizationUser> GetOrganizationUser(string userId);
        Task<OrganizationUser> FindOrganizationUserBySignInName(string userSignInName);
        Task<IEnumerable<AppRoleAssignment>> GetUserAppRoleAssignments(User u);
        Task<IEnumerable<AppRoleAssignment>> GetUserAppRoleAssignments(string userObjectId);
        Task<OrganizationUser> SetUserOrganization(OrganizationMembership membership);
        Task<IEnumerable<OrganizationUser>> SearchUser(string query);
    }
}