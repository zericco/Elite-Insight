#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 22.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using Newtonsoft.Json;

namespace RegulatedNoise.DataProviders.Eddb
{
	public class EddbSystem
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("x")]
		public double X { get; set; }

		[JsonProperty("y")]
		public double Y { get; set; }

		[JsonProperty("z")]
		public double Z { get; set; }

		[JsonProperty("faction")]
		public string Faction { get; set; }

		[JsonProperty("population")]
		public long? Population { get; set; }

		[JsonProperty("government")]
		public string Government { get; set; }

		[JsonProperty("allegiance")]
		public string Allegiance { get; set; }

		[JsonProperty("state")]
		public string State { get; set; }

		[JsonProperty("security")]
		public string Security { get; set; }

		[JsonProperty("primary_economy")]
		public string PrimaryEconomy { get; set; }

		[JsonProperty("needs_permit")]
		public int? NeedsPermit { get; set; }

		[JsonProperty("updated_at")]
		public int UpdatedAt { get; set; }		 
	}
}