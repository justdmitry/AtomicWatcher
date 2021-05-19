namespace AtomicWatcher
{
    public class AtomicOptions
    {
        public string Collection { get; set; } = "none";

        public string? Rarity { get; set; }

        public uint? MinPrice { get; set; }

        public uint? MaxPrice { get; set; }

        public uint? MinMin { get; set; }

        public uint? MaxMin { get; set; }

        public uint PageSize { get; set; } = 20;

        public uint MaxPages { get; set; } = 5;
    }
}
