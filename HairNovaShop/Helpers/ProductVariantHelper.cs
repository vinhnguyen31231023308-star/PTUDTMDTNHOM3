using System.Text.Json;
using HairNovaShop.Models;

namespace HairNovaShop.Helpers;

public static class ProductVariantHelper
{
    public class VariantInfo
    {
        public string Capacity { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public int Stock { get; set; }
    }

    // Parse variants from JSON string
    public static List<VariantInfo> ParseVariants(string? jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
            return new List<VariantInfo>();

        try
        {
            return JsonSerializer.Deserialize<List<VariantInfo>>(jsonString) ?? new List<VariantInfo>();
        }
        catch
        {
            return new List<VariantInfo>();
        }
    }

    // Serialize variants to JSON string
    public static string SerializeVariants(List<VariantInfo> variants)
    {
        if (variants == null || !variants.Any())
            return string.Empty;

        return JsonSerializer.Serialize(variants);
    }

    // Get total stock from variants
    public static int GetTotalStock(List<VariantInfo> variants)
    {
        return variants?.Sum(v => v.Stock) ?? 0;
    }

    // Get stock for specific capacity
    public static int GetStockByCapacity(List<VariantInfo> variants, string capacity)
    {
        var variant = variants?.FirstOrDefault(v => v.Capacity.Trim().Equals(capacity.Trim(), StringComparison.OrdinalIgnoreCase));
        return variant?.Stock ?? 0;
    }

    // Get price for specific capacity (returns product price if variant price is null)
    public static decimal GetPriceByCapacity(List<VariantInfo> variants, string capacity, decimal defaultPrice)
    {
        var variant = variants?.FirstOrDefault(v => v.Capacity.Trim().Equals(capacity.Trim(), StringComparison.OrdinalIgnoreCase));
        return variant?.Price ?? defaultPrice;
    }
}
