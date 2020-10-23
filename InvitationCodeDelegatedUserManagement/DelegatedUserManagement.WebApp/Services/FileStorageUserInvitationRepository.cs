using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DelegatedUserManagement.WebApp
{
    // Stores user invitations in files; in real production scenarios you would likely use a database for this.
    public class FileStorageUserInvitationRepository : IUserInvitationRepository
    {
        private readonly string basePath;

        public FileStorageUserInvitationRepository(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            // Ensure that the directory exists.
            this.basePath = basePath;
            Directory.CreateDirectory(this.basePath);
        }

        public async Task CreateUserInvitationAsync(UserInvitation userInvitation)
        {
            var contents = JsonSerializer.Serialize(userInvitation);
            await File.WriteAllTextAsync(GetUserInvitationFileName(userInvitation.InvitationCode), contents);
        }

        public Task<UserInvitation> GetPendingUserInvitationAsync(string invitationCode)
        {
            return GetUserInvitationAsync(GetUserInvitationFileName(invitationCode));
        }

        public Task RedeemUserInvitationAsync(string invitationCode)
        {
            File.Delete(GetUserInvitationFileName(invitationCode));
            return Task.CompletedTask;
        }

        public Task DeletePendingUserInvitationAsync(string invitationCode)
        {
            File.Delete(GetUserInvitationFileName(invitationCode));
            return Task.CompletedTask;
        }

        public async Task<IList<UserInvitation>> GetPendingUserInvitationsAsync(string companyId = null)
        {
            var userInvitations = new List<UserInvitation>();
            foreach (var fileName in Directory.EnumerateFiles(this.basePath))
            {
                var userInvitation = await GetUserInvitationAsync(fileName);
                if (string.IsNullOrWhiteSpace(companyId) || string.Equals(userInvitation.CompanyId, companyId, StringComparison.InvariantCultureIgnoreCase))
                {
                    userInvitations.Add(userInvitation);
                }
            }
            return userInvitations;
        }

        private async Task<UserInvitation> GetUserInvitationAsync(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }
            var contents = await File.ReadAllTextAsync(fileName);
            return JsonSerializer.Deserialize<UserInvitation>(contents);
        }

        private string GetUserInvitationFileName(string invitationCode)
        {
            if (string.IsNullOrWhiteSpace(invitationCode))
            {
                throw new ArgumentNullException(nameof(invitationCode));
            }
            return Path.Combine(this.basePath, invitationCode + ".json");
        }
    }
}