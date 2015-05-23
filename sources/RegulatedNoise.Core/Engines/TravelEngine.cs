#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 19.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using RegulatedNoise.Core.DomainModel;

namespace RegulatedNoise.Core.Engines
{
	public static class TravelEngine
	{
		private static readonly Dictionary<string, double> _systemDistances;

		static TravelEngine()
		{
			
			_systemDistances = new Dictionary<string, double>();
		}

		public static double? DistanceInLightYears(this StarSystem system1, StarSystem system2)
		{
			double readDistance;
			double? distance;
			string systemPairKey = GetSystemDistanceKey(system1.Name, system2.Name);
			if (!_systemDistances.TryGetValue(systemPairKey, out readDistance))
			{
				distance = SystemDistance(system1, system2);
				if (distance.HasValue)
				{
					_systemDistances.Add(systemPairKey, distance.Value);
				}
			}
			else
			{
				distance = readDistance;
			}
			return distance;
		}

		private static string GetSystemDistanceKey(string system1, string system2)
		{
			if (String.IsNullOrWhiteSpace(system1)) throw new ArgumentException("system1");
			if (String.IsNullOrWhiteSpace(system2)) throw new ArgumentException("system2");
			return String.Compare(system1, system2, StringComparison.InvariantCultureIgnoreCase) > 0 ? system2.Trim().ToUpper() + "_" + system1.Trim().ToUpper() : system1.Trim().ToUpper() + "_" + system2.Trim().ToUpper();
		}

		private static double? SystemDistance(StarSystem currentSystemLocation, StarSystem remoteSystemLocation)
		{
			if (currentSystemLocation == null || remoteSystemLocation == null) return null;
			double xDelta = currentSystemLocation.X - remoteSystemLocation.X;
			double yDelta = currentSystemLocation.Y - remoteSystemLocation.Y;
			double zDelta = currentSystemLocation.Z - remoteSystemLocation.Z;
			return Math.Sqrt(Math.Pow(xDelta, 2) + Math.Pow(yDelta, 2) + Math.Pow(zDelta, 2));
		}		 
	}
}