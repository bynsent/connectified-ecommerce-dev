using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Unleash.Models;
using StackExchange.Redis;

namespace Nop.Unleash.Service
{
    public interface IProductUnleashService
    {
        Task<ProductsResponse> FetchFromUnleash(int page);
        Task<string> CreateProductOnUnleash(CreateProduct createProduct);
        Task<StockOnHandResponse> GetStockOnHand(int page);
        Task<Product> GetProductByUnleashGuidAsync(string unleashGuid);
        Task<GetStockOnHand.Items> GetStockOnHandByGuid(string unleashGuid);
        Task<SalesOrderRequest> CreateSalesOrderOnUnleash(SalesOrderRequest salesOrderRequest);
        Task<Nop.Core.Domain.Orders.Order> UpdateSalesOrderGuidInOrder(string guid, int orderId);
        Task<Category> InsertProductCategoryAsync(Category category);
        Task InsertProductCategoryMapping(ProductCategory productCategory);
        Task<SalesOrderResponse> GetSalesOrder(int page);
        Task<Nop.Core.Domain.Orders.Order> GetSalesOrderByGuiIdAsync(string guid);
        Task<SalesOrderRequest> GetUnleashSalesOrderByGuiIdAsync(string guid);
        //Task<Stripe.Product> CreateProductOnStripe(string productName);
        //Task<Stripe.Price> CreatePriceOnStripe(decimal price, string productId);
        Task<Product> FindProductByNameAsync(string productName);
        //Task<Stripe.Customer> CreateCustomerOnStripe(Stripe.CustomerCreateOptions customer);
        //Task<Stripe.Subscription> CreateSubscriptionOnStripe(string customerId, int quantity, string priceId);
        //Task<Stripe.Token> CreateStripeTokenForCard(Stripe.TokenCardOptions tokenCardOptions);
    }
}
