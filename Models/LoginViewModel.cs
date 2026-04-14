using System.ComponentModel.DataAnnotations;

namespace CMS.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "User ID/Mobile is required")]
        public string MobileNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}
