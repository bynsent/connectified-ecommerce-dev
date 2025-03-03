using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Unleash.Common;
using Nop.Unleash.Models;
using Nop.Unleash.Service;

namespace Nop.Unleash.Controllers
{
    public partial class ProductController : Controller
    {
        #region Fields
        private readonly IProductService _productService;
        private readonly IProductUnleashService _productUnleashService;

        #endregion

        #region Ctor

        public ProductController(
            IProductService productService,
            IProductUnleashService productUnleashService)
        {
            _productService = productService;
            _productUnleashService = productUnleashService;
        }

        #endregion

        #region UnleashProduct

        [HttpGet("GetStockOnHand")]
        public async Task<string> GetStockOnHand()
        {
            var pageNumber = 1;
            var response = await _productUnleashService.GetStockOnHand(pageNumber);

            if (response is not null && response.Pagination is not null)
            {
                while (pageNumber <= response.Pagination.NumberOfPages)
                {
                    foreach (var item in response.Items)
                    {
                        var productObj = _productUnleashService.GetProductByUnleashGuidAsync(item.Guid);

                        if (productObj.Result is not null)
                        {
                            productObj.Result.StockQuantity = Convert.ToInt32(item.QtyOnHand);
                            await _productService.UpdateProductAsync(productObj.Result);
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
