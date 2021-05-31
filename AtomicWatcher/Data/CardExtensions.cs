namespace System
{
    using System.Text;

    public static class CardExtensions
    {
        public static string GetRaritySymbol(this string rarity)
        {
            return rarity.ToUpperInvariant() switch
            {
                "UNIQUE" => Emoji.BlackCircle,
                "LEGENDARY" => Emoji.RedSquare,
                "EPIC" => Emoji.OrangeSquare,
                "RARE" => Emoji.YellowSquare,
                "UNCOMMON" => Emoji.GreenSquare,
                "COMMON" => Emoji.BlueSquare,
                _ => Emoji.QuestionMark,
            };

        }

        public static string? GetRarityIcon(this string rarity)
        {
            return rarity.ToUpperInvariant() switch
            {
                "LEGENDARY" => "https://cdn.discordapp.com/emojis/753660996790255830.png?v=1",
                "EPIC" => "https://cdn.discordapp.com/emojis/753660996941381753.png?v=1",
                "RARE" => "https://cdn.discordapp.com/emojis/753660996928667730.png?v=1",
                "UNCOMMON" => "https://cdn.discordapp.com/emojis/753660997130256385.png?v=1",
                "COMMON" => "https://cdn.discordapp.com/emojis/753660997377589391.png?v=1",
                _ => null,
            };

        }

        public static uint GetRarityDiscordColor(this string rarity)
        {
            return rarity.ToUpperInvariant() switch
            {
                "RARE" => 0x3FD83F,
                "UNCOMMON" => 0x418723,
                "COMMON" => 0x274E13,
                "EPIC" => 0xC1E841,
                "LEGENDARY" => 0xF4F435,
                _ => 0,
            };
        }
    }
}