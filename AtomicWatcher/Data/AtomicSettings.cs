namespace AtomicWatcher.Data
{
    public class AtomicSettings : Setting
    {
        public const string SettingName = "AtomicLastSale";

        public AtomicSettings()
            : base(SettingName)
        {
            // Nothing
        }

        public long Value { get; set; }
    }
}
