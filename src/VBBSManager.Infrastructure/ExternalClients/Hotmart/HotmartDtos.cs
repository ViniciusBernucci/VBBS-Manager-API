using System.Text.Json.Serialization;

namespace VBBSManager.Infrastructure.ExternalClients.Hotmart;

public record AuthResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);

public record HotmartSalesResponse(
    [property: JsonPropertyName("items")] List<HotmartSaleItem>? Items,
    [property: JsonPropertyName("page_info")] HotmartPageInfo? PageInfo);

public record HotmartPageInfo(
    [property: JsonPropertyName("next_page_token")] string? NextPageToken);

public record HotmartSaleItem(
    [property: JsonPropertyName("purchase")] HotmartPurchase? Purchase);

public record HotmartPurchase(
    [property: JsonPropertyName("price")] HotmartPrice? Price,
    [property: JsonPropertyName("hotmart_fee")] HotmartFee? HotmartFee,
    [property: JsonPropertyName("approved_date")] long? ApprovedDate,
    [property: JsonPropertyName("order_date")] long? OrderDate,
    [property: JsonPropertyName("purchase_date")] long? PurchaseDate);

public record HotmartPrice(
    [property: JsonPropertyName("value")] decimal Value,
    [property: JsonPropertyName("currency_code")] string CurrencyCode);

public record HotmartFee(
    [property: JsonPropertyName("fixed")] decimal Fixed,
    [property: JsonPropertyName("percentage")] decimal Percentage,
    [property: JsonPropertyName("total")] decimal Total,
    [property: JsonPropertyName("currency_code")] string CurrencyCode,
    [property: JsonPropertyName("base")] decimal Base);

// TotalRevenueByCurrency = soma de hotmart_fee.base (valor do produto sem taxa do cartão)
// TotalHotmartFeeByCurrency = soma de hotmart_fee.total (taxa real cobrada pela Hotmart)
public record SalesConsolidatedReport(
    int TotalSales,
    IReadOnlyDictionary<string, decimal> TotalRevenueByCurrency,
    IReadOnlyDictionary<string, decimal> TotalHotmartFeeByCurrency);

public record DailySaleData(DateOnly Date, int TotalSales, decimal GrossRevenue, decimal HotmartFeeAmount);
