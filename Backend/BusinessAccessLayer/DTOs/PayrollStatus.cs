using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs
{
    public static class PayrollStatus
    {
        public const string Draft = "Bảng lương mới tạo";
        public const string Approved = "Đã duyệt";
        public const string Paid = "Đã chi trả lương";
        public const string Pending = "Chờ duyệt";
        public const string Rejected = "Từ chối";
    }

}
