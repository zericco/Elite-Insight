#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 22.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using Newtonsoft.Json;

namespace RegulatedNoise.Core.DataProviders.Eddn
{
	public class EddbStation
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("system_id")]
		public int SystemId { get; set; }

		[JsonProperty("max_landing_pad_size")]
		public string MaxLandingPadSize { get; set; }

		[JsonProperty("distance_to_star")]
		public int? DistanceToStar { get; set; }

		[JsonProperty("faction")]
		public string Faction { get; set; }

		[JsonProperty("government")]
		public string Government { get; set; }

		[JsonProperty("allegiance")]
		public string Allegiance { get; set; }

		[JsonProperty("state")]
		public string State { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("has_blackmarket")]
		public int? HasBlackmarket { get; set; }

		[JsonProperty("has_commodities")]
		public int? HasCommodities { get; set; }

		[JsonProperty("has_refuel")]
		public int? HasRefuel { get; set; }

		[JsonProperty("has_repair")]
		public int? HasRepair { get; set; }

		[JsonProperty("has_rearm")]
		public int? HasRearm { get; set; }

		[JsonProperty("has_outfitting")]
		public int? HasOutfitting { get; set; }

		[JsonProperty("has_shipyard")]
		public int? HasShipyard { get; set; }

		[JsonProperty("import_commodities")]
		public string[] ImportCommodities { get; set; }

		[JsonProperty("export_commodities")]
		public string[] ExportCommodities { get; set; }

		[JsonProperty("prohibited_commodities")]
		public string[] ProhibitedCommodities { get; set; }

		[JsonProperty("economies")]
		public string[] Economies { get; set; }

		[JsonProperty("updated_at")]
		public int UpdatedAt { get; set; }

		[JsonProperty("listings")]
		public MarketData[] MarketDatas { get; set; }

		public class MarketData
		{
			[JsonProperty("station_id")]
			public int StationId { get; set; }

			[JsonProperty("commodity_id")]
			public int CommodityId { get; set; }

			[JsonProperty("supply")]
			public int Supply { get; set; }

			[JsonProperty("buy_price")]
			public int BuyPrice { get; set; }

			[JsonProperty("sell_price")]
			public int SellPrice { get; set; }

			[JsonProperty("demand")]
			public int Demand { get; set; }

			[JsonProperty("collected_at")]
			public long CollectedAt { get; set; }

			[JsonProperty("update_count")]
			public int UpdateCount { get; set; }
		}
	}
}