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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Elite.Insight.Core.DomainModel;
using Elite.Insight.Core.Helpers;
using Elite.Insight.Core.Messaging;

namespace Elite.Insight.DataProviders.Eddb
{
	public class EddbDataProvider
	{
		private readonly Dictionary<int, string> _systemIdToNameMap;
		private readonly Dictionary<int, string> _commodityNameMap;
		public const string EDDB_COMMODITIES_DATAFILE = @"./Data/commodities.json";
		public const string EDDB_STATIONS_LITE_DATAFILE = @"./Data/stations_lite.json";
		public const string EDDB_STATIONS_FULL_DATAFILE = @"./Data/stations.json";
		public const string EDDB_SYSTEMS_DATAFILE = @"./Data/systems.json";

		public const string EDDB_COMMODITIES_URL = @"http://eddb.io/archive/v3/commodities.json";
		public const string EDDB_SYSTEMS_URL = @"http://eddb.io/archive/v3/systems.json";
		public const string EDDB_STATIONS_LITE_URL = @"http://eddb.io/archive/v3/stations_lite.json";
		public const string SOURCENAME = "EDDB";
		private const string EDDB_STATIONS_FULL_URL = @"http://eddb.io/archive/v3/stations.json";

		public ImportMode ImportMode { get; protected set; }

		public EddbDataProvider()
		{
			_systemIdToNameMap = new Dictionary<int, string>();
			_commodityNameMap = new Dictionary<int, string>();
		}

		public void ImportData(DataModel model, bool importMarketData, ImportMode importMode)
		{
			if (model == null)
			{
				throw new ArgumentNullException("model");
			}
			ImportMode = importMode;
			DownloadDataFiles();
			ImportSystems(model.StarMap);
			ImportCommodities(model.Commodities);
			if (importMarketData)
			{
				ImportStations(model.StarMap, model.GalacticMarket);
			}
			else
			{
				ImportStations(model.StarMap);				
			}
		}

		internal void ImportCommodities(Commodities commodities)
		{
			List<EddbCommodity> eddbCommodities = SerializationHelpers.ReadJsonFromFile<List<EddbCommodity>>(new FileInfo(EDDB_COMMODITIES_DATAFILE));
			int count = 0;
			int correlationId = EventBus.Start("importing commodities...", eddbCommodities.Count);
			foreach (EddbCommodity commodity in eddbCommodities)
			{
				_commodityNameMap.Add(commodity.Id, commodity.Name);
				commodities.Update(ToCommodity(commodity));
				++count;
			}
			EventBus.Completed("commodities imported", correlationId);
		}

		private Commodity ToCommodity(EddbCommodity eddbCommodity)
		{
			var commodity = new Commodity(eddbCommodity.Name)
			{
				AveragePrice = eddbCommodity.AveragePrice
				,
				Category = eddbCommodity.Category != null ? eddbCommodity.Category.Name : null
				,
				Source = SOURCENAME
			};
			return commodity;
		}

		internal void ImportStations(StarMap starMap, GalacticMarket market = null)
		{
			int count = 0;
			if (File.Exists(EDDB_STATIONS_FULL_DATAFILE) && market != null)
			{
				EventBus.Progress("parsing stations data file...", 0, 1);
				List<EddbStation> eddbStations = SerializationHelpers.ReadJsonFromFile<List<EddbStation>>(new FileInfo(EDDB_STATIONS_FULL_DATAFILE));
				int correlationId = EventBus.Start("importing stations and market datas...", eddbStations.Count);
				foreach (EddbStation eddbStation in eddbStations)
				{
					starMap.Update(ToStation(eddbStation));
					ImportMarketData(eddbStation, market);
					++count;
					EventBus.Progress("importing stations and market datas...", count, eddbStations.Count, correlationId);
				}
				EventBus.Completed("stations imported", correlationId);
			}
			else if (File.Exists(EDDB_STATIONS_LITE_DATAFILE))
			{
				List<EddbStation> eddbStations = SerializationHelpers.ReadJsonFromFile<List<EddbStation>>(new FileInfo(EDDB_STATIONS_FULL_DATAFILE));
				int correlationId = EventBus.Start("importing stations...", eddbStations.Count);
				foreach (EddbStation eddbStation in eddbStations)
				{
					starMap.Update(ToStation(eddbStation));
					++count;
					EventBus.Progress("importing stations...", count, eddbStations.Count, correlationId);
				}
				EventBus.Completed("stations imported", correlationId);
			}
		}

		private void ImportMarketData(EddbStation station, GalacticMarket market)
		{
			if (station.MarketDatas == null) return;
			int count = 0;
			foreach (EddbStation.MarketData marketData in station.MarketDatas)
			{
				if (ImportMode == ImportMode.Update)
				{
					market.Update(ToMarketData(marketData, station.Name, RetrieveSystemName(station.SystemId)));
				}
				else
				{
					market.Import(ToMarketData(marketData, station.Name, RetrieveSystemName(station.SystemId)));
				}
				++count;
			}
		}

