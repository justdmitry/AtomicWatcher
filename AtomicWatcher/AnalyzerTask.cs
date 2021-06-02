namespace AtomicWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AtomicWatcher.Data;
    using Microsoft.Extensions.Logging;
    using NetTelegramBotApi;
    using NetTelegramBotApi.Requests;
    using RecurrentTasks;

    public class AnalyzerTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

        private readonly ILogger logger;
        private readonly IDbProvider dbProvider;
        private readonly ITelegramBot telegramBot;

        public AnalyzerTask(ILogger<AnalyzerTask> logger, IDbProvider dbProvider, ITelegramBot telegramBot)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.telegramBot = telegramBot ?? throw new ArgumentNullException(nameof(telegramBot));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            if (dbProvider.AtomicSalesAnalysisQueue.FindOne(x => true) == null)
            {
                return;
            }

            var accounts = dbProvider.WaxAccounts.Query().Where(x => x.IsActive).ToList();
            logger.LogDebug($"Loaded {accounts.Count} active wax accounts");

            var queue = new List<(WaxAccount account, int existingMint)>();

            var me = await telegramBot.MakeRequestAsync(new GetMe()).ConfigureAwait(false);
            if (me == null)
            {
                throw new ApplicationException("Failed to connect to Telegram");
            }
            else
            {
                logger.LogDebug($"Connected to Telegram as @{me.Username} / #{me.Id}");
            }

            var allRules = dbProvider.WatchRules.FindAll().ToList();

            while (true)
            {
                var sale = dbProvider.AtomicSalesAnalysisQueue.FindOne(x => true);
                if (sale == null)
                {
                    break;
                }

                logger.LogDebug($"Analyzing sale {sale.Id} of №{sale.CardId} {sale.Name} (mint {sale.Mint}, template {sale.TemplateId})...");

                queue.Clear();
                foreach (var acc in accounts)
                {
                    if (acc.TemplatesAndMints == null)
                    {
                        logger.LogDebug($"Account '{acc.Id}' is not loaded yet, skipping");
                        continue;
                    }

                    if (!acc.TemplatesAndMints.TryGetValue(sale.TemplateId, out var mint) && acc.NotifyNonOwned)
                    {
                        logger.LogDebug($"Account '{acc.Id}' has no template {sale.TemplateId} (card {sale.CardId} {sale.Name}), will notify");
                        queue.Add((acc, 0));
                        continue;
                    }

                    if (mint > sale.Mint && acc.NotifyLowerMints)
                    {
                        logger.LogDebug($"Account '{acc.Id}' has older mint of template {sale.TemplateId} (card {sale.CardId} {sale.Name}): {mint} vs {sale.Mint}, will notify");
                        queue.Add((acc, mint));
                        continue;
                    }

                    var rules = allRules.Where(x => x.WaxAccountId == acc.Id);

                    var ignoreRule = rules.Where(x => x.Ignore).FirstOrDefault(x => Matches(x, sale));
                    if (ignoreRule != null)
                    {
                        logger.LogDebug($"Account '{acc.Id}' has 'ignore' rule {ignoreRule.Id} for card {sale.CardId} ({sale.Name}), #{sale.Mint}");
                        continue;
                    }

                    var acceptRule = rules.Where(x => !x.Ignore).FirstOrDefault(x => Matches(x, sale));
                    if (acceptRule != null)
                    {
                        logger.LogDebug($"Account '{acc.Id}' has 'notify' rule {acceptRule.Id} for card {sale.CardId} ({sale.Name}), #{sale.Mint}, will notify");
                        queue.Add((acc, mint));
                        continue;
                    }
                }

                logger.LogDebug($"Should notify {queue.Count} accounts about sale #{sale.Id}");
                if (queue.Count > 0)
                {
                    await SendTelegramNotifications(queue, sale);
                }

                dbProvider.AtomicSalesAnalysisQueue.Delete(sale.Id);
            }
        }

        protected async Task SendTelegramNotifications(IList<(WaxAccount account, int existingMint)> accounts, AtomicSale sale)
        {
            foreach (var acc in accounts)
            {
                if (acc.account.TelegramId == 0)
                {
                    logger.LogWarning($"Account '{acc.account.Id}' have no telegram userId, skipped");
                    continue;
                }

                var text = $@"№{sale.CardId} {sale.Rarity?.GetRaritySymbol()} <b>{sale.Name}</b>
Mint {sale.Mint} (you have {(acc.existingMint == 0 ? "-NONE-" : acc.existingMint)})
<a href='{sale.Link}'>For <b>{sale.Price}</b> WAX</a> by {sale.Seller}";
                var msg = new SendMessage(acc.account.TelegramId, text) { ParseMode = SendMessage.ParseModeEnum.HTML, DisableWebPagePreview = true };
                await telegramBot.MakeRequestAsync(msg).ConfigureAwait(false);
            }
        }

        private static bool Matches(WatchRule rule, AtomicSale sale)
        {
            if (!string.IsNullOrEmpty(rule.Rarity) && sale.Rarity != rule.Rarity)
            {
                return false;
            }

            if (rule.MinCardId.HasValue && sale.CardId < rule.MinCardId)
            {
                return false;
            }

            if (rule.MaxCardId.HasValue && sale.CardId > rule.MaxCardId)
            {
                return false;
            }

            if (rule.MinMint.HasValue && sale.Mint < rule.MinMint)
            {
                return false;
            }

            if (rule.MaxMint.HasValue && sale.Mint > rule.MaxMint)
            {
                return false;
            }

            if (rule.MinPrice.HasValue && sale.Price < rule.MinPrice)
            {
                return false;
            }

            if (rule.MaxPrice.HasValue && sale.Price > rule.MaxPrice)
            {
                return false;
            }

            return true;
        }
    }
}
