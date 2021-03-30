using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web.Resource;

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
        public async Task<IActionResult> GetUsers()
        {
            return new OkObjectResult(await _userRepo.GetUsers());
        }

        [HttpGet]
        [Route("{userId:guid}")]
        public async Task<IActionResult> Get(Guid userId)
        {
            return new OkObjectResult(await _userRepo.GetUser(userId.ToString()));
        }

        [HttpGet]
        [Route("{userId:guid}/appRoleAssignments")]
        public async Task<IActionResult> GetUserAppRoleAssignments(Guid userId)
        {
            return new OkObjectResult(await _userRepo.GetUserAppRoleAssignments(userId.ToString()));
        }
    }
}
