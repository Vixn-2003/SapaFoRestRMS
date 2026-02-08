using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace SapaFoRestRMSAPI.Hubs
{
    public class TableHub : Hub
    {
        // SỬA: Đổi tên hàm thành JoinGroup và tham số là string groupName
        // Để Frontend tự quyết định gửi chuỗi "Reservation_123" lên
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // SỬA: Tương tự với hàm rời nhóm
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
