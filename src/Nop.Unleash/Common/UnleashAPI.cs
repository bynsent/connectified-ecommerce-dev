using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;
using Microsoft.Extensions.Configuration;

namespace Nop.Unleash.Common
{
    public static class UnleashAPI
    {
        private static IConfiguration _configuration;

        public static void AppSettingConfigure(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public static string GetAuthSignature()
        {
            string args = string.Empty;
            string privatekey = GetUnleashAPIKey();
            var encoding = new UTF8Encoding();
            byte[] key = encoding.GetBytes(privatekey);
            var myhmacsha256 = new HMACSHA256(key);
            byte[] hashValue = myhmacsha256.ComputeHash(encoding.GetBytes(args));
            string hmac64 = Convert.ToBase64String(hashValue);
            myhmacsha256.Clear();
            return hmac64;
        }


        public static string Setting(string key)
        {
            var baseUrl = _configuration.GetSection(string.Concat(Constant.UNLEASH_API_URLS,":", Constant.BASE_URL)).Value;
            var endPoint = _configuration.GetSection(string.Concat(Constant.UNLEASH_API_URLS, ":", key)).Value;
            return string.Concat(baseUrl, endPoint);
        }

        public static string GetAPIAuthId()
        {
            var authId = _configuration.GetSection(string.Concat(Constant.UNLEASH_API_URLS,":",Constant.API_AUTH_ID)).Value;
            return authId;
        }

        public static string GetAPIAuthSignature()
        {
            var authSignature = _configuration.GetSection(string.Concat(Constant.UNLEASH_API_URLS,":",Constant.API_AUTH_SIGNATURE)).Value;
            return authSignature;
        }

        public static string GetProductsURL()
        {
            var getProductUrl = Setting(Constant.GET_PRODUCTS);
            return getProductUrl;
        }

        public static string CreateStockAdjustmentURL() {
            var getStockAdjustment = Setting(Constant.CREATE_STOCK_ADJUSTMENT_URL);
            return getStockAdjustment;
        }

        public static string GetStockOnHandURL()
        {
            var getStockUrl = Setting(Constant.GET_STOCK_ONHAND);
            return getStockUrl;
        }

        public static string GetStockOnHandByGuidURL()
        {
            var getStockOnHandUrl = Setting(Constant.GET_STOCKONHAND_BYGUID);
            return getStockOnHandUrl;
        }

        public static string GetSalesOrdersURL()
        {
            var salesOrders = Setting(Constant.SALES_ORDER);
            return salesOrders;
        }
        
        public static string GetBaseURL()
        {
            var value = _configuration.GetSection(Constant.BASEURL).Value;
            return value;
        }

        public static string StripeSetting(string key)
        {
            var baseUrl = _configuration.GetSection(string.Concat(Constant.Stripe, ":", Constant.BASE_URL)).Value;
            var endPoint = _configuration.GetSection(string.Concat(Constant.Stripe, ":", key)).Value;
            return string.Concat(baseUrl, endPoint);
        }


        public static string GetStripeBaseURL()
        {
            var value = StripeSetting(Constant.Stripe_BaseURL);
            return value;
        }

        public static string GetStripeProductURL()
        {
            var value = StripeSetting(Constant.Stripe_Product);
            return value;
        }

        public static string GetStripeAPIKey()
        {
            var value = _configuration.GetSection(string.Concat(Constant.Stripe, ":", Constant.Stripe_APIKey)).Value;
            return value;
        }

        public static string GetUnleashAPIKey()
        {
            var value = _configuration.GetSection(string.Concat(Constant.UNLEASH_API_URLS, ":", Constant.UNLEASH_API_KEY)).Value;
            return value;
        }
        public static string GetCustomerCode()
        {
            var value = _configuration.GetSection(string.Concat(Constant.UNLEASH_API_URLS, ":", Constant.CUSTOMER_CODE)).Value;
            return value;
        }
        public static int GetCustomerCurrencyId()
        {
            var value = _configuration.GetSection(string.Concat(Constant.UNLEASH_API_URLS, ":", Constant.CUSTOMER_CURRENCY_ID)).Value;
            return Convert.ToInt32(value);
        }
        public static string GetCurrencyCode()
        {
            var value = _configuration.GetSection(string.Concat(Constant.UNLEASH_API_URLS, ":", Constant.CURRENCY_CODE)).Value;
            return value;
        }
        public static string GetCurrencyGuid()
        {
            var value = _configuration.GetSection(string.Concat(Constant.UNLEASH_API_URLS, ":", Constant.CURRENCY_GUID)).Value;
            return value;
        }
    }
}
