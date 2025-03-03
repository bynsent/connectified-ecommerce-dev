using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.Excel;
using Newtonsoft.Json;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Orders;
using Nop.Unleash.Common;
using Nop.Unleash.Models;
//using Stripe;
using static Nop.Unleash.Models.SalesOrder;

namespace Nop.Unleash.Service
{
    public class ProductUnleashService : IProductUnleashService
    {
        protected readonly IRepository<Nop.Core.Domain.Catalog.Product> _productRepository;
        protected readonly IRepository<Order> _orderRepository;
        protected readonly INopDataProvider _nopDataProvider;

        private HttpClient HttpClient { get; }
        private readonly ICategoryService _categoryService;
        private readonly IOrderService _orderService;

        public ProductUnleashService(HttpClient httpClient, IRepository<Nop.Core.Domain.Catalog.Product> productRepository, ICategoryService categoryService, INopDataProvider nopDataProvider, IRepository<Order> orderRepository, IOrderService orderService)
        {
            HttpClient = httpClient;
            _productRepository = productRepository;
            _categoryService = categoryService;
            _nopDataProvider = nopDataProvider;
            _orderRepository = orderRepository;
            _orderService = orderService;
        }

        public async Task<string> CreateProductOnUnleash(CreateProduct createProduct)
        {
            // specify the API endpoint you want to call
            var url = UnleashAPI.GetProductsURL();

            var query = string.Concat(url, "/", createProduct.Guid);

            HttpClient.DefaultRequestHeaders.Add(Constant.ACCEPT, Constant.APPLICATION_TYPE_JSON);
            HttpClient.DefaultRequestHeaders.Add(Constant.API_AUTH_ID, UnleashAPI.GetAPIAuthId());
            HttpClient.DefaultRequestHeaders.Add(Constant.API_AUTH_SIGNATURE, UnleashAPI.GetAuthSignature());

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(createProduct);
            var data = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(query, data);

            if (response.IsSuccessStatusCode)
                return Constant.ACTION_COMPLETED;
            else
                return Constant.ACTION_NOT_COMPLETED;
        }

        public async Task<ProductsResponse> FetchFromUnleash(int page)
        {
            var url = UnleashAPI.GetProductsURL();
            var query = $"{url}/{page}";

            var request = new HttpRequestMessage(HttpMethod.Get, query);
            request.Headers.Clear();
            request.Headers.Add(Constant.ACCEPT, Constant.APPLICATION_TYPE_JSON);
            request.Headers.Add(Constant.API_AUTH_ID, UnleashAPI.GetAPIAuthId());
            request.Headers.Add(Constant.API_AUTH_SIGNATURE, UnleashAPI.GetAuthSignature());

            var response = await HttpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var jsonstring = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<ProductsResponse>(jsonstring);
                return responseObject;
            }
            else
                return null;
        }

