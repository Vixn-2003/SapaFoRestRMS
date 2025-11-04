using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainAccessLayer.Models
{
    public partial class KitchenTicketDetail
    {
        [Key]
        public int TicketDetailId { get; set; }

        public int TicketId { get; set; }

        public int OrderDetailId { get; set; }

        /// <summary>
        /// Status of this item: Pending, Cooking, Done
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// When the station staff started cooking this item
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When the station staff completed this item
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Which user (station staff) is assigned to cook this item
        /// </summary>
        public int? AssignedUserId { get; set; }

        // Navigation properties
        [ForeignKey("TicketId")]
        public virtual KitchenTicket Ticket { get; set; } = null!;

        [ForeignKey("OrderDetailId")]
        public virtual OrderDetail OrderDetail { get; set; } = null!;

        [ForeignKey("AssignedUserId")]
        public virtual User? AssignedUser { get; set; }
    }
}