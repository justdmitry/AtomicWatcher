namespace AtomicWatcher
{
    using System.Collections.Generic;

    public class WaxAccountUpdaterOptions
    {
        public uint AssetsPageSize { get; set; } = 100;

        public uint AssetsMaxPages { get; set; } = 5;

        public Dictionary<string, string>? AlwaysWatchedAccounts { get; set; }
    }
}
