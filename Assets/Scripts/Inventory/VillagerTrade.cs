namespace CraftSharp.Inventory
{
    /// <summary>
    /// Represents a trade of a villager
    /// </summary>
    public class VillagerTrade
    {
        public ItemStack InputItem1;
        public ItemStack OutputItem;
        public ItemStack InputItem2;
        public bool TradeDisabled;
        public int NumberOfTradeUses;
        public int MaximumNumberOfTradeUses;
        public int Xp;
        public int SpecialPrice;
        public float PriceMultiplier;
        public int Demand;

        public VillagerTrade(ItemStack inputItem1, ItemStack outputItem, ItemStack inputItem2, bool tradeDisabled, int numberOfTradeUses, int maximumNumberOfTradeUses, int xp, int specialPrice, float priceMultiplier, int demand)
        {
            this.InputItem1 = inputItem1;
            this.OutputItem = outputItem;
            this.InputItem2 = inputItem2;
            this.TradeDisabled = tradeDisabled;
            this.NumberOfTradeUses = numberOfTradeUses;
            this.MaximumNumberOfTradeUses = maximumNumberOfTradeUses;
            this.Xp = xp;
            this.SpecialPrice = specialPrice;
            this.PriceMultiplier = priceMultiplier;
            this.Demand = demand;
        }
    }
}
