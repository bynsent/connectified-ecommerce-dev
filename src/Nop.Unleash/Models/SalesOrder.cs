using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nop.Unleash.Models.SalesOrder;

namespace Nop.Unleash.Models
{
    public class SalesOrder
    {
        public SalesOrderRequest SalesOrderRequest { get; set; }

        public class Warehouse
        {
            public string WarehouseCode { get; set; }
            public string WarehouseName { get; set; }
            public bool IsDefault { get; set; }
            public string StreetNo { get; set; }
            public string AddressLine1 { get; set; }
            public string? AddressLine2 { get; set; }
            public string Suburb { get; set; }
            public string City { get; set; }
            public string Region { get; set; }
            public string Country { get; set; }
            public string PostCode { get; set; }
            public string PhoneNumber { get; set; }
            public string? FaxNumber { get; set; }
            public string? MobileNumber { get; set; }
            public string? DDINumber { get; set; }
            public string ContactName { get; set; }
            public bool Obsolete { get; set; }
            public string Guid { get; set; }
            public string LastModifiedOn { get; set; }

        }

        public class SalesOrderLines
        {
            public int LineNumber { get; set; }
            public string? LineType { get; set; }
            public Product Product { get; set; }
            public string DueDate { get; set; }
            public decimal OrderQuantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountRate { get; set; }
            public decimal LineTotal { get; set; }
            public decimal? Volume { get; set; }
            public decimal? Weight { get; set; }
            public string? Comments { get; set; }
            public decimal AverageLandedPriceAtTimeOfSale { get; set; }
            public decimal TaxRate { get; set; }
            public decimal LineTax { get; set; }
            public string? XeroTaxCode { get; set; }
            public decimal BCUnitPrice { get; set; }
            public decimal BCLineTotal { get; set; }
            public decimal BCLineTax { get; set; }
            public string? LineTaxCode { get; set; }
            public string? XeroSalesAccount { get; set; }
            //public string? SerialNumbers { get; set; }
            public string? BatchNumbers { get; set; }
            public string Guid { get; set; }
            public string LastModifiedOn { get; set; }

        }

        public class Product
        {
            //public string Guid { get; set; }
            public string ProductCode { get; set; }
        }

        public class Customer
        {
            public string CustomerCode { get; set; }
            public string CustomerName { get; set; }
            public int CurrencyId { get; set; }
            public string Guid { get; set; }
            public string LastModifiedOn { get; set; }
        }

        public class Currency
        {
            public string CurrencyCode { get; set;}
            public string Description { get; set; }
            public string Guid { get; set;}
            public string LastModifiedOn { get; set;}
        }

        public class Tax
        {
            public string TaxCode { get; set;}
            public string? Description { get; set;}
            public decimal TaxRate { get; set;}
            public bool CanApplyToExpenses { get; set;}
            public bool CanApplyToRevenue { get; set;}
            public bool Obsolete { get; set;}
            public string Guid { get; set;}
            public string? LastModifiedOn { get; set; }
        }

        public class SalesPerson
        {

        }
    }

    public class SalesOrderRequest
    {
        public List<SalesOrderLines> SalesOrderLines { get; set; } = new List<SalesOrderLines> { };
        public string OrderNumber { get; set; }
        public string OrderDate { get; set; }
        public string? RequiredDate { get; set; }
        public string? CompletedDate { get; set; }
        public string OrderStatus { get; set; }
        public Customer Customer { get; set; }
        public string? CustomerRef { get; set; }
        public string? Comments { get; set; }
        public Warehouse Warehouse { get; set; }
        public string? ReceivedDate { get; set; }
        public string DeliveryInstruction { get; set; }
        public string? DeliveryName { get; set; }
        public string? DeliveryStreetAddress { get; set; }
        public string? DeliveryStreetAddress2 { get; set; }
        public string? DeliverySuburb { get; set; }
        public string? DeliveryCity { get; set; }
        public string? DeliveryRegion { get; set; }
        public string? DeliveryCountry { get; set; }
        public string? DeliveryPostCode { get; set; }
        public Currency Currency { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal DiscountRate { get; set; }
        public Tax Tax { get; set; }
        public decimal TaxRate { get; set; }
        public string XeroTaxCode { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal Total { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal BCSubTotal { get; set; }
        public decimal BCTaxTotal { get; set; }
        public decimal BCTotal { get; set; }
        public string PaymentDueDate { get; set; }
        public bool AllocateProduct { get; set; }
        public string? SalesOrderGroup { get; set; }
        public string? DeliveryMethod { get; set; }
        public SalesPerson SalesPerson { get; set; }
        public string? SourceId { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
        public string LastModifiedBy { get; set; }
        public string Guid { get; set; }
        public string LastModifiedOn { get; set; }

    }

    public class SalesOrderResponse
    {
        public Paginations Pagination { get; set; }
        public List<SalesOrder> Items { get; set; }


        public class Paginations
        {
            public int NumberOfItems { get; set; }
            public int PageSize { get; set; }
            public int PageNumber { get; set; }
            public int NumberOfPages { get; set; }

        }
    }

}
