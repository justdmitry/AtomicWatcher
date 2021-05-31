namespace AtomicWatcher.Telegram
{
    using System;

    public class TelegramOptions
    {
        public string BotId { get; set; } = string.Empty;

        public string ChannelName { get; set; } = string.Empty;

        public long[] Administrators { get; set; } = Array.Empty<long>();
    }
}
