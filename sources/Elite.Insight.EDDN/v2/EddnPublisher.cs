#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 24.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Elite.Insight.Core.DomainModel;

namespace Elite.Insight.EDDN.v2
{
	public class EddnPublisher: EddnPublisherBase
	{
		private const string POST_URL = "http://eddn-gateway.elite-markets.net:8080/upload/";
		private const string EDDN_SCHEMA_URL = "http://schemas.elite-markets.net/eddn/commodity/2";
		private const string EDDN_SCHEMA_TEST_URL = EDDN_SCHEMA_URL + "/test";

		public EddnPublisher()
			: base(POST_URL)
		{
		}

		public void Publish(IEnumerable<MarketDataRow> marketDataRows)
		{
			if (marketDataRows == null)
			{
				throw new ArgumentNullException("marketDataRows");
			}
			bool any = false;
			foreach (MarketDataRow marketDataRow in marketDataRows)
			{
				if (marketDataRow == null) throw new ArgumentException("trying to send null marketdata");
				any = true;
			}
			if (!any)
				return;

			foreach (string message in marketDataRows.GroupBy(md => md.StationName).Select(ToMessage))
			{
				EnqueueMessage(message);
			}
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
				, Message = new EddnMessage.MessageContent()
				{
					Commodities = new []{
							new EddnMessage.Commodity()
							{
								CommodityName = commodityData.CommodityName
								,BuyPrice = commodityData.BuyPrice
								,Demand = commodityData.Demand
								,DemandLevel = commodityData.DemandLevel
								,SellPrice = commodityData.SellPrice
								,Supply = commodityData.Stock
								,SupplyLevel = commodityData.SupplyLevel
							}
						}
					,StationName = commodityData.StationName
					,SystemName = commodityData.SystemName
					,Timestamp = commodityData.SampleDate.ToUniversalTime()
				},
				SchemaRef = UseTestSchema ? EDDN_SCHEMA_TEST_URL : EDDN_SCHEMA_URL
			}.ToJson();
		}

		protected string ToMessage(IEnumerable<MarketDataRow> stationMarket)
		{
			if (stationMarket == null)
			{
				throw new ArgumentNullException("stationMarket");
			}
			var marketData = stationMarket.FirstOrDefault();
			if (marketData == null) throw new ArgumentException("no market data available");
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
					Commodities = stationMarket.Select( md => 
							new EddnMessage.Commodity()
							{
								CommodityName = md.CommodityName
								,BuyPrice = md.BuyPrice
								,Demand = md.Demand
								,DemandLevel = md.DemandLevel
								,SellPrice = md.SellPrice
								,Supply = md.Stock
								,SupplyLevel = md.SupplyLevel
							}).ToArray()
					,StationName = marketData.StationName
					,SystemName = marketData.SystemName
					,Timestamp = stationMarket.Max(md => md.SampleDate).ToUniversalTime()
				}
				,SchemaRef = UseTestSchema ? EDDN_SCHEMA_TEST_URL : EDDN_SCHEMA_URL
			}.ToJson();
		}
	}
}