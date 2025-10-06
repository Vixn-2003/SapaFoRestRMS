using BusinessAccessLayer.DTOs;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services.Interfaces
{
    public interface IReservationService
    {
        Task<Reservation> CreateReservationAsync(ReservationCreateDto dto);
    }
}
