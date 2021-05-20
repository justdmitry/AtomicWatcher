namespace AtomicWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Discord;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using MoreLinq;
    using NetTelegramBotApi;
    using NetTelegramBotApi.Requests;
    using RecurrentTasks;

    public class WatcherTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

        private const string LastIdFileName = "lastId.json";

        private readonly ILogger logger;
        private readonly AtomicService atomicService;
        private readonly TelegramOptions telegramOptions;
        private readonly AtomicOptions atomicOptions;
        private readonly DiscordOptions discordOptions;

        public WatcherTask(ILogger<WatcherTask> logger, AtomicService atomicService, IOptions<TelegramOptions> telegramOptions, IOptions<AtomicOptions> atomicOptions, IOptions<DiscordOptions> discordOptions)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.atomicService = atomicService ?? throw new ArgumentNullException(nameof(atomicService));
            this.telegramOptions = telegramOptions?.Value ?? throw new ArgumentNullException(nameof(telegramOptions));
            this.atomicOptions = atomicOptions?.Value ?? throw new ArgumentNullException(nameof(atomicOptions));
            this.discordOptions = discordOptions?.Value ?? throw new ArgumentNullException(nameof(discordOptions));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            long lastId = 0;

            if (File.Exists(LastIdFileName))
            {
                var lastIdText = await File.ReadAllTextAsync(LastIdFileName, cancellationToken).ConfigureAwait(false);
                lastId = long.Parse(lastIdText, CultureInfo.InvariantCulture);
                logger.LogDebug($"Loaded LastId from file: {lastId}");
            }

            var sales = await atomicService.GetNewSales(lastId).ConfigureAwait(false);
            logger.LogDebug($"Loaded {sales.Count} new sales");

            if (sales.Count == 0)
            {
                return;
            }

            sales.Reverse();
            var overlimit = sales.Count == atomicOptions.PageSize * atomicOptions.MaxPages;

            var send1Task = SendToTelegramAsync(sales, overlimit);
            var send2Task = SendToDiscordAsync(sales, overlimit);

            await Task.WhenAll(send1Task, send2Task).ConfigureAwait(false);

            lastId = sales.Max(x => x.Id);
            await File.WriteAllTextAsync(LastIdFileName, lastId.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
        }

        private async Task SendToTelegramAsync(IList<AtomicSale> sales, bool sendOverlimitMessage)
        {
            if (string.IsNullOrWhiteSpace(telegramOptions.BotId))
            {
                logger.LogWarning("Telegram BotId is empty, skipping Telegram");
                return;
            }

            var bot = new TelegramBot(telegramOptions.BotId);
            var me = await bot.MakeRequestAsync(new GetMe()).ConfigureAwait(false);
            if (me == null)
            {
                throw new ApplicationException("Failed to connect to Telegram");
            }

            logger.LogDebug($"Connected to Telegram as @{me.Username} / #{me.Id}");

            var sb = new StringBuilder(1024);
            foreach (var batch in sales.Batch(7))
            {
                sb.Clear();

                foreach (var sale in batch)
                {
                    sb.AppendLine($@"{sale.RaritySymbol} <b>{sale.Name}</b> #{sale.Mint} <a href='{sale.Link}'>for <b>{sale.Price}</b> WAX</a>");
                    sb.AppendLine();
                }

                var msg = new SendMessage(telegramOptions.ChannelName, sb.ToString()) { ParseMode = SendMessage.ParseModeEnum.HTML, DisableWebPagePreview = true };
                var msgResult = await bot.MakeRequestAsync(msg).ConfigureAwait(false);
                logger.LogDebug($"Msg #{msgResult.MessageId} done");
            }

            if (sendOverlimitMessage)
            {
                var text = $"*Too many new sales, only last {sales.Count} shown! Please visit [AtomicHub](https://wax.atomichub.io/market?collection_name=crptomonkeys&order=desc&sort=created&symbol=WAX) to view other sales!";
                var msg = new SendMessage(telegramOptions.ChannelName, text) { ParseMode = SendMessage.ParseModeEnum.Markdown, DisableWebPagePreview = true };
                var msgResult = await bot.MakeRequestAsync(msg).ConfigureAwait(false);
                logger.LogDebug($"Sent 'TOO MANY NEW SALES' message #{msgResult.MessageId}");
            }
        }

        private async Task SendToDiscordAsync(IList<AtomicSale> sales, bool sendOverlimitMessage)
        {
            if (string.IsNullOrWhiteSpace(discordOptions.Webhook))
            {
                logger.LogWarning("Discord Webhook is empty, skipping Discord");
                return;
            }

            using var client = new Discord.Webhook.DiscordWebhookClient(discordOptions.Webhook);

            var embeds = new List<Embed>();

            foreach (var batch in sales.Batch(5))
            {
                embeds.Clear();
                foreach (var sale in batch)
                {
                    var eb = new EmbedBuilder()
                        .WithAuthor(sale.Name, sale.RarityIcon)
                        .WithColor(sale.RarityColor)
                        .WithTimestamp(sale.Created)
                        .WithFooter("Made with ♥ and potassium");
                    eb.AddField("Seller", sale.Seller, true);
                    eb.AddField("Price", $"[{sale.Price} WAX]({sale.Link})", true);
                    eb.AddField("Mint", $"{sale.Mint} / {sale.IssuedSupply} (max {sale.MaxSupply})");

                    if (CardImageFinderTask.CardImageLocations.ContainsKey(sale.CardId))
                    {
                        eb.WithThumbnailUrl(CardImageFinderTask.CardImageLocations[sale.CardId]);
                    }

                    embeds.Add(eb.Build());
                }

                await client.SendMessageAsync(embeds: embeds).ConfigureAwait(false);
            }

            if (sendOverlimitMessage)
            {
                var text = $"*Too many new sales, only last {sales.Count} shown! Please visit [AtomicHub](https://wax.atomichub.io/market?collection_name=crptomonkeys&order=desc&sort=created&symbol=WAX) to view other sales!";
                await client.SendMessageAsync(text).ConfigureAwait(false);
            }
        }
    }
}
