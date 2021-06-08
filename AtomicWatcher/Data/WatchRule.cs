namespace AtomicWatcher.Data
{
    using System;
    using LiteDB;

    public class WatchRule
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        public string WaxAccountId { get; set; } = string.Empty;

        public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;

        public bool Ignore { get; set; }

        public bool LowerMints { get; set; }

        public bool Absent { get; set; }

        public string? Rarity { get; set; }

        public int? MinCardId { get; set; }

        public int? MaxCardId { get; set; }

        public int? MinMint { get; set; }

        public int? MaxMint { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }
    }
}
