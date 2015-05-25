using System;
using Elite.Insight.Core.Helpers;
using Newtonsoft.Json;

namespace Elite.Insight.Core.DomainModel
{
	public class Commodity : UpdatableEntity
	{
		private string _localizedName;
		private string _name;

		[JsonProperty("name")]
		public string Name
		{
			get { return _name; }
			set { _name = value.ToCleanTitleCase(); }
		}

		[JsonProperty("category")]
		public string Category { get; set; }

		[JsonProperty("average_price")]
		public int? AveragePrice { get; set; }

		[JsonProperty("demand_price_levels")]
		public WarningLevels DemandWarningLevels { get; private set; }

		[JsonProperty("supply_price_levels")]
		public WarningLevels SupplyWarningLevels { get; private set; }

		[JsonIgnore]
		public string LocalizedName
		{
			get
			{
				if (_localizedName == null)
				{
					return Name;
				}
				else
				{
					return _localizedName;
				}
			}
			set
			{
				_localizedName = value;
			}
		}

		private Commodity()
		{
			DemandWarningLevels = new WarningLevels();
			SupplyWarningLevels = new WarningLevels();
		}

		public Commodity(string name)
			:this()
		{
			Name = name;
		}

		public Commodity(Commodity source)
			:this()
		{
			UpdateFrom(source, UpdateMode.Clone);
		}

		public void UpdateFrom(Commodity sourceCommodity, UpdateMode updateMode)
		{
			bool doCopy = updateMode == UpdateMode.Clone || updateMode == UpdateMode.Copy;
			if (updateMode == UpdateMode.Clone)
			{
				Name = sourceCommodity.Name;
			}
			if (doCopy || String.IsNullOrEmpty(Category))
				Category = sourceCommodity.Category;
			if (doCopy || !AveragePrice.HasValue)
				AveragePrice = sourceCommodity.AveragePrice;
			if (doCopy || !DemandWarningLevels.Buy.Low.HasValue)
				DemandWarningLevels.Buy.Low = sourceCommodity.DemandWarningLevels.Buy.Low;
			if (doCopy || !DemandWarningLevels.Buy.High.HasValue)
				DemandWarningLevels.Buy.High = sourceCommodity.DemandWarningLevels.Buy.High;
			if (doCopy || !SupplyWarningLevels.Buy.Low.HasValue)
				SupplyWarningLevels.Buy.Low = sourceCommodity.SupplyWarningLevels.Buy.Low;
			if (doCopy || !SupplyWarningLevels.Buy.High.HasValue)
				SupplyWarningLevels.Buy.High = sourceCommodity.SupplyWarningLevels.Buy.High;
			base.UpdateFrom(sourceCommodity, updateMode);
		}

		public static bool AreEqual(Commodity lhs, Commodity rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (lhs == null || rhs == null) return false;
			return String.Compare(lhs.Name, rhs.Name, StringComparison.InvariantCultureIgnoreCase) == 0;
		}

		public override bool Equals(object obj)
		{
			return AreEqual(this, obj as Commodity);
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}

	public class WarningLevels
	{
		[JsonProperty("sell")]
		public PriceBounds Sell { get; private set; }

		[JsonProperty("buy")]
		public PriceBounds Buy { get; private set; }

		public WarningLevels()
		{
			Sell = new PriceBounds();
			Buy = new PriceBounds();
		}
	}

	public class PriceBounds
	{
		[JsonProperty("low")]
		public int? Low { get; set; }

		[JsonProperty("high")]
		public int? High { get; set; }

		public bool IsInRange(int price)
		{
			return ((Low.HasValue) && (price < Low))
						|| ((High.HasValue) && (price > High));
		}
	}
}