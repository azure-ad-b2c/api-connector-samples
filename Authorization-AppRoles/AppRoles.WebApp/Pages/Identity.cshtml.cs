using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AppRoles.WebApp.Pages
{
    [Authorize]
    public class IdentityModel : PageModel
    {
        public string CheckRoleResult { get; set; }

        public void OnGet()
        {
        }

        public void OnPost(string roleName)
        {
            if (!string.IsNullOrEmpty(roleName))
            {
                this.CheckRoleResult = this.User.IsInRole(roleName) ? $"You have the \"{roleName}\" role." : $"You do not have the \"{roleName}\" role.";
            }
        }
    }
}