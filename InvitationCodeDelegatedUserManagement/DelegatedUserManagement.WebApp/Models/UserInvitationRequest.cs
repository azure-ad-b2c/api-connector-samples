namespace DelegatedUserManagement.WebApp
{
    public class UserInvitationRequest
    {
        public string CompanyId { get; set; }
        public string DelegatedUserManagementRole { get; set; }
        public int ValidHours { get; set; }
    }
}