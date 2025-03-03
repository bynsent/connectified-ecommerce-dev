using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Nop.Core.Domain.Customers;
using Nop.Services.Catalog;
using Nop.Unleash.Common;
using Nop.Unleash.Models;
using Nop.Unleash.Service;
using static System.Net.Mime.MediaTypeNames;

namespace Nop.Unleash.WebJob
{
    public class RecurringTasks : IRecurringTasks
    {
        private const string STOCK_ON_HAND = "GetStockOnHand";
        private const string SALES_ORDERS = "GetSalesOrders";

      
        public async void RecurringStockUpdate()
        {
            var baseURL = UnleashAPI.GetBaseURL();
            HttpClient HttpClient = new HttpClient();
            HttpClient.BaseAddress = new Uri(baseURL);
            //call controller
            var response = await HttpClient.GetAsync(STOCK_ON_HAND);

            // Check if the response was successful
            response.EnsureSuccessStatusCode();
        }

        public async void RecurringOrderStatusUpdate()
        {
            var baseURL = UnleashAPI.GetBaseURL();

            HttpClient HttpClient = new HttpClient();
            HttpClient.BaseAddress = new Uri(baseURL);
            var response = await HttpClient.GetAsync(SALES_ORDERS);
            response.EnsureSuccessStatusCode();
        }

    }
}
