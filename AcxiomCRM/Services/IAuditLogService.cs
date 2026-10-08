using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string? userId, string action, string? entityName = null, string? recordId = null, string? oldValue = null, string? newValue = null, string? ipAddress = null, string result = "Success", string? details = null);
        Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100);
        Task<IEnumerable<AuditLog>> GetLogsByUserAsync(string userId);
    }
}
