using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RegulatedNoise.Core;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.Core.Helpers;
using RegulatedNoise.Enums_and_Utility_Classes;

namespace RegulatedNoise.EDDB_Data
{
	internal class EddbDataProvider
	{
		private readonly Dictionary<int,string> _systemIdToNameMap;
		private const string EDDB_COMMODITIES_DATAFILE = @"./Data/commodities.json";
		private const string EDDB_STATIONS_LITE_DATAFILE = @"./Data/stations_lite.json";
		private const string EDDB_STATIONS_FULL_DATAFILE = @"./Data/stations.json";
		private const string EDDB_SYSTEMS_DATAFILE = @"./Data/systems.json";

		private const string EDDB_COMMODITIES_URL = @"http://eddb.io/archive/v3/commodities.json";
		private const string EDDB_SYSTEMS_URL = @"http://eddb.io/archive/v3/systems.json";
		private const string EDDB_STATIONS_LITE_URL = @"http://eddb.io/archive/v3/stations_lite.json";
		public const string SOURCENAME = "EDDB";
		//private const string EDDB_STATIONS_FULL_URL = @"http://eddb.io/archive/v3/stations.json";

		public EddbDataProvider()
		{
			_systemIdToNameMap = new Dictionary<int, string>();
		}

		public void ImportData(DataModel model)
		{
			DownloadDataFiles();
			ImportSystems(model.StarMap);
			ImportCommodities(model.Commodities);
			ImportStations(model.StarMap);
		}

		private void ImportCommodities(Commodities commodities)
		{
			List<EDCommodities> eddbCommodities = SerializationHelpers.ReadJsonFromFile<List<EDCommodities>>(new FileInfo(EDDB_COMMODITIES_DATAFILE));
			foreach (EDCommodities commodity in eddbCommodities)
			{
				commodities.Update(ToCommodity(commodity));
			}
		}

		private Commodity ToCommodity(EDCommodities eddbCommodity)
		{
			var commodity = new Commodity(eddbCommodity.Name)
			{
				AveragePrice = eddbCommodity.AveragePrice
				, Category = eddbCommodity.Category != null ? eddbCommodity.Category.Name: null
				, Source = SOURCENAME
			};
			return commodity;
		}

		private void ImportStations(StarMap starMap)
		{
			if (File.Exists(EDDB_STATIONS_FULL_DATAFILE))
			{
				List<EDStation> eddbStations = SerializationHelpers.ReadJsonFromFile<List<EDStation>>(new FileInfo(EDDB_STATIONS_FULL_DATAFILE));
				foreach (EDStation eddbStation in eddbStations)
				{
					starMap.Update(ToStation(eddbStation));
				}
			}
			else if (File.Exists(EDDB_STATIONS_LITE_DATAFILE))
			{
				//imports only stations
			}
		}

		private Station ToStation(EDStation eddbStation)
		{
			Station station = new Station(Extensions_StringNullable.ToCleanTitleCase(eddbStation.Name))
			{
				Allegiance = eddbStation.Allegiance
				,DistanceToStar = eddbStation.DistanceToStar
				,Economies = eddbStation.Economies
				,ExportCommodities = eddbStation.ExportCommodities
				,Faction = eddbStation.Faction
				,Government = eddbStation.Government
				,HasBlackmarket = ToNBool(eddbStation.HasBlackmarket)
				,HasCommodities = ToNBool(eddbStation.HasCommodities)
				,HasOutfitting = ToNBool(eddbStation.HasOutfitting)
				,HasRearm = ToNBool(eddbStation.HasRearm)
				,HasRepair = ToNBool(eddbStation.HasRepair)
				,HasRefuel = ToNBool(eddbStation.HasRefuel)
				,HasShipyard = ToNBool(eddbStation.HasShipyard)
				,ImportCommodities = eddbStation.ImportCommodities
				,MaxLandingPadSize = ParseLandingPadSize(eddbStation.MaxLandingPadSize)
				,ProhibitedCommodities = eddbStation.ProhibitedCommodities
				,Source = SOURCENAME
				,State = eddbStation.State
				,System = RetrieveSystemName(eddbStation.SystemId)
				,Type = eddbStation.Type
				,UpdatedAt = eddbStation.UpdatedAt
			};
			return station;
		}

