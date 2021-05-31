namespace AtomicWatcher.AtomicHub
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

    public class AtomicService : IAtomicService
    {
        private readonly ILogger logger;
        private readonly AtomicOptions options;
        private readonly HttpClient httpClient;

        public AtomicService(ILogger<AtomicService> logger, IOptions<AtomicOptions> options, HttpClient httpClient)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<List<SaleData>> GetNewSales(long lastId)
        {
            logger.LogDebug($"Loading sales for collection '{options.Collection}' after lastId={lastId}...");

            var res = new List<SaleData>();

            for (var page = 1; page < options.SalesMaxPages; page++)
            {
                var batch = await GetSalesPage(page).ConfigureAwait(false);
                if (lastId == 0)
                {
                    return batch;
                }

                var foundNew = batch.Where(x => long.Parse(x.sale_id, CultureInfo.InvariantCulture) > lastId).ToList();
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

        public async Task<List<Asset>> GetAccountAssets(string account)
        {
            var res = new List<Asset>();

            for (var page = 1; page < options.AssetsMaxPages; page++)
            {
                var assets = await GetAccountAssetsPage(account, page);
                if (assets.Count == 0)
                {
                    break;
                }
                else
                {
                    res.AddRange(assets);
                }
            }

            logger.LogDebug($"Loaded {res.Count} assets for {account}");
            return res;
        }

        public async Task<List<AssetTemplate>> GetTemplates()
        {
            var res = new List<AssetTemplate>();

            for (var page = 1; page < options.TemplatesMaxPages; page++)
            {
                var templates = await GetTemplatesPage(page);
                if (templates.Count == 0)
                {
                    break;
                }
                else
                {
                    res.AddRange(templates);
                }
            }

            logger.LogDebug($"Loaded {res.Count} templates");
            return res;
        }

        protected async Task<List<SaleData>> GetSalesPage(int page)
        {
            var reqData = new
            {
                state = "1",
                max_assets = "1", // exclude bundles
                limit = options.SalesPageSize.ToString(CultureInfo.InvariantCulture),
                page = page.ToString(CultureInfo.InvariantCulture),
                order = "desc",
                sort = "created",
                symbol = "WAX",
                collection_name = options.Collection,
            };

            var reqText = System.Text.Json.JsonSerializer.Serialize(reqData);

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://wax.api.atomicassets.io/atomicmarket/v1/sales");
            req.Content = new StringContent(reqText, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

            using var resp = await httpClient.SendAsync(req).ConfigureAwait(false);

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

            logger.LogDebug($"Returning {data.data.Count} sales");

            return data.data;
        }

        protected async Task<List<Asset>> GetAccountAssetsPage(string account, int page)
        {
            var url = $"https://wax.api.atomicassets.io/atomicassets/v1/assets?owner={account}&collection_name={options.Collection}&page={page}&limit={options.AssetsPageSize}&sort=asset_id";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            using var resp = await httpClient.SendAsync(req).ConfigureAwait(false);

            var respText = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                logger.LogDebug(respText);
                resp.EnsureSuccessStatusCode(); // and throw error
            }

            var data = System.Text.Json.JsonSerializer.Deserialize<AssetsRootObject>(respText);

            if (data == null || !data.success)
            {
                logger.LogDebug(respText);
                throw new ApplicationException("Atomic call failed");
            }

            logger.LogDebug($"Returning {data.data.Count} assets");

            return data.data;
        }

        protected async Task<List<AssetTemplate>> GetTemplatesPage(int page)
        {
            var url = $"https://wax.api.atomicassets.io/atomicassets/v1/templates?collection_name={options.Collection}&page={page}limit={options.TemplatesPageSize}&order=desc&sort=created";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            using var resp = await httpClient.SendAsync(req).ConfigureAwait(false);

            var respText = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                logger.LogDebug(respText);
                resp.EnsureSuccessStatusCode(); // and throw error
            }

            var data = System.Text.Json.JsonSerializer.Deserialize<TemplatesRootObject>(respText);

            if (data == null || !data.success)
            {
                logger.LogDebug(respText);
                throw new ApplicationException("Atomic call failed");
            }

            logger.LogDebug($"Returning {data.data.Count} templates");

            return data.data;
        }
    }
}
