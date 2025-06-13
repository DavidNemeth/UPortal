using System.ComponentModel.DataAnnotations;

namespace UPortal.Dtos
{
    /// <summary>
    /// Data Transfer Object for external application details.
    /// </summary>
    public class ExternalApplicationDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the external application.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the external application.
        /// This field is required and has a maximum length of 100 characters.
        /// </summary>
        [Required(ErrorMessage = "App Name is required.")]
        [StringLength(100, ErrorMessage = "App Name must be less than 100 characters.")]
        public string AppName { get; set; }

        /// <summary>
        /// Gets or sets the URL of the external application.
        /// This field is required and must be a valid URL format (e.g., http://example.com).
        /// </summary>
        [Required(ErrorMessage = "App URL is required.")]
        [Url(ErrorMessage = "Invalid URL format. Please enter a full URL (e.g., http://example.com).")]
        public string AppUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of the icon used to represent the external application.
        /// This field is required. The value typically corresponds to a key for an icon in a UI library.
        /// </summary>
        [Required(ErrorMessage = "Icon is required.")]
        public string IconName { get; set; }
    }
}
