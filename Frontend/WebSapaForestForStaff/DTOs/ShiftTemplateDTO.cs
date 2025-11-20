namespace WebSapaForestForStaff.DTOs
{
    public class ShiftTemplateDTO
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = null!;
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
        public string ShiftType { get; set; } = null!;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = null!;
    }

    public class ShiftTemplateCreateDTO
    {
        public string Name { get; set; } = null!;
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
        public string ShiftType { get; set; } = null!;
        public int DepartmentId { get; set; }
    }
}
