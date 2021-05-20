namespace AtomicWatcher
{
    using System;
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

                    services
                        .Configure<AtomicOptions>(hostContext.Configuration.GetSection("AtomicOptions"))
                        .AddTransient<AtomicService>();

                    services.Configure<TelegramOptions>(hostContext.Configuration.GetSection("TelegramOptions"));
                    services.Configure<DiscordOptions>(hostContext.Configuration.GetSection("DiscordOptions"));

                    services.AddTask<CardImageFinderTask>(o => o.AutoStart(CardImageFinderTask.Interval, TimeSpan.FromSeconds(3)));
                    services.AddTask<WatcherTask>(o => o.AutoStart(WatcherTask.Interval, TimeSpan.FromSeconds(15)));
                });
    }
}
