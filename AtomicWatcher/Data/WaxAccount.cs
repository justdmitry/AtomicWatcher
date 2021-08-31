namespace AtomicWatcher.Data
{
    using System;
    using System.Collections.Generic;

    public class WaxAccount
    {
        public string Id { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTimeOffset LastUpdated { get; set; }

        public Dictionary<int, int>? TemplatesAndMints { get; set; }

        public long TelegramId { get; set; }
    }
}
