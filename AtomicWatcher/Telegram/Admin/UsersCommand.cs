namespace AtomicWatcher.Telegram.Admin
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

    public class UsersCommand : ICommandHandler
    {
        private readonly IDbProvider dbProvider;
        private readonly TelegramOptions options;

        public UsersCommand(IDbProvider dbProvider, IOptions<TelegramOptions> options)
        {
            this.dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task ExecuteAsync(ICommand command, BotBase bot, Message message, IServiceProvider serviceProvider)
        {
            if (!options.Administrators.Contains(message.From.Id))
            {
                return bot.SendAsync(new SendMessage(message.Chat.Id, Messages.AccessDenied) { ReplyToMessageId = message.MessageId });
            }

            if (command.Args == null || command.Args.Length == 0)
            {
                return List(bot, message);
            }
            else if (command.Args[0].ToUpperInvariant() == "ENABLE")
            {
                return Enable(command, bot, message);
            }
            else if (command.Args[0].ToUpperInvariant() == "DISABLE")
            {
                return Disable(command, bot, message);
            }
            else if (command.Args[0].ToUpperInvariant() == "ADD")
            {
                return Add(command, bot, message);
            }
            else
            {
                return bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Users_Help) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
        }

        private async Task List(BotBase bot, Message message)
        {
            var accounts = dbProvider.WaxAccounts.FindAll().OrderBy(x => x.Id).ToList();
            var sb = new StringBuilder();
            var counter = 0;

            foreach (var batch in accounts.Batch(19))
            {
                sb.Clear();
                foreach (var account in batch)
                {
                    counter++;
                    sb.Append(counter);
                    sb.Append(". ");
                    sb.Append(account.Id);
                    sb.Append(" -> ");
                    sb.Append(account.TelegramId);

                    if (!account.IsActive)
                    {
                        sb.Append(" *INACTIVE*");
                    }

                    sb.AppendLine();
                }

                await bot.SendAsync(new SendMessage(message.Chat.Id, sb.ToString()) { ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
        }

        private async Task Enable(ICommand command, BotBase bot, Message message)
        {
            if (command.Args?.Length != 2)
            {
                await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Users_Enable_WrongArguments) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }

            var accountName = command.Args![1];
            var account = dbProvider.WaxAccounts.FindById(accountName);
            if (account == null)
            {
                var msg = string.Format(Messages.Users_Enable_NotFound, accountName);
                await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
            else
            {
                account.IsActive = true;
                dbProvider.WaxAccounts.Update(account);

                var msg = string.Format(Messages.Users_Enable_Success, accountName);
                await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
        }

        private async Task Disable(ICommand command, BotBase bot, Message message)
        {
            if (command.Args?.Length != 2)
            {
                await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Users_Disable_WrongArguments) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }

            var accountName = command.Args![1];
            var account = dbProvider.WaxAccounts.FindById(accountName);
            if (account == null)
            {
                var msg = string.Format(Messages.Users_Disable_NotFound, command.Args[0]);
                await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
            else
            {
                account.IsActive = false;
                dbProvider.WaxAccounts.Update(account);

                var msg = string.Format(Messages.Users_Disable_Success, account.Id);
                await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
        }

        private async Task Add(ICommand command, BotBase bot, Message message)
        {
            if (command.Args?.Length != 3)
            {
                await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Users_Add_WrongArguments) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }

            var accName = command.Args[1];
            var tgidtext = command.Args[2];

            if (!long.TryParse(tgidtext, out var tgid))
            {
                await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Users_Add_WrongArguments) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }

            var account = dbProvider.WaxAccounts.FindById(accName);
            if (account == null)
            {
                account = new WaxAccount()
                {
                    Id = accName,
                    TelegramId = tgid,
                    IsActive = true,
                };
            }
            else
            {
                account.TelegramId = tgid;
            }

            dbProvider.WaxAccounts.Upsert(account);

            var msg = string.Format(Messages.Users_Add_Success, account.Id, account.TelegramId);
            await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
        }
    }
}
