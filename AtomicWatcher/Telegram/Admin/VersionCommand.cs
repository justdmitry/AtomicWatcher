namespace AtomicWatcher.Telegram.Admin
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NetTelegramBot.Framework;
    using NetTelegramBotApi.Requests;
    using NetTelegramBotApi.Types;

    public class VersionCommand : ICommandHandler
    {
        private readonly TelegramOptions options;

        public VersionCommand(IOptions<TelegramOptions> options)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task ExecuteAsync(ICommand command, BotBase bot, Message message, IServiceProvider serviceProvider)
        {
            if (!options.Administrators.Contains(message.From.Id))
            {
                return bot.SendAsync(new SendMessage(message.Chat.Id, Messages.AccessDenied) { ReplyToMessageId = message.MessageId });
            }

            var ver = this.GetType().Assembly.GetName().Version;
            var msg = $"Version: {ver}";

            return bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
        }
    }
}
