#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 22.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.Core.Helpers;
using RegulatedNoise.Core.Messaging;

namespace RegulatedNoise.DataProviders.Eddb
{
	public class EddbDataProvider
	{
		private readonly Dictionary<int, string> _systemIdToNameMap;
		private readonly Dictionary<int, string> _commodityNameMap;
		private const string EDDB_COMMODITIES_DATAFILE = @"./Data/commodities.json";
		private const string EDDB_STATIONS_LITE_DATAFILE = @"./Data/stations_lite.json";
		private const string EDDB_STATIONS_FULL_DATAFILE = @"./Data/stations.json";
		private const string EDDB_SYSTEMS_DATAFILE = @"./Data/systems.json";

		private const string EDDB_COMMODITIES_URL = @"http://eddb.io/archive/v3/commodities.json";
		private const string EDDB_SYSTEMS_URL = @"http://eddb.io/archive/v3/systems.json";
		private const string EDDB_STATIONS_LITE_URL = @"http://eddb.io/archive/v3/stations_lite.json";
		public const string SOURCENAME = "EDDB";
		private const string EDDB_STATIONS_FULL_URL = @"http://eddb.io/archive/v3/stations.json";

		public EddbDataProvider()
		{
			_systemIdToNameMap = new Dictionary<int, string>();
			_commodityNameMap = new Dictionary<int, string>();
		}

		public void ImportData(DataModel model)
		{
			DownloadDataFiles();
			ImportSystems(model.StarMap);
			ImportCommodities(model.Commodities);
			ImportStations(model.StarMap, model.GalacticMarket);
		}

		private void ImportCommodities(Commodities commodities)
		{
			List<EddbCommodity> eddbCommodities = SerializationHelpers.ReadJsonFromFile<List<EddbCommodity>>(new FileInfo(EDDB_COMMODITIES_DATAFILE));
			foreach (EddbCommodity commodity in eddbCommodities)
			{
				_commodityNameMap.Add(commodity.Id, commodity.Name);
				commodities.Update(ToCommodity(commodity));
			}
		}

		private Commodity ToCommodity(EddbCommodity eddbCommodity)
		{
			var commodity = new Commodity(eddbCommodity.Name)
			{
				AveragePrice = eddbCommodity.AveragePrice
				,Category = eddbCommodity.Category != null ? eddbCommodity.Category.Name : null
				,Source = SOURCENAME
			};
			return commodity;
		}

		private void ImportStations(StarMap starMap, GalacticMarket market)
		{
			if (File.Exists(EDDB_STATIONS_FULL_DATAFILE))
			{
				List<EddbStation> eddbStations = SerializationHelpers.ReadJsonFromFile<List<EddbStation>>(new FileInfo(EDDB_STATIONS_FULL_DATAFILE));
				foreach (EddbStation eddbStation in eddbStations)
				{
					starMap.Update(ToStation(eddbStation));
					ImportMarketData(eddbStation, market);
				}
			}
			else if (File.Exists(EDDB_STATIONS_LITE_DATAFILE))
			{
				List<EddbStation> eddbStations = SerializationHelpers.ReadJsonFromFile<List<EddbStation>>(new FileInfo(EDDB_STATIONS_FULL_DATAFILE));
				foreach (EddbStation eddbStation in eddbStations)
				{
					starMap.Update(ToStation(eddbStation));
				}
			}
		}

		private void ImportMarketData(EddbStation station, GalacticMarket market)
		{
			if (station.MarketDatas == null) return;
			foreach (EddbStation.MarketData marketData in station.MarketDatas)
			{
				market.Update(ToMarketData(marketData, station.Name, RetrieveSystemName(station.SystemId)));
			}
		}

		private MarketDataRow ToMarketData(EddbStation.MarketData marketData, string stationName, string systemName)
		{
			return new MarketDataRow()
			{
				CommodityName = RetrieveCommodityName(marketData.CommodityId)
				, BuyPrice = marketData.BuyPrice
				, Demand = marketData.Demand
				, SellPrice = marketData.SellPrice
				, StationName = stationName
				, Source = SOURCENAME
				, Stock = marketData.Supply
				, SystemName = systemName
				, SampleDate = UnixTimeStamp.ToDateTime(marketData.CollectedAt)
			};
		}

		private string RetrieveCommodityName(int commodityId)
		{
			return _commodityNameMap[commodityId];
		}

		private Station ToStation(EddbStation eddbStation)
		{
			Station station = new Station(eddbStation.Name.ToCleanTitleCase())
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

		private void ImportSystems(StarMap starMap)
		{
			List<EddbSystem> eddbSystems = SerializationHelpers.ReadJsonFromFile<List<EddbSystem>>(new FileInfo(EDDB_SYSTEMS_DATAFILE));
			foreach (EddbSystem system in (IEnumerable<EddbSystem>)eddbSystems)
			{
				_systemIdToNameMap.Add(system.Id, system.Name.ToUpperInvariant());
				starMap.Update(ToStarSystem(system));
			}
		}

		private static StarSystem ToStarSystem(EddbSystem eddbSystem)
		{
			var starSystem = new StarSystem(eddbSystem.Name.ToUpperInvariant())
			{
				Allegiance = eddbSystem.Allegiance
				 ,Faction = eddbSystem.Faction
				 ,Government = eddbSystem.Government
				 ,NeedsPermit = ToNBool(eddbSystem.NeedsPermit)
				 ,Population = eddbSystem.Population
				 ,PrimaryEconomy = eddbSystem.PrimaryEconomy
				 ,Security = eddbSystem.Security
				 ,Source = SOURCENAME
				 ,State = eddbSystem.State
				 ,UpdatedAt = eddbSystem.UpdatedAt
				 ,X = eddbSystem.X
				 ,Y = eddbSystem.Y
				 ,Z = eddbSystem.Z
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
			if (!File.Exists(EDDB_STATIONS_FULL_DATAFILE))
			{
				Task.Run(() => DownloadDataFile(new Uri(EDDB_STATIONS_FULL_URL), EDDB_STATIONS_FULL_DATAFILE, "eddb stations full data"));
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