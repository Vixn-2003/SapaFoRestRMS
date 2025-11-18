using System.ComponentModel.DataAnnotations;

namespace WebSapaFoRestForCustomer.DTOs.OrderTable
{
    public class TableViewModel
    {
        [Required]
        public int TableId { get; set; }
        public string TableNumber { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; }
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public int Floor {  get; set; }
        public bool IsAvailable { get; set; }
    }
}
