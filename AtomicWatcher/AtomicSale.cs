namespace AtomicWatcher
{
    using System;
    using System.Text;

    public class AtomicSale
    {
        public long Id { get; set; }

        public string Seller { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Rarity { get; set; }

        public DateTimeOffset Created { get; set; }

        public long Mint { get; set; }

        public string? RarityCode
        {
            get
            {
                return Rarity switch
                {
                    "Unique" => "Unq",
                    "Legendary" => "Lgn",
                    "Uncommon" => "Unc",
                    "Common" => "Cmn",
                    _ => Rarity,
                };
            }
        }

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

        public string Link
        {
            get
            {
                return "https://wax.atomichub.io/market/sale/" + Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
