using System;
using System.Collections;
using System.Collections.Generic;
using RegulatedNoise.Annotations;
using RegulatedNoise.Core.Helpers;

namespace RegulatedNoise.Core.DomainModel
{
	public class StarMap : IReadOnlyCollection<StarSystem>
	{
		private readonly object _updating = new object();

		private readonly SystemCollection _systems;

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

		public int StationsCount { get { throw new NotImplementedException(); } }

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

		public IReadOnlyCollection<StarSystem> FindSystem(string text)
		{
			throw new NotImplementedException();
		}

		public Station GetStation(string stationName)
		{
			throw new NotImplementedException();
		}

		public IReadOnlyCollection<Station> FindStation(string text)
		{
			throw new NotImplementedException();
		}

		public double DistanceInLightYears(string system1, string system2)
		{
			throw new NotImplementedException();
		}

		public int? GetStationDistance(string systemName, string stationName)
		{
			throw new NotImplementedException();
		}
	}
}