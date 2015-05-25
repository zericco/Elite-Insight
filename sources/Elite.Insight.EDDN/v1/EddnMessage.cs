#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 22.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using Elite.Insight.Core.DomainModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Elite.Insight.EDDN.v1
{
	public class EddnMessage
	{
		public static EddnMessage ReadJson(string json)
		{
			var eddnMessage = JsonConvert.DeserializeObject<EddnMessage>(json);
			eddnMessage.RawText = json;
			return eddnMessage;
		}

		[JsonProperty(PropertyName = "header")]
		public HeaderContent Header { get; set; }
		[JsonProperty(PropertyName = "$schemaRef")]
		public string SchemaRef { get; set; }
		[JsonProperty(PropertyName = "message")]
		public MessageContent Message { get; set; }
		[JsonIgnore]
		public string RawText { get; set; }

		[JsonIgnore]
		public bool IsTest
		{
			get { return SchemaRef != null && SchemaRef.Contains("Test"); }
		}

		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}

		public class HeaderContent
		{
			[JsonProperty(PropertyName = "softwareVersion")]
			public string SoftwareVersion { get; set; }
			[JsonProperty(PropertyName = "softwareName")]
			public string SoftwareName { get; set; }
			[JsonProperty(PropertyName = "uploaderID")]
			public string UploaderId { get; set; }

			public override string ToString()
			{
				return SoftwareName + "v" + SoftwareVersion + " by " + UploaderId;
			}
		}

		public class MessageContent
		{
			[JsonProperty(PropertyName = "systemName")]
			public string SystemName { get; set; }
			[JsonProperty(PropertyName = "stationName")]
			public string StationName { get; set; }
			[JsonProperty(PropertyName = "timestamp")]
			public DateTime Timestamp { get; set; }
			[JsonProperty(PropertyName = "itemName")]
			public string CommodityName { get; set; }
			[JsonProperty(PropertyName = "buyPrice", NullValueHandling = NullValueHandling.Ignore)]
			public int? BuyPrice { get; set; }
			[JsonProperty(PropertyName = "stationStock")]
			public int Supply { get; set; }
			[JsonProperty(PropertyName = "supplyLevel", NullValueHandling = NullValueHandling.Ignore)]
			[JsonConverter(typeof(StringEnumConverter))]
			public ProposalLevel? SupplyLevel { get; set; }
			[JsonProperty(PropertyName = "sellPrice")]
			public int SellPrice { get; set; }
			[JsonProperty(PropertyName = "demand")]
			public int Demand { get; set; }
			[JsonProperty(PropertyName = "demandLevel", NullValueHandling = NullValueHandling.Ignore)]
			[JsonConverter(typeof(StringEnumConverter))]
			public ProposalLevel? DemandLevel { get; set; }
			[JsonIgnore]
			public string Source { get { return "EDDN"; } }

			public override string ToString()
			{
				return CommodityName + " [" + StationName + "] " + BuyPrice + "/" + SellPrice;
			}
		}
	}
}