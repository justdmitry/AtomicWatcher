namespace AtomicWatcher.AtomicHub
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IAtomicService
    {
        Task<List<SaleData>> GetNewSales(long lastId);

        Task<List<Asset>> GetAccountAssets(string account);

        Task<List<AssetTemplate>> GetTemplates();
    }
}
