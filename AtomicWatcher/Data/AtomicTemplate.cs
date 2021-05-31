namespace AtomicWatcher.Data
{
    public class AtomicTemplate
    {
        public int Id { get; set; }

        public int CardId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Rarity { get; set; } = string.Empty;

        public int IssuedSupply { get; set; }

        public int MaxSupply { get; set; }

        public string Image { get; set; } = string.Empty;
    }
}
