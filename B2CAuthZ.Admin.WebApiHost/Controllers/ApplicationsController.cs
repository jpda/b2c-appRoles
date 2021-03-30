using System;
using System.Linq;
using System.Security.Claims;
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
    [Route("v{v:apiVersion}")]
    public class ApplicationsController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IApplicationRepository _appsRepo;

        public ApplicationsController(ILogger<UsersController> logger, IApplicationRepository appsRepo)
        {
            _logger = logger;
            _appsRepo = appsRepo;
        }

        [HttpGet]
        [Route("applications")]
        public async Task<IActionResult> GetApplications()
        {
            return new OkObjectResult(await _appsRepo.GetResources());
        }

        [HttpGet]
        [Route("servicePrincipals/{resourceId:guid}")]
        public async Task<IActionResult> Get(Guid resourceId)
        {
            return new OkObjectResult(await _appsRepo.GetResource(resourceId));
        }

        [HttpGet]
        [Route("servicePrincipals/{resourceId:guid}/appRoles")]
        public async Task<IActionResult> GetServicePrincipalRoles(Guid resourceId)
        {
            var userId = HttpContext.User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value;
            return new OkObjectResult(await _appsRepo.GetAppRolesByResource(resourceId));
        }

        [HttpGet]
        [Route("servicePrincipals/{servicePrincipalId}/appRoleAssignedTo")]
        public async Task<IActionResult> GetApplicationAppRoleAssignments(Guid resourceId)
        {
            var userId = HttpContext.User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value;
            return new OkObjectResult(await _appsRepo.GetAppRoleAssignmentsByResource(resourceId));
        }

        [HttpPost]
        [Route("servicePrincipals/{servicePrincipalId}/appRoleAssignedTo")]
        public async Task<IActionResult> AddApplicationRoleAssignment(Guid resourceId, [FromBody] Microsoft.Graph.AppRoleAssignment request)
        {
            var userId = HttpContext.User.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value;
            request.ResourceId = resourceId;
            return new OkObjectResult(await _appsRepo.AssignAppRole(request));
        }
    }
}
