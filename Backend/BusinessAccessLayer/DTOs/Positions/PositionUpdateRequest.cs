using System.ComponentModel.DataAnnotations;

namespace BusinessAccessLayer.DTOs.Positions
{
    public class PositionUpdateRequest
    {
        [Required]
        [StringLength(100)]
        public string PositionName { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 2)]
        public int Status { get; set; }
    }
}

