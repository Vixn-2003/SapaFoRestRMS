//using System;
//using System.Threading.Tasks;
//using DataAccessLayer.Dbcontext;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;

//namespace SapaFoRestRMSAPI.Services
//{
//    public static class DataSeederManager
//    {
//        public static async Task SeedAllAsync(SapaFoRestRmsContext context)
//        {
//            if (context == null)
//            {
//                throw new ArgumentNullException(nameof(context));
//            }

//            Console.WriteLine($"[{Timestamp()}] Starting global data seeding...");

//            try
//            {
//                await context.Database.MigrateAsync();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"❌ Database migration failed: {ex.Message}");
//                throw;
//            }

//            await ExecuteStepAsync("Seeding Positions", () => DataSeeder.SeedPositionsAsync(context));
//            await ExecuteStepAsync("Seeding Admin", () => DataSeeder.SeedAdminAsync(context));
//            await ExecuteStepAsync("Seeding Test Customer", () => DataSeeder.SeedTestCustomerAsync(context));
//            await ExecuteStepAsync("Seeding Test Staff and Manager", () => DataSeeder.SeedTestStaffAndManagerAsync(context));
//            await ExecuteStepAsync("Seeding Staff with All Positions", () => DataSeeder.SeedStaffWithAllPositionsAsync(context));

//            Console.WriteLine("🎉 All seed operations completed successfully.");
//        }

//        private static async Task ExecuteStepAsync(string stepName, Func<Task> action)
//        {
//            Console.WriteLine($"[{Timestamp()}] {stepName}...");
//            try
//            {
//                await action();
//                Console.WriteLine($"✅ {stepName} completed");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"❌ {stepName} failed: {ex.Message}");
//            }
//        }

//        private static string Timestamp() => DateTime.Now.ToString("HH:mm:ss");
//    }

//    public static class SeederExtensions
//    {
//        public static async Task EnsureSeededAsync(this IHost app)
//        {
//            using var scope = app.Services.CreateScope();
//            var ctx = scope.ServiceProvider.GetRequiredService<SapaFoRestRmsContext>();
//            await DataSeederManager.SeedAllAsync(ctx);
//        }
//    }
//}
