namespace AtomicWatcher
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
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
            var templates = await atomicService.GetTemplates().ConfigureAwait(false);
            var at = templates.Select(x =>
                new AtomicTemplate
                {
                    Id = int.Parse(x.template_id, CultureInfo.InvariantCulture),
                    IssuedSupply = int.Parse(x.issued_supply, CultureInfo.InvariantCulture),
                    MaxSupply = int.Parse(x.max_supply, CultureInfo.InvariantCulture),
                    Name = x.immutable_data.name,
                    Rarity = x.immutable_data.rarity,
                })
                .ToList();

            dbProvider.AtomicTemplates.DeleteAll();
            dbProvider.AtomicTemplates.InsertBulk(at);
        }
    }
}
