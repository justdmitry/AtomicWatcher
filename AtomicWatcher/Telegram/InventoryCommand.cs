namespace AtomicWatcher.Telegram
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AtomicWatcher.Data;
    using Microsoft.Extensions.Options;
    using MoreLinq;
    using NetTelegramBot.Framework;
    using NetTelegramBotApi.Requests;
    using NetTelegramBotApi.Types;

    public class InventoryCommand : ICommandHandler
    {
        private readonly IDbProvider dbProvider;
        private readonly TelegramOptions options;

        public InventoryCommand(IDbProvider dbProvider, IOptions<TelegramOptions> options)
        {
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task ExecuteAsync(ICommand command, BotBase bot, Message message, IServiceProvider serviceProvider)
        {
            if (options.Administrators.Contains(message.From.Id) && command.Args != null)
            {
                var acc = dbProvider.WaxAccounts.FindById(command.Args[0]);
                if (acc == null)
                {
                    var msg = string.Format(Messages.Inventory_AccountNotFound, command.Args[0]);
                    return bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                }

                return ShowInventory(bot, message, acc);
            }
            else
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

                return ShowInventory(bot, message, acc);
            }
        }

        private async Task ShowInventory(BotBase bot, Message message, WaxAccount account)
        {
            var templates = dbProvider.AtomicTemplates.FindAll().ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Current content of `{account.Id}` (updated {(int)DateTimeOffset.Now.Subtract(account.LastUpdated).TotalMinutes}min ago):");
            sb.AppendLine();
            foreach (var batch in templates.OrderBy(x => x.Id).Batch(19))
            {
                foreach (var template in batch)
                {
                    sb.Append("№");
                    sb.Append(template.Id);
                    sb.Append(" ");
                    sb.Append(template.Rarity.GetRaritySymbol());
                    sb.Append(" ");
                    sb.Append(template.Name.Replace("`", @"\`"));
                    sb.Append(": ");
                    if (account.TemplatesAndMints != null && account.TemplatesAndMints.TryGetValue(template.Id, out var mint))
                    {
                        sb.Append("*mint ");
                        sb.Append(mint);
                        sb.Append("*");
                    }
                    else
                    {
                        sb.Append("❌ none");
                    }

                    sb.AppendLine();
                }

                await bot.SendAsync(new SendMessage(message.Chat.Id, sb.ToString()) { ParseMode = SendMessage.ParseModeEnum.Markdown }).ConfigureAwait(false);
                sb.Clear();
            }
        }
    }
}
