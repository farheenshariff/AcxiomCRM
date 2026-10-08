using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api
{
    [Route("api/auditlog")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogApiController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogApiController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetRecentLogs([FromQuery] int count = 50)
        {
            var logs = await _auditLogService.GetRecentLogsAsync(count);
            return Ok(logs);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetUserLogs(string userId)
        {
            var logs = await _auditLogService.GetLogsByUserAsync(userId);
            return Ok(logs);
        }
    }
}