		private MarketDataRow ToMarketData(EddbStation.MarketData marketData, string stationName, string systemName)
		{
			return new MarketDataRow()
			{
				CommodityName = RetrieveCommodityName(marketData.CommodityId)
				,
				BuyPrice = marketData.BuyPrice
				,
				Demand = marketData.Demand
				,
				SellPrice = marketData.SellPrice
				,
				StationName = stationName
				,
				Source = SOURCENAME
				,
				Stock = marketData.Supply
				,
				SystemName = systemName
				,
				SampleDate = UnixTimeStamp.ToDateTime(marketData.CollectedAt)
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
				,
				DistanceToStar = eddbStation.DistanceToStar
				,
				Economies = eddbStation.Economies
				,
				ExportCommodities = eddbStation.ExportCommodities
				,
				Faction = eddbStation.Faction
				,
				Government = eddbStation.Government
				,
				HasBlackmarket = ToNBool(eddbStation.HasBlackmarket)
				,
				HasCommodities = ToNBool(eddbStation.HasCommodities)
				,
				HasOutfitting = ToNBool(eddbStation.HasOutfitting)
				,
				HasRearm = ToNBool(eddbStation.HasRearm)
				,
				HasRepair = ToNBool(eddbStation.HasRepair)
				,
				HasRefuel = ToNBool(eddbStation.HasRefuel)
				,
				HasShipyard = ToNBool(eddbStation.HasShipyard)
				,
				ImportCommodities = eddbStation.ImportCommodities
				,
				MaxLandingPadSize = ParseLandingPadSize(eddbStation.MaxLandingPadSize)
				,
				ProhibitedCommodities = eddbStation.ProhibitedCommodities
				,
				Source = SOURCENAME
				,
				State = eddbStation.State
				,
				System = RetrieveSystemName(eddbStation.SystemId)
				,
				Type = eddbStation.Type
				,
				UpdatedAt = eddbStation.UpdatedAt
			};
			return station;
		}

		private string RetrieveSystemName(int systemId)
		{
			return _systemIdToNameMap[systemId];
		}

		internal void ImportSystems(StarMap starMap)
		{
			List<EddbSystem> eddbSystems = SerializationHelpers.ReadJsonFromFile<List<EddbSystem>>(new FileInfo(EDDB_SYSTEMS_DATAFILE));
			int correlationId = EventBus.Start("importing systems...", eddbSystems.Count);
			int count = 0;
			foreach (EddbSystem system in (IEnumerable<EddbSystem>)eddbSystems)
			{
				_systemIdToNameMap.Add(system.Id, system.Name.ToUpperInvariant());
				starMap.Update(ToStarSystem(system));
				++count;
				EventBus.Progress("importing systems...", count, eddbSystems.Count, correlationId);
			}
			EventBus.Completed("importing systems", correlationId);
		}

		private static StarSystem ToStarSystem(EddbSystem eddbSystem)
		{
			var starSystem = new StarSystem(eddbSystem.Name.ToUpperInvariant())
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

		internal void DownloadDataFiles()
		{
			var tasks = new List<Task>();
			int correlationId = EventBus.Start("trying to download data files...");
			if (!File.Exists(EDDB_COMMODITIES_DATAFILE))
			{
				tasks.Add(Task.Run(() => DownloadDataFile(new Uri(EDDB_COMMODITIES_URL), EDDB_COMMODITIES_DATAFILE,
					 "eddb commodities data", correlationId)));
			}
			if (!File.Exists(EDDB_SYSTEMS_DATAFILE))
			{
				tasks.Add(Task.Run(() => DownloadDataFile(new Uri(EDDB_SYSTEMS_URL), EDDB_SYSTEMS_DATAFILE,
					 "eddb stations lite data", correlationId)));
			}
			if (!File.Exists(EDDB_STATIONS_FULL_DATAFILE) && !File.Exists(EDDB_STATIONS_LITE_DATAFILE))
			{
				tasks.Add(Task.Run(() => DownloadDataFile(new Uri(EDDB_STATIONS_LITE_URL), EDDB_STATIONS_LITE_DATAFILE,
					 "eddb stations lite data", correlationId)));
			}
			if (!File.Exists(EDDB_STATIONS_FULL_DATAFILE))
			{
				Task.Run(() => DownloadDataFile(new Uri(EDDB_STATIONS_FULL_URL), EDDB_STATIONS_FULL_DATAFILE, "eddb stations full data", correlationId));
			}
			if (tasks.Any())
			{
				while (!Task.WaitAll(tasks.ToArray(), TimeSpan.FromMinutes(5)) && EventBus.Request("eddb server not responding, still waiting?"))
				{
				}
				EventBus.Completed("data files downloaded", correlationId);
			}
		}

		private static void DownloadDataFile(Uri address, string filepath, string contentDescription, int correlationId)
		{
			EventBus.Progress("starting download of " + contentDescription, 1, 2, correlationId);
			try
			{
				using (var webClient = new WebClient())
				{
					webClient.DownloadFile(address, filepath);
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("unable to download file: " + ex);
			}
			EventBus.Progress("completed download of " + contentDescription, 2, 2, correlationId);
		}
	}

	public enum ImportMode
	{
		Import,
		Update
	}
}