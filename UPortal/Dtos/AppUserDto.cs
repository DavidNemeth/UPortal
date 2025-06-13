namespace UPortal.Dtos
{
    public class AppUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }
        public string AzureAdObjectId { get; set; } = string.Empty;
    }
    public class UpdateAppUserDto
    {
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; }

    }
}