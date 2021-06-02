namespace AtomicWatcher.Data
{
    using System;
    using LiteDB;

    public class WatchRule
    {
        public ObjectId Id { get; set; } = ObjectId.Empty;

        public string WaxAccountId { get; set; } = string.Empty;

        public DateTimeOffset Created { get; set; }

        public bool Ignore { get; set; }

        public string? Rarity { get; set; }

        public int? MinCardId { get; set; }

        public int? MaxCardId { get; set; }

        public int? MinMint { get; set; }

        public int? MaxMint { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }
    }
}
