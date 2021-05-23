namespace AtomicWatcher
{
    public class AtomicOptions
    {
        public string Collection { get; set; } = "none";

        public uint PageSize { get; set; } = 20;

        public uint MaxPages { get; set; } = 5;
    }
}
