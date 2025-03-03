using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Unleash.Common
{
    public static class Constant
    {
        //appsetting
        public const string CONNECTION_STRING = "ConnectionString";
        public const string BASE_URL = "BaseURL";
        public const string UNLEASH_API_URLS = "UnleashAPIURLs";
        public const string API_AUTH_ID = "api-auth-id";
        public const string API_AUTH_SIGNATURE = "api-auth-signature";
        public const string GET_PRODUCTS = "GetProducts";
        public const string GET_STOCKONHAND_BYGUID = "GetStockOnHandByGuid";
        public const string GET_STOCK_ONHAND = "GetStockOnHand";
        public const string CREATE_STOCK_ADJUSTMENT_URL = "CreateStockAdjustment";
        public const string SALES_ORDER = "SalesOrders";
        public const string UNLEASH_API_KEY = "api-key";
        
        public const string ACCEPT = "Accept";
        public const string APPLICATION_TYPE_JSON = "application/json";
        public const string BASEURL = "BaseURL";

        public const string Stripe = "Stripe";
        public const string Stripe_BaseURL = "BaseURL";
        public const string Stripe_Product = "Product";
        public const string Stripe_APIKey = "ApiKey";

        //Message
        public const string NORECORDFOUND = "No record found";
        public const string ACTION_COMPLETED = "Action Completed";
        public const string ACTION_NOT_COMPLETED = "Action Not Completed";

        public const string CUSTOMER_CODE = "customer-code";
        public const string CUSTOMER_CURRENCY_ID = "customer-currency-id";
        public const string CURRENCY_CODE = "currency-code";
        public const string CURRENCY_GUID = "currency-guid";

    }
    public class Enumeration
    {
        public enum OrderStatus
        {
            Parked = 10,
            Placed = 20,
            Completed = 30,
            Deleted = 40
        }
    }
}
