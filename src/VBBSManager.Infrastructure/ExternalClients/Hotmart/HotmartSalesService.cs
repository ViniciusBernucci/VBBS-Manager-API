using Microsoft.Extensions.Logging;

namespace VBBSManager.Infrastructure.ExternalClients.Hotmart;

public interface IHotmartSalesService
{
    Task<SalesConsolidatedReport> GetConsolidatedReportAsync(
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

                // base  = valor do produto (sem taxa do cartão parcelado)
                // total = taxa real cobrada pela Hotmart (fixo + percentual)
                revenueByCurrency[currency] = revenueByCurrency.GetValueOrDefault(currency) + fee.Base;
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
