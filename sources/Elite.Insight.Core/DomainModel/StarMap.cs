using System;
using System.Collections;
using System.Collections.Generic;
using Elite.Insight.Core.Engines;
using Elite.Insight.Core.Helpers;
using Elite.Insight.Annotations;

namespace Elite.Insight.Core.DomainModel
{
	public class StarMap : IReadOnlyCollection<StarSystem>
	{
		private readonly object _updating = new object();

		private readonly SystemCollection _systems;

		private readonly StationCollection _stations;

		public IEnumerator<StarSystem> GetEnumerator()
		{
			return _systems.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Count
		{
			get { return _systems.Count; }
		}

		public int StationsCount { get { return _stations.Count; } }

		public void Remove(Station station)
		{
			throw new NotImplementedException();
		}

		public void Remove(StarSystem starSystem)
		{
			throw new NotImplementedException();
		}

		public StarMap()
		{
			_systems = new SystemCollection();
			_stations = new StationCollection();
		}

		public void UpdateRange([NotNull] IEnumerable<StarSystem> systems)
		{
			if (systems == null) throw new ArgumentNullException("systems");
			foreach (StarSystem system in systems)
			{
				Update(system);
			}
		}

		public void Update(Station station)
		{
			lock (_updating)
			{
				StarSystem existingSystem;
				if (!_systems.TryGetValue(station.System, out existingSystem))
				{
					existingSystem = new StarSystem(station.System);
					_systems.Add(existingSystem);
				}
				existingSystem.UpdateStations(station);
				_stations.UpdateFrom(station);
			}
		}

		public void Update(StarSystem system)
		{
			lock (_updating)
			{
				StarSystem existingSystem;
				if (!_systems.TryGetValue(system.Name, out existingSystem))
				{
					_systems.Add(system);
				}
				else
				{
					existingSystem.UpdateFrom(system, UpdateMode.Update);
				}
			}
		}

		public StarSystem GetSystem(string systemName)
		{
			return _systems[systemName.ToCleanUpperCase()];
		}

		public StarSystem TryGetSystem(string systemName)
		{
			StarSystem system;
			_systems.TryGetValue(systemName.ToCleanUpperCase(), out system);
			return system;
		}

		public IReadOnlyCollection<StarSystem> FindSystem(string text)
		{
			return _systems.LevenFilter(text, s => s.Name);
		}

		public Station GetStation(string stationName)
		{
			return _stations[stationName.ToCleanTitleCase()];
		}

		public Station TryGetStation(string stationName)
		{
			Station station;
			_stations.TryGetValue(stationName.ToCleanTitleCase(), out station);
			return station;
		}

		public IReadOnlyCollection<Station> FindStation(string text)
		{
			return _stations.LevenFilter(text, s => s.Name);
		}

		public double? DistanceInLightYears(string system1, string system2)
		{
			StarSystem starSystem1 = TryGetSystem(system1);
			if (starSystem1 == null)
				return null;
			StarSystem starSystem2 = TryGetSystem(system2);
			if (starSystem2 == null)
				return null;
			return starSystem1.DistanceInLightYears(starSystem2);
		}

		public int? GetStationDistance(string stationName)
		{
			var station = TryGetStation(stationName);
			return station == null ? null : station.DistanceToStar;
		}
	}
}