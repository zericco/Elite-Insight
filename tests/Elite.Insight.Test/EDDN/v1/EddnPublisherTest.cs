#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 24.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using Elite.Insight.Core.DomainModel;
using Elite.Insight.EDDN.v1;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Elite.Insight.Test.EDDN.v1
{
	[TestClass]
	public class EddnPublisherTest
	{
		[TestMethod]
		public void i_can_send_message_to_eddn()
		{
			using (var publisher = new EddnPublisher())
			{
				publisher.TestMode = true;
				publisher.Publish(new MarketDataRow() { CommodityName = "aCommodity", BuyPrice = 1234, Demand = 2345, SellPrice = 5325, StationName = "aStation", Supply = 53535, SystemName = "aSystem", DemandLevel = ProposalLevel.Low, SupplyLevel = ProposalLevel.High, SampleDate = DateTime.Today});
			}
		}
		 
	}
}