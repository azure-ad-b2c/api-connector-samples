using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace DelegatedUserManagement.WebApp.Pages
{
    [Authorize]
    public class UserModel : PageModel
    {
        private readonly ILogger<UserModel> logger;
        private readonly B2cGraphService b2cGraphService;
        public bool CanManageUsers { get; set; }
        public bool CanSelectGlobalAdmins { get; set; }
        public bool CanSelectCompany { get; set; }
        public IList<WebApp.User> Users { get; set; }

        public UserModel(ILogger<UserModel> logger, B2cGraphService b2cGraphService)
        {
            this.logger = logger;
            this.b2cGraphService = b2cGraphService;
        }

        public async Task OnGetAsync()
        {
            if (this.User.IsInRole(Constants.DelegatedUserManagementRoles.GlobalAdmin))
            {
                // If the current user is a global admin, show all users.
                this.Users = await this.b2cGraphService.GetUsersAsync();
                this.CanManageUsers = true;
                this.CanSelectGlobalAdmins = true;
                this.CanSelectCompany = true;
            }
            else if (this.User.IsInRole(Constants.DelegatedUserManagementRoles.CompanyAdmin))
            {
                // If the current user is a company admin, show only that company's users.
                var userCompanyId = this.User.FindFirst(this.b2cGraphService.GetUserAttributeClaimName(Constants.UserAttributes.CompanyId))?.Value;
                this.Users = await this.b2cGraphService.GetUsersAsync(userCompanyId);
                this.CanManageUsers = true;
                this.CanSelectGlobalAdmins = false;
                this.CanSelectCompany = false;
            }
            else
            {
                // If the current user is no admin, they cannot see or manage any users.
                this.CanManageUsers = false;
            }

            this.Users = this.Users?.OrderBy(u => u.CompanyId).ThenBy(u => u.DelegatedUserManagementRole).ThenBy(u => u.Name).ToArray();
        }

        public async Task<IActionResult> OnPostUpdateUserAsync(User user)
        {
            // Check that the current user has permissions to create the invitation.
            if (!this.User.IsInRole(Constants.DelegatedUserManagementRoles.GlobalAdmin) && !this.User.IsInRole(Constants.DelegatedUserManagementRoles.CompanyAdmin))
            {
                return this.Unauthorized();
            }

            // In a real production scenario, additional validation would be needed here especially for Company Admins:
            // - Ensure that the user being modified is of the same company as the current user.
            // - Ensure that the user being modified isn't being changed to a different company.
            // - Ensure that the user's role isn't being elevated to global admin.
            // - ...

            await this.b2cGraphService.UpdateUserAsync(user);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string id)
        {
            // Check that the current user has permissions to create the invitation.
            if (!this.User.IsInRole(Constants.DelegatedUserManagementRoles.GlobalAdmin) && !this.User.IsInRole(Constants.DelegatedUserManagementRoles.CompanyAdmin))
            {
                return this.Unauthorized();
            }

            // In a real production scenario, additional validation would be needed here especially for Company Admins:
            // - Ensure that the user being deleted is of the same company as the current user.
            // - ...

            // Ensure you can't delete yourself.
            var currentUserId = this.User.FindFirst(Constants.ClaimTypes.ObjectId).Value;
            if (!string.Equals(currentUserId, id))
            {
                await this.b2cGraphService.DeleteUserAsync(id);
            }
            return RedirectToPage();
        }
    }
}