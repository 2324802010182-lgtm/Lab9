namespace ASC.Utilities
{
    public class CurrentUser
    {
        public string UserId { get; set; }
        public string EmailAddress { get; set; }
        public string FullName { get; set; }
        public string UserRole { get; set; }
        public bool IsAuthenticated { get; set; }
    }
}