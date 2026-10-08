using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure database schema is created/migrated cleanly according to active provider
            if (context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            {
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.MigrateAsync();
            }

            // 1. Seed Identity Roles
            string[] roles = { "Admin", "Manager", "SalesExecutive" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@acxiomcrm.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Manager User
            var managerEmail = "manager@acxiomcrm.com";
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FullName = "Sales Manager",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }

            // 4. Seed SalesExecutive User
            var salesEmail = "sales@acxiomcrm.com";
            var salesUser = await userManager.FindByEmailAsync(salesEmail);
            if (salesUser == null)
            {
                salesUser = new ApplicationUser
                {
                    UserName = salesEmail,
                    Email = salesEmail,
                    FullName = "Alex SalesExec",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(salesUser, "Sales@12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser, "SalesExecutive");
                }
            }

            // 5. Seed Sample CRM Data if empty
            if (!await context.Customers.AnyAsync())
            {
                var cust1 = new Customer
                {
                    CustomerCode = "CUST-1001",
                    CustomerName = "Acme Global Enterprise",
                    Email = "contact@acmeglobal.com",
                    Phone = "+1-555-0192",
                    CompanyName = "Acme Corp",
                    Address = "100 Tech Parkway",
                    City = "Austin",
                    State = "TX",
                    Status = "Active",
                    CreatedBy = adminEmail,
                    CreatedDate = DateTime.UtcNow.AddDays(-30)
                };

                var cust2 = new Customer
                {
                    CustomerCode = "CUST-1002",
                    CustomerName = "Nexus Cloud Solutions",
                    Email = "info@nexuscloud.io",
                    Phone = "+1-555-0481",
                    CompanyName = "Nexus Inc",
                    Address = "500 Silicon Way",
                    City = "San Jose",
                    State = "CA",
                    Status = "Active",
                    CreatedBy = managerEmail,
                    CreatedDate = DateTime.UtcNow.AddDays(-15)
                };

                await context.Customers.AddRangeAsync(cust1, cust2);
                await context.SaveChangesAsync();

                // Seed Leads
                var lead1 = new Lead
                {
                    LeadCode = "LEAD-2001",
                    LeadName = "Sarah Jenkins",
                    Email = "sjenkins@vertexmedia.com",
                    Phone = "+1-555-0822",
                    CompanyName = "Vertex Media",
                    Source = "Website",
                    Status = "New",
                    Priority = "High",
                    ExpectedValue = 45000.00m,
                    AssignedTo = salesEmail,
                    CreatedDate = DateTime.UtcNow.AddDays(-10)
                };

                var lead2 = new Lead
                {
                    LeadCode = "LEAD-2002",
                    LeadName = "David Miller",
                    Email = "dmiller@apexlogistics.net",
                    Phone = "+1-555-0371",
                    CompanyName = "Apex Logistics",
                    Source = "Referral",
                    Status = "In Progress",
                    Priority = "Medium",
                    ExpectedValue = 75000.00m,
                    AssignedTo = managerEmail,
                    CreatedDate = DateTime.UtcNow.AddDays(-5)
                };

                await context.Leads.AddRangeAsync(lead1, lead2);
                await context.SaveChangesAsync();

                // Seed Opportunities
                var opp1 = new Opportunity
                {
                    OpportunityName = "Acme Cloud Migration Enterprise Upgrade",
                    CustomerId = cust1.CustomerId,
                    Amount = 120000.00m,
                    Stage = "Proposal",
                    Probability = 75,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                    Status = "Open",
                    AssignedTo = salesEmail,
                    Notes = "Initial demo completed successfully. Proposal submitted.",
                    CreatedDate = DateTime.UtcNow.AddDays(-12)
                };

                var opp2 = new Opportunity
                {
                    OpportunityName = "Vertex Media Digital Transformation",
                    LeadId = lead1.LeadId,
                    Amount = 45000.00m,
                    Stage = "Qualification",
                    Probability = 40,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                    Status = "Open",
                    AssignedTo = salesEmail,
                    Notes = "Qualified requirement for 50 CRM seats.",
                    CreatedDate = DateTime.UtcNow.AddDays(-8)
                };

                await context.Opportunities.AddRangeAsync(opp1, opp2);
                await context.SaveChangesAsync();

                // Seed FollowUps
                var follow1 = new FollowUp
                {
                    CustomerId = cust1.CustomerId,
                    FollowUpDate = DateTime.UtcNow.AddDays(2),
                    FollowUpType = "Call",
                    Subject = "Contract Terms Review Call",
                    Remarks = "Follow up with procurement team on SLA terms.",
                    Status = "Pending",
                    AssignedTo = salesEmail
                };

                await context.FollowUps.AddAsync(follow1);

                // Seed Activities
                var act1 = new Activity
                {
                    ActivityType = "Email",
                    Subject = "Sent Product Catalog & Pricing Overview",
                    Description = "Email sent to Sarah Jenkins with current enterprise tiers.",
                    ActivityDate = DateTime.UtcNow.AddDays(-3),
                    LeadId = lead1.LeadId,
                    AssignedTo = salesEmail,
                    Status = "Completed"
                };

                await context.Activities.AddAsync(act1);

                // Initial Audit Log
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = adminUser.Id,
                    Action = "System Seed",
                    EntityName = "Database",
                    RecordId = "SYSTEM",
                    CreatedDate = DateTime.UtcNow,
                    Result = "Success",
                    Details = "Initial system roles, admin accounts, and seed CRM entities created."
                });

                await context.SaveChangesAsync();
            }
        }
    }
}
