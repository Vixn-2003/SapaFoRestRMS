using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Models
{
    public class AssistanceRequest
    {
        [Key]
        public int RequestId { get; set; }

        public int TableId { get; set; }
        public Table Table { get; set; }

        public int? ReservationId { get; set; }
        public Reservation Reservation { get; set; }

        public DateTime RequestTime { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime? HandledTime { get; set; }

    }
}
