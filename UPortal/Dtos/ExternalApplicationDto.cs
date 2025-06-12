using System.ComponentModel.DataAnnotations;

namespace UPortal.Dtos
{
    public class ExternalApplicationDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "App Name is required.")]
        [StringLength(100, ErrorMessage = "App Name must be less than 100 characters.")]
        public string AppName { get; set; }

        [Required(ErrorMessage = "App URL is required.")]
        [Url(ErrorMessage = "Invalid URL format. Please enter a full URL (e.g., http://example.com).")]
        public string AppUrl { get; set; }

        [Required(ErrorMessage = "Icon is required.")]
        public string IconName { get; set; }
    }
}
