using DataAccessLayer.Dbcontext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SapaFoRestRMSAPI.Services
{
    // IHostedService là dịch vụ chạy ngầm của ASP.NET Core
    public class OrderStatusUpdaterService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceScopeFactory _scopeFactory;

        public OrderStatusUpdaterService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            // Bắt đầu chạy hàm DoWork ngay lập tức,
            // và lặp lại sau mỗi 30 giây
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            // Dịch vụ chạy ngầm (Singleton) cần tạo một "scope" mới
            // để lấy DbContext (là Scoped)
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<SapaFoRestRmsContext>();

                // 1. Lấy mốc thời gian 2 phút trước
                var twoMinutesAgo = DateTime.UtcNow.AddMinutes(-2);

                // 2. Tìm tất cả các món CÒN "Đã gửi" VÀ đã được tạo 
                //    hơn 2 phút trước
                var itemsToUpdate = context.OrderDetails
                    .Where(od => od.Status == "Đã gửi" && od.CreatedAt < twoMinutesAgo)
                    .ToList();

                if (itemsToUpdate.Any())
                {
                    foreach (var item in itemsToUpdate)
                    {
                        // 3. Tự động chuyển trạng thái
                        item.Status = "Đang chế biến";

                        // (Sau này chúng ta sẽ thêm SignalR ở đây
                        // để báo cho khách hàng)
                    }

                    // 4. Lưu tất cả thay đổi vào DB
                    context.SaveChanges();
                }
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
