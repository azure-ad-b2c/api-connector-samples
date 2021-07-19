using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppRoles.WebApp.Services
{
    public interface IAppRolesProvider
    {
        Task<ICollection<string>> GetAppRolesAsync(string userId, string appId);
    }
}