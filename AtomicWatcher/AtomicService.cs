namespace AtomicWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class AtomicService
    {
        private readonly ILogger logger;
        private readonly AtomicOptions options;
        private readonly IHttpClientFactory httpClientFactory;

        public AtomicService(ILogger<AtomicService> logger, IOptions<AtomicOptions> options, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
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

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://wax.api.aa.atomichub.io/atomicmarket/v1/sales");
            req.Content = new StringContent(reqText, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
            req.Headers.Add("Referer", "https://wax.atomichub.io/");
            req.Headers.Add("Origin", "https://wax.atomichub.io/");

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
        public AssetTemplate template { get;set; }
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
