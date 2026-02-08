using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace WebSapaForestForStaff.DTOs
{
    public class EventDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Location { get; set; }
    }

    public class EventCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
        public string Title { get; set; } = null!;

        [DataType(DataType.Upload)]
        public IFormFile? Image { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        [DataType(DataType.Date)]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        [DataType(DataType.Date)]
        public DateOnly? EndDate { get; set; }

        [StringLength(200, ErrorMessage = "Địa điểm tối đa 200 ký tự")]
        public string? Location { get; set; }

        // Validate ngày kết thúc
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            {
                yield return new ValidationResult(
                    "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu",
                    new[] { nameof(EndDate) }
                );
            }
        }
    }

    public class EventUpdateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
        public string Title { get; set; } = null!;

        [DataType(DataType.Upload)]
        public IFormFile? Image { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        [DataType(DataType.Date)]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        [DataType(DataType.Date)]
        public DateOnly? EndDate { get; set; }

        [StringLength(200, ErrorMessage = "Địa điểm tối đa 200 ký tự")]
        public string? Location { get; set; }

        // Validate ngày kết thúc
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate.HasValue && EndDate.HasValue && EndDate < StartDate)
            {
                yield return new ValidationResult(
                    "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu",
                    new[] { nameof(EndDate) }
                );
            }
        }
    }
}
