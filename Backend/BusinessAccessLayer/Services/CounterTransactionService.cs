using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using BusinessAccessLayer.DTOs.CounterStaff;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using OfficeOpenXml;

namespace BusinessAccessLayer.Services
{
    /// <summary>
    /// Service implementation cho Counter Transaction History - UC124
    /// </summary>
    public class CounterTransactionService : ICounterTransactionService
    {
        private readonly ICounterTransactionRepository _transactionRepository;
        private readonly IMapper _mapper;

        public CounterTransactionService(
            ICounterTransactionRepository transactionRepository,
            IMapper mapper)
        {
            _transactionRepository = transactionRepository;
            _mapper = mapper;
        }

        public async Task<TransactionHistoryListDto> GetTransactionHistoryAsync(TransactionFilterDto filter, CancellationToken ct = default)
        {
            var (transactions, totalCount) = await _transactionRepository.GetTransactionHistoryAsync(
                filter.FromDate,
                filter.ToDate,
                filter.PaymentMethod,
                filter.Status,
                filter.PageNumber,
                filter.PageSize);

            var transactionDtos = _mapper.Map<List<TransactionHistoryDto>>(transactions);

            return new TransactionHistoryListDto
            {
                Transactions = transactionDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<byte[]> ExportTransactionsToExcelAsync(TransactionFilterDto filter, CancellationToken ct = default)
        {
            var transactions = await _transactionRepository.ExportTransactionsToListAsync(
                filter.FromDate,
                filter.ToDate,
                filter.PaymentMethod,
                filter.Status);

            var excelDtos = _mapper.Map<List<TransactionExcelDto>>(transactions);

            // EPPlus license is configured in Program.cs at application startup
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Transactions");

            // Header
            worksheet.Cells[1, 1].Value = "Receipt ID";
            worksheet.Cells[1, 2].Value = "Order ID";
            worksheet.Cells[1, 3].Value = "Reservation ID";
            worksheet.Cells[1, 4].Value = "Amount";
            worksheet.Cells[1, 5].Value = "Payment Method";
            worksheet.Cells[1, 6].Value = "Cashier";
            worksheet.Cells[1, 7].Value = "Timestamp";
            worksheet.Cells[1, 8].Value = "Status";

            // Style header
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data
            int row = 2;
            foreach (var item in excelDtos)
            {
                worksheet.Cells[row, 1].Value = item.TransactionCode;
                worksheet.Cells[row, 2].Value = item.OrderCode;
                worksheet.Cells[row, 3].Value = item.ReservationId.HasValue ? $"RES{item.ReservationId.Value:D6}" : "N/A";
                worksheet.Cells[row, 4].Value = item.Amount;
                worksheet.Cells[row, 5].Value = item.PaymentMethod;
                worksheet.Cells[row, 6].Value = item.CashierName;
                worksheet.Cells[row, 7].Value = item.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                worksheet.Cells[row, 8].Value = item.Status;
                row++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}

