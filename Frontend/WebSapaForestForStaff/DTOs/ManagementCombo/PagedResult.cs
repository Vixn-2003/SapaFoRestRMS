using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSapaForestForStaff.DTOs.ManagementCombo
{
    public class PagedResult<T>
    {
        [JsonProperty("items")]
        public List<T> Items { get; set; }

        [JsonProperty("totalRecords")]
        public int TotalRecords { get; set; }
        [JsonProperty("pageIndex")]
        public int PageIndex { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        public PagedResult(List<T> items, int count, int pageIndex, int pageSize)
        {
            Items = items;
            TotalRecords = count;
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }
}
