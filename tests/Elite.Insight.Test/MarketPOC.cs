#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 24.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elite.Insight.Core.DomainModel;
using NUnit.Framework;

namespace Elite.Insight.Test
{
	[TestFixture]
	public class MarketPOCTest
	{
		[Test]
		public void i_can_retrieve_by_commodity()
		{
			var market = new MarketPOC();
			foreach (MarketDataRow marketDataRow in Enumerable.Range(1,100).Select(NewMarketData))
			{
				market.Add(marketDataRow);
			}
			IEnumerable<MarketDataRow> commodityMarket = market.GetCommodityMarket("aCommodity");
			IEnumerable<MarketDataRow> stationMarket = market.GetStationMarket("aStation");
		}

		private MarketDataRow NewMarketData(int seed)
		{
			return new MarketDataRow()
			{
				CommodityName = "aCommodity" + (seed%10).ToString("00"),
				BuyPrice = seed*1500,
				Demand = seed*20000,
				SellPrice = seed*1700,
				StationName = "aStation" + (seed/10).ToString("00"),
				Stock = (seed%10)*10000,
				SystemName = "aSystem",
				DemandLevel = ProposalLevel.Low,
				SupplyLevel = ProposalLevel.High,
				SampleDate = DateTime.Today.AddDays(seed)
			};
		}
	}

	internal class MarketPOC: IReadOnlyCollection<MarketDataRow>
	{
		public int Count
		{
			get { throw new System.NotImplementedException(); }
		}

		public MarketPOC()
		{
			
		}

		public MarketPOC(IEnumerable<MarketDataRow> marketDataRows)
		{
			
		}

		public void Add(MarketDataRow marketDataRow)
		{
			throw new System.NotImplementedException();
		}

		public IEnumerator<MarketDataRow> GetEnumerator()
		{
			throw new System.NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerable<MarketDataRow> GetCommodityMarket(string commodity)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<MarketDataRow> GetStationMarket(string station)
		{
			throw new NotImplementedException();
		}
	}
}