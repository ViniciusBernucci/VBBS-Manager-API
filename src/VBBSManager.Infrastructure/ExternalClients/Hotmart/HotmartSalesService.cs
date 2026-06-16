using Microsoft.Extensions.Logging;

namespace VBBSManager.Infrastructure.ExternalClients.Hotmart;

public interface IHotmartSalesService
{
    Task<SalesConsolidatedReport> GetConsolidatedReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);

    Task<List<DailySaleData>> GetSalesByDayAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default);
}

public class HotmartSalesService(
    IHotmartAuthClient authClient,
    IHotmartClient salesClient,
    ILogger<HotmartSalesService> logger) : IHotmartSalesService
{
    public async Task<SalesConsolidatedReport> GetConsolidatedReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        if (endDate < startDate)
            throw new ArgumentException("A data final deve ser maior ou igual à data inicial.", nameof(endDate));

        var startDateMs = ToUnixMilliseconds(startDate);
        var endDateMs = ToUnixMilliseconds(endDate);

        logger.LogInformation(
            "Consolidating Hotmart sales from {StartDate} to {EndDate} ({StartMs}–{EndMs} ms UTC)",
            startDate,
            endDate,
            startDateMs,
            endDateMs);

        var auth = await authClient.GetAccessTokenAsync(ct);

        var totalSales = 0;
        var revenueByCurrency = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var feeByCurrency    = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        string? pageToken = null;

        do
        {
            var page = await salesClient.GetSalesPageAsync(
                auth.AccessToken,
                startDateMs,
                endDateMs,
                pageToken,
                ct);

            var items = page.Items ?? [];

            foreach (var item in items)
            {
                var fee = item.Purchase?.HotmartFee;
                if (fee is null)
                    continue;

                totalSales++;

                var currency = string.IsNullOrWhiteSpace(fee.CurrencyCode) ? "UNKNOWN" : fee.CurrencyCode;

                // purchase.price.value = valor pago pelo comprador (receita bruta real)
                // hotmart_fee.total = taxa real cobrada pela Hotmart (fixo + percentual)
                var grossAmount = item.Purchase?.Price?.Value ?? 0m;
                revenueByCurrency[currency] = revenueByCurrency.GetValueOrDefault(currency) + grossAmount;
                feeByCurrency[currency]     = feeByCurrency.GetValueOrDefault(currency)     + fee.Total;
            }

            pageToken = page.PageInfo?.NextPageToken;
        }
        while (!string.IsNullOrWhiteSpace(pageToken));

        logger.LogInformation(
            "Hotmart consolidation completed: {TotalSales} sales, gross {Gross}, fees {Fees}",
            totalSales,
            revenueByCurrency.Values.Sum(),
            feeByCurrency.Values.Sum());

        return new SalesConsolidatedReport(totalSales, revenueByCurrency, feeByCurrency);
    }

    public async Task<List<DailySaleData>> GetSalesByDayAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct = default)
    {
        if (endDate < startDate)
            throw new ArgumentException("A data final deve ser maior ou igual à data inicial.", nameof(endDate));

        var startDateMs = ToUnixMilliseconds(startDate);
        var endDateMs   = ToUnixMilliseconds(endDate);

        // Fallback date when no date field is found: first day of the requested range in SP time
        var fallbackDay = DateOnly.FromDateTime(startDate.AddHours(-3));

        logger.LogInformation(
            "Fetching Hotmart sales by day from {StartDate} to {EndDate} ({StartMs}–{EndMs} ms)",
            startDate, endDate, startDateMs, endDateMs);

        var auth = await authClient.GetAccessTokenAsync(ct);

        var byDay = new Dictionary<DateOnly, (int sales, decimal revenue, decimal fee)>();
        string? pageToken = null;
        var totalItems = 0;
        var itemsWithDate = 0;

        do
        {
            var page = await salesClient.GetSalesPageAsync(
                auth.AccessToken, startDateMs, endDateMs, pageToken, ct);

            foreach (var item in page.Items ?? [])
            {
                var purchase = item.Purchase;
                var fee = purchase?.HotmartFee;
                if (fee is null) continue;

                totalItems++;

                // Try all known Hotmart date field names — prefer approved_date, fall back to order_date / purchase_date
                var dateMs = purchase.ApprovedDate ?? purchase.OrderDate ?? purchase.PurchaseDate;

                DateOnly day;
                if (dateMs is long ms)
                {
                    // Convert to São Paulo date (UTC-3, no DST since 2019)
                    var spDt = DateTimeOffset.FromUnixTimeMilliseconds(ms)
                        .ToOffset(TimeSpan.FromHours(-3))
                        .Date;
                    day = DateOnly.FromDateTime(spDt);
                    itemsWithDate++;
                }
                else
                {
                    day = fallbackDay;
                }

                // purchase.price.value = valor pago pelo comprador
                // hotmart_fee.base pode ser 0 em alguns meios de pagamento (ex: Pix)
                var grossAmount = purchase.Price?.Value ?? 0m;

                var prev = byDay.GetValueOrDefault(day);
                byDay[day] = (prev.sales + 1, prev.revenue + grossAmount, prev.fee + fee.Total);
            }

            pageToken = page.PageInfo?.NextPageToken;
        }
        while (!string.IsNullOrWhiteSpace(pageToken));

        logger.LogInformation(
            "Hotmart daily fetch complete: {Total} vendas totais, {WithDate} com data, {Days} dias com vendas (fallback: {FallbackDay})",
            totalItems, itemsWithDate, byDay.Count, fallbackDay);

        if (totalItems > 0 && itemsWithDate == 0)
            logger.LogWarning(
                "Nenhuma venda retornou campo de data (approved_date/order_date/purchase_date). " +
                "Todas as {Total} vendas foram agrupadas no dia {FallbackDay}. " +
                "Verifique os campos disponíveis na resposta da API Hotmart.",
                totalItems, fallbackDay);

        return [.. byDay
            .Select(kv => new DailySaleData(kv.Key, kv.Value.sales, kv.Value.revenue, kv.Value.fee))
            .OrderBy(x => x.Date)];
    }

    private static long ToUnixMilliseconds(DateTime dateTime)
    {
        var utc = dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
        };

        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }
}
