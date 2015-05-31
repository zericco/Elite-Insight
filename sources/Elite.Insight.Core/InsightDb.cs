#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 25.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Elite.Insight.Core.DomainModel;
using Elite.Insight.Core.Helpers;
using Elite.Insight.Core.Messaging;
using Newtonsoft.Json;

namespace Elite.Insight.Core
{
	public class InsightDb
	{
		private readonly string _dbFilepath;
		private const string DEFAULT_DB_PATH = "elite-insight.db";

		private readonly Lazy<Cache<long>> _stationNameToIdCache;
		private readonly Lazy<Cache<long>> _systemNameToIdCache;
		private readonly Lazy<Cache<long>> _commodityNameToIdCache;

		public InsightDb(string filepath)
		{
			_dbFilepath = filepath;
			_stationNameToIdCache = new Lazy<Cache<long>>(() => new Cache<long>(StationNameToId));
			_systemNameToIdCache = new Lazy<Cache<long>>(() => new Cache<long>(SystemNameToId));
			_commodityNameToIdCache = new Lazy<Cache<long>>(() => new Cache<long>(CommodityNameToId));
		}

		public InsightDb()
			: this(DEFAULT_DB_PATH)
		{
		}

		public string DbFilepath
		{
			get { return _dbFilepath; }
		}

		public SQLiteConnection NewConnection()
		{
			if (!File.Exists(DbFilepath))
			{
				SQLiteConnection.CreateFile(DbFilepath);
				InitDb();
			}
			var connection = new SQLiteConnection("Data Source='" + _dbFilepath + "';Version=3;Pooling=True;Max Pool Size=10;");
			connection.Open();
			return connection;
		}

		public void InitDb()
		{
			using (var cx = NewConnection())
			{
				CreateIdTable(cx);
				CreateSystemsTable(cx);
				CreateStationsTable(cx);
				CreateCommoditiesTable(cx);
				CreateMarketDataTable(cx);
			}
		}

		public void LoadModel(DataModel model, int daysInPast = 2)
		{
			int eventId = EventBus.Start("loading data model");
			using (var cx = NewConnection())
			{
				using (var cmd = cx.CreateCommand())
				{
					cmd.CommandText = "create temp table perimeter as select * from marketdata where lastupdate > @lastUpdate";
					cmd.AddParameter("@lastUpdate", DateTime.Today.AddDays(-daysInPast));
					cmd.ExecuteNonQuery();
				}
				LoadCommodities(model.Commodities, cx);
				LoadStarMap(model.StarMap, cx);
				LoadMarketData(model.GalacticMarket, model.Commodities, model.StarMap, cx);
			}
			EventBus.Completed("data model loaded", eventId);
		}

		private void LoadMarketData(GalacticMarket galacticMarket, Commodities commodities, StarMap starmap, SQLiteConnection cx)
		{
			int eventId = EventBus.Start("loading market data");
			cx.Read("select * from perimeter", record => galacticMarket.Import(LoadMarketData(record, commodities, starmap)));
			EventBus.Completed("market data loaded", eventId);
		}

		private MarketDataRow LoadMarketData(IDataRecord record, Commodities commodities, StarMap starmap)
		{
			long commodityId = record.ReadInt64("commodityId");
			long stationId = record.ReadInt64("stationId");
			var commodity = commodities.First(c => c.Id == commodityId);
			var station = starmap.Stations.First(s => s.Id == stationId);
			return new MarketDataRow()
			{
				CommodityId = commodityId
				,CommodityName = commodity.Name
				,BuyPrice = record.ReadInt32("buyPrice", -1)
				,SellPrice = record.ReadInt32("sellPrice", -1)
				,Demand = record.ReadInt32("demand", 0)
				,DemandLevel = record.ReadEnum<ProposalLevel>("demandLevel", null)
				,Supply = record.ReadInt32("supply", 0)
				,SupplyLevel = record.ReadEnum<ProposalLevel>("supplyLevel", null)
				,SampleDate = record.ReadDate("lastUpdate", DateTime.MinValue)
				,StationId = stationId
				,StationName = station.Name
			};
		}

