using System.ComponentModel.DataAnnotations;

namespace WebSapaForestForStaff.Models
{
    public class SystemLogoViewModel
    {
        public int LogoId { get; set; }


        [Required(ErrorMessage = "Tên logo không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên logo tối đa 100 ký tự.")]
        public string LogoName { get; set; } = string.Empty;

        public IFormFile? File { get; set; }

       
        public string? LogoUrl { get; set; }


        [StringLength(300, ErrorMessage = "Mô tả tối đa 300 ký tự.")]
        public string? Description { get; set; }

        public bool IsActive { get; set; }


        public string? CreatedByName { get; set; }

        
        public int? CreatedBy { get; set; }
    }
}
