using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual ICollection<BrandBanner> BrandBanners { get; set; } = new List<BrandBanner>();

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<MarketingCampaign> MarketingCampaigns { get; set; } = new List<MarketingCampaign>();

    public virtual ICollection<Regulation> Regulations { get; set; } = new List<Regulation>();

    public virtual ICollection<RestaurantIntro> RestaurantIntros { get; set; } = new List<RestaurantIntro>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual ICollection<SystemLogo> SystemLogos { get; set; } = new List<SystemLogo>();
}
