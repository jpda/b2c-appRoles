using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
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
        public async Task<IEnumerable<UserApplication>> GetApplications() => await _appsRepo.GetResources();

        [HttpGet]
        [Route("servicePrincipals/{resourceId:guid}")]
        public async Task<UserApplication> GetApplication(Guid resourceId) => await _appsRepo.GetResource(resourceId);

        [HttpGet]
        [Route("servicePrincipals/{resourceId:guid}/appRoles")]
        public async Task<IEnumerable<AppRole>> GetServicePrincipalRoles(Guid resourceId) => await _appsRepo.GetAppRolesByResource(resourceId);

        [HttpGet]
        [Route("servicePrincipals/{resourceId:guid}/appRoleAssignedTo")]
        public async Task<IEnumerable<AppRoleAssignment>> GetApplicationAppRoleAssignments(Guid resourceId) => await _appsRepo.GetAppRoleAssignmentsByResource(resourceId);

        [HttpDelete]
        [Route("servicePrincipals/{resourceId:guid}/appRoleAssignedTo/{appRoleAssignmentId}")]
        public async Task<IActionResult> DeleteApplicationAppRoleAssignment(Guid resourceId, string appRoleAssignmentId)
        {
            var deletion = await _appsRepo.DeleteAppRoleAssignmentByResource(resourceId, appRoleAssignmentId);
            if (deletion)
            {
                return new NoContentResult();
            }
            return new UnauthorizedResult();
        }

        [HttpPost]
        [Route("servicePrincipals/{resourceId:guid}/appRoleAssignedTo")]
        public async Task<AppRoleAssignment> AddApplicationRoleAssignment(Guid resourceId, [FromBody] AppRoleAssignment request)
        {
            request.ResourceId = resourceId;
            return await _appsRepo.AssignAppRole(request);
        }
    }
}