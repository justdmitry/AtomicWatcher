﻿namespace AtomicWatcher.Data
{
    using System;
    using LiteDB;
    using Microsoft.Extensions.Options;

    public class DbProvider : IDbProvider, IDisposable
    {
        private readonly object syncRoot = new object();
        private readonly DbOptions options;

        private LiteDatabase? db;

        private bool disposedValue;

        public DbProvider(IOptions<DbOptions> options)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public ILiteCollection<AtomicSale> AtomicSales => this.GetDb().GetCollection<AtomicSale>("AtomicSales");

        public ILiteCollection<AtomicSale> AtomicSalesTelegramQueue => this.GetDb().GetCollection<AtomicSale>("AtomicSalesTelegramQueue");

        public ILiteCollection<AtomicSale> AtomicSalesDiscordQueue => this.GetDb().GetCollection<AtomicSale>("AtomicSalesDiscordQueue");

        public ILiteCollection<AtomicSale> AtomicSalesAnalysisQueue => this.GetDb().GetCollection<AtomicSale>("AtomicSalesAnalysisQueue");

        public ILiteCollection<WaxAccount> WaxAccounts => this.GetDb().GetCollection<WaxAccount>("WaxAccounts");

        public ILiteCollection<Setting> Settings => this.GetDb().GetCollection<Setting>("Settings");

        public LiteDatabase GetDb()
        {
            if (db == null)
            {
                lock (syncRoot)
                {
                    if (db == null)
                    {
                        db = new LiteDatabase(options.Path);

                        var sales = db.GetCollection<AtomicSale>("AtomicSales");
                        sales.EnsureIndex(x => x.CardId);

                        var settings = db.GetCollection<Setting>("Settings");
                        settings.EnsureIndex(x => x.Id, true);

                        var waxAcc = db.GetCollection<WaxAccount>("WaxAccounts");
                        waxAcc.EnsureIndex(x => x.IsActive);
                    }
                }
            }

            return db;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    db?.Dispose();
                    db = null;
                }

                disposedValue = true;
            }
        }
    }
}
