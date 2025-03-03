using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Orders;
using Nop.Unleash.Common;
using Nop.Unleash.Models;
using Nop.Unleash.Service;

namespace Nop.Unleash.Controllers
{
    public partial class OrderController : Controller
    {
        #region Fields
        private readonly IProductService _productService;
        private readonly IProductUnleashService _productUnleashService;
        private readonly IOrderService _orderService;


        #endregion

        #region Ctor

        public OrderController(
            IProductService productService,
            IProductUnleashService productUnleashService,
            IOrderService orderService)
        {
            _productService = productService;
            _productUnleashService = productUnleashService;
            _orderService = orderService;
        }

        #endregion

        #region UnleashSalesOrder

        [HttpGet("GetSalesOrders")]
        public async Task<string> GetSalesOrders()
        {
            var pageNumber = 1;
            var response = await _productUnleashService.GetSalesOrder(pageNumber);
            if(response is not null &&  response.Pagination is not null)
            {
                while (pageNumber <= response.Pagination.NumberOfPages)
                {
                    foreach (var item in response.Items)
                    {
                        if(item.SalesOrderRequest != null && item.SalesOrderRequest.Guid != null)
                        {
                            var salesOrder = _productUnleashService.GetSalesOrderByGuiIdAsync(item.SalesOrderRequest.Guid);

                            if (salesOrder.Result is not null)
                            {
                                var status = item.SalesOrderRequest.OrderStatus;
                                var statusId = HelperMethod.GetEnumIntFromString(typeof(Enumeration.OrderStatus), item.SalesOrderRequest.OrderStatus);
                                salesOrder.Result.OrderStatusId = statusId;
                        
                                    await _orderService.UpdateOrderAsync(salesOrder.Result);
                            }
                        }
                    }

                    pageNumber++;
                }
            }
            return Constant.ACTION_COMPLETED;
        }

        #endregion
    }
}
