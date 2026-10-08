using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? entityName,
            [FromQuery] string? result,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.AuditLogs.AsNoTracking();

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedDate >= fromDate.Value.ToUniversalTime());

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedDate <= toDate.Value.ToUniversalTime().AddDays(1));

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(a => a.EntityName != null && a.EntityName.ToLower() == entityName.Trim().ToLower());

            if (!string.IsNullOrWhiteSpace(result))
                query = query.Where(a => a.Result != null && a.Result.ToLower() == result.Trim().ToLower());

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a => a.Action.ToLower().Contains(s)
                                      || (a.UserId != null && a.UserId.ToLower().Contains(s))
                                      || (a.EntityName != null && a.EntityName.ToLower().Contains(s))
                                      || (a.Details != null && a.Details.ToLower().Contains(s))
                                      || (a.IpAddress != null && a.IpAddress.ToLower().Contains(s)));
            }

            var totalItems = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<AuditLog>(logs, totalItems, page, pageSize);

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.EntityName = entityName;
            ViewBag.Result = result;
            ViewBag.Search = search;

            return View(paginatedList);
        }
    }
}
