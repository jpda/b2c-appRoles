using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web.Resource;
using B2CAuthZ.Admin;

namespace B2CAuthZ.Admin.WebApiHost.Controllers
{
    [Authorize]
    [RequiredScope("Access")]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("v{v:apiVersion}/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IUserRepository _userRepo;

        public UsersController(ILogger<UsersController> logger, IUserRepository userRepo)
        {
            _logger = logger;
            _userRepo = userRepo;
        }

        [HttpGet]
        public async Task<IEnumerable<OrganizationUser>> GetUsers() => await _userRepo.GetOrganizationUsers();
        // {
        //     return new OkObjectResult(await _userRepo.GetUsers());
        // }

        [HttpGet]
        [Route("{userId:guid}")]
        public async Task<OrganizationUser> Get(Guid userId) => await _userRepo.GetOrganizationUser(userId.ToString());
        // {
        //     return new OkObjectResult(await _userRepo.GetUser(userId.ToString()));
        // }

        [HttpGet]
        [Route("{userId:guid}/appRoleAssignments")]
        public async Task<IEnumerable<AppRoleAssignment>> GetUserAppRoleAssignments(Guid userId) => await _userRepo.GetUserAppRoleAssignments(userId.ToString());
        // {
        //     return new OkObjectResult(await _userRepo.GetUserAppRoleAssignments(userId.ToString()));
        // }
    }


}
