#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 16.05.2015
// ///
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System.Diagnostics;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.DataProviders;
using RegulatedNoise.DataProviders.Eddb;
using RegulatedNoise.EDDB_Data;

namespace RegulatedNoise
{
	public class MarketDataValidator: IValidator<MarketDataRow>
	{
		public PlausibilityState Validate(MarketDataRow marketData)
		{
			bool simpleEDDNCheck = marketData.Source == Eddn.SOURCENAME || marketData.Source == EddbDataProvider.SOURCENAME || marketData.Source == TradeDangerousDataProvider.SOURCENAME;

			Commodity commodityData = ApplicationContext.Model.Commodities.TryGet(marketData.CommodityName);

			if (marketData.CommodityName == "Panik")
				Debug.Print("STOP");

			PlausibilityState plausibility = new PlausibilityState(true);

			if (commodityData != null)
			{
				if (marketData.SupplyLevel.HasValue && marketData.DemandLevel.HasValue)
				{
					// demand AND supply !?
					plausibility = new PlausibilityState(false, "both demand and supply");
				}
				else if ((marketData.SellPrice <= 0) && (marketData.BuyPrice <= 0))
				{
					// both on 0 is not plausible
					plausibility = new PlausibilityState(false, "nor sell, nor buy price");
				}
				else if (marketData.SupplyLevel.HasValue || (simpleEDDNCheck && (marketData.Stock > 0)))
				{
					if (marketData.BuyPrice <= 0)
					{
						plausibility = new PlausibilityState(false, "buy price not provided when demand available");
					}
					// check supply data     
					else if (commodityData.SupplyWarningLevels.Sell.IsInRange(marketData.SellPrice))
					{
						// sell price is out of range
						plausibility = new PlausibilityState(false, "sell price out of supply prices warn level "
							 + marketData.SellPrice
							 + " [" + commodityData.SupplyWarningLevels.Sell.Low + "," + commodityData.SupplyWarningLevels.Sell.High + "]");
					}
					else if (commodityData.SupplyWarningLevels.Buy.IsInRange(marketData.BuyPrice))
					{
						// buy price is out of range
						plausibility = new PlausibilityState(false, "buy price out of supply prices warn level "
																				  + marketData.SellPrice
																				  + " [" +
																				  commodityData.SupplyWarningLevels.Buy.Low +
																				  "," +
																				  commodityData.SupplyWarningLevels.Buy.High +
																				  "]");
					}
					if (marketData.Stock <= 0)
					{
						// no supply quantity
						plausibility = new PlausibilityState(false, "supply not provided");
					}
				}
				else if (marketData.DemandLevel.HasValue || (simpleEDDNCheck && (marketData.Demand > 0)))
				{
					// check demand data
					if (marketData.SellPrice <= 0)
					{
						// at least the sell price must be present
						plausibility = new PlausibilityState(false, "sell price not provided when supply available");
					}
					else if (commodityData.DemandWarningLevels.Sell.IsInRange(marketData.SellPrice))
					{
						// buy price is out of range
						plausibility = new PlausibilityState(false, "sell price out of demand prices warn level "
																				  + marketData.SellPrice
																				  + " [" +
																				  commodityData.DemandWarningLevels.Sell.Low +
																				  "," +
																				  commodityData.DemandWarningLevels.Sell.High +
																				  "]");
					}
					else if (marketData.BuyPrice > 0 && commodityData.DemandWarningLevels.Buy.IsInRange(marketData.BuyPrice))
					{
						// buy price is out of range
						plausibility = new PlausibilityState(false, "buy price out of supply prices warn level "
																				  + marketData.BuyPrice
																				  + " [" +
																				  commodityData.DemandWarningLevels.Buy.Low +
																				  "," +
																				  commodityData.DemandWarningLevels.Buy.High +
																				  "]");
					}
					if (marketData.Demand <= 0)
					{
						// no demand quantity
						plausibility = new PlausibilityState(false, "demand not provided");
					}
				}
				else
				{
					// nothing ?!
					plausibility = new PlausibilityState(false, "nor demand,nor supply provided");
				}
			}
			else
			{
				plausibility = new PlausibilityState(false, "unknown commodity");
			}
			return plausibility;
		}
	}
}