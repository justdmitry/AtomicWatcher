namespace AtomicWatcher
{
    using System;
    using System.Globalization;
    using System.Linq;
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
        private readonly WaxAccountUpdaterOptions options;
        private readonly AtomicHub.IAtomicService atomicService;

        public WaxAccountUpdaterTask(ILogger<WaxAccountUpdaterTask> logger, IDbProvider dbProvider, IOptions<WaxAccountUpdaterOptions> options, AtomicHub.IAtomicService atomicService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.atomicService = atomicService ?? throw new ArgumentNullException(nameof(atomicService));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            currentTask.Options.Interval = DefaultInterval;

            //if (options.AlwaysWatchedAccounts != null)
            //{
            //    var allAccounts = dbProvider.WaxAccounts.FindAll().ToList();
            //    foreach (var awa in options.AlwaysWatchedAccounts)
            //    {
            //        var acc = allAccounts.FirstOrDefault(x => x.Id == awa.Key);
            //        if (acc == null)
            //        {
            //            acc = new WaxAccount() { Id = awa.Key, IsActive = true, TelegramUserId = awa.Value };
            //            dbProvider.WaxAccounts.Insert(acc);
            //            logger.LogInformation($"New WaxAccount added: {acc.Id} => {acc.TelegramUserId}");
            //        }
            //        else
            //        {
            //            if (!acc.IsActive)
            //            {
            //                acc.IsActive = true;
            //                dbProvider.WaxAccounts.Update(acc);
            //                logger.LogInformation($"New WaxAccount RE-ACTIVATED: {acc.Id} => {acc.TelegramUserId}");
            //            }

            //            if (acc.TelegramUserId != awa.Value)
            //            {
            //                acc.TelegramUserId = awa.Value;
            //                dbProvider.WaxAccounts.Update(acc);
            //                logger.LogInformation($"Telegram userId updated for {acc.Id}: {acc.TelegramUserId}");
            //            }

            //            allAccounts.Remove(acc);
            //        }
            //    }

            //    foreach (var acc in allAccounts)
            //    {
            //        if (acc.IsActive)
            //        {
            //            acc.IsActive = false;
            //            dbProvider.WaxAccounts.Update(acc);
            //            logger.LogInformation($"New WaxAccount DISABLED: {acc.Id} => {acc.TelegramUserId}");
            //        }
            //    }
            //}

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
