using System;
using System.Collections.Generic;

namespace SapaFoRestRMSAPI.Models;

public partial class RestaurantIntro
{
    public int IntroId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? VideoUrl { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool? IsActive { get; set; }

    public int? CreatedBy { get; set; }

    public virtual User? CreatedByNavigation { get; set; }
}
