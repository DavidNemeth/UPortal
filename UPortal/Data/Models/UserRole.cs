namespace UPortal.Data.Models
{
    public class UserRole
    {
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }
    }
}
