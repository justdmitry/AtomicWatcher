namespace AtomicWatcher
{
    using System;
    using System.Linq;
    using AtomicWatcher.Data;
    using AtomicWatcher.Telegram;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NetTelegramBotApi;

    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            new MigrationService().Migrate(host.Services);
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder => builder.AddSystemdConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "hh:mm:ss ";
                }))
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddLogging(o => o.AddConfiguration(hostContext.Configuration.GetSection("Logging")).AddConsole())
                        .AddHttpClient();

                    services.Configure<DbOptions>(hostContext.Configuration.GetSection("DbOptions"));
                    services.AddSingleton<IDbProvider, DbProvider>();

                    services
                        .Configure<AtomicHub.AtomicOptions>(hostContext.Configuration.GetSection("AtomicOptions"))
                        .AddHttpClient<AtomicHub.IAtomicService, AtomicHub.AtomicService>();

                    services
                        .Configure<TelegramOptions>(hostContext.Configuration.GetSection("TelegramOptions"))
                        .AddTask<TelegramPublisherTask>(o => o.AutoStart(TelegramPublisherTask.Interval))
                        .AddHttpClient<ITelegramBot, TelegramBot>((hc, sp) => new TelegramBot(sp.GetRequiredService<IOptions<TelegramOptions>>().Value.BotId, hc));

                    services
                        .AddSingleton<NetTelegramBot.Framework.ICommandParser>(_ => new NetTelegramBot.Framework.CommandParser('_'))
                        .AddTelegramBot<NotifierBot>(hostContext.Configuration["TelegramOptions:BotId"], default(Uri));

                    foreach (var command in Telegram.NotifierBot.AdminCommandHandlers.Concat(Telegram.NotifierBot.UserCommandHandlers))
                    {
                        services.AddTransient(command.Value);
                    }

                    services
                        .Configure<DiscordOptions>(hostContext.Configuration.GetSection("DiscordOptions"))
                        .AddTask<DiscordPublisherTask>(o => o.AutoStart(DiscordPublisherTask.Interval));

                    services
                        .AddTask<WaxAccountUpdaterTask>(o => o.AutoStart(WaxAccountUpdaterTask.DefaultInterval))
                        .AddTask<NewSalesLoaderTask>(o => o.AutoStart(NewSalesLoaderTask.Interval))
                        .AddTask<CardsUpdaterTask>(o => o.AutoStart(CardsUpdaterTask.Interval))
                        .AddTask<AnalyzerTask>(o => o.AutoStart(AnalyzerTask.Interval));
                });
    }
}
