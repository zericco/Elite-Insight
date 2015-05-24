#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 24.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.EDDN.v1;

namespace RegulatedNoise.Test.EDDN
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
				publisher.Publish(new MarketDataRow() { CommodityName = "aCommodity", BuyPrice = 1234, Demand = 2345, SellPrice = 5325, StationName = "aStation", Stock = 53535, SystemName = "aSystem", DemandLevel = ProposalLevel.Low, SupplyLevel = ProposalLevel.High, SampleDate = DateTime.Today});
			}
		}
		 
	}
}