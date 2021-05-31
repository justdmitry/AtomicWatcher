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
        public string Link
        {
            get
            {
                return "https://wax.atomichub.io/market/sale/" + Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
