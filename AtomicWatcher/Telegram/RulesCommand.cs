namespace AtomicWatcher.Telegram
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AtomicWatcher.Data;
    using LiteDB;
    using MoreLinq;
    using NetTelegramBot.Framework;
    using NetTelegramBotApi.Requests;
    using NetTelegramBotApi.Types;

    public class RulesCommand : ICommandHandler
    {
        private const int MaxRulesCount = 19;

        private readonly IDbProvider dbProvider;

        public RulesCommand(IDbProvider dbProvider)
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
                return List(bot, message, acc.Id);
            }
            else if (command.Args[0].ToUpperInvariant() == "DEL")
            {
                return Delete(command, bot, message, acc.Id);
            }
            else if (command.Args[0].ToUpperInvariant() == "ADD")
            {
                return Add(command, bot, message, acc.Id);
            }
            else
            {
                return bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Rules_Help) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
        }

        private async Task List(BotBase bot, Message message, string accountId)
        {
            var rules = dbProvider.WatchRules.Find(x => x.WaxAccountId == accountId).OrderByDescending(x => x.Ignore).ThenBy(x => x.Created).ToList();
            if (rules.Count == 0)
            {
                await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Rules_None) { ReplyToMessageId = message.MessageId });
                return;
            }

            var sb = new StringBuilder();
            var counter = 0;

            foreach (var batch in rules.Batch(19))
            {
                sb.Clear();
                foreach (var rule in batch)
                {
                    counter++;

                    var ruleRarity = string.IsNullOrEmpty(rule.Rarity)
                        ? null
                        : $"rarity = {rule.Rarity}";

                    var ruleCard = (rule.MinCardId, rule.MaxCardId) switch
                    {
                        (null, null) => null,
                        (null, _) => $"card <= {rule.MaxCardId}",
                        (_, null) => $"card >= {rule.MinCardId}",
                        (_, _) => rule.MinCardId == rule.MaxCardId ? $"card = {rule.MaxCardId}" : $"card {rule.MinCardId}…{rule.MaxCardId}",
                    };

                    var ruleMint = (rule.MinMint, rule.MaxMint) switch
                    {
                        (null, null) => null,
                        (null, _) => $"mint <= {rule.MaxMint}",
                        (_, null) => $"mint >= {rule.MinMint}",
                        (_, _) => rule.MinMint == rule.MaxMint ? $"mint = {rule.MaxMint}" : $"mint {rule.MinMint}…{rule.MaxMint}",
                    };

                    var rulePrice = (rule.MinPrice, rule.MaxPrice) switch
                    {
                        (null, null) => null,
                        (null, _) => $"price <= {rule.MaxPrice}",
                        (_, null) => $"price >= {rule.MinPrice}",
                        (_, _) => rule.MinPrice == rule.MaxPrice ? $"price = {rule.MaxPrice}" : $"price {rule.MinPrice}…{rule.MaxPrice}",
                    };

                    var ruleAbsent = rule.Absent ? "absent (don't have)" : null;
                    var ruleLowerMint = rule.LowerMints ? "with lower mint" : null;

                    sb.Append(counter);
                    sb.Append(". ");
                    sb.Append(rule.Ignore ? "*IGNORE* " : "*Notify* ");
                    sb.AppendLine(string.Join(", ", new[] { ruleAbsent, ruleLowerMint, ruleRarity, ruleCard, ruleMint, rulePrice }.Where(x => x != null)));
                    sb.AppendLine($"    delete: /rules\\_del\\_{rule.Id}");
                }

                await bot.SendAsync(new SendMessage(message.Chat.Id, sb.ToString()) { ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
        }

        private async Task Delete(ICommand command, BotBase bot, Message message, string accountId)
        {
            if (command.Args?.Length != 2)
            {
                await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Rules_Delete_WrongArguments) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }

            var ruleId = command.Args![1];

            var rules = dbProvider.WatchRules.Find(x => x.WaxAccountId == accountId).ToList();
            foreach (var rule in rules)
            {
                if (StringComparer.InvariantCultureIgnoreCase.Equals(rule.Id.ToString(), ruleId))
                {
                    dbProvider.WatchRules.Delete(rule.Id);
                    var msg = string.Format(Messages.Rules_Delete_Ok, rule.Id);
                    await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                    return;
                }
            }

            {
                var msg = string.Format(Messages.Rules_Delete_NotFound, ruleId);
                await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
        }

        private async Task Add(ICommand command, BotBase bot, Message message, string accountId)
        {
            if (command.Args == null || command.Args.Length < 2)
            {
                await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Rules_Add_WrongArguments) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }

            var rules = dbProvider.WatchRules.Find(x => x.WaxAccountId == accountId).ToList();
            if (rules.Count >= MaxRulesCount)
            {
                await bot.SendAsync(new SendMessage(message.Chat.Id, Messages.Rules_TooMany) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }

            var rule = new WatchRule() { WaxAccountId = accountId };

            var parts =
                string.Join(' ', message.Text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Replace(" =", "=").Replace("= ", "=")
                .Split(' ');

            bool looksGood = false;

            for (var i = 0; i < parts.Length; i++)
            {
                switch (parts[i])
                {
                    case "/rules":
                        continue;
                    case "add":
                        continue;

                    case "ignore":
                        rule.Ignore = true;
                        continue;

                    case "absent":
                        rule.Absent = true;
                        continue;

                    case "lower":
                        rule.LowerMints = true;
                        continue;

                    default:
                        var subparts = parts[i].Split('=');
                        if (subparts.Length != 2)
                        {
                            var msg = $"Invalid param: '{parts[i]}'";
                            await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                            return;
                        }

                        if (subparts[0] == "rarity")
                        {
                            rule.Rarity = subparts[1] switch
                            {
                                "common" => "Common",
                                "uncommon" => "Uncommon",
                                "rare" => "Rare",
                                "epic" => "Epic",
                                "legendary" => "Legendary",
                                _ => null
                            };

                            if (rule.Rarity == null)
                            {
                                var msg = $"Invalid rarity value: '{subparts[1]}'";
                                await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                                return;
                            }

                            looksGood = true;
                            continue;
                        }

                        if (!decimal.TryParse(subparts[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var decimalValue))
                        {
                            var msg = $"Non-numeric value: '{subparts[1]}' in '{parts[i]}'";
                            await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                            return;
                        }

                        switch (subparts[0])
                        {
                            case "min-price":
                                rule.MinPrice = decimalValue;
                                looksGood = true;
                                continue;
                            case "max-price":
                                rule.MaxPrice = decimalValue;
                                looksGood = true;
                                continue;
                        }

                        if (!int.TryParse(subparts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var intValue))
                        {
                            var msg = $"Non-integer value: '{subparts[1]}' in '{parts[i]}'";
                            await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                            return;
                        }

                        switch (subparts[0])
                        {
                            case "min-card":
                                rule.MinCardId = intValue;
                                looksGood = true;
                                continue;
                            case "max-card":
                                rule.MaxCardId = intValue;
                                looksGood = true;
                                continue;
                            case "min-mint":
                                rule.MinMint = intValue;
                                looksGood = true;
                                continue;
                            case "max-mint":
                                rule.MaxMint = intValue;
                                looksGood = true;
                                continue;
                        }

                        {
                            var msg = $"Unknown param: '{subparts[0]}' in '{parts[i]}'";
                            await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                            return;
                        }
                }
            }

            if (!looksGood)
            {
                var msg = $"Failed to parse your command to valid rule. Please verity and report to my master.";
                await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }

            if (rule.Absent && rule.LowerMints)
            {
                var msg = $"Invalid params: can't have 'absent' and 'lower' in one rule (it will never match).";
                await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }

            dbProvider.WatchRules.Insert(rule);

            {
                var msg = string.Format(Messages.Rule_Add_Ok, rule.Id);
                await bot.SendAsync(new SendMessage(message.Chat.Id, msg) { ReplyToMessageId = message.MessageId, ParseMode = SendMessage.ParseModeEnum.Markdown });
                return;
            }
        }
    }
}
