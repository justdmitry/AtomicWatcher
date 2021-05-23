namespace AtomicWatcher.Data
{
    using LiteDB;

    public interface IDbProvider
    {
        ILiteCollection<AtomicSale> AtomicSales { get; }

        ILiteCollection<AtomicSale> AtomicSalesTelegramQueue { get; }

        ILiteCollection<AtomicSale> AtomicSalesDiscordQueue { get; }

        ILiteCollection<Setting> Settings { get; }
    }
}
