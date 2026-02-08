using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.Department
{
    public class DepartmentCreateDTO
    {
        public string Name { get; set; } = null!;
        public int Status { get; set; }
    }

    public class DepartmentUpdateDTO
    {
        public string Name { get; set; } = null!;
        public int Status { get; set; }
    }

    public class DepartmentDTO
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; } = null!;
        public int Status { get; set; }
    }
}
