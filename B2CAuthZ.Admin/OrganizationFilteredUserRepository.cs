using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace B2CAuthZ.Admin
{
    [Obsolete("Inject the repositories directly, this is used by FuncHost only")]
    public class UserRepositoryFactory
    {
        private readonly GraphServiceClient _graphClient;
        private readonly IOptions<OrganizationOptions> _orgOptions;
        public UserRepositoryFactory(GraphServiceClient client, IOptions<OrganizationOptions> options)
        {
            _graphClient = client;
            _orgOptions = options;
        }
        public IUserRepository CreateForOrgId(System.Security.Claims.ClaimsPrincipal user)
        {
            return new OrganizationFilteredUserRepository(_graphClient, user, _orgOptions);
        }
    }

    public class OrganizationFilteredUserRepository : FilteredRepository, IUserRepository
    {
        // todo: better way to get the user data in here without using the httpContext
        public OrganizationFilteredUserRepository(
            GraphServiceClient client,
            IHttpContextAccessor httpContext,
            IOptions<OrganizationOptions> options
        ) : base(client, httpContext.HttpContext.User, options) { }

        public OrganizationFilteredUserRepository(
            GraphServiceClient client,
            System.Security.Claims.ClaimsPrincipal user,
            IOptions<OrganizationOptions> options
        ) : base(client, user, options) { }

        public async Task<User> GetUser(string userId)
        {
            var user = await _graphClient.Users[userId]
                .Request()
                .Select(_options.UserFieldSelection)
                .GetAsync();

            // todo: wrap this in ServiceResult or similar
            if (!user.AdditionalData.Any()) return null;
            if (user.AdditionalData.ContainsKey(_options.OrgIdExtensionName))
            {
                var orgData = user.AdditionalData[_options.OrgIdExtensionName].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    return user;
                }
            }
            return null;
        }

        public async Task<User> FindUserBySignInName(string userSignInName)
        {
            var filter = new QueryOption(
                "$filter"
                , $"identities/any(x:x/issuer eq '{_options.TenantIssuerName}' and x/issuerAssignedId eq '{userSignInName}')"
                );
            var userList = await _graphClient.Users
                .Request(new List<QueryOption>() { filter })
                .Select(_options.UserFieldSelection)
                .GetAsync();

            if (!userList.Any()) throw new Exception("user not found");
            if (userList.Count > 1) throw new Exception("too many users");

            var user = userList.Single();
            if (!user.AdditionalData.Any()) throw new Exception("user doesn't have an orgid");

            if (user.AdditionalData.ContainsKey(_options.OrgIdExtensionName))
            {
                var orgData = user.AdditionalData[_options.OrgIdExtensionName].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    return user;
                }
            }
            throw new Exception("user has no org id or malformed");
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            var filter = new QueryOption("$filter", $"{_options.OrgIdExtensionName} eq '{_orgId}'");
            var users = await _graphClient.Users
                .Request(new List<QueryOption>() { filter })
                .Select(_options.UserFieldSelection)
                .GetAsync();
            return users;
        }

        public async Task<IEnumerable<AppRoleAssignment>> GetUserAppRoleAssignments(User u)
        {
            return await this.GetUserAppRoleAssignments(u.Id);
        }
        public async Task<IEnumerable<AppRoleAssignment>> GetUserAppRoleAssignments(string userObjectId)
        {
            var user = await _graphClient.Users[userObjectId]
                .Request()
                .Select(_options.UserFieldSelection)
                .GetAsync();

            if (!user.AdditionalData.Any()) return null;
            if (user.AdditionalData.ContainsKey(_options.OrgIdExtensionName))
            {
                var orgData = user.AdditionalData[_options.OrgIdExtensionName].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    return await _graphClient.Users[userObjectId].AppRoleAssignments
                        .Request()
                        .GetAsync();
                }
            }
            return new List<AppRoleAssignment>();
        }

        public async Task<OrganizationUser> SetUserOrganization(OrganizationMembership membership)
        {
            if (membership.OrgId != _orgId) return null; // get out, user is trying to add a user to a different org than their own

            // get the target user
            var userRequest = _graphClient.Users[membership.UserId]
              .Request()
              .Select(_options.UserFieldSelection)
              ;
            var user = await userRequest.GetAsync();

            if (!user.AdditionalData.Any())  // no org, let's set a new one
            {
                user.AdditionalData[_options.OrgIdExtensionName] = membership.OrgId;
                user.AdditionalData[_options.OrgRoleExtensionName] = membership.Role;
                await userRequest.UpdateAsync(user);
                return new OrganizationUser(user, _options.OrgIdExtensionName, _options.OrgRoleExtensionName);
            }

            if (user.AdditionalData.ContainsKey(_options.OrgIdExtensionName))
            {
                var orgData = user.AdditionalData[_options.OrgIdExtensionName].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    // already in org, set role
                    user.AdditionalData[_options.OrgRoleExtensionName] = membership.Role;
                    await userRequest.UpdateAsync(user);
                    return new OrganizationUser(user, _options.OrgIdExtensionName, _options.OrgRoleExtensionName);
                }
            }
            return new OrganizationUser(user, _options.OrgIdExtensionName, _options.OrgRoleExtensionName);
        }

        public async Task<OrganizationUser> GetOrganizationUser(string userId)
        {
            var user = await GetUser(userId);
            return new OrganizationUser(user, _options.OrgIdExtensionName, _options.OrgRoleExtensionName);
        }

        public async Task<IEnumerable<OrganizationUser>> GetOrganizationUsers()
        {
            var users = await GetUsers();
            return users.Select(x => new OrganizationUser(x, _options.OrgIdExtensionName, _options.OrgRoleExtensionName));
        }

        public async Task<OrganizationUser> FindOrganizationUserBySignInName(string name)
        {
            var user = await FindUserBySignInName(name);
            return new OrganizationUser(user, _options.OrgIdExtensionName, _options.OrgRoleExtensionName);
        }
    }
}