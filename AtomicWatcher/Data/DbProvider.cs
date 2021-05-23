namespace AtomicWatcher.Data
{
    using System;
    using LiteDB;
    using Microsoft.Extensions.Options;

    public class DbProvider : IDbProvider
    {
        private readonly DbOptions options;

        private LiteDatabase? db;

        public DbProvider(IOptions<DbOptions> options)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public ILiteCollection<AtomicSale> AtomicSales => this.GetDb().GetCollection<AtomicSale>("AtomicSales");

        public ILiteCollection<AtomicSale> AtomicSalesTelegramQueue => this.GetDb().GetCollection<AtomicSale>("AtomicSalesTelegramQueue");

        public ILiteCollection<AtomicSale> AtomicSalesDiscordQueue => this.GetDb().GetCollection<AtomicSale>("AtomicSalesDiscordQueue");

        public ILiteCollection<Setting> Settings => this.GetDb().GetCollection<Setting>("Settings");

        public LiteDatabase GetDb()
        {
            if (db == null)
            {
                db = new LiteDatabase(options.Path);

                var sales = db.GetCollection<AtomicSale>("AtomicSales");
                sales.EnsureIndex(x => x.CardId);

                var settings = db.GetCollection<Setting>("Settings");
                settings.EnsureIndex(x => x.Id, true);
            }

            return db;
        }
    }
}
