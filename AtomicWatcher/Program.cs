namespace AtomicWatcher
{
    using System;
    using AtomicWatcher.Data;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddLogging(o => o.AddConfiguration(hostContext.Configuration.GetSection("Logging")).AddConsole())
                        .AddHttpClient();

                    services.AddTask<CardImageFinderTask>(o => o.AutoStart(CardImageFinderTask.Interval, TimeSpan.FromSeconds(3)));

                    services.Configure<DbOptions>(hostContext.Configuration.GetSection("DbOptions"));
                    services.AddSingleton<IDbProvider, DbProvider>();

                    services
                        .Configure<AtomicOptions>(hostContext.Configuration.GetSection("AtomicOptions"))
                        .AddTask<AtomicLoaderTask>(o => o.AutoStart(AtomicLoaderTask.Interval));

                    services
                        .Configure<TelegramOptions>(hostContext.Configuration.GetSection("TelegramOptions"))
                        .AddTask<TelegramPublisherTask>(o => o.AutoStart(TelegramPublisherTask.Interval));

                    services
                        .Configure<DiscordOptions>(hostContext.Configuration.GetSection("DiscordOptions"))
                        .AddTask<DiscordPublisherTask>(o => o.AutoStart(DiscordPublisherTask.Interval));

                    services
                        .Configure<WaxAccountUpdaterOptions>(hostContext.Configuration.GetSection("WaxAccountUpdaterOptions"))
                        .AddTask<WaxAccountUpdaterTask>(o => o.AutoStart(WaxAccountUpdaterTask.DefaultInterval))
                        .AddTask<AnalyzerTask>(o => o.AutoStart(AnalyzerTask.Interval));
                });
    }
}
