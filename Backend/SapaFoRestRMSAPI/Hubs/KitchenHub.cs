using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using BusinessAccessLayer.DTOs.Kitchen;

namespace SapaFoRestRMSAPI.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time kitchen display updates
    /// </summary>
    public class KitchenHub : Hub
    {
        /// <summary>
        /// Called when station updates item status
        /// Broadcasts to all Sous Chef screens
        /// </summary>
        public async Task NotifyItemStatusChanged(KitchenStatusChangeNotification notification)
        {
            await Clients.All.SendAsync("ItemStatusChanged", notification);
        }

        /// <summary>
        /// Called when new order arrives
        /// </summary>
        public async Task NotifyNewOrder(KitchenOrderCardDto order)
        {
            await Clients.All.SendAsync("NewOrderReceived", order);
        }

        /// <summary>
        /// Called when Sous Chef completes entire order
        /// </summary>
        public async Task NotifyOrderCompleted(int orderId)
        {
            await Clients.All.SendAsync("OrderCompleted", orderId);
        }

        // Connection lifecycle
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}