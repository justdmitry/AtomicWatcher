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

    public class WaxAccountUpdaterTask : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan HaveMoreDataInterval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan AccountUpdateInterval = TimeSpan.FromHours(1);

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly AtomicHub.IAtomicService atomicService;

        public WaxAccountUpdaterTask(ILogger<WaxAccountUpdaterTask> logger, IDbProvider dbProvider, AtomicHub.IAtomicService atomicService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.atomicService = atomicService ?? throw new ArgumentNullException(nameof(atomicService));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            currentTask.Options.Interval = DefaultInterval;

            var accounts = dbProvider.WaxAccounts.Query().Where(x => x.IsActive).OrderBy(x => x.LastUpdated).Limit(1).ToList();
            if (accounts.Count == 0 || accounts[0].LastUpdated.Add(AccountUpdateInterval) > DateTimeOffset.UtcNow)
            {
                return;
            }

            var account = accounts[0];
            logger.LogDebug($"Updating {account.Id} (prev update {account.LastUpdated})...");

            var assets = await atomicService.GetAccountAssets(account.Id).ConfigureAwait(false);
            logger.LogDebug($"Loaded {assets.Count} for {account.Id}");

            account.LastUpdated = DateTimeOffset.UtcNow;
            account.TemplatesAndMints = assets
                .Select(x => new
                {
                    template = int.Parse(x.template.template_id, CultureInfo.InvariantCulture),
                    mint = int.Parse(x.template_mint, CultureInfo.InvariantCulture),
                })
                .GroupBy(x => x.template)
                .ToDictionary(x => x.Key, x => x.Min(y => y.mint));

            dbProvider.WaxAccounts.Update(account);

            logger.LogDebug($"Saved {account.TemplatesAndMints.Count} unique assets for {account.Id}");

            currentTask.Options.Interval = HaveMoreDataInterval;
        }
   }
}
