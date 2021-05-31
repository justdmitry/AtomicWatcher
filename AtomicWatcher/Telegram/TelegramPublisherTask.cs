namespace AtomicWatcher.Telegram
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AtomicWatcher.Data;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NetTelegramBotApi;
    using NetTelegramBotApi.Requests;
    using RecurrentTasks;

    public class TelegramPublisherTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(20);

        protected static readonly TimeSpan Delay = TimeSpan.FromMinutes(2);

        private readonly ILogger logger;
        private readonly TelegramOptions options;
        private readonly IDbProvider dbProvider;
        private readonly ITelegramBot telegramBot;

        public TelegramPublisherTask(ILogger<TelegramPublisherTask> logger, IOptions<TelegramOptions> options, IDbProvider dbProvider, ITelegramBot telegramBot)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.telegramBot = telegramBot ?? throw new ArgumentNullException(nameof(telegramBot));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(options.BotId))
            {
                logger.LogWarning("Telegram BotId is empty, skipping Telegram");
                return;
            }

            var salesQueue = dbProvider.AtomicSalesTelegramQueue;

            var firstSale = salesQueue.FindOne(x => true);
            if (firstSale == null)
            {
                logger.LogInformation("No new sales in queue, nothing to send.");
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

            var sb = new StringBuilder(1024);
            var boundary = DateTimeOffset.Now.Subtract(Delay);
            while (true)
            {
                sb.Clear();
                var sales = salesQueue.Find(x => x.Created < boundary, limit: 7);
                foreach (var sale in sales)
                {
                    sb.AppendLine($@"№{sale.CardId} {sale.Rarity?.GetRaritySymbol()} <b>{sale.Name}</b> #{sale.Mint} <a href='{sale.Link}'>for <b>{sale.Price}</b> WAX</a>");
                    sb.AppendLine();
                    salesQueue.Delete(sale.Id);
                }

                if (sb.Length == 0)
                {
                    break;
                }
                else
                {
                    var msg = new SendMessage(options.ChannelName, sb.ToString()) { ParseMode = SendMessage.ParseModeEnum.HTML, DisableWebPagePreview = true };
                    var msgResult = await telegramBot.MakeRequestAsync(msg).ConfigureAwait(false);
                    logger.LogDebug($"Msg #{msgResult.MessageId} done");
                }
            }
        }
    }
}
