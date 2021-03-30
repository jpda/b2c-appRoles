using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace B2CAuthZ.Admin.WebApiHost
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetUsers();
        Task<User> GetUser(string userId);
        Task<User> FindUserBySignInName(string userSignInName);
        Task<IEnumerable<AppRoleAssignment>> GetUserAppRoleAssignments(User u);
        Task<IEnumerable<AppRoleAssignment>> GetUserAppRoleAssignments(string userObjectId);
        Task<User> SetUserOrganization(OrganizationMembership membership);
    }
}