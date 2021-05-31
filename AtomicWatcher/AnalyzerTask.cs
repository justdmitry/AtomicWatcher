namespace AtomicWatcher
{
    using System;
    using System.Collections.Generic;
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
            var accounts = dbProvider.WaxAccounts.Query().Where(x => x.IsActive).ToList();
            logger.LogDebug($"Loaded {accounts.Count} active wax accounts");

            var queue = new List<(WaxAccount account, int existingMint)>();

            if (dbProvider.AtomicSalesAnalysisQueue.FindOne(x => true) == null)
            {
                return;
            }

            var me = await telegramBot.MakeRequestAsync(new GetMe()).ConfigureAwait(false);
            if (me == null)
            {
                throw new ApplicationException("Failed to connect to Telegram");
            }
            else
            {
                logger.LogDebug($"Connected to Telegram as @{me.Username} / #{me.Id}");
            }

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

        private async Task SendTelegramNotifications(IList<(WaxAccount account, int existingMint)> accounts, AtomicSale sale)
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
    }
}
