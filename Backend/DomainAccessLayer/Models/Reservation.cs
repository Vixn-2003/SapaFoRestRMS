using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainAccessLayer.Models;

public partial class Reservation
{
    public int ReservationId { get; set; }

    public int CustomerId { get; set; }
    public string CustomerNameReservation { get; set; } = null!;
    public int? StaffId { get; set; }
    public User? Staff { get; set; }
    [Required]
    public DateTime ReservationDate { get; set; }

    [Required, MaxLength(50)]
    public string TimeSlot { get; set; } // Ca sáng / Ca tối
    public DateTime ReservationTime { get; set; }
    public TimeSpan? ArrivalTime { get; set; }
    public int NumberOfGuests { get; set; }
    public bool RequireDeposit { get; set; } = false;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DepositAmount { get; set; }

    public bool DepositPaid { get; set; } = false;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalDepositPaid { get; set; } = 0;

    public string? Status { get; set; }

    public string? Notes { get; set; }
    public string? ZaloMessageId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ReservationTable> ReservationTables { get; set; } = new List<ReservationTable>();
    public virtual ICollection<ReservationDeposit> ReservationDeposits { get; set; } = new List<ReservationDeposit>();

}
