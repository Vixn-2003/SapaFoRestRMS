using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IDashboardTableRepository
    {
        // Trả về danh sách (Bàn, Reservation đang hoạt động (hoặc null))
        // Tuple (System.ValueTuple) là một cách tiện lợi để trả về nhiều
        // giá trị mà không cần tạo DTO riêng cho Repository.
        Task<List<(Table Table, Reservation ActiveReservation)>> GetFilteredTablesWithStatusAsync(string? areaName, int? floor, string? searchString);
    }
}
