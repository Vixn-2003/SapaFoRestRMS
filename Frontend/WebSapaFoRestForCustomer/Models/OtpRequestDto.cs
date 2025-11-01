using System.ComponentModel.DataAnnotations;

namespace WebSapaFoRestForCustomer.Models
{
    public class OtpRequestDto
    {
        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = null!;
    }
}
