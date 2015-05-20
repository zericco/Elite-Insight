using System.Collections.ObjectModel;
using RegulatedNoise.Core.Helpers;

namespace RegulatedNoise.Core.DomainModel
{
	internal class StationCollection : KeyedCollection<string, Station>
	{
		private readonly object _updating = new object();

		protected override string GetKeyForItem(Station item)
		{
			return item.Name.ToCleanTitleCase();
		}

		public void UpdateFrom(Station station)
		{
			lock (_updating)
			{
				Station existingStation;
				if (Dictionary == null || !Dictionary.TryGetValue(GetKeyForItem(station), out existingStation))
				{
					Add(station);
				}
				else
				{
					existingStation.UpdateFrom(station, UpdateMode.Update);
				}
			}
		}

		public bool TryGetValue(string stationName, out Station station)
		{
			if (Dictionary != null && Dictionary.TryGetValue(stationName, out station))
			{
				return true;
			}
			else
			{
				station = null;
				return false;
			}
		}
	}
}