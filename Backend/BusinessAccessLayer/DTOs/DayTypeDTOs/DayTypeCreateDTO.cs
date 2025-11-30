using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.DayTypeDTOs
{
    public class DayTypeCreateDTO
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class DayTypeUpdateDTO
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class DayTypeResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
