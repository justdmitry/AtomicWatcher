namespace AtomicWatcher.Data
{
    using LiteDB;

    public interface IDbProvider
    {
        ILiteCollection<AtomicSale> AtomicSales { get; }

        ILiteCollection<AtomicSale> AtomicSalesTelegramQueue { get; }

        ILiteCollection<AtomicSale> AtomicSalesDiscordQueue { get; }

        ILiteCollection<AtomicSale> AtomicSalesAnalysisQueue { get; }

        ILiteCollection<AtomicTemplate> AtomicTemplates { get; }

        ILiteCollection<WaxAccount> WaxAccounts { get; }

        ILiteCollection<WatchRule> WatchRules { get; }

        ILiteCollection<Setting> Settings { get; }
    }
}
