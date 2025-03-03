using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Unleash.Models
{
    public class StockOnHandResponse
    {
        public GetStockOnHand.Pagination Pagination { get; set; }
        public List<GetStockOnHand.Items> Items { get; set; }
    }

    public class GetStockOnHand
    {
        public class Items
        {
            public string ProductCode { get; set; }
            public string ProductDescription { get; set; }
            public string? ProductGuid { get; set; }
            public string? ProductSourceId { get; set; }
            public string ProductGroupName { get; set; }
            public string WarehouseId { get; set; }
            public string Warehouse { get; set; }
            public string WarehouseCode { get; set; }
            public int? DaysSinceLastSale { get; set; }
            public decimal OnPurchase { get; set; }
            public decimal AllocatedQty { get; set; }
            public decimal AvailableQty { get; set; }
            public decimal QtyOnHand { get; set; }
            public decimal AvgCost { get; set; }
            public decimal TotalCost { get; set; }
            public string Guid { get; set; }
            public string LastModifiedOn { get; set; }
        }

        public class Pagination
        {
            public int NumberOfItems { get; set; }
            public int PageSize { get; set; }
            public int PageNumber { get; set; }
            public int NumberOfPages { get; set; }

        }
    }
   
}
