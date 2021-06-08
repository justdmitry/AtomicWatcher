namespace AtomicWatcher.Data
{
    using System;
    using System.Linq;
    using LiteDB;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

#pragma warning disable CS0618 // Type or member is obsolete

    public class MigrationService
    {
        public void Migrate(IServiceProvider services)
        {
            Migrate20210609(services);
        }

        private static void Migrate20210609(IServiceProvider services)
        {
            var db = services.GetRequiredService<IDbProvider>();
            var logger = services.GetRequiredService<ILogger<MigrationService>>();

            var accounts = db.WaxAccounts.FindAll().ToList();

            foreach (var acc in accounts)
            {
                var updated = false;

                if (acc.NotifyLowerMints == true)
                {
                    db.WatchRules.Insert(new WatchRule
                    {
                        WaxAccountId = acc.Id,
                        LowerMints = true,
                    });
                    acc.NotifyLowerMints = null;
                    updated = true;
                }

                if (acc.NotifyNonOwned == true)
                {
                    db.WatchRules.Insert(new WatchRule
                    {
                        WaxAccountId = acc.Id,
                        Absent = true,
                    });
                    acc.NotifyNonOwned = null;
                    updated = true;
                }

                if (updated)
                {
                    db.WaxAccounts.Update(acc);
                    logger.LogInformation($"Account upgraded: {acc.Id}");
                }
            }
        }
    }
}
