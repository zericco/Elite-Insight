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

namespace Elite.Insight.EDDN.v1
{
	public class EddnPublisher : EddnPublisherBase
	{
		private const string POST_URL = "http://eddn-gateway.elite-markets.net:8080/upload/";
		private const string EDDN_SCHEMA_URL = "http://schemas.elite-markets.net/eddn/commodity/1";
		private const string EDDN_SCHEMA_TEST_URL = EDDN_SCHEMA_URL + "/test";

		public EddnPublisher() : base(POST_URL)
		{
		}

		protected override string ToMessage(MarketDataRow commodityData)
		{
			if (commodityData == null)
			{
				throw new ArgumentNullException("commodityData");
			}
			return new EddnMessage
			{
				Header = new EddnMessage.HeaderContent()
				{
					SoftwareName = SOFTWARE_NAME
					,SoftwareVersion = CurrentVersion
					,UploaderId = CurrentUser
				}
				,Message = new EddnMessage.MessageContent()
				{
					CommodityName = commodityData.CommodityName
					,BuyPrice = commodityData.BuyPrice > 0 ? (int?)commodityData.BuyPrice : null
					,Demand = commodityData.Demand
					,DemandLevel = commodityData.DemandLevel
					,SellPrice = commodityData.SellPrice
					,StationName = commodityData.StationName
					,Supply = commodityData.Stock
					,SupplyLevel = commodityData.SupplyLevel
					,SystemName = commodityData.SystemName
					,Timestamp = commodityData.SampleDate.ToUniversalTime()
				},
				SchemaRef = UseTestSchema ? EDDN_SCHEMA_TEST_URL : EDDN_SCHEMA_URL
			}.ToJson();
		}
	}
}