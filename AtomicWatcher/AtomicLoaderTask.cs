namespace AtomicWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AtomicWatcher.Data;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class AtomicLoaderTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private readonly ILogger logger;
        private readonly AtomicOptions options;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IDbProvider dbProvider;

        public AtomicLoaderTask(ILogger<AtomicLoaderTask> logger, IOptions<AtomicOptions> options, IHttpClientFactory httpClientFactory, IDbProvider dbProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var settings = dbProvider.Settings;
            var set = settings.FindOne(x => x.Id == Setting.AtomicLastSaleId);

            var lastId = set?.LongValue ?? 0;
            var sales = await GetNewSales(lastId).ConfigureAwait(false);

            if (sales.Count == 0)
            {
                logger.LogInformation("No new sales found");
                return;
            }

            lastId = sales.Max(x => x.Id);

            foreach (var sale in sales)
            {
                dbProvider.AtomicSales.Upsert(sale);
                dbProvider.AtomicSalesTelegramQueue.Upsert(sale);
                dbProvider.AtomicSalesDiscordQueue.Upsert(sale);
            }

            settings.Upsert(new Setting(Setting.AtomicLastSaleId) { LongValue = lastId });

            logger.LogInformation($"Saved {sales.Count} new sales");
        }

        public async Task<List<AtomicSale>> GetNewSales(long lastId)
        {
            logger.LogDebug($"Loading sales for collection '{options.Collection}' after lastId={lastId}...");

            var res = new List<AtomicSale>();

            for (var page = 1; page < options.MaxPages; page++)
            {
                var batch = await GetSales(page).ConfigureAwait(false);
                if (lastId == 0)
                {
                    return batch;
                }

                var foundNew = batch.Where(x => x.Id > lastId).ToList();
                if (foundNew.Count > 0)
                {
                    res.AddRange(foundNew);
                }

                if (foundNew.Count < batch.Count)
                {
                    break;
                }
            }

            logger.LogDebug($"Found {res.Count} new sales for collection '{options.Collection}' after lastId={lastId}.");

            return res;
        }

        private async Task<List<AtomicSale>> GetSales(int page)
        {
            var reqData = new
            {
                state = "1",
                max_assets = "1", // exclude bundles
                limit = options.PageSize.ToString(CultureInfo.InvariantCulture),
                page = page.ToString(CultureInfo.InvariantCulture),
                order = "desc",
                sort = "created",
                symbol = "WAX",
                collection_name = options.Collection,
            };

            var reqText = System.Text.Json.JsonSerializer.Serialize(reqData);

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://wax.api.atomicassets.io/atomicmarket/v1/sales");
            req.Content = new StringContent(reqText, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

            using var client = httpClientFactory.CreateClient();

            using var resp = await client.SendAsync(req).ConfigureAwait(false);

            var respText = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                logger.LogDebug(respText);
                resp.EnsureSuccessStatusCode(); // and throw error
            }

            var data = System.Text.Json.JsonSerializer.Deserialize<SalesRootObject>(respText);

            if (data == null || !data.success)
            {
                logger.LogDebug(respText);
                throw new ApplicationException("Atomic call failed");
            }

            var res = data.data
                .Select(x => new AtomicSale
                {
                    Id = long.Parse(x.sale_id, CultureInfo.InvariantCulture),
                    Created = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(x.created_at_time, CultureInfo.InvariantCulture)),
                    Name = x.assets[0].data.name,
                    Rarity = x.assets[0].data.rarity,
                    Seller = x.seller,
                    CardId = int.Parse(x.assets[0].data?.card_id ?? "0", CultureInfo.InvariantCulture),
                    Price = decimal.Parse(x.price.amount, CultureInfo.InvariantCulture) / (decimal)Math.Pow(10, x.price.token_precision),
                    Mint = long.Parse(x.assets[0].template_mint, CultureInfo.InvariantCulture),
                    IssuedSupply = long.Parse(x.assets[0].template.issued_supply, CultureInfo.InvariantCulture),
                    MaxSupply = long.Parse(x.assets[0].template.max_supply, CultureInfo.InvariantCulture),
                })
                .ToList();

            logger.LogDebug($"Returning {res.Count} sales");

            return res;
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable IDE1006 // Naming Styles
    public class SalesRootObject
    {
        public bool success { get; set; }
        public List<SaleData> data { get; set; }
    }

    public class SaleData
    {
        public string sale_id { get; set; }
        public string seller { get; set; }
        public string created_at_time { get; set; }
        public Price price { get; set; }
        public Asset[] assets { get; set; }
    }

    public class Price
    {
        public int token_precision { get; set; }
        public string amount { get; set; }
    }

    public class Asset
    {
        public string asset_id { get; set; }
        public AssetData data { get; set; }
        public AssetTemplate template { get; set; }
        public string template_mint { get; set; }
    }

    public class AssetData
    {
        public string card_id { get; set; }
        public string name { get; set; }
        public string rarity { get; set; }
    }

    public class AssetTemplate
    {
        public string issued_supply { get; set; }
        public string max_supply { get; set; }
    }
}
