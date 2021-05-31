namespace AtomicWatcher.Telegram
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using NetTelegramBot.Framework;
    using NetTelegramBotApi.Requests;
    using NetTelegramBotApi.Types;

    public class NotifierBot : BotBase
    {
        public static readonly Dictionary<string, Type> AdminCommandHandlers = new Dictionary<string, Type>()
        {
            ["users"] = typeof(Admin.UsersCommand),
        };

        public static readonly Dictionary<string, Type> UserCommandHandlers = new Dictionary<string, Type>()
        {
            ["help"] = typeof(HelpCommand),
            ["inventory"] = typeof(InventoryCommand),
            ["set"] = typeof(SettingsCommand),
            ["settings"] = typeof(SettingsCommand),
        };

        public NotifierBot(ILogger<NotifierBot> logger, ICommandParser commandParser, IHttpClientFactory httpClientFactory)
            : base(logger, commandParser, httpClientFactory)
        {
            foreach (var pair in AdminCommandHandlers.Concat(UserCommandHandlers))
            {
                this.RegisteredCommandHandlers[pair.Key] = pair.Value;
            }
        }

        public override Task OnNonMessageAsync(Update update, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public override Task OnTextMessageAsync(Message message, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        public override Task OnUnknownCommandMessageAsync(Message message, ICommand command, IServiceProvider serviceProvider)
        {
            var reply = new SendMessage(message.Chat.Id, "Unknown command: " + command.Name) { ReplyToMessageId = message.MessageId };
            return this.SendAsync(reply);
        }
    }
}
