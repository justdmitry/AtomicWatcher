namespace AtomicWatcher.Telegram
{
    using System;
    using System.Text;
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

            if (command.Args == null || command.Args.Length == 0)
            {
                return ShowSettings(bot, message, acc);
            }

            if (command.Args.Length == 2)
            {
                return ToggleSetting(command, bot, message, acc);
            }

            return bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Settings_Invalid) { ReplyToMessageId = message.MessageId });
        }

        private static async Task ShowSettings(BotBase bot, Message message, WaxAccount account)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Current /settings for `{account.Id}`:");
            sb.AppendLine();

            if (account.NotifyNonOwned)
            {
                sb.AppendLine(@"Notify not owned: *YES* /set\_nno\_off");
            }
            else
            {
                sb.AppendLine(@"Notify not owned: *NO* /set\_nno\_on");
            }

            if (account.NotifyLowerMints)
            {
                sb.AppendLine(@"Notify lower mints: *YES* /set\_nlm\_off");
            }
            else
            {
                sb.AppendLine(@"Notify lower mints: *NO* /set\_nlm\_on");
            }

            //if (account.NotifyPriceGreaterThanBalance)
            //{
            //    sb.AppendLine(@"Notify when price more than balance: *YES* /set\_npmtb\_off");
            //}
            //else
            //{
            //    sb.AppendLine(@"Notify when price more than balance: *NO* /set\_npmtb\_on");
            //}

            sb.AppendLine();
            sb.AppendLine("Click on command to toggle.");

            sb.AppendLine();
            sb.AppendLine("Also, check /rules command for additional notifications.");

            await bot.SendAsync(new SendMessage(message.Chat.Id, sb.ToString()) { ParseMode = SendMessage.ParseModeEnum.Markdown }).ConfigureAwait(false);
        }

        private async Task ToggleSetting(ICommand command, BotBase bot, Message message, WaxAccount account)
        {
            var settingText = command.Args![0].ToUpperInvariant();
            var valueText = command.Args[1].ToUpperInvariant();

            var value = valueText switch
            {
                "ON" => (bool?)true,
                "OFF" => (bool?)false,
                _ => null,
            };

            if (value == null)
            {
                await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Settings_Invalid) { ReplyToMessageId = message.MessageId });
                return;
            }

            switch (settingText)
            {
                case "NNO":
                    account.NotifyNonOwned = value.Value;
                    break;
                case "NLM":
                    account.NotifyLowerMints = value.Value;
                    break;
                case "NPMTB":
                    account.NotifyPriceGreaterThanBalance = value.Value;
                    break;
                default:
                    await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Settings_Invalid) { ReplyToMessageId = message.MessageId });
                    return;
            }

            dbProvider.WaxAccounts.Update(account);

            await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Settings_Changed) { ReplyToMessageId = message.MessageId });

            await ShowSettings(bot, message, account);
        }
    }
}
