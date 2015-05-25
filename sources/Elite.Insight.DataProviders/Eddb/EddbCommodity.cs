#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 22.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using Newtonsoft.Json;

namespace Elite.Insight.DataProviders.Eddb
{
	public class EddbCommodity
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("average_price")]
		public int? AveragePrice { get; set; }

		[JsonProperty("category")]
		public EddbCategory Category { get; set; }

		public class EddbCategory
		{
			[JsonProperty("name")]
			public string Name { get; set; }
		}
	}
}