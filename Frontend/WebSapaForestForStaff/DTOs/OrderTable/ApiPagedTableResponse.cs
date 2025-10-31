namespace WebSapaFoRestForStaff.DTOs.OrderTable
{
 
        public class ApiPagedTableResponse
        {
            // Tên "Data" phải khớp với chữ "data" trong JSON
            public List<TableViewModel> Data { get; set; }
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        
    }
}
