namespace AtomicWatcher.Data
{
    using System;
    using System.Text;
    using LiteDB;

    public class AtomicSale
    {
        public long Id { get; set; }

        public string Seller { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Rarity { get; set; }

        public DateTimeOffset Created { get; set; }

        public int Mint { get; set; }

        public int IssuedSupply { get; set; }

        public int MaxSupply { get; set; }

        public int CardId { get; set; }

        public int TemplateId { get; set; }

        public long AssetId { get; set; }

        [BsonIgnore]
        public string? RaritySymbol
        {
            get
            {
                return Rarity switch
                {
                    "Unique" => Emoji.BlackCircle,
                    "Legendary" => Emoji.RedSquare,
                    "Epic" => Emoji.OrangeSquare,
                    "Rare" => Emoji.YellowSquare,
                    "Uncommon" => Emoji.GreenSquare,
                    "Common" => Emoji.BlueSquare,
                    _ => Emoji.QuestionMark,
                };
            }
        }

        [BsonIgnore]
        public string? RarityIcon
        {
            get
            {
                return Rarity switch
                {
                    "Legendary" => "https://cdn.discordapp.com/emojis/753660996790255830.png?v=1",
                    "Epic" => "https://cdn.discordapp.com/emojis/753660996941381753.png?v=1",
                    "Rare" => "https://cdn.discordapp.com/emojis/753660996928667730.png?v=1",
                    "Uncommon" => "https://cdn.discordapp.com/emojis/753660997130256385.png?v=1",
                    "Common" => "https://cdn.discordapp.com/emojis/753660997377589391.png?v=1",
                    _ => null,
                };
            }
        }

        [BsonIgnore]
        public uint RarityColor
        {
            get
            {
                return Rarity switch
                {
                    "Rare" => 0x3FD83F,
                    "Uncommon" => 0x418723,
                    "Common" => 0x274E13,
                    "Epic" => 0xC1E841,
                    "Legendary" => 0xF4F435,
                    _ => 0,
                };
            }
        }

        [BsonIgnore]
        public string Link
        {
            get
            {
                return "https://wax.atomichub.io/market/sale/" + Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
