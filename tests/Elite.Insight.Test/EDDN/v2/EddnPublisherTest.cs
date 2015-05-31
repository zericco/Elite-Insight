#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 24.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Linq;
using Elite.Insight.Core.DomainModel;
using Elite.Insight.EDDN.v2;
using NUnit.Framework;

namespace Elite.Insight.Test.EDDN.v2
{
	[TestFixture]
	public class EddnPublisherTest
	{
		[Test]
		public void i_can_publish_market_item()
		{
			using (var publisher = new EddnPublisher())
			{
				publisher.TestMode = true;
				publisher.Publish(new MarketDataRow() { CommodityName = "aCommodity", BuyPrice = 1234, Demand = 2345, SellPrice = 5325, StationName = "aStation", Supply = 53535, SystemName = "aSystem", DemandLevel = ProposalLevel.Low, SupplyLevel = ProposalLevel.High, SampleDate = DateTime.Today });				
			}
		}

		[Test]
		public void i_can_publish_station_market()
		{
			using (var publisher = new EddnPublisher())
			{
				publisher.UseTestSchema = true;
				publisher.CurrentUser = "Zericco";
				publisher.CurrentVersion = "0.1";
				//publisher.TestMode = true;
				publisher.Publish(Enumerable.Range(1,30).Select(i =>
					new MarketDataRow()
					{
						CommodityName = "aCommodity" + (i % 10).ToString("00"),
						BuyPrice = i * 1500,
						Demand = i * 20000,
						SellPrice = i * 1700,
						StationName = "aStation" + (i / 10).ToString("00"),
						Supply = (i % 10) * 10000,
						SystemName = "aSystem",
						DemandLevel = ProposalLevel.Low,
						SupplyLevel = ProposalLevel.High,
						SampleDate = DateTime.Today.AddDays(i)
					}));
			}
		}
	}
}