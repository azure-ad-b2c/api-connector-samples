using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace AppRoles.WebApp.Pages
{
    // Only allow users with the "Admin" role to see this page.
    [Authorize(Roles = AdminRoleName)]
    public class AdminOnlyModel : PageModel
    {
        public const string AdminRoleName = "Admin";

        private readonly ILogger<AdminOnlyModel> _logger;

        public AdminOnlyModel(ILogger<AdminOnlyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}
