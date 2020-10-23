namespace DelegatedUserManagement.WebApp
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string InvitationCode { get; set; }
        public string CompanyId { get; set; }
        public string DelegatedUserManagementRole { get; set; }
    }
}