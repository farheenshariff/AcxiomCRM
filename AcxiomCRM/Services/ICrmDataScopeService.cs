using System.Security.Claims;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface ICrmDataScopeService
    {
        IQueryable<Customer> ApplyCustomerScope(IQueryable<Customer> query, ClaimsPrincipal user);
        IQueryable<Lead> ApplyLeadScope(IQueryable<Lead> query, ClaimsPrincipal user);
        IQueryable<Opportunity> ApplyOpportunityScope(IQueryable<Opportunity> query, ClaimsPrincipal user);
        IQueryable<FollowUp> ApplyFollowUpScope(IQueryable<FollowUp> query, ClaimsPrincipal user);
        IQueryable<Activity> ApplyActivityScope(IQueryable<Activity> query, ClaimsPrincipal user);

        bool CanAccessCustomer(Customer customer, ClaimsPrincipal user);
        bool CanAccessLead(Lead lead, ClaimsPrincipal user);
        bool CanAccessOpportunity(Opportunity opp, ClaimsPrincipal user);
        bool CanAccessFollowUp(FollowUp follow, ClaimsPrincipal user);
        bool CanAccessActivity(Activity act, ClaimsPrincipal user);
    }
}
