namespace AtomicWatcher.Telegram
{
    using System;
    using System.Threading.Tasks;
    using AtomicWatcher.Data;
    using NetTelegramBot.Framework;
    using NetTelegramBotApi.Requests;
    using NetTelegramBotApi.Types;

    public class SettingsCommand : ICommandHandler
    {
        private readonly IDbProvider dbProvider;

        public SettingsCommand(IDbProvider dbProvider)
        {
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        public Task ExecuteAsync(ICommand command, BotBase bot, Message message, IServiceProvider serviceProvider)
        {
            var acc = dbProvider.WaxAccounts.FindOne(x => x.TelegramId == message.From.Id);
            if (acc == null)
            {
                return bot.SendAsync(new SendMessage(message.Chat.Id, Messages.YouAreNotRegistered) { ReplyToMessageId = message.MessageId });
            }

            if (!acc.IsActive)
            {
                return bot.SendAsync(new SendMessage(message.Chat.Id, Messages.YouAreDisabled) { ReplyToMessageId = message.MessageId });
            }

            return bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Settings_Deprecated) { ReplyToMessageId = message.MessageId });
        }
    }
}
