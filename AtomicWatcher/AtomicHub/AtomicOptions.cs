namespace AtomicWatcher.AtomicHub
{
    public class AtomicOptions
    {
        public string Collection { get; set; } = "none";

        public uint SalesPageSize { get; set; } = 20;

        public uint SalesMaxPages { get; set; } = 5;

        public uint AssetsPageSize { get; set; } = 100;

        public uint AssetsMaxPages { get; set; } = 5;

        public uint TemplatesPageSize { get; set; } = 100;

        public uint TemplatesMaxPages { get; set; } = 5;
    }
}
