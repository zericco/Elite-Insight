using System;
using Elite.Insight.Annotations;

namespace Elite.Insight.Core.DomainModel.Trading
{
    public struct TradeRoute
    {
        public readonly string CommodityName;

        public readonly string OriginStationId;

        public readonly int BuyPrice;

        public readonly int Stock;

        public readonly int Supply;

        public readonly ProposalLevel? SupplyLevel;

        public readonly string TargetStationId;

        public readonly int SellPrice;

        public readonly int Demand;

        public readonly ProposalLevel? DemandLevel;

        public readonly DateTime Age;

        public readonly int Profit;

        public readonly double? Distance;

        public TradeRoute ([NotNull] MarketDataRow origin, [NotNull] MarketDataRow destination, double? distance = null)
        {
            if (origin == null) throw new ArgumentNullException("origin");
            if (destination == null) throw new ArgumentNullException("destination");
            if (String.Compare(origin.CommodityName, destination.CommodityName, StringComparison.InvariantCultureIgnoreCase) != 0)
                throw new ArgumentException("marketdata commodities must match");
            CommodityName = origin.CommodityName;
            OriginStationId = origin.StationFullName;
            TargetStationId = destination.StationFullName;
            Age = origin.SampleDate < destination.SampleDate ? origin.SampleDate : destination.SampleDate;
            Profit = destination.SellPrice - origin.BuyPrice;
            Distance = distance;
            Stock = origin.Supply;
            BuyPrice = origin.BuyPrice;
            Supply = origin.Supply;
            SupplyLevel = origin.SupplyLevel;
            Demand = destination.Demand;
            DemandLevel = destination.DemandLevel;
            SellPrice = destination.SellPrice;
        } 
    }
}