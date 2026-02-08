using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Inventory
{
    public class CreateSupplierDTO
    {
        [Required(ErrorMessage = "Mã nhà cung cấp không được để trống")]
        [StringLength(50, ErrorMessage = "Mã nhà cung cấp không được quá 50 ký tự")]
        public string CodeSupplier { get; set; } = null!;

        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
        [StringLength(200, ErrorMessage = "Tên nhà cung cấp không được quá 200 ký tự")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Thông tin liên hệ không được để trống")]
        [StringLength(200)]
        public string ContactInfo { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải có 10 chữ số và bắt đầu bằng 0")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(500)]
        public string Address { get; set; } = null!;
    }
}
