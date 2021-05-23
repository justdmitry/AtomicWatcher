namespace AtomicWatcher.Data
{
    public class Setting
    {
        public const string AtomicLastSaleId = "AtomicLastSale";

        public Setting(string id)
        {
            this.Id = id;
        }

        public string Id { get; set; }

        public int? IntValue { get; set; }

        public long? LongValue { get; set; }

        public string? StringValue { get; set; }
    }
}
