using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Unleash.Models
{
    public class ProductsResponse
    {
        public Pagination Pagination { get; set; }
        public List<Items> Items { get; set; }
    }
    public class Pagination
    {
        public int NumberOfItems { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public int NumberOfPages { get; set; }

    }

    public class Items
    {
        public string ProductCode { get; set; }
        public string ProductDescription { get; set; }
        public string? Barcode { get; set; }
        public string? PackSize { get; set; }
        public string? Width { get; set; }
        public string? Height { get; set; }
        public string? Depth { get; set; }
        public string? Weight { get; set; }
        public string? MinStockAlertLevel { get; set; }
        public string? MaxStockAlertLevel { get; set; }
        public string? ReOrderPoint { get; set; }
        public UnitOfMeasure UnitOfMeasure { get; set; }
        public bool NeverDiminishing { get; set; }

        public decimal? LastCost { get; set; }
        public decimal? DefaultPurchasePrice { get; set; }
        public decimal? DefaultSellPrice { get; set; }
        public string? CustomerSellPrice { get; set; }
        public decimal? AverageLandPrice { get; set; }
        public bool Obsolete { get; set; }
        public string? Notes { get; set; }
        public List<Images> Images { get; set; }
        public string? ImageUrl { get; set; }
        public SellPriceTier1 SellPriceTier1 { get; set; }
        public SellPriceTier2 SellPriceTier2 { get; set; }
        public SellPriceTier3 SellPriceTier3 { get; set; }
        public SellPriceTier4 SellPriceTier4 { get; set; }
        public SellPriceTier5 SellPriceTier5 { get; set; }
        public SellPriceTier6 SellPriceTier6 { get; set; }
        public SellPriceTier7 SellPriceTier7 { get; set; }
        public SellPriceTier8 SellPriceTier8 { get; set; }
        public SellPriceTier9 SellPriceTier9 { get; set; }
        public SellPriceTier10 SellPriceTier10 { get; set; }

        public string? XeroTaxCode { get; set; }
        public string? XeroTaxRate { get; set; }
        public bool TaxablePurchase { get; set; }
        public bool TaxableSales { get; set; }
        public string? XeroSalesTaxCode { get; set; }
        public string? XeroSalesTaxRate { get; set; }
        public bool IsComponent { get; set; }
        public bool IsAssembledProduct { get; set; }
        public ProductGroup ProductGroup { get; set; }
        public string? XeroSalesAccount { get; set; }
        public string? XeroCostOfGoodsAccount { get; set; }
        public string? PurchaseAccount { get; set; }
        public string? BinLocation { get; set; }
        public Supplier Supplier { get; set; }
        public string? AttributeSet { get; set; }
        public int? SourceId { get; set; }
        public int? SourceVariantParentId { get; set; }
        public bool IsSerialized { get; set; }
        public bool IsBatchTracked { get; set; }
        public bool IsSellable { get; set; }
        public decimal? MinimumSellPrice { get; set; }
        public decimal? MinimumSaleQuantity { get; set; }
        public decimal? MinimumOrderQuantity { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public string? CommerceCode { get; set; }
        public string? CustomsDescription { get; set; }
        public string? SupplementaryClassificationAbbreviation { get; set; }
        public string? ICCCountryCode { get; set; }
        public string? ICCCountryName { get; set; }
        public List<InventoryDetails> InventoryDetails { get; set; }
        public List<AlternateUnitsOfMeasure> AlternateUnitsOfMeasure { get; set; }
        public string? Guid { get; set; }
        public DateTime LastModifiedOn { get; set; }

    }

    public class Images
    {
        public string? Url { get; set; }
    }

    public class Supplier
    {
        public string? SupplierProductCode { get; set; }
        public string? SupplierProductDescription { get; set; }
        public string? SupplierProductPrice { get; set; }
        public string? Guid { get; set; }
        public string? SupplierCode { get; set; }
        public string? SupplierName { get; set; }

    }

    public class UnitOfMeasure
        {
            public string? Guid { get; set; }
            //public string? Name { get; set; }
            //public bool Obsolete { get; set; }
        }

    public class SellPriceTier1
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class SellPriceTier2
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class SellPriceTier3
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class SellPriceTier4
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class SellPriceTier5
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class SellPriceTier6
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class SellPriceTier7
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class SellPriceTier8
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class SellPriceTier9
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class SellPriceTier10
    {
        public string Name { get; set; }
        public string? Value { get; set; }
    }

    public class ProductGroup
    {
        public string? GroupName { get; set; }
        public string Guid { get; set; }
        // public DateTime LastModifiedOn { get; set; }
    }

    public class InventoryDetails
    {
        public Warehouse Warehouse { get; set; }
        public string? WarehouseMinStockAlertLevel { get; set; }
        public string? WarehouseMaxStockAlertLevel { get; set; }
    }

    public class Warehouse
    {
        // public string? Guid { get; set; }
        public string? WarehouseCode { get; set; }
        //public string? WarehouseName { get; set; }
    }

    public class AlternateUnitsOfMeasure
    {

    }

    public class CreateProduct
    {
        public string ProductCode { get; set; }
        public string ProductDescription { get; set; }
        public string? Barcode { get; set; }
        public string? PackSize { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public decimal? Depth { get; set; }
        public decimal? Weight { get; set; }
        public string? MinStockAlertLevel { get; set; }
        public string? MaxStockAlertLevel { get; set; }
        public string? ReOrderPoint { get; set; }
        public UnitOfMeasure UnitOfMeasure { get; set; }
        public bool NeverDiminishing { get; set; }
        public decimal? LastCost { get; set; }
        public decimal? DefaultPurchasePrice { get; set; }
        public decimal? DefaultSellPrice { get; set; }
        public string? CustomerSellPrice { get; set; }
        public decimal? AverageLandPrice { get; set; }
        public bool Obsolete { get; set; }
        public string? XeroTaxCode { get; set; }
        public string? XeroTaxRate { get; set; }
        public bool TaxablePurchase { get; set; }
        public bool TaxableSales { get; set; }
        public string? XeroSalesTaxCode { get; set; }
        public string? XeroSalesTaxRate { get; set; }
        public bool IsComponent { get; set; }
        public bool IsAssembledProduct { get; set; }
        public string? CommerceCode { get; set; }
        public string? CustomsDescription { get; set; }
        public string? SupplementaryClassificationAbbreviation { get; set; }
        public string? ICCCountryCode { get; set; }
        public string? ICCCountryName { get; set; }
        public ProductGroup ProductGroup { get; set; }
        public string? XeroSalesAccount { get; set; }
        public string? XeroCostOfGoodsAccount { get; set; }
        public string? PurchaseAccount { get; set; }
        public string? BinLocation { get; set; }
        public Supplier Supplier { get; set; }
        public string? AttributeSet { get; set; }
        public int? SourceId { get; set; }
        public int? SourceVariantParentId { get; set; }
        public bool IsSerialized { get; set; }
        public bool IsBatchTracked { get; set; }
        public decimal? MinimumSellPrice { get; set; }
        public decimal? MinimumSaleQuantity { get; set; }
        public decimal? MinimumOrderQuantity { get; set; }
        public string Guid { get; set; }


    }

    public class CreateStock
    {
        public string AdjustmentNumber { get; set; }
        public Warehouse Warehouse { get; set; }
        public string? AdjustmentDate { get; set; }
        public string? AdjustmentReason { get; set; }
        public string? Status { get; set; }
        public string? AccountCode { get; set; }
        public string? Guid { get; set; }

        public List<StockAdjustmentLines> StockAdjustmentLine {get;set;}
    }

    public class StockAdjustmentLines
    {
        public string LineNumber { get; set; }
        public Products Product { get; set; }
        public int NewQuantity { get; set; }
        public int NewActualValue { get; set; }
        public string Comments { get; set; }
        public SerialNumbers SerialNumbers { get; set; }
        public BatchNumbers BatchNumbers { get; set; }
        public string Guid { get; set; }
    }

    public class Products
    {
        public string ProductCode { get; set; }
    }

    public class SerialNumbers{ }

    public class BatchNumbers { }

  
}
