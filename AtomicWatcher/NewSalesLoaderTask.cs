namespace AtomicWatcher
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AtomicWatcher.Data;
    using Microsoft.Extensions.Logging;
    using RecurrentTasks;

    public class NewSalesLoaderTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly AtomicHub.IAtomicService atomicService;
        private readonly ITask analyzerTask;

        public NewSalesLoaderTask(ILogger<NewSalesLoaderTask> logger, IDbProvider dbProvider, AtomicHub.IAtomicService atomicService, ITask<AnalyzerTask> analyzerTask)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.atomicService = atomicService ?? throw new ArgumentNullException(nameof(atomicService));
            this.analyzerTask = analyzerTask ?? throw new ArgumentNullException(nameof(analyzerTask));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var settings = dbProvider.Settings;
            var set = settings.FindOne(x => x.Id == Setting.AtomicLastSaleId);

            var lastId = set?.LongValue ?? 0;
            var sales = await atomicService.GetNewSales(lastId).ConfigureAwait(false);

            if (sales.Count == 0)
            {
                logger.LogInformation("No new sales found");
                return;
            }

            var atomicSales = sales
                .Select(x => new AtomicSale
                {
                    Id = long.Parse(x.sale_id, CultureInfo.InvariantCulture),
                    Created = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(x.created_at_time, CultureInfo.InvariantCulture)),
                    Name = x.assets[0].data.name,
                    Rarity = x.assets[0].data.rarity,
                    Seller = x.seller,
                    Price = decimal.Parse(x.price.amount, CultureInfo.InvariantCulture) / (decimal)Math.Pow(10, x.price.token_precision),
                    Mint = int.Parse(x.assets[0].template_mint, CultureInfo.InvariantCulture),
                    IssuedSupply = int.Parse(x.assets[0].template.issued_supply, CultureInfo.InvariantCulture),
                    MaxSupply = int.Parse(x.assets[0].template.max_supply, CultureInfo.InvariantCulture),
                    TemplateId = int.Parse(x.assets[0].template.template_id, CultureInfo.InvariantCulture),
                    AssetId = long.Parse(x.assets[0].asset_id, CultureInfo.InvariantCulture),
                })
                .ToList();

            lastId = atomicSales.Max(x => x.Id);

            foreach (var sale in atomicSales)
            {
                dbProvider.AtomicSales.Upsert(sale);
                ////dbProvider.AtomicSalesTelegramQueue.Upsert(sale);
                ////dbProvider.AtomicSalesDiscordQueue.Upsert(sale);
                dbProvider.AtomicSalesAnalysisQueue.Upsert(sale);
            }

            settings.Upsert(new Setting(Setting.AtomicLastSaleId) { LongValue = lastId });

            logger.LogInformation($"Saved {sales.Count} new sales");

            analyzerTask.TryRunImmediately();
        }
    }
}
