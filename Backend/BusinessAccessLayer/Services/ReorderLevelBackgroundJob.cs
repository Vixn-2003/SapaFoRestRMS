using BusinessAccessLayer.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class ReorderLevelBackgroundJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ReorderLevelBackgroundJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chờ start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var analytics = scope.ServiceProvider.GetRequiredService<IInventoryAnalyticsService>();

                try
                {
                    // Ví dụ: luôn tính lại theo 30 ngày
                    var updated = await analytics.RecalculateReorderLevelsAsync(30, stoppingToken);
                    Console.WriteLine($"[ReorderJob] Updated {updated} ingredients at {DateTime.Now}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReorderJob] Error: {ex.Message}");
                }

                // Lặp lại sau 24h
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }

}