		private string RetrieveSystemName(int systemId)
		{
			return _systemIdToNameMap[systemId];
		}

		private static LandingPadSize? ParseLandingPadSize(string maxLandingPadSize)
		{
			LandingPadSize size;
			if (Enum.TryParse(maxLandingPadSize, true, out size))
			{
				return size;
			}
			else
			{
				return null;
			}
		}

		private void ImportSystems(StarMap starMap)
		{
			List<EDSystem> eddbSystems = SerializationHelpers.ReadJsonFromFile<List<EDSystem>>(new FileInfo(EDDB_SYSTEMS_DATAFILE));
			foreach (EDSystem system in (IEnumerable<EDSystem>)eddbSystems)
			{
				_systemIdToNameMap.Add(system.Id, Extensions_StringNullable.ToCleanTitleCase(system.Name));
				starMap.Update(ToStarSystem(system));
			}
		}

		private static StarSystem ToStarSystem(EDSystem eddbSystem)
		{
			var starSystem = new StarSystem(eddbSystem.Name)
			{
				Allegiance = eddbSystem.Allegiance
				 ,
				Faction = eddbSystem.Faction
				 ,
				Government = eddbSystem.Government
				 ,
				NeedsPermit = ToNBool(eddbSystem.NeedsPermit)
				 ,
				Population = eddbSystem.Population
				 ,
				PrimaryEconomy = eddbSystem.PrimaryEconomy
				 ,
				Security = eddbSystem.Security
				 ,
				Source = SOURCENAME
				 ,
				State = eddbSystem.State
				 ,
				UpdatedAt = eddbSystem.UpdatedAt
				 ,
				X = eddbSystem.X
				 ,
				Y = eddbSystem.Y
				 ,
				Z = eddbSystem.Z
			};
			return starSystem;
		}

		private static bool? ToNBool(int? needsPermit)
		{
			if (needsPermit.HasValue)
			{
				if (needsPermit == 0)
				{
					return false;
				}
				else if (needsPermit == 1)
				{
					return true;
				}
				else
				{
					throw new NotSupportedException(needsPermit + ": unable to convert from int to bool");
				}
			}
			else
			{
				return null;
			}
		}

		private static void DownloadDataFiles()
		{
			var tasks = new List<Task>();
			if (!File.Exists(EDDB_COMMODITIES_DATAFILE))
			{
				tasks.Add(Task.Run(() => DownloadDataFile(new Uri(EDDB_COMMODITIES_URL), EDDB_COMMODITIES_DATAFILE,
					 "eddb commodities data")));
			}
			if (!File.Exists(EDDB_SYSTEMS_DATAFILE))
			{
				tasks.Add(Task.Run(() => DownloadDataFile(new Uri(EDDB_SYSTEMS_URL), EDDB_SYSTEMS_DATAFILE,
					 "eddb stations lite data")));
			}
			if (!File.Exists(EDDB_STATIONS_FULL_DATAFILE) && !File.Exists(EDDB_STATIONS_LITE_DATAFILE))
			{
				tasks.Add(Task.Run(() => DownloadDataFile(new Uri(EDDB_STATIONS_LITE_URL), EDDB_STATIONS_LITE_DATAFILE,
					 "eddb stations lite data")));
			}
			if (tasks.Any())
			{
				while (!Task.WaitAll(tasks.ToArray(), TimeSpan.FromMinutes(5)) && EventBus.Request("eddb server not responding, still waiting?"))
				{
				}
			}
		}

		private static void DownloadDataFile(Uri address, string filepath, string contentDescription)
		{
			EventBus.InitializationProgress("trying to download " + contentDescription + "...");
			using (var webClient = new WebClient())
			{
				webClient.DownloadFile(address, filepath);
			}
			EventBus.InitializationProgress("..." + contentDescription + " download completed");
		}
	}
}