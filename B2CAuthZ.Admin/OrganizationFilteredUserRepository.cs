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

        public async Task<ServiceResult<User>> GetUser(string userId)
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
                    return ServiceResult<User>.FromResult(user);
                }
            }
            return null;
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
            if (user.AdditionalData == null || user.AdditionalData.Any()) return ServiceResult<User>.FromError("user doesn't have an orgid");

            if (user.AdditionalData.ContainsKey(_options.OrgIdExtensionName))
            {
                var orgData = user.AdditionalData[_options.OrgIdExtensionName].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult<User>.FromResult(user);
                }
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

            if (user.AdditionalData == null || !user.AdditionalData.Any()) return null;
            if (user.AdditionalData.ContainsKey(_options.OrgIdExtensionName))
            {
                var orgData = user.AdditionalData[_options.OrgIdExtensionName].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    var results = await _graphClient.Users[userObjectId].AppRoleAssignments
                        .Request()
                        .GetAsync();
                    return ServiceResult<IEnumerable<AppRoleAssignment>>.FromResult(results);
                }
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

            if (!user.AdditionalData.Any())  // no org, let's set a new one
            {
                user.AdditionalData[_options.OrgIdExtensionName] = membership.OrgId;
                user.AdditionalData[_options.OrgRoleExtensionName] = membership.Role;
                await userRequest.UpdateAsync(user);
                return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user, _options.OrgIdExtensionName, _options.OrgRoleExtensionName));
            }

            if (user.AdditionalData.ContainsKey(_options.OrgIdExtensionName))
            {
                var orgData = user.AdditionalData[_options.OrgIdExtensionName].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    // already in org, set role
                    user.AdditionalData[_options.OrgRoleExtensionName] = membership.Role;
                    await userRequest.UpdateAsync(user);
                    return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user, _options.OrgIdExtensionName, _options.OrgRoleExtensionName));
                }
            }
            return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user, _options.OrgIdExtensionName, _options.OrgRoleExtensionName));
        }

        public async Task<ServiceResult<OrganizationUser>> GetOrganizationUser(string userId)
        {
            var user = await GetUser(userId);
            if (user.Success)
            {
                return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user.Value, _options.OrgIdExtensionName, _options.OrgRoleExtensionName));
            }
            return ServiceResult<OrganizationUser>.FromError(user.Exception);
        }

        public async Task<ServiceResult<IEnumerable<OrganizationUser>>> GetOrganizationUsers()
        {
            var users = await this.GetUsers();
            if (users.Success)
            {
                return ServiceResult<IEnumerable<OrganizationUser>>.FromResult(users.Value.Select(x => new OrganizationUser(x, _options.OrgIdExtensionName, _options.OrgRoleExtensionName)));
            }
            return ServiceResult<IEnumerable<OrganizationUser>>.FromError(users.Exception);
        }

        public async Task<ServiceResult<OrganizationUser>> FindOrganizationUserBySignInName(string name)
        {
            var user = await FindUserBySignInName(name);
            if (user.Success)
            {
                return ServiceResult<OrganizationUser>.FromResult(new OrganizationUser(user.Value, _options.OrgIdExtensionName, _options.OrgRoleExtensionName));
            }
            return ServiceResult<OrganizationUser>.FromError(user.Exception);
        }

        public async Task<ServiceResult<IEnumerable<OrganizationUser>>> SearchUser(string query)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class ServiceResult
    {
        public string Message { get; set; }
        public bool Success { get; set; }
        public Exception Exception { get; set; }
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T Value { get; set; }
        public ServiceResult() { }

        public ServiceResult(T value)
        {
            Value = value;
            Success = true;
        }

        public static ServiceResult<T> FromError(string error)
        {
            return new ServiceResult<T>()
            {
                Message = error,
                Success = false,
                Exception = new Exception(error)
            };
        }
        public static ServiceResult<T> FromError(Exception ex)
        {
            return new ServiceResult<T>()
            {
                Message = ex.Message,
                Success = false,
                Exception = ex
            };
        }

        public static ServiceResult<T> FromResult(T thing)
        {
            return new ServiceResult<T>(thing);
        }
        public static ServiceResult<T> FromError(string message, T value)
        {
            var result = FromError(message);
            result.Value = value;
            return result;
        }

        public static ServiceResult<T> FromError(Exception ex, T value)
        {
            var result = FromError(ex);
            result.Value = value;
            return result;
        }
    }
}