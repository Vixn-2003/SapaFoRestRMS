namespace WebSapaForestForStaff.DTOs
{
    public class MenuItemDTO
    {
        public int MenuItemId { get; set; }
        public int? CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string CourseType { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsAds { get; set; }
        public int? TimeCook { get; set; }
        public int BillingType { get; set; } // 0, 1, 2

        public List<RecipeDTO> Recipes { get; set; } = new List<RecipeDTO>();

        public int ServedToday { get; set; }
        public int ServedYesterday { get; set; }
        public double Average7Days { get; set; }
        public double Average30Days { get; set; }
        public double Average90Days { get; set; }

        public double CompareWithYesterday { get; set; }
        public double CompareWith7Days { get; set; }
        public double CompareWith30Days { get; set; }

        public string BillingTypeDisplay
        {
            get
            {
                return BillingType switch
                {
                    0 => "Chưa xác định",
                    1 => "Tính theo tiêu thụ",
                    2 => "Món chế biến",
                    _ => "Không xác định"
                };
            }
        }

        public string PriceFormatted => Price.ToString("N0") + " ₫";

        public string TimeCookDisplay => TimeCook.HasValue ? $"{TimeCook} phút" : "N/A";

        public string AvailabilityStatus => IsAvailable ? "Còn món" : "Hết món";

        public string AvailabilityBadgeClass => IsAvailable ? "badge-success" : "badge-danger";

        public string CompareYesterdayFormatted
        {
            get
            {
                var sign = CompareWithYesterday > 0 ? "+" : "";
                return $"{sign}{CompareWithYesterday:F2}%";
            }
        }

        public string CompareYesterdayClass
        {
            get
            {
                if (CompareWithYesterday > 0) return "text-success";
                if (CompareWithYesterday < 0) return "text-danger";
                return "text-muted";
            }
        }

        public string Compare7DaysFormatted
        {
            get
            {
                var sign = CompareWith7Days > 0 ? "+" : "";
                return $"{sign}{CompareWith7Days:F2}%";
            }
        }

        public string Compare7DaysClass
        {
            get
            {
                if (CompareWith7Days > 0) return "text-success";
                if (CompareWith7Days < 0) return "text-danger";
                return "text-muted";
            }
        }
    }
}
