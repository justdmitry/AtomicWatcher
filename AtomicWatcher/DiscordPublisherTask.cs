namespace AtomicWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AtomicWatcher.Data;
    using Discord;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class DiscordPublisherTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

        private readonly ILogger logger;
        private readonly DiscordOptions options;
        private readonly IDbProvider dbProvider;

        public DiscordPublisherTask(ILogger<DiscordPublisherTask> logger, IOptions<DiscordOptions> options, IDbProvider dbProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(options.Webhook))
            {
                logger.LogWarning("Discord Webhook is empty, skipping Discord");
                return;
            }

            var salesQueue = dbProvider.AtomicSalesDiscordQueue;

            var firstSale = salesQueue.FindOne(x => true);
            if (firstSale == null)
            {
                logger.LogInformation("No new sales in queue, nothing to send.");
                return;
            }

            using var client = new Discord.Webhook.DiscordWebhookClient(options.Webhook);

            var embeds = new List<Embed>();

            while (true)
            {
                embeds.Clear();
                var sales = salesQueue.Find(x => true, limit: 5);
                foreach (var sale in sales)
                {
                    var eb = new EmbedBuilder()
                        .WithAuthor($"{sale.Name} (№{sale.CardId})", sale.RarityIcon)
                        .WithColor(sale.RarityColor)
                        .WithTimestamp(sale.Created)
                        .WithFooter("Made with ♥ and potassium");
                    eb.AddField("Seller", sale.Seller, true);
                    eb.AddField("Price", $"[{sale.Price} WAX]({sale.Link})", true);
                    eb.AddField("Mint", $"{sale.Mint} / {sale.IssuedSupply} (max {sale.MaxSupply})");

                    if (CardImageFinderTask.CardImageLocations.TryGetValue(sale.CardId, out var cardUrl))
                    {
                        eb.WithThumbnailUrl(cardUrl);
                    }

                    embeds.Add(eb.Build());
                    salesQueue.Delete(sale.Id);
                }

                if (embeds.Count == 0)
                {
                    break;
                }
                else
                {
                    var msgid = await client.SendMessageAsync(embeds: embeds).ConfigureAwait(false);
                    logger.LogInformation($"Msg #{msgid} is sent.");
                }
            }
        }
    }
}
