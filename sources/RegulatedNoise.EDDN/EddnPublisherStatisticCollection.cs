using System.Collections.ObjectModel;

namespace RegulatedNoise.EDDN
{
	public class EddnPublisherStatisticCollection : KeyedCollection<string, EddnPublisherVersionStats>
	{
		protected override string GetKeyForItem(EddnPublisherVersionStats item)
		{
			return item.Publisher;
		}

		public bool TryGetValue(string publisher, out EddnPublisherVersionStats stats)
		{
			if (Dictionary != null)
			{
				return Dictionary.TryGetValue(publisher, out stats);
			}
			else
			{
				stats = null;
				return false;
			}
		}

		public void UpdateStats(object sender, v1.EddnMessageEventArgs e)
		{
			var nameAndVersion = (e.Message.Header.SoftwareName + " / " + e.Message.Header.SoftwareVersion);
			UpdateStats(nameAndVersion);
		}

		public void UpdateStats(object sender, v2.EddnMessageEventArgs e)
		{
			var nameAndVersion = (e.Message.Header.SoftwareName + " / " + e.Message.Header.SoftwareVersion);
			UpdateStats(nameAndVersion);
		}

		protected void UpdateStats(string nameAndVersion)
		{
			EddnPublisherVersionStats stats;
			if (!TryGetValue(nameAndVersion, out stats))
			{
				stats = new EddnPublisherVersionStats(nameAndVersion);
				Add(stats);
			}
			++stats.MessagesReceived;
		}
	}
}