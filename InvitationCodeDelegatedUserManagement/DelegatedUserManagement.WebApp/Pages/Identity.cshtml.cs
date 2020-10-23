using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DelegatedUserManagement.WebApp.Pages
{
    [Authorize]
    public class IdentityModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}