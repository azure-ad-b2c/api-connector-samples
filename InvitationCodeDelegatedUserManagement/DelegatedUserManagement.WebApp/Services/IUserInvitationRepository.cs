using System.Collections.Generic;
using System.Threading.Tasks;

namespace DelegatedUserManagement.WebApp
{
    public interface IUserInvitationRepository
    {
        Task CreateUserInvitationAsync(UserInvitation userInvitation);
        Task<UserInvitation> GetPendingUserInvitationAsync(string invitationCode);
        Task RedeemUserInvitationAsync(string invitationCode);
        Task DeletePendingUserInvitationAsync(string invitationCode);
        Task<IList<UserInvitation>> GetPendingUserInvitationsAsync(string companyId = null);
    }
}