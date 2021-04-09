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

    public static class GraphClientExtensions
    {
        public static IBaseRequest AddOrganizationFilter(this IBaseRequest req, string orgId, string orgIdExtensionName)
        {
            req.QueryOptions.Add(new QueryOption("$filter", $"{orgIdExtensionName} eq '${orgId}'"));
            return req;
        }
        public static T AddOrganizationFilter<T>(this T req, string orgId, OrganizationOptions options) where T : IBaseRequest
        {
            req.QueryOptions.Add(new QueryOption("$filter", $"{options.OrgIdExtensionName} eq '${orgId}'"));
            return req;
        }
        public static bool VerifyAccess(this Microsoft.Graph.User user, string orgId, OrganizationOptions options)
        {
            if (!user.AdditionalData.Any()) return false;
            if (user.AdditionalData == null || user.AdditionalData.ContainsKey(options.OrgIdExtensionName))
            {
                var orgData = user.AdditionalData[options.OrgIdExtensionName].ToString();
                return string.Equals(orgData, orgId, StringComparison.OrdinalIgnoreCase);
            }
            return false;
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

        public async Task<ServiceResult<User>> GetUser(string userId)
        {
            var user = await _graphClient.Users[userId]
                .Request()
                .Select(_options.UserFieldSelection)
                .GetAsync();

            if (user.VerifyAccess(_orgId, _options))
            {
                return ServiceResult<User>.FromResult(user);
            }
            return ServiceResult<User>.FromError("Not found");
        }

        public async Task<ServiceResult<User>> FindUserBySignInName(string userSignInName)
        {
            var filter = new QueryOption(
                "$filter"
                , $"identities/any(x:x/issuer eq '{_options.TenantIssuerName}' and x/issuerAssignedId eq '{userSignInName}')"
                );
            var userList = await _graphClient.Users
                .Request(new List<QueryOption>() { filter })
                .Select(_options.UserFieldSelection)
                .GetAsync();

            if (!userList.Any()) return ServiceResult<User>.FromError("user not found");
            if (userList.Count > 1) return ServiceResult<User>.FromError("too many users");

            var user = userList.Single();
            if (user.VerifyAccess(_orgId, _options))
            {
                return ServiceResult<User>.FromResult(user);
            }
            return ServiceResult<User>.FromError("user has no org id or malformed");
        }

        public async Task<ServiceResult<IEnumerable<User>>> GetUsers()
        {
            var filter = new QueryOption("$filter", $"{_options.OrgIdExtensionName} eq '{_orgId}'");
            var users = await _graphClient.Users
                .Request(new List<QueryOption>() { filter })
                .Select(_options.UserFieldSelection)
                .GetAsync();

            return ServiceResult<IEnumerable<User>>.FromResult(users.AsEnumerable());
        }

        public async Task<ServiceResult<IEnumerable<AppRoleAssignment>>> GetUserAppRoleAssignments(User u)
        {
            return await this.GetUserAppRoleAssignments(u.Id);
        }
        public async Task<ServiceResult<IEnumerable<AppRoleAssignment>>> GetUserAppRoleAssignments(string userObjectId)
        {
            var user = await _graphClient.Users[userObjectId]
                .Request()
                .Select(_options.UserFieldSelection)
                .GetAsync();

            if (user.VerifyAccess(_orgId, _options))
            {
                var results = await _graphClient.Users[userObjectId].AppRoleAssignments
                      .Request()
                      .GetAsync();
                return ServiceResult<IEnumerable<AppRoleAssignment>>.FromResult(results);
            }
            return ServiceResult<IEnumerable<AppRoleAssignment>>.FromError("No roles found");
        }

        public async Task<ServiceResult<OrganizationUser>> SetUserOrganization(OrganizationMembership membership)
        {
            if (membership.OrgId != _orgId) return null; // get out, user is trying to add a user to a different org than their own

            // get the target user
            var userRequest = _graphClient.Users[membership.UserId]
              .Request()
              .Select(_options.UserFieldSelection)
              ;
            var user = await userRequest.GetAsync();

            if (user.AdditionalData == null || !user.AdditionalData.Any())  // no org, let's set a new one
            {
                user.AdditionalData[_options.OrgIdExtensionName] = membership.OrgId;
                user.AdditionalData[_options.OrgRoleExtensionName] = membership.Role;
                await userRequest.UpdateAsync(user);
                return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user, _options));
            }

            if (user.AdditionalData.ContainsKey(_options.OrgIdExtensionName))
            {
                var orgData = user.AdditionalData[_options.OrgIdExtensionName].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    // already in org, set role
                    user.AdditionalData[_options.OrgRoleExtensionName] = membership.Role;
                    await userRequest.UpdateAsync(user);
                    return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user, _options));
                }
            }
            return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user, _options));
        }

        public async Task<ServiceResult<OrganizationUser>> GetOrganizationUser(string userId)
        {
            var user = await GetUser(userId);
            if (user.Success)
            {
                return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user.Value, _options));
            }
            return ServiceResult<OrganizationUser>.FromError(user.Exception);
        }

        public async Task<ServiceResult<IEnumerable<OrganizationUser>>> GetOrganizationUsers()
        {
            var users = await this.GetUsers();
            if (users.Success)
            {
                return ServiceResult<IEnumerable<OrganizationUser>>.FromResult(users.Value.Select(x => new OrganizationUser(x, _options)));
            }
            return ServiceResult<IEnumerable<OrganizationUser>>.FromError(users.Exception);
        }

        public async Task<ServiceResult<OrganizationUser>> FindOrganizationUserBySignInName(string name)
        {
            var user = await FindUserBySignInName(name);
            if (user.Success)
            {
                return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user.Value, _options));
            }
            return ServiceResult<OrganizationUser>.FromError(user.Exception);
        }

        public async Task<ServiceResult<IEnumerable<OrganizationUser>>> SearchUser(string query)
        {
            throw new NotImplementedException();
        }
    }
}