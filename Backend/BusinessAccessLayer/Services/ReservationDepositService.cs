using BusinessAccessLayer.DTOs.ReservationDepositDto;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class ReservationDepositService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IReservationDepositRepository _depositRepository;
        private readonly ICloudinaryService _cloudinaryService;

        public ReservationDepositService(
            IReservationRepository reservationRepository,
            IReservationDepositRepository depositRepository,
            ICloudinaryService cloudinaryService)
        {
            _reservationRepository = reservationRepository;
            _depositRepository = depositRepository;
            _cloudinaryService = cloudinaryService;
        }

        // ➕ Thêm mới giao dịch đặt cọc
        public async Task<ReservationDepositResponseDto> AddDepositAsync(ReservationDepositDto dto)
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(dto.ReservationId);
            if (reservation == null)
                throw new Exception("Không tìm thấy đơn đặt bàn.");

            string? imageUrl = null;
            if (dto.ReceiptImage != null)
            {
                imageUrl = await _cloudinaryService.UploadImageAsync(dto.ReceiptImage, "reservation_receipts");
            }

            var deposit = new ReservationDeposit
            {
                ReservationId = dto.ReservationId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                DepositCode = dto.DepositCode,
                DepositDate = DateTime.Now,
                Notes = dto.Notes,
                ReceiptImageUrl = imageUrl
            };

            await _depositRepository.CreateAsync(deposit);

            // Cập nhật tổng tiền cọc
            reservation.TotalDepositPaid = (await _depositRepository.GetByReservationIdAsync(reservation.ReservationId))
     .Sum(d => d.Amount);


            // Đánh dấu đã cọc nếu đủ
            if (reservation.DepositAmount.HasValue && reservation.TotalDepositPaid >= reservation.DepositAmount)
                reservation.DepositPaid = true;

            await _reservationRepository.SaveChangesAsync();

            // Trả về DTO phản hồi
            return new ReservationDepositResponseDto
            {
                DepositId = deposit.DepositId,
                ReservationId = deposit.ReservationId,
                Amount = deposit.Amount,
                PaymentMethod = deposit.PaymentMethod,
                DepositCode = deposit.DepositCode,
                DepositDate = deposit.DepositDate,
                Notes = deposit.Notes,
                ReceiptImageUrl = deposit.ReceiptImageUrl
            };
        }

        // ✏️ Cập nhật giao dịch cọc
        public async Task<ReservationDepositResponseDto> UpdateDepositAsync(int depositId, ReservationDepositDto dto)
        {
            var deposit = await _depositRepository.GetByIdAsync(depositId);
            if (deposit == null)
                throw new Exception("Không tìm thấy giao dịch cọc.");

            var reservation = await _reservationRepository.GetReservationByIdAsync(deposit.ReservationId);
            if (reservation == null)
                throw new Exception("Không tìm thấy đơn đặt bàn.");

            if (dto.ReceiptImage != null)
            {
                if (!string.IsNullOrEmpty(deposit.ReceiptImageUrl))
                    await _cloudinaryService.DeleteImageAsync(deposit.ReceiptImageUrl);

                deposit.ReceiptImageUrl = await _cloudinaryService.UploadImageAsync(dto.ReceiptImage, "reservation_receipts");
            }

            reservation.TotalDepositPaid = (reservation.TotalDepositPaid ?? 0) - deposit.Amount + dto.Amount;

            deposit.Amount = dto.Amount;
            deposit.PaymentMethod = dto.PaymentMethod;
            deposit.DepositCode = dto.DepositCode;
            deposit.Notes = dto.Notes;
            deposit.DepositDate = DateTime.Now;

            reservation.DepositPaid = reservation.DepositAmount.HasValue &&
                                      reservation.TotalDepositPaid >= reservation.DepositAmount;

            await _depositRepository.SaveChangesAsync();
            await _reservationRepository.SaveChangesAsync();

            return new ReservationDepositResponseDto
            {
                DepositId = deposit.DepositId,
                ReservationId = deposit.ReservationId,
                Amount = deposit.Amount,
                PaymentMethod = deposit.PaymentMethod,
                DepositCode = deposit.DepositCode,
                DepositDate = deposit.DepositDate,
                Notes = deposit.Notes,
                ReceiptImageUrl = deposit.ReceiptImageUrl
            };
        }
        // Lấy danh sách giao dịch theo ReservationId
        public async Task<List<ReservationDepositResponseDto>> GetDepositsByReservationIdAsync(int reservationId)
        {
            var deposits = await _depositRepository.GetByReservationIdAsync(reservationId);

            return deposits.Select(d => new ReservationDepositResponseDto
            {
                DepositId = d.DepositId,
                ReservationId = d.ReservationId,
                Amount = d.Amount,
                PaymentMethod = d.PaymentMethod,
                DepositCode = d.DepositCode,
                DepositDate = d.DepositDate,
                Notes = d.Notes,
                ReceiptImageUrl = d.ReceiptImageUrl
            }).ToList();
        }

        // Xóa giao dịch cọc
        public async Task<bool> DeleteDepositAsync(int depositId)
        {
            var deposit = await _depositRepository.GetByIdAsync(depositId);
            if (deposit == null) throw new Exception("Không tìm thấy giao dịch cọc.");

            var reservation = await _reservationRepository.GetReservationByIdAsync(deposit.ReservationId);
            if (reservation == null) throw new Exception("Không tìm thấy đơn đặt bàn.");

            // Trừ tiền cọc
            reservation.TotalDepositPaid = (reservation.TotalDepositPaid ?? 0) - deposit.Amount;

            // Cập nhật trạng thái cọc
            reservation.DepositPaid = reservation.TotalDepositPaid >= reservation.DepositAmount;

            // Xóa ảnh nếu có
            if (!string.IsNullOrEmpty(deposit.ReceiptImageUrl))
                await _cloudinaryService.DeleteImageAsync(deposit.ReceiptImageUrl);

            await _depositRepository.DeleteAsync(deposit.DepositId);
            await _reservationRepository.SaveChangesAsync();
            return true;
        }

    }
}
