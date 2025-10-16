using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainAccessLayer.Models
{
    public class ZaloMessage
    {
        [Key]
        public int MessageId { get; set; }

        public int? ReservationId { get; set; }
        public Reservation? Reservation { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public string? MessageText { get; set; }

        [MaxLength(50)]
        public string? MessageType { get; set; } // Confirm / Deposit / Cancel

        public DateTime SentAt { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string Status { get; set; } = "Sent";

        public string? ZaloMessageId { get; set; }
    }
}
