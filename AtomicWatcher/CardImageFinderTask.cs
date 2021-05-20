namespace AtomicWatcher
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using RecurrentTasks;

    public class CardImageFinderTask : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        public static readonly IDictionary<int, string> CardImageLocations = new Dictionary<int, string>();

        private readonly ILogger logger;
        private readonly IHttpClientFactory httpClientFactory;

        public CardImageFinderTask(ILogger<CardImageFinderTask> logger, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            using var httpClient = httpClientFactory.CreateClient();

            var page = await httpClient.GetStringAsync("https://www.cryptomonkeys.cc/gallery", cancellationToken).ConfigureAwait(false);

            var re = new Regex(@"src=""(https://www.cryptomonkeys.cc/wp-content/uploads/20\d\d/\d\d/card_[^""]*)""");

            var matches = re.Matches(page);
            var urls = new List<string>();

            foreach (Match m in matches)
            {
                var url = m.Groups[1].Value;
                urls.Add(url);
                logger.LogDebug(url);
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
                    CardImageLocations[index] = found;
                    index++;
                }
            }

            logger.LogInformation("Found card images:");
            foreach (var item in CardImageLocations)
            {
                logger.LogInformation($"{item.Key}: {item.Value}");
            }
        }
    }
}
