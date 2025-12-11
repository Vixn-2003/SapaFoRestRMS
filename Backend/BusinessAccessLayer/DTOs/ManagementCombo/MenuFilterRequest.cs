using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.ManagementCombo
{
    public class MenuFilterRequest
    {
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Giá trị nhận: "price_asc", "price_desc", "name"
        public string? SortBy { get; set; }

        [Range(1, int.MaxValue)]
        public int PageIndex { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        public bool IsAvaiable { get; set; }
    }
}
