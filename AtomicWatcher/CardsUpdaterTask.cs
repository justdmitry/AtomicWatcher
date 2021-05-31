namespace AtomicWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using AtomicWatcher.AtomicHub;
    using AtomicWatcher.Data;
    using Microsoft.Extensions.Logging;
    using RecurrentTasks;

    public class CardsUpdaterTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        private readonly ILogger logger;
        private readonly HttpClient httpClient;
        private readonly IAtomicService atomicService;
        private readonly IDbProvider dbProvider;

        public CardsUpdaterTask(ILogger<CardsUpdaterTask> logger, HttpClient httpClient, IAtomicService atomicService, IDbProvider dbProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.atomicService = atomicService ?? throw new ArgumentNullException(nameof(atomicService));
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var imagesTask = GetCardImagesAsync(cancellationToken).ConfigureAwait(false);

            var templates = await atomicService.GetTemplates().ConfigureAwait(false);
            var at = templates.Select(x =>
                new AtomicTemplate
                {
                    Id = int.Parse(x.template_id, CultureInfo.InvariantCulture),
                    IssuedSupply = int.Parse(x.issued_supply, CultureInfo.InvariantCulture),
                    MaxSupply = int.Parse(x.max_supply, CultureInfo.InvariantCulture),
                    CardId = int.Parse(x.immutable_data.card_id, CultureInfo.InvariantCulture),
                    Name = x.immutable_data.name,
                    Rarity = x.immutable_data.rarity,
                })
                .ToList();

            var images = await imagesTask;
            foreach (var t in at)
            {
                if (images.TryGetValue(t.CardId, out var url))
                {
                    t.Image = url;
                }
            }

            dbProvider.AtomicTemplates.DeleteAll();
            dbProvider.AtomicTemplates.InsertBulk(at);
        }

        private async Task<Dictionary<int, string>> GetCardImagesAsync(CancellationToken cancellationToken)
        {
            var res = new Dictionary<int, string>();

            var page = await httpClient.GetStringAsync("https://www.cryptomonkeys.cc/gallery", cancellationToken).ConfigureAwait(false);

            var re = new Regex(@"src=""(https://www.cryptomonkeys.cc/wp-content/uploads/20\d\d/\d\d/card_[^""]*)""");

            var matches = re.Matches(page);
            var urls = new List<string>();

            foreach (Match m in matches)
            {
                var url = m.Groups[1].Value;
                urls.Add(url);
                logger.LogTrace(url);
            }

            var index = 1;
            while (true)
            {
                var substring = $"card_{index:0000}";
                var found = urls.Find(x => x.Contains(substring));
                if (found == null)
                {
                    break;
                }
                else
                {
                    res[index] = found;
                    index++;
                }
            }

            logger.LogInformation("Found card images:");
            foreach (var item in res)
            {
                logger.LogInformation($"{item.Key}: {item.Value}");
            }

            return res;
        }
    }
}
