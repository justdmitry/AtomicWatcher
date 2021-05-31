namespace AtomicWatcher.Telegram
{
    using System;
    using System.Threading.Tasks;
    using NetTelegramBot.Framework;
    using NetTelegramBotApi.Requests;
    using NetTelegramBotApi.Types;

    public class HelpCommand : ICommandHandler
    {
        public Task ExecuteAsync(ICommand command, BotBase bot, Message message, IServiceProvider serviceProvider)
        {
            return bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Help));
        }
    }
}
