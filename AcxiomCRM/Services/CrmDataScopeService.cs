using System.Security.Claims;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public class CrmDataScopeService : ICrmDataScopeService
    {
        public IQueryable<Customer> ApplyCustomerScope(IQueryable<Customer> query, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager))
            {
                return query;
            }

            var email = user.Identity?.Name ?? string.Empty;
            return query.Where(c => c.CreatedBy == email || c.Opportunities.Any(o => o.AssignedTo == email) || c.FollowUps.Any(f => f.AssignedTo == email));
        }

        public IQueryable<Lead> ApplyLeadScope(IQueryable<Lead> query, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager))
            {
                return query;
            }

            var email = user.Identity?.Name ?? string.Empty;
            return query.Where(l => l.AssignedTo == email);
        }

        public IQueryable<Opportunity> ApplyOpportunityScope(IQueryable<Opportunity> query, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager))
            {
                return query;
            }

            var email = user.Identity?.Name ?? string.Empty;
            return query.Where(o => o.AssignedTo == email);
        }

        public IQueryable<FollowUp> ApplyFollowUpScope(IQueryable<FollowUp> query, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager))
            {
                return query;
            }

            var email = user.Identity?.Name ?? string.Empty;
            return query.Where(f => f.AssignedTo == email);
        }

        public IQueryable<Activity> ApplyActivityScope(IQueryable<Activity> query, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager))
            {
                return query;
            }

            var email = user.Identity?.Name ?? string.Empty;
            return query.Where(a => a.AssignedTo == email);
        }

        public bool CanAccessCustomer(Customer customer, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager)) return true;
            var email = user.Identity?.Name ?? string.Empty;
            return customer.CreatedBy == email || customer.Opportunities.Any(o => o.AssignedTo == email);
        }

        public bool CanAccessLead(Lead lead, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager)) return true;
            var email = user.Identity?.Name ?? string.Empty;
            return lead.AssignedTo == email;
        }

        public bool CanAccessOpportunity(Opportunity opp, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager)) return true;
            var email = user.Identity?.Name ?? string.Empty;
            return opp.AssignedTo == email;
        }

        public bool CanAccessFollowUp(FollowUp follow, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager)) return true;
            var email = user.Identity?.Name ?? string.Empty;
            return follow.AssignedTo == email;
        }

        public bool CanAccessActivity(Activity act, ClaimsPrincipal user)
        {
            if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Manager)) return true;
            var email = user.Identity?.Name ?? string.Empty;
            return act.AssignedTo == email;
        }
    }
}
