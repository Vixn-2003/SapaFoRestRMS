using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.DTOs.OrderGuest
{
    public class SaveOrderRequest
    {
        public int TableId { get; set; }

        public List<SaveOrderItemDto> Items { get; set; } = new List<SaveOrderItemDto>();
    }
}
