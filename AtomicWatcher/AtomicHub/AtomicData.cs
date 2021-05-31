namespace AtomicWatcher.AtomicHub
{
    using System.Collections.Generic;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable IDE1006 // Naming Styles

    public class AssetsRootObject
    {
        public bool success { get; set; }
        public List<Asset> data { get; set; }
    }

    public class SalesRootObject
    {
        public bool success { get; set; }
        public List<SaleData> data { get; set; }
    }

    public class TemplatesRootObject
    {
        public bool success { get; set; }
        public List<AssetTemplate> data { get; set; }
    }

    public class SaleData
    {
        public string sale_id { get; set; }
        public string seller { get; set; }
        public string created_at_time { get; set; }
        public Price price { get; set; }
        public Asset[] assets { get; set; }
    }

    public class Price
    {
        public int token_precision { get; set; }
        public string amount { get; set; }
    }

    public class Asset
    {
        public string asset_id { get; set; }
        public AssetData data { get; set; }
        public AssetTemplate template { get; set; }
        public string template_mint { get; set; }
        public string owner { get; set; }
    }

    public class AssetData
    {
        public string card_id { get; set; }
        public string name { get; set; }
        public string rarity { get; set; }
    }

    public class AssetTemplate
    {
        public AssetData immutable_data { get; set; }
        public string issued_supply { get; set; }
        public string max_supply { get; set; }
        public string template_id { get; set; }
    }
}
