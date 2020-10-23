using System;

namespace DelegatedUserManagement.WebApp
{
    public class UserInvitation
    {
        public string InvitationCode { get; set; }
        public string CompanyId { get; set; }
        public string DelegatedUserManagementRole { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset ExpiresTime { get; set; }
    }
}