		private void LoadStarMap(StarMap starMap, SQLiteConnection cx)
		{
			int eventId = EventBus.Start("loading star map");
			var systemIds = new Dictionary<long, bool>();
			cx.Read(@"select 
										sy.id AS systemId
										,sy.name AS systemName
										,x
										,y
										,z
										,population
										,sy.faction AS systemFaction
										,sy.government AS systemGovernment
										,sy.allegiance AS systemAllegiance
										,sy.state AS systemState
										,security
										,primaryEconomy
										,needsPermit
										,sy.lastUpdate AS systemLastUpdate
										,st.id
										,st.name
										,landingPadSize
										,distanceToStar
										,st.faction
										,st.government
										,st.allegiance
										,st.state
										,type
										,hasBlackmarket
										,hasCommodities
										,hasRefuel
										,hasRepair
										,hasRearm
										,hasOutfitting
										,hasShipyard
										,availableShips
										,importCommodities
										,exportCommodities
										,prohibitedCommodities
										,economies
										,st.lastUpdate
					from systems sy inner join stations st on sy.id = st.systemId inner join perimeter md on md.stationId = st.id",
				record =>
				{
					long systemId = record.ReadInt64("systemId");
					if (!systemIds.ContainsKey(systemId))
					{
						starMap.Update(LoadSystem(record));
						systemIds.Add(systemId, true);
					}
					starMap.Update(LoadStation(record));
				});
			EventBus.Completed("star map loaded", eventId);
		}

		private void LoadCommodities(Commodities commodities, SQLiteConnection cx)
		{
			int eventId = EventBus.Start("loading commodities");
			cx.Read("select id, name, category from commodities", record => commodities.Add(LoadCommodity(record)));
			cx.Read(@"select commodityId, min(buyprice) low, max(buyprice) high from marketdata
								where buyprice >= 0 and demand >0
								group by commodityId", record => UpdateCommodityBuyDemandStats(commodities, record));
			cx.Read(@"select commodityId, min(buyprice) low, max(buyprice) high from marketdata
								where buyprice >= 0 and supply >0
								group by commodityId", record => UpdateCommodityBuySupplyStats(commodities, record));
			cx.Read(@"select commodityId, min(sellprice) low, max(sellprice) high from marketdata
								where buyprice >= 0 and demand >0
								group by commodityId", record => UpdateCommoditySellDemandStats(commodities, record));
			cx.Read(@"select commodityId, min(sellprice) low, max(sellprice) high from marketdata
								where buyprice >= 0 and supply >0
								group by commodityId", record => UpdateCommoditySellSupplyStats(commodities, record));
			EventBus.Completed("commodities loaded", eventId);
		}

		private void UpdateCommodityBuyDemandStats(Commodities commodities, IDataRecord record)
		{
			var comodity = commodities.First(c => c.Id == record.ReadInt64("commodityId"));
			comodity.DemandWarningLevels.Buy.Low = (int)record.ReadInt64("low");
			comodity.DemandWarningLevels.Buy.High = (int)record.ReadInt64("high");
		}

		private void UpdateCommodityBuySupplyStats(Commodities commodities, IDataRecord record)
		{
			var comodity = commodities.First(c => c.Id == record.ReadInt64("commodityId"));
			comodity.SupplyWarningLevels.Buy.Low = (int)record.ReadInt64("low");
			comodity.SupplyWarningLevels.Buy.High = (int)record.ReadInt64("high");
		}

		private void UpdateCommoditySellDemandStats(Commodities commodities, IDataRecord record)
		{
			var comodity = commodities.First(c => c.Id == record.ReadInt64("commodityId"));
			comodity.DemandWarningLevels.Sell.Low = (int)record.ReadInt64("low");
			comodity.DemandWarningLevels.Sell.High = (int)record.ReadInt64("high");
		}

		private void UpdateCommoditySellSupplyStats(Commodities commodities, IDataRecord record)
		{
			var comodity = commodities.First(c => c.Id == record.ReadInt64("commodityId"));
			comodity.SupplyWarningLevels.Sell.Low = (int)record.ReadInt64("low");
			comodity.SupplyWarningLevels.Sell.High = (int)record.ReadInt64("high");
		}

		private Commodity LoadCommodity(IDataRecord record)
		{
			return new Commodity(record.ReadString("name"))
			{
				Id = record.ReadInt64("id")
				,
				Category = record.ReadString("category")
			};
		}

		private Station LoadStation(IDataRecord record)
		{
			return new Station(record.ReadString("name"))
			{
				Id = record.ReadInt64("id"),
				Faction = record.ReadString("faction"),
				Allegiance = record.ReadString("allegiance"),
				Government = record.ReadString("government"),
				//Source = record.ReadString("source"),
				State = record.ReadString("state"),
				DistanceToStar = record.ReadInt32("distanceToStar"),
				AvailableShips = record.ReadJson("availableShips", new string[0]),
				Economies = record.ReadJson("economies", new string[0]),
				ExportCommodities = record.ReadJson("exportCommodities", new string[0]),
				HasRearm = record.ReadBoolean("hasRearm"),
				HasBlackmarket = record.ReadBoolean("hasBlackmarket"),
				HasOutfitting = record.ReadBoolean("hasOutfitting"),
				HasCommodities = record.ReadBoolean("hasCommodities"),
				HasRefuel = record.ReadBoolean("hasRefuel"),
				HasRepair = record.ReadBoolean("hasRepair"),
				HasShipyard = record.ReadBoolean("hasShipyard"),
				ImportCommodities = record.ReadJson("importCommodities", new string[0]),
				MaxLandingPadSize = record.ReadEnum<LandingPadSize>("landingPadSize", null),
				ProhibitedCommodities = record.ReadJson("prohibitedCommodities", new string[0]),
				Type = record.ReadString("type"),
				SystemId = record.ReadInt64("systemId"),
				SystemName = record.ReadString("systemName"),
				UpdatedAt = record.ReadInt64("lastUpdate")
			};
		}

		private StarSystem LoadSystem(IDataRecord record)
		{
			return new StarSystem(record.ReadString("systemName"))
			{
				Id = record.ReadInt64("systemId"),
				Faction = record.ReadString("systemFaction"),
				Allegiance = record.ReadString("systemAllegiance"),
				Government = record.ReadString("systemGovernment"),
				NeedsPermit = record.ReadBoolean("needsPermit"),
				Population = record.ReadInt64("population", null),
				PrimaryEconomy = record.ReadString("primaryEconomy"),
				Security = record.ReadString("security"),
				//Source = record.ReadString("source"),
				State = record.ReadString("systemState"),
				UpdatedAt = record.ReadInt64("systemLastUpdate"),
				X = record.ReadDouble("x").Value,
				Y = record.ReadDouble("y").Value,
				Z = record.ReadDouble("z").Value
			};
		}

		public long GetNextId(int count = 1)
		{
			using (var cx = NewConnection())
			{
				using (var cmd = cx.CreateCommand())
				{
					cmd.CommandText = @"SELECT nextId + 1 from constants LIMIT 1;
											UPDATE constants
												SET nextId = nextId + " + count + ";";
					return (long)cmd.ExecuteScalar();
				}
			}
		}

		public void StoreSystems(IReadOnlyCollection<StarSystem> starSystems)
		{
			if (starSystems == null)
			{
				throw new ArgumentNullException("starSystems");
			}
			long id = GetNextId(starSystems.Count(s => s.Id == 0));
			foreach (StarSystem starSystem in starSystems)
			{
				if (starSystem.Id == 0)
					starSystem.Id = id++;
				Store(starSystem);
			}
		}

		public void Store(StarSystem starSystem)
		{
			using (var cx = NewConnection())
			{
				using (var cmd = cx.CreateCommand())
				{
					try
					{
						cmd.CommandText = @"INSERT INTO systems
												(id,name,x,y,z,population
													,faction
													,government
													,allegiance
													,state
													,security
													,primaryEconomy
													,needsPermit
													,lastUpdate)
												VALUES ("
												+ starSystem.Id + ","
												+ DbExtension.ToParameter(starSystem.Name) + ","
												+ DbExtension.ToParameter(starSystem.X) + ","
												+ DbExtension.ToParameter(starSystem.Y) + ","
												+ DbExtension.ToParameter(starSystem.Z) + ","
												+ DbExtension.ToParameter(starSystem.Population) + ","
												+ DbExtension.ToParameter(starSystem.Faction) + ","
												+ DbExtension.ToParameter(starSystem.Government) + ","
												+ DbExtension.ToParameter(starSystem.Allegiance) + ","
												+ DbExtension.ToParameter(starSystem.State) + ","
												+ DbExtension.ToParameter(starSystem.Security) + ","
												+ DbExtension.ToParameter(starSystem.PrimaryEconomy) + ","
												+ DbExtension.ToParameter(starSystem.NeedsPermit) + ","
												+ DbExtension.ToParameter(starSystem.UpdatedAt)
												+ ")";
						cmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						throw new DataException("unable to save " + starSystem + Environment.NewLine + cmd.CommandText, ex);
					}
				}
			}
		}

		public void StoreCommodities(IReadOnlyCollection<Commodity> commodities)
		{
			if (commodities == null)
			{
				throw new ArgumentNullException("commodities");
			}
			long id = GetNextId(commodities.Count(s => s.Id == 0));
			foreach (Commodity commodity in commodities)
			{
				if (commodity.Id == 0)
					commodity.Id = id++;
				Store(commodity);
			}
		}

		public void Store(Commodity commodity)
		{
			using (var cx = NewConnection())
			{
				using (var cmd = cx.CreateCommand())
				{
					try
					{
						cmd.CommandText = @"INSERT INTO commodities
												(id,name,category
													,lastUpdate)
												VALUES ("
												+ commodity.Id + ","
												+ DbExtension.ToParameter(commodity.Name) + ","
												+ DbExtension.ToParameter(commodity.Category) + ","
												+ DbExtension.ToParameter(DateTime.Now)
												+ ")";
						cmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						throw new DataException("unable to save " + commodity + Environment.NewLine + cmd.CommandText, ex);
					}
				}
			}
		}

		public void StoreStations(IReadOnlyCollection<Station> stations)
		{
			if (stations == null)
			{
				throw new ArgumentNullException("stations");
			}
			long id = GetNextId(stations.Count(s => s.Id == 0));
			foreach (Station station in stations)
			{
				if (station.Id == 0)
					station.Id = id++;
				Store(station);
			}
		}

		public void Store(Station station)
		{
			using (var cx = NewConnection())
			{
				long systemId = ToSystemId(station.SystemName);
				using (var cmd = cx.CreateCommand())
				{
					try
					{
						cmd.CommandText = @"INSERT INTO stations
												(id,name,systemId,landingPadSize,distanceToStar
													,faction,government,allegiance,state,type
													,hasBlackmarket
													,hasCommodities
													,hasRefuel
													,hasRepair
													,hasRearm
													,hasOutfitting
													,hasShipyard
													,AvailableShips
													,ImportCommodities
													,ExportCommodities
													,ProhibitedCommodities
													,Economies
													,lastUpdate)
												VALUES ("
												+ station.Id + ","
												+ DbExtension.ToParameter(station.Name) + ","
												+ DbExtension.ToParameter(systemId) + ","
												+ DbExtension.ToParameter(station.MaxLandingPadSize) + ","
												+ DbExtension.ToParameter(station.DistanceToStar) + ","
												+ DbExtension.ToParameter(station.Faction) + ","
												+ DbExtension.ToParameter(station.Government) + ","
												+ DbExtension.ToParameter(station.Allegiance) + ","
												+ DbExtension.ToParameter(station.State) + ","
												+ DbExtension.ToParameter(station.Type) + ","
												+ DbExtension.ToParameter(station.HasBlackmarket) + ","
												+ DbExtension.ToParameter(station.HasCommodities) + ","
												+ DbExtension.ToParameter(station.HasRefuel) + ","
												+ DbExtension.ToParameter(station.HasRepair) + ","
												+ DbExtension.ToParameter(station.HasRearm) + ","
												+ DbExtension.ToParameter(station.HasOutfitting) + ","
												+ DbExtension.ToParameter(station.HasShipyard) + ","
												+ DbExtension.ToParameter(station.AvailableShips) + ","
												+ DbExtension.ToParameter(station.ImportCommodities) + ","
												+ DbExtension.ToParameter(station.ExportCommodities) + ","
												+ DbExtension.ToParameter(station.ProhibitedCommodities) + ","
												+ DbExtension.ToParameter(station.Economies) + ","
												+ DbExtension.ToParameter(station.UpdatedAt)
												+ ")";
						cmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						throw new DataException("unable to save " + station + Environment.NewLine + cmd.CommandText, ex);
					}
				}
			}
		}

		public void StoreMarketData(IReadOnlyCollection<MarketDataRow> marketDataRows)
		{
			if (marketDataRows == null)
			{
				throw new ArgumentNullException("marketDataRows");
			}
			foreach (MarketDataRow marketData in marketDataRows)
			{
				Store(marketData);
			}
		}

		public void Store(MarketDataRow marketData)
		{
			using (var cx = NewConnection())
			{
				long stationId = ToStationId(marketData.StationName);
				long commodityId = ToCommodityId(marketData.CommodityName);
				using (var cmd = cx.CreateCommand())
				{
					try
					{
						cmd.CommandText = @"INSERT OR REPLACE INTO marketdata
												(   commodityId
													,stationId
													,buyPrice
													,sellPrice
													,demand
													,demandLevel
													,supply
													,supplyLevel
													,lastUpdate) VALUES ("
												+ DbExtension.ToParameter(commodityId) + ","
												+ DbExtension.ToParameter(stationId) + ","
												+ DbExtension.ToParameter(marketData.BuyPrice) + ","
												+ DbExtension.ToParameter(marketData.SellPrice) + ","
												+ DbExtension.ToParameter(marketData.Demand) + ","
												+ DbExtension.ToParameter(marketData.DemandLevel) + ","
												+ DbExtension.ToParameter(marketData.Supply) + ","
												+ DbExtension.ToParameter(marketData.SupplyLevel) + ","
												+ DbExtension.ToParameter(marketData.SampleDate)
												+ ")";
						cmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						throw new DataException("unable to save " + marketData + Environment.NewLine + cmd.CommandText, ex);
					}
				}
			}
		}

		private object ExecuteScalar(string request)
		{
			using (var cx = NewConnection())
			{
				using (var cmd = cx.CreateCommand())
				{
					cmd.CommandText = request;
					try
					{
						return cmd.ExecuteScalar();
					}
					catch (Exception ex)
					{
						throw new DataException("unable to execute scalar request '" + request + "'", ex);
					}
				}
			}
		}

		private long ToSystemId(string systemName)
		{
			return _systemNameToIdCache.Value[systemName];
		}

		private long ToCommodityId(string commodityName)
		{
			return _commodityNameToIdCache.Value[commodityName];
		}

		private long ToStationId(string stationName)
		{
			return _stationNameToIdCache.Value[stationName];
		}

		private long SystemNameToId(string systemName)
		{
			object result = ExecuteScalar("SELECT id FROM systems WHERE name like '" + systemName.Replace("'", "''") + "'");
			if (result == null)
			{
				throw new ArgumentException("unknown system: " + systemName);
			}
			long systemId = (long)result;
			return systemId;
		}

		private long CommodityNameToId(string commodityName)
		{
			object result =
				ExecuteScalar("SELECT id FROM commodities WHERE name like '" + commodityName.Replace("'", "''") + "'");
			if (result == null)
			{
				throw new ArgumentException("unknown commodity: " + commodityName);
			}
			long commodityId = (long)result;
			return commodityId;
		}

		private long StationNameToId(string stationName)
		{
			object result = ExecuteScalar("SELECT id FROM stations WHERE name like '" + stationName.Replace("'", "''") + "'");
			if (result == null)
			{
				throw new ArgumentException("unknown station: " + stationName);
			}
			long stationId = (long)result;
			return stationId;
		}

		private static void CreateIdTable(SQLiteConnection cx)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = @"CREATE TABLE IF NOT EXISTS constants
												(
													nextid INTEGER
												)";
				cmd.ExecuteNonQuery();
			}
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = @"INSERT INTO constants
												(nextid) VALUES (1)";
				cmd.ExecuteNonQuery();
			}
		}

		private static void CreateSystemsTable(SQLiteConnection cx)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = @"CREATE TABLE IF NOT EXISTS systems
												(
													id INTEGER PRIMARY KEY
													,name TEXT UNIQUE NOT NULL
													,x REAL NOT NULL
													,y REAL NOT NULL
													,z REAL NOT NULL
													,population INTEGER
													,faction TEXT
													,government TEXT
													,allegiance TEXT
													,state TEXT
													,security TEXT
													,primaryEconomy TEXT
													,needsPermit INTEGER
													,source TEXT
													,lastUpdate INTEGER
												);
												CREATE UNIQUE INDEX IF NOT EXISTS system_name_idx ON systems(name ASC)";
				cmd.ExecuteNonQuery();
			}
		}

		private static void CreateStationsTable(SQLiteConnection cx)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = @"CREATE TABLE IF NOT EXISTS stations
												(
													id INTEGER PRIMARY KEY
													,systemId INTEGER NOT NULL
													,name TEXT UNIQUE NOT NULL
													,landingPadSize TEXT
													,distanceToStar INTEGER
													,faction TEXT
													,government TEXT
													,allegiance TEXT
													,state TEXT
													,type TEXT
													,hasBlackmarket INTEGER
													,hasCommodities INTEGER
													,hasRefuel INTEGER
													,hasRepair INTEGER
													,hasRearm INTEGER
													,hasOutfitting INTEGER
													,hasShipyard INTEGER
													,availableShips TEXT
													,importCommodities TEXT
													,exportCommodities TEXT
													,prohibitedCommodities TEXT
													,economies TEXT
													,source TEXT
													,lastUpdate INTEGER
												);
												CREATE UNIQUE INDEX IF NOT EXISTS station_name_idx ON stations(name ASC)";
				cmd.ExecuteNonQuery();
			}
		}

		private static void CreateCommoditiesTable(SQLiteConnection cx)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = @"CREATE TABLE IF NOT EXISTS commodities
												(
													id INTEGER PRIMARY KEY
													,name TEXT UNIQUE NOT NULL
													,category TEXT
													,source TEXT
													,lastUpdate INTEGER
												);
												CREATE UNIQUE INDEX IF NOT EXISTS commodity_name_idx ON commodities(name)";
				cmd.ExecuteNonQuery();
			}
		}

		private static void CreateMarketDataTable(SQLiteConnection cx)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = @"CREATE TABLE IF NOT EXISTS marketdata
												(
													commodityId INTEGER NOT NULL
													,stationId INTEGER NOT NULL
													,buyPrice INTEGER
													,sellPrice INTEGER
													,demand INTEGER
													,demandLevel TEXT
													,supply INTEGER
													,supplyLevel TEXT
													,lastUpdate INTEGER
												);
												CREATE UNIQUE INDEX marketdata_station_commodity_idx ON marketdata(stationId, commodityId);
												CREATE INDEX marketdata_commodity_idx ON marketdata(commodityId);";
				cmd.ExecuteNonQuery();
			}
		}

		public override string ToString()
		{
			return "Db [" + DbFilepath + "]";
		}
	}

	internal class Cache<TValue>
	{
		private readonly Func<string, TValue> _provider;
		private readonly Dictionary<string, TValue> _dictionary;

		public Cache(Func<string, TValue> provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}
			_provider = provider;
			_dictionary = new Dictionary<string, TValue>();
		}

		public TValue this[string key]
		{
			get
			{
				TValue value;
				if (!_dictionary.TryGetValue(key, out value))
				{
					value = _provider(key);
					_dictionary.Add(key, value);
				}
				return value;
			}
		}
	}

	internal static class DbExtension
	{
		public const string NULL_VALUE = "NULL";

		public static SQLiteParameter AddParameter(this SQLiteCommand cmd, string parameterName, DateTime value)
		{
			var parameter = cmd.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value.ToUniversalTime().ToUnixTimestamp();
			cmd.Parameters.Add(parameter);
			return parameter;
		}

		public static SQLiteParameter AddParameter(this SQLiteCommand cmd, string parameterName, string value)
		{
			var parameter = cmd.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value;
			cmd.Parameters.Add(parameter);
			return parameter;
		}

		public static SQLiteParameter AddParameter(this SQLiteCommand cmd, string parameterName, int value)
		{
			var parameter = cmd.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value;
			cmd.Parameters.Add(parameter);
			return parameter;
		}

		public static SQLiteParameter AddParameter(this SQLiteCommand cmd, string parameterName, double value)
		{
			var parameter = cmd.CreateParameter();
			parameter.ParameterName = parameterName;
			parameter.Value = value;
			cmd.Parameters.Add(parameter);
			return parameter;
		}

		public static void Read(this SQLiteConnection cx, string request, Action<IDataRecord> mapper)
		{
			using (var cmd = cx.CreateCommand())
			{
				Read(cmd, request, mapper);
			}
		}

		public static void Read(this SQLiteCommand cmd, string request, Action<IDataRecord> mapper)
		{
			cmd.CommandText = request;
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					mapper(reader);
				}
			}
		}

		public static DateTime ReadDate(this IDataRecord record, string columnName, DateTime defaultValue)
		{
			return ReadDate(record, columnName) ?? defaultValue;
		}

		public static DateTime ReadDate(this IDataRecord record, int index, DateTime defaultValue)
		{
			return ReadDate(record, index) ?? defaultValue;
		}

		public static DateTime? ReadDate(this IDataRecord record, string columnName, DateTime? defaultValue = null)
		{
			try
			{
				return ReadDate(record, record.GetOrdinal(columnName), defaultValue);
			}
			catch (IndexOutOfRangeException ex)
			{
				throw new DataException("unknown column " + columnName, ex);
			}
			catch (DataException ex)
			{
				throw new DataException("error occured when reading column " + columnName, ex);
			}
		}

		public static DateTime? ReadDate(this IDataRecord record, int index, DateTime? defaultValue = null)
		{
			try
			{
				if (record.IsDBNull(index))
				{
					return defaultValue;
				}
				else
				{
					return UnixTimeStamp.ToDateTime(record.GetInt64(index));
				}
			}
			catch (Exception ex)
			{
				throw new DataException("error occured when reading column #" + index, ex);
			}
		}

		public static string ReadString(this IDataRecord record, string columnName, string defaultValue = null)
		{
			try
			{
				return ReadString(record, record.GetOrdinal(columnName), defaultValue);
			}
			catch (IndexOutOfRangeException ex)
			{
				throw new DataException("unknown column " + columnName, ex);
			}
			catch (DataException ex)
			{
				throw new DataException("error occured when reading column " + columnName, ex);
			}
		}

		public static string ReadString(this IDataRecord record, int index, string defaultValue)
		{
			try
			{
				if (record.IsDBNull(index))
				{
					return defaultValue;
				}
				else
				{
					return record.GetString(index);
				}
			}
			catch (Exception ex)
			{
				throw new DataException("error occured when reading column #" + index, ex);
			}
		}

		public static int? ReadInt32(this IDataRecord record, string columnName, int? defaultValue = null)
		{
			try
			{
				return ReadInt32(record, record.GetOrdinal(columnName), defaultValue);
			}
			catch (IndexOutOfRangeException ex)
			{
				throw new DataException("unknown column " + columnName, ex);
			}
			catch (DataException ex)
			{
				throw new DataException("error occured when reading column " + columnName, ex);
			}
		}

		public static int? ReadInt32(this IDataRecord record, int index, int? defaultValue = null)
		{
			try
			{
				if (record.IsDBNull(index))
				{
					return defaultValue;
				}
				else
				{
					return record.GetInt32(index);
				}
			}
			catch (Exception ex)
			{
				throw new DataException("error occured when reading column #" + index, ex);
			}
		}

		public static int ReadInt32(this IDataRecord record, string columnName, int defaultValue)
		{
			return ReadInt32(record, columnName) ?? defaultValue;
		}

		public static int ReadInt32(this IDataRecord record, int index, int defaultValue)
		{
			return ReadInt32(record, index) ?? defaultValue;

		}

		public static long? ReadInt64(this IDataRecord record, string columnName, long? defaultValue)
		{
			try
			{
				return ReadInt64(record, record.GetOrdinal(columnName), defaultValue);
			}
			catch (IndexOutOfRangeException ex)
			{
				throw new DataException("unknown column " + columnName, ex);
			}
			catch (DataException ex)
			{
				throw new DataException("error occured when reading column " + columnName, ex);
			}
		}

		public static long? ReadInt64(this IDataRecord record, int index, long? defaultValue)
		{
			try
			{
				if (record.IsDBNull(index))
				{
					return defaultValue;
				}
				else
				{
					return record.GetInt64(index);
				}
			}
			catch (Exception ex)
			{
				throw new DataException("error occured when reading column #" + index, ex);
			}
		}

		public static long ReadInt64(this IDataRecord record, string columnName, long defaultValue)
		{
			return ReadInt64(record, columnName, null) ?? defaultValue;

		}

		public static long ReadInt64(this IDataRecord record, int index, long defaultValue)
		{
			return ReadInt64(record, index, null) ?? defaultValue;

		}

		public static long ReadInt64(this IDataRecord record, string columnName)
		{
			long? readInt64 = ReadInt64(record, columnName, null);
			if (!readInt64.HasValue)
				throw new MissingDataException("field " + columnName + " is not set");
			return readInt64.Value;
		}

		public static long ReadInt64(this IDataRecord record, int index)
		{
			long? readInt64 = ReadInt64(record, index, null);
			if (!readInt64.HasValue)
				throw new MissingDataException("field #" + index + " is not set");
			return readInt64.Value;
		}

		public static bool? ReadBoolean(this IDataRecord record, string columnName, bool? defaultValue = null)
		{
			try
			{
				return ReadBoolean(record, record.GetOrdinal(columnName), defaultValue);
			}
			catch (IndexOutOfRangeException ex)
			{
				throw new DataException("unknown column " + columnName, ex);
			}
			catch (DataException ex)
			{
				throw new DataException("error occured when reading column " + columnName, ex);
			}
		}

		public static bool? ReadBoolean(this IDataRecord record, int index, bool? defaultValue = null)
		{
			try
			{
				if (record.IsDBNull(index))
				{
					return defaultValue;
				}
				else
				{
					return record.GetBoolean(index);
				}
			}
			catch (Exception ex)
			{
				throw new DataException("error occured when reading column #" + index, ex);
			}
		}

		public static bool ReadBoolean(this IDataRecord record, string columnName, bool defaultValue)
		{
			return ReadBoolean(record, columnName) ?? defaultValue;

		}

		public static bool ReadBoolean(this IDataRecord record, int index, bool defaultValue)
		{
			return ReadBoolean(record, index) ?? defaultValue;

		}

		public static double? ReadDouble(this IDataRecord record, string columnName, double? defaultValue = null)
		{
			try
			{
				return ReadDouble(record, record.GetOrdinal(columnName), defaultValue);
			}
			catch (IndexOutOfRangeException ex)
			{
				throw new DataException("unknown column " + columnName, ex);
			}
			catch (DataException ex)
			{
				throw new DataException("error occured when reading column " + columnName, ex);
			}
		}

		public static double? ReadDouble(this IDataRecord record, int index, double? defaultValue = null)
		{
			try
			{
				if (record.IsDBNull(index))
				{
					return defaultValue;
				}
				else
				{
					return record.GetDouble(index);
				}
			}
			catch (Exception ex)
			{
				throw new DataException("error occured when reading column #" + index, ex);
			}
		}

		public static double ReadDouble(this IDataRecord record, string columnName, double defaultValue)
		{
			return ReadDouble(record, columnName) ?? defaultValue;
		}

		public static double ReadDouble(this IDataRecord record, int index, double defaultValue)
		{
			return ReadDouble(record, index) ?? defaultValue;
		}

		public static TEnum? ReadEnum<TEnum>(this IDataRecord record, string columnName, TEnum? defaultValue)
			where TEnum : struct
		{
			try
			{
				return ReadEnum(record, record.GetOrdinal(columnName), defaultValue);
			}
			catch (IndexOutOfRangeException ex)
			{
				throw new DataException("unknown column " + columnName, ex);
			}
			catch (DataException ex)
			{
				throw new DataException("error occured when reading column " + columnName, ex);
			}			
		}

		public static TEnum? ReadEnum<TEnum>(this IDataRecord record, int index, TEnum? defaultValue)
			where TEnum:struct
		{
			try
			{
				if (record.IsDBNull(index))
				{
					return defaultValue;
				}
				else
				{
					return (TEnum)Enum.Parse(typeof (TEnum), record.GetString(index));
				}
			}
			catch (Exception ex)
			{
				throw new DataException("error occured when reading column #" + index, ex);
			}			
		}

		public static TValue ReadJson<TValue>(this IDataRecord record, string columnName, TValue defaultValue)
		{
			string value = record.ReadString(columnName);
			if (value == null)
			{
				return defaultValue;
			}
			else
			{
				return JsonConvert.DeserializeObject<TValue>(value);
			}
		}

		public static string ToParameter(DateTime dateTime)
		{
			return "strftime('%s','" + dateTime.ToUniversalTime().ToString("u") + "')";
		}

		public static string ToParameter(double value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}

		public static string ToParameter<TValue>(TValue? value) where TValue : struct
		{
			if (value.HasValue)
				return ToParameter(value.Value);
			else
			{
				return NULL_VALUE;
			}
		}

		public static string ToParameter(string value)
		{
			if (value == null)
				return NULL_VALUE;
			else
				return "'" + value.Replace("'", "''") + "'";
		}

		public static string ToParameter(object value)
		{
			if (value == null)
			{
				return NULL_VALUE;
			}
			else if (value is string)
			{
				return ToParameter((string)value);
			}
			else if (value is double)
			{
				return ((double)value).ToString(CultureInfo.InvariantCulture);
			}
			else if (value is DateTime)
			{
				return "strftime('%s','" + ((DateTime)value).ToUniversalTime().ToString("u") + "')";
			}
			else if (value is bool)
			{
				return ((bool)value) ? "1" : "0";
			}
			else if (value is IEnumerable)
			{
				bool empty = true;
				foreach (object o in (IEnumerable)value)
				{
					empty = false;
					break;
				}
				return empty ? NULL_VALUE : "'" + JsonConvert.SerializeObject(value).Replace("'", "''") + "'";
			}
			else if (value.GetType().IsEnum)
			{
				return "'" + value + "'";
			}
			else
			{
				return value.ToString();
			}
		}
	}
}