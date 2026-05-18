namespace Hiemdall_bridge.Models
{
    public class UserAuthModel
    {
        public int Id { get; set; }
        public string RoleType { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // Note: In production, never store plain text passwords
    }
}