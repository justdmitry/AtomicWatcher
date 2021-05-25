namespace AtomicWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using AtomicWatcher.Data;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class WaxAccountUpdaterTask : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan HaveMoreDataInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan AccountUpdateInterval = TimeSpan.FromHours(1);

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly AtomicOptions atomicOptions;
        private readonly WaxAccountUpdaterOptions options;
        private readonly IHttpClientFactory httpClientFactory;

        public WaxAccountUpdaterTask(ILogger<WaxAccountUpdaterTask> logger, IDbProvider dbProvider, IOptions<WaxAccountUpdaterOptions> options, IOptions<AtomicOptions> atomicOptions, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.atomicOptions = atomicOptions?.Value ?? throw new ArgumentNullException(nameof(atomicOptions));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            currentTask.Options.Interval = DefaultInterval;

            if (options.AlwaysWatchedAccounts != null)
            {
                var allAccounts = dbProvider.WaxAccounts.FindAll().ToList();
                foreach (var awa in options.AlwaysWatchedAccounts)
                {
                    var acc = allAccounts.FirstOrDefault(x => x.Id == awa.Key);
                    if (acc == null)
                    {
                        acc = new WaxAccount() { Id = awa.Key, IsActive = true, TelegramUserId = awa.Value };
                        dbProvider.WaxAccounts.Insert(acc);
                        logger.LogInformation($"New WaxAccount added: {acc.Id} => {acc.TelegramUserId}");
                    }
                    else
                    {
                        if (!acc.IsActive)
                        {
                            acc.IsActive = true;
                            dbProvider.WaxAccounts.Update(acc);
                            logger.LogInformation($"New WaxAccount RE-ACTIVATED: {acc.Id} => {acc.TelegramUserId}");
                        }

                        if (acc.TelegramUserId != awa.Value)
                        {
                            acc.TelegramUserId = awa.Value;
                            dbProvider.WaxAccounts.Update(acc);
                            logger.LogInformation($"Telegram userId updated for {acc.Id}: {acc.TelegramUserId}");
                        }

                        allAccounts.Remove(acc);
                    }
                }

                foreach (var acc in allAccounts)
                {
                    if (acc.IsActive)
                    {
                        acc.IsActive = false;
                        dbProvider.WaxAccounts.Update(acc);
                        logger.LogInformation($"New WaxAccount DISABLED: {acc.Id} => {acc.TelegramUserId}");
                    }
                }
            }

            var accounts = dbProvider.WaxAccounts.Query().Where(x => x.IsActive).OrderBy(x => x.LastUpdated).Limit(1).ToList();
            if (accounts.Count == 0 || accounts[0].LastUpdated.Add(AccountUpdateInterval) > DateTimeOffset.UtcNow)
            {
                return;
            }

            var account = accounts[0];
            var allAssets = new List<(int templateId, int mint)>();

            logger.LogDebug($"Updating {account.Id} (prev update {account.LastUpdated})...");

            for (var page = 1; page < options.AssetsMaxPages; page++)
            {
                var assets = await GetAssets(account.Id, page);
                if (assets.Count == 0)
                {
                    break;
                }
                else
                {
                    allAssets.AddRange(assets);
                }
            }

            logger.LogDebug($"Loaded {allAssets.Count} for {account.Id}");

            account.TemplatesAndMints = allAssets.GroupBy(x => x.templateId).ToDictionary(x => x.Key, x => x.Min(y => y.mint));

            account.LastUpdated = DateTimeOffset.UtcNow;
            dbProvider.WaxAccounts.Update(account);

            logger.LogDebug($"Saved {account.TemplatesAndMints.Count} unique assets for {account.Id}");

            currentTask.Options.Interval = HaveMoreDataInterval;
        }

        private async Task<List<(int templateId, int mint)>> GetAssets(string account, int page)
        {
            var url = $"https://wax.api.atomicassets.io/atomicassets/v1/assets?owner={account}&collection_name={atomicOptions.Collection}&page={page}&limit={options.AssetsPageSize}&sort=asset_id";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            using var client = httpClientFactory.CreateClient();

            using var resp = await client.SendAsync(req).ConfigureAwait(false);

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

            var res = data.data
                .Select(x => (int.Parse(x.template.template_id, CultureInfo.InvariantCulture), int.Parse(x.template_mint, CultureInfo.InvariantCulture)))
                .ToList();

            logger.LogDebug($"Returning {res.Count} assets");

            return res;
        }
    }
}