        public async Task<StockOnHandResponse> GetStockOnHand(int page)
        {
            var url = UnleashAPI.GetStockOnHandURL();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Clear();
            request.Headers.Add(Constant.ACCEPT, Constant.APPLICATION_TYPE_JSON);
            request.Headers.Add(Constant.API_AUTH_ID, UnleashAPI.GetAPIAuthId());
            request.Headers.Add(Constant.API_AUTH_SIGNATURE, UnleashAPI.GetAuthSignature());

            var response = await HttpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonstring = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<StockOnHandResponse>(jsonstring);
                return responseObject;
            }
            else
                return null;
        }

        public virtual async Task<Nop.Core.Domain.Catalog.Product> GetProductByUnleashGuidAsync(string unleashGuid)
        {
            if (string.IsNullOrEmpty(unleashGuid))
                return null;

            var query = from p in _productRepository.Table
                        orderby p.Id
                        where !p.Deleted &&
                        p.UnleashGuid == unleashGuid
                        select p;

            var product = await query.FirstOrDefaultAsync();

            return product;
        }

        public async Task<GetStockOnHand.Items> GetStockOnHandByGuid(string unleashGuid)
        {
            var url = UnleashAPI.GetStockOnHandByGuidURL();
            var query = $"{url}{unleashGuid}";

            var request = new HttpRequestMessage(HttpMethod.Get, query);
            request.Headers.Clear();
            request.Headers.Add(Constant.ACCEPT, Constant.APPLICATION_TYPE_JSON);
            request.Headers.Add(Constant.API_AUTH_ID, UnleashAPI.GetAPIAuthId());
            request.Headers.Add(Constant.API_AUTH_SIGNATURE, UnleashAPI.GetAuthSignature());

            var response = await HttpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonstring = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<GetStockOnHand.Items>(jsonstring);
                return responseObject;
            }
            else
                return null;

        }

        public async Task<SalesOrderRequest> CreateSalesOrderOnUnleash(SalesOrderRequest salesOrderRequest)
        {
            var url = UnleashAPI.GetSalesOrdersURL();
            var query = $"{url}/{salesOrderRequest.Guid}";
            HttpClient.DefaultRequestHeaders.Add(Constant.ACCEPT, Constant.APPLICATION_TYPE_JSON);
            HttpClient.DefaultRequestHeaders.Add(Constant.API_AUTH_ID, UnleashAPI.GetAPIAuthId());
            //HttpClient.DefaultRequestHeaders.Add(Constant.API_AUTH_SIGNATURE, UnleashAPI.GetAPIAuthSignature());
            HttpClient.DefaultRequestHeaders.Add(Constant.API_AUTH_SIGNATURE, UnleashAPI.GetAuthSignature());

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(salesOrderRequest);
            var data = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(query, data);
            if (response.IsSuccessStatusCode)
            {
                var jsonstring = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<SalesOrderRequest>(jsonstring);
                return responseObject;

            }
            else
                return null;

            //if (response.IsSuccessStatusCode)
            //    return Constant.ACTION_COMPLETED;
            //else
            //    return Constant.ACTION_NOT_COMPLETED;
        }

        public async Task<Category> InsertProductCategoryAsync(Category category)
        {
            var response = await _nopDataProvider.InsertEntityAsync(category);
            return response;
        }

        public async Task InsertProductCategoryMapping(ProductCategory productCategory)
        {
            _nopDataProvider.InsertEntity(productCategory);
        }

        public async Task<SalesOrderResponse> GetSalesOrder(int page)
        {
            var url = UnleashAPI.GetSalesOrdersURL();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Clear();
            request.Headers.Add(Constant.ACCEPT, Constant.APPLICATION_TYPE_JSON);
            request.Headers.Add(Constant.API_AUTH_ID, UnleashAPI.GetAPIAuthId());
            request.Headers.Add(Constant.API_AUTH_SIGNATURE, UnleashAPI.GetAuthSignature());

            var response = await HttpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonstring = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<SalesOrderResponse>(jsonstring);
                return responseObject;
            }
            else
                return null;
        }

        public async Task<Nop.Core.Domain.Orders.Order> GetSalesOrderByGuiIdAsync(string Guid)
        {
            if (string.IsNullOrEmpty(Guid))
                return null;

            var query = from o in _orderRepository.Table
                        orderby o.Id
                        where !o.Deleted &&
                        o.SalesOrderUnleashGuid == Guid
                        select o;

            var salesOrder = await query.FirstOrDefaultAsync();
            return salesOrder;
        }
        public async Task<SalesOrderRequest> GetUnleashSalesOrderByGuiIdAsync(string guid)
        {
            var url = UnleashAPI.GetSalesOrdersURL();
            var query = $"{url}/{guid}";
            var request = new HttpRequestMessage(HttpMethod.Get, query);
            request.Headers.Clear();
            request.Headers.Add(Constant.ACCEPT, Constant.APPLICATION_TYPE_JSON);
            request.Headers.Add(Constant.API_AUTH_ID, UnleashAPI.GetAPIAuthId());
            request.Headers.Add(Constant.API_AUTH_SIGNATURE, UnleashAPI.GetAuthSignature());

            var response = await HttpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonstring = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<SalesOrderRequest>(jsonstring);
                return responseObject;
            }
            else
                return null;
        }
        public async Task<Nop.Core.Domain.Orders.Order> UpdateSalesOrderGuidInOrder(string guid, int orderId)
        {
            if (string.IsNullOrEmpty(guid))
                return null;
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
                return null;

            order.SalesOrderUnleashGuid = guid;
            await _orderService.UpdateOrderAsync(order);
            return order;
        }

        //public async Task<Stripe.Product> CreateProductOnStripe(string productName)
        //{
        //    var apiKey = UnleashAPI.GetStripeAPIKey();
        //    StripeConfiguration.SetApiKey(apiKey);

        //    var options = new ProductCreateOptions
        //    {
        //        Name = productName,
        //    };

        //    var service = new Stripe.ProductService();
        //    Stripe.Product product = await service.CreateAsync(options);
        //    return product;
        //}

        public async Task<Nop.Core.Domain.Catalog.Product> FindProductByNameAsync(string productName)
        {
            //get by id
            if (string.IsNullOrEmpty(productName))
                return null;

            var query = from o in _productRepository.Table
                        orderby o.Id
                        where !o.Deleted &&
                        o.Name == productName
                        select o;

            var product = await query.FirstOrDefaultAsync();
            return product;
        }

        //public async Task<Stripe.Customer> CreateCustomerOnStripe(Stripe.CustomerCreateOptions customer)
        //{
        //    var apiKey = UnleashAPI.GetStripeAPIKey();
        //    StripeConfiguration.SetApiKey(apiKey);

        //    var service = new Stripe.CustomerService();
        //    var createdCustomer = await service.CreateAsync(customer);
        //    return createdCustomer;
        //}

        //public async Task<Subscription> CreateSubscriptionOnStripe(string customerId, int quantity, string priceId)
        //{
        //    var apiKey = UnleashAPI.GetStripeAPIKey();
        //    StripeConfiguration.SetApiKey(apiKey);

        //    var subscriptionOptions = new SubscriptionCreateOptions
        //    {
        //        Customer = customerId,
        //        Items = new List<SubscriptionItemOptions>
        //        {
        //            new SubscriptionItemOptions
        //            {
        //                Quantity = quantity,
        //                Price = priceId,
        //            },
        //        },
        //        CollectionMethod = "charge_automatically"
        //    };

        //    subscriptionOptions.AddExpand("latest_invoice.payment_intent");
        //    Subscription subscription = await new SubscriptionService().CreateAsync(subscriptionOptions);
        //    return subscription;
        //}

        //public async Task<Price> CreatePriceOnStripe(decimal price, string priceId)
        //{
        //    var priceOptions = new PriceCreateOptions
        //    {
        //        UnitAmount = (long)price,
        //        Currency = "usd",
        //        Recurring = new PriceRecurringOptions
        //        {
        //            Interval = "month",
        //        },
        //        Product = priceId,
        //    };

        //    var priceService = new PriceService();
        //    var priceResponse = await priceService.CreateAsync(priceOptions);
        //    return priceResponse;
        //}

        //public Task<Stripe.Token> CreateStripeTokenForCard(Stripe.TokenCardOptions cardOptions)
        //{
        //    var apiKey = UnleashAPI.GetStripeAPIKey();
        //    StripeConfiguration.SetApiKey(apiKey);

        //    var options = new TokenCreateOptions
        //    {
        //        Card = new TokenCardOptions
        //        {
        //            Number = cardOptions.Number,
        //            ExpMonth = cardOptions.ExpMonth,
        //            ExpYear = cardOptions.ExpYear,
        //            Cvc = cardOptions.Cvc,
        //        },
        //    };
        //    var service = new TokenService();
        //    var token = service.CreateAsync(options);
        //    return token;
        //}
    }
}
