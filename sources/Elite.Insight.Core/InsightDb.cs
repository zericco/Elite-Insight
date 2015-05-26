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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using Elite.Insight.Core.DomainModel;
using Newtonsoft.Json;

namespace Elite.Insight.Core
{
	public class InsightDb
	{
		private readonly string _dbFilepath;
		private const string DEFAULT_DB_PATH = "elite-insight.db";

		private Lazy<Cache<long>> _stationNameToIdCache;
		private Lazy<Cache<long>> _systemNameToIdCache;
		private Lazy<Cache<long>> _commodityNameToIdCache;

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

		private const string NULL_VALUE = "NULL";

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
				CreateComoditiesTable(cx);
				CreateMarketDataTable(cx);
			}
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
												+ ToParameter(starSystem.Name) + ","
												+ ToParameter(starSystem.X) + ","
												+ ToParameter(starSystem.Y) + ","
												+ ToParameter(starSystem.Z) + ","
												+ ToParameter(starSystem.Population) + ","
												+ ToParameter(starSystem.Faction) + ","
												+ ToParameter(starSystem.Government) + ","
												+ ToParameter(starSystem.Allegiance) + ","
												+ ToParameter(starSystem.State) + ","
												+ ToParameter(starSystem.Security) + ","
												+ ToParameter(starSystem.PrimaryEconomy) + ","
												+ ToParameter(starSystem.NeedsPermit) + ","
												+ ToParameter(starSystem.UpdatedAt)
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
												+ ToParameter(commodity.Name) + ","
												+ ToParameter(commodity.Category) + ","
												+ ToParameter(DateTime.Now)
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
				long systemId = ToSystemId(station.System);
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
												+ ToParameter(station.Name) + ","
												+ ToParameter(systemId) + ","
												+ ToParameter(station.MaxLandingPadSize) + ","
												+ ToParameter(station.DistanceToStar) + ","
												+ ToParameter(station.Faction) + ","
												+ ToParameter(station.Government) + ","
												+ ToParameter(station.Allegiance) + ","
												+ ToParameter(station.State) + ","
												+ ToParameter(station.Type) + ","
												+ ToParameter(station.HasBlackmarket) + ","
												+ ToParameter(station.HasCommodities) + ","
												+ ToParameter(station.HasRefuel) + ","
												+ ToParameter(station.HasRepair) + ","
												+ ToParameter(station.HasRearm) + ","
												+ ToParameter(station.HasOutfitting) + ","
												+ ToParameter(station.HasShipyard) + ","
												+ ToParameter(station.AvailableShips) + ","
												+ ToParameter(station.ImportCommodities) + ","
												+ ToParameter(station.ExportCommodities) + ","
												+ ToParameter(station.ProhibitedCommodities) + ","
												+ ToParameter(station.Economies) + ","
												+ ToParameter(station.UpdatedAt)
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
												+ ToParameter(commodityId) + ","
												+ ToParameter(stationId) + ","
												+ ToParameter(marketData.BuyPrice) + ","
												+ ToParameter(marketData.SellPrice) + ","
												+ ToParameter(marketData.Demand) + ","
												+ ToParameter(marketData.DemandLevel) + ","
												+ ToParameter(marketData.Stock) + ","
												+ ToParameter(marketData.SupplyLevel) + ","
												+ ToParameter(marketData.SampleDate)
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

		private static string ToParameter(DateTime dateTime)
		{
			return "strftime('%s','" + dateTime.ToUniversalTime().ToString("u") + "')";
		}

		private static string ToParameter(double value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}

		private static string ToParameter<TValue>(TValue? value) where TValue : struct
		{
			if (value.HasValue)
				return ToParameter(value.Value);
			else
			{
				return NULL_VALUE;
			}
		}

		private static string ToParameter(string value)
		{
			if (value == null)
				return NULL_VALUE;
			else
				return "'" + value.Replace("'", "''") + "'";
		}

		private static string ToParameter(object value)
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
				return empty ? NULL_VALUE : "'" + JsonConvert.SerializeObject(value).Replace("'","''") + "'";
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
			long systemId = (long) result;
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
			long commodityId = (long) result;
			return commodityId;
		}

		private long StationNameToId(string stationName)
		{
			object result = ExecuteScalar("SELECT id FROM stations WHERE name like '" + stationName.Replace("'", "''") + "'");
			if (result == null)
			{
				throw new ArgumentException("unknown station: " + stationName);
			}
			long stationId = (long) result;
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
													,AvailableShips TEXT
													,ImportCommodities TEXT
													,ExportCommodities TEXT
													,ProhibitedCommodities TEXT
													,Economies TEXT
													,lastUpdate INTEGER
												);
												CREATE UNIQUE INDEX IF NOT EXISTS station_name_idx ON stations(name ASC)";
				cmd.ExecuteNonQuery();
			}
		}

		private static void CreateComoditiesTable(SQLiteConnection cx)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = @"CREATE TABLE IF NOT EXISTS commodities
												(
													id INTEGER PRIMARY KEY
													,name TEXT UNIQUE NOT NULL
													,category TEXT
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
}