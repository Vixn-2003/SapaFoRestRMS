using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Auth
{
    public class GoogleLoginRequest
    {
        [Required]
        public string IdToken { get; set; } = null!;
    }
}


