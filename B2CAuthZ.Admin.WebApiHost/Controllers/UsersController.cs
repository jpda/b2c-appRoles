using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web.Resource;
//using B2CAuthZ.Admin;

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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OrganizationUser>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Exception))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUsers() => await GenerateReturn(async () => await _userRepo.GetOrganizationUsers());

        [HttpGet]
        [Route("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrganizationUser))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Exception))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(Guid userId) => await GenerateReturn(async () => await _userRepo.GetOrganizationUser(userId.ToString()));

        [HttpGet]
        [Route("search/{query}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrganizationUser))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Exception))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(string query) => await GenerateReturn(async () => await _userRepo.FindOrganizationUserBySignInName(query));

        [HttpGet]
        [Route("{userId:guid}/appRoleAssignments")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AppRoleAssignment>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Exception))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserAppRoleAssignments(Guid userId) => await GenerateReturn(() => _userRepo.GetUserAppRoleAssignments(userId.ToString()));

        private async Task<IActionResult> GenerateReturn<T>(Func<Task<ServiceResult<T>>> work)
        {
            var result = await work();
            if (result.Success)
            {
                return new OkObjectResult(result.Value);
            }
            return new BadRequestObjectResult(result.Exception);
        }
    }
}