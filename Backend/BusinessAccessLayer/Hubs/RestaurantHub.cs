using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Hubs
{
    using Microsoft.AspNetCore.SignalR;

    public class RestaurantHub : Hub
    {
        // Hàm này để Client (Nhân viên/Khách) tham gia vào nhóm
        // Ví dụ: Nhân viên tham gia nhóm "Employees"
        // Khách bàn 2 tham gia nhóm "Table-2"
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
