﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using Newtonsoft.Json;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.Exceptions;

namespace RegulatedNoise.EDDB_Data
{

	internal class EDMilkyway
	{
		private const string EDDB_COMMODITIES_DATAFILE = @"./Data/commodities.json";
		private const string REGULATEDNOISE_COMMODITIES_DATAFILE = @"./Data/commodities_RN.json";
		private const string EDDB_STATIONS_LITE_DATAFILE = @"./Data/stations_lite.json";
		private const string EDDB_STATIONS_FULL_DATAFILE = @"./Data/stations.json";
		private const string EDDB_SYSTEMS_DATAFILE = @"./Data/systems.json";
		private const string OWN_STATIONS_DATAFILE = @"./Data/stations_own.json";
		private const string OWN_SYSTEMS_DATAFILE = @"./Data/systems_own.json";

		private const string EDDB_COMMODITIES_URL = @"http://eddb.io/archive/v3/commodities.json";
		private const string EDDB_SYSTEMS_URL = @"http://eddb.io/archive/v3/systems.json";
		private const string EDDB_STATIONS_LITE_URL = @"http://eddb.io/archive/v3/stations_lite.json";
		private const string EDDB_STATIONS_FULL_URL = @"http://eddb.io/archive/v3/stations.json";

		public enum DataSource
		{
			Data_EDDB,
			Data_Own,
			Data_Merged
		}

		private List<EDSystem>[] m_Systems;
		private List<EDStation>[] m_Stations;
		private List<EDCommoditiesExt> m_Commodities;

		private bool m_changedStations = false;

		// a quick cache for systemlocations
		private readonly Dictionary<string, Point3D> m_cachedLocations;

		// a quick cache for distances of stations
		private readonly Dictionary<string, int> m_cachedStationDistances;
		private readonly Dictionary<string, double> _systemDistances;
		public const double NO_DISTANCE = Double.MaxValue;

		/// <summary>
		/// creates a new Milkyway :-)
		/// </summary>
		public EDMilkyway()
		{
			_systemDistances = new Dictionary<string, double>();
			ChangedSystems = false;
			int enumBound = Enum.GetNames(typeof(DataSource)).Length;

			m_Systems = new List<EDSystem>[enumBound];
			m_Stations = new List<EDStation>[enumBound];
			m_Commodities = new List<EDCommoditiesExt>();
			m_cachedLocations = new Dictionary<string, Point3D>();
			m_cachedStationDistances = new Dictionary<string, int>();
		}

		/// <summary>
		/// returns all systems
		/// </summary>
		public IEnumerable<EDSystem> GetSystems()
		{
			return m_Systems[(int)DataSource.Data_Merged];
		}

		/// <summary>
		/// returns all stations
		/// </summary>
		public IEnumerable<EDStation> GetStations()
		{
			return m_Stations[(int)DataSource.Data_Merged];
		}

		/// <summary>
		/// returns a station in a system by name
		/// </summary>
		/// <param name="systemName"></param>
		/// <param name="stationName"></param>
		/// <returns></returns>
		/// <param name="System"></param>
		public EDStation GetStation(string systemName, string stationName)
		{
			EDSystem tempSystem;

			return GetStation(systemName, stationName, out tempSystem);
		}

		/// <summary>
		/// returns a station in a system by name
		/// </summary>
		/// <param name="systemName"></param>
		/// <param name="stationName"></param>
		/// <returns></returns>
		/// <param name="System"></param>
		private EDStation GetStation(string systemName, string stationName, out EDSystem System)
		{
			EDStation retValue;

				EDSystem SystemData = m_Systems[(int)DataSource.Data_Merged].Find(x => x.Name.Equals(systemName, StringComparison.InvariantCultureIgnoreCase));

			if (SystemData != null)
				retValue = m_Stations[(int)DataSource.Data_Merged].Find(x => x.SystemId == SystemData.Id && x.Name.Equals(stationName, StringComparison.InvariantCultureIgnoreCase));
			else
				retValue = null;

			System = SystemData;

			return retValue;
		}

		/// <summary>
		/// returns distance of a station to the star
		/// </summary>
		/// <param name="systemName"></param>
		/// <param name="stationName"></param>
		/// <returns></returns>
		public int? GetStationDistance(string systemName, string stationName)
		{
			Int32 Distance = 0;

			if (!m_cachedStationDistances.TryGetValue(systemName + "|" + stationName, out Distance))
			{
				EDStation retValue = GetStation(systemName, stationName);

				if ((retValue != null) && (retValue.DistanceToStar != null))
				{
					Distance = (int)(retValue.DistanceToStar);
				}
				else
				{
					Distance = -1;
				}

				m_cachedStationDistances.Add(systemName + "|" + stationName, Distance);
			}

			return Distance;
		}

		/// <summary>
		/// 
		/// </summary>
		public void SetCommodities(IEnumerable<EDCommoditiesExt> newList)
		{
			m_Commodities.Distinct();
			m_Commodities = new List<EDCommoditiesExt>(newList);

			SaveRNCommodityData(REGULATEDNOISE_COMMODITIES_DATAFILE, true);
		}

		/// <summary>
		/// loads the stationsdata from a file
		/// </summary>
		/// <param name="filename">json-file to load</param>
		/// <param name="stationtype"></param>
		/// <param name="createNonExistingFile"></param>
		private void LoadStationData(string filename, DataSource stationtype, bool createNonExistingFile)
		{
			if (File.Exists(filename))
				m_Stations[(int)stationtype] = JsonConvert.DeserializeObject<List<EDStation>>(File.ReadAllText(filename));
			else
			{
				m_Stations[(int)stationtype] = new List<EDStation>();

				if (createNonExistingFile)
					SaveStationData(filename, stationtype, true);
			}

		}

		/// <summary>
		/// returns a cloned list of the systems
		/// </summary>
		private List<EDSystem> CloneSystems(DataSource systemstype)
		{
			return JsonConvert.DeserializeObject<List<EDSystem>>(JsonConvert.SerializeObject(m_Systems[(int)systemstype]));
		}

		/// <summary>
		/// returns a cloned list of the stations
		/// </summary>
		private List<EDStation> CloneStations(DataSource stationtype)
		{
			return JsonConvert.DeserializeObject<List<EDStation>>(JsonConvert.SerializeObject(m_Stations[(int)stationtype]));
		}

		/// <summary>
		/// returns a cloned list of the commodities
		/// </summary>
		public IEnumerable<EDCommoditiesExt> CloneCommodities()
		{
			return JsonConvert.DeserializeObject<List<EDCommoditiesExt>>(JsonConvert.SerializeObject(m_Commodities));
		}

		/// <summary>
		/// loads the systemsdata from a file
		/// </summary>
		/// <param name="filename">json-file to load</param>
		/// <param name="systemtype"></param>
		/// <param name="createNonExistingFile"></param>
		private void LoadSystemData(string filename, DataSource systemtype, bool createNonExistingFile)
		{
			if (File.Exists(filename))
				m_Systems[(int)systemtype] = JsonConvert.DeserializeObject<List<EDSystem>>(File.ReadAllText(filename));
			else
			{
				m_Systems[(int)systemtype] = new List<EDSystem>();

				if (createNonExistingFile)
					SaveSystemData(filename, systemtype, true);
			}

		}

		/// <summary>
		/// saves the stationsdata to a file
		/// </summary>
		/// <param name="filename">json-file to save</param>
		/// <param name="stationtype"></param>
		/// <param name="backupOldFile"></param>
		private void SaveStationData(string filename, DataSource stationtype, bool backupOldFile)
		{
			string newFile, backupFile;

			newFile = String.Format("{0}_new{1}", Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)), Path.GetExtension(filename));
			backupFile = String.Format("{0}_bak{1}", Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)), Path.GetExtension(filename));

			File.WriteAllText(newFile, JsonConvert.SerializeObject(m_Stations[(int)stationtype]));

			// we delete the current file not until the new file is written without errors

			RotateSaveFiles(filename, newFile, backupFile, backupOldFile);
		}

		/// <summary>
		/// loads the systemsdata to a file
		/// </summary>
		/// <param name="filename">json-file to save</param>
		/// <param name="systemtype"></param>
		/// <param name="backupOldFile"></param>
		private void SaveSystemData(string filename, DataSource systemtype, bool backupOldFile)
		{
			string newFile, backupFile;

			newFile = String.Format("{0}_new{1}", Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)), Path.GetExtension(filename));
			backupFile = String.Format("{0}_bak{1}", Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)), Path.GetExtension(filename));

			File.WriteAllText(newFile, JsonConvert.SerializeObject(m_Systems[(int)systemtype]));

			// we delete the current file not until the new file is written without errors

			RotateSaveFiles(filename, newFile, backupFile, backupOldFile);
		}

		private static void RotateSaveFiles(string filename, string newFile, string backupFile, bool backupOldFile)
		{
			// delete old backup
			if (File.Exists(backupFile))
				File.Delete(backupFile);

			// rename current file to old backup
			if (backupOldFile && File.Exists(filename))
				File.Move(filename, backupFile);

			// rename new file to current file
			File.Move(newFile, filename);
		}

		/// <summary>
		/// merging EDDB and own data to one big list
		/// </summary>
		private bool MergeData()
		{
			int StartingCount;
			bool SystemCanBeDeleted = false;

			//if (false)
			//{ 
			//    // create some data for testing 
			//    if (File.Exists(@".\Data\systems_own.json"))
			//        File.Delete(@".\Data\systems_own.json");
			//    if (File.Exists(@".\Data\stations_own.json"))
			//        File.Delete(@".\Data\stations_own.json");
			//    m_Systems[(int)DataSource.Data_Merged] = new List<EDSystem>(m_Systems[(int)DataSource.Data_EDDB].Where(x => x.Id <= 10));
			//    m_Stations[(int)DataSource.Data_Merged] = new List<EDStation>(m_Stations[(int)DataSource.Data_EDDB].Where(x => x.SystemId <= 10));
			//    saveSystemData(@".\Data\systems_own.json", DataSource.Data_Merged, false);
			//    saveStationData(@".\Data\stations_own.json", DataSource.Data_Merged, false);
			//}


			// get the base list from EDDB assuming it's the bigger one
			m_Systems[(int)DataSource.Data_Merged] = CloneSystems(DataSource.Data_EDDB);
			m_Stations[(int)DataSource.Data_Merged] = CloneStations(DataSource.Data_EDDB);

			StartingCount = m_Systems[(int)DataSource.Data_Own].Count;

			for (int i = 0; i < StartingCount; i++)
			{
				SystemCanBeDeleted = true;


				EDSystem ownSystem = m_Systems[(int)DataSource.Data_Own][StartingCount - i - 1];
				EDSystem existingEDDNSystem = GetSystem(ownSystem.Name);

				//if (existingEDDNSystem != null)
				//    Debug.Print("Id=" + existingEDDNSystem.Id);
				//else
				//    Debug.Print("Id=null");

				if (existingEDDNSystem != null)
				{

					if (existingEDDNSystem.EqualsED(ownSystem))
					{
						// systems are equal, check the stations
						CheckStations(ownSystem, existingEDDNSystem, ref SystemCanBeDeleted, ref m_changedStations);
					}
					else
					{
						// system is existing, but there are differences -> own version has a higher priority
						SystemCanBeDeleted = false;
						existingEDDNSystem.getValues(ownSystem);

						// now check the stations
						CheckStations(ownSystem, existingEDDNSystem, ref SystemCanBeDeleted, ref m_changedStations);

					}
				}
				else
				{
					// system is unknown in the EDDB, copy our own system into the merged list
					SystemCanBeDeleted = false;
					int newSystemIndex = m_Systems[(int)DataSource.Data_Merged].Max(X => X.Id) + 1;

					// create system cloneSystems
					EDSystem newSystem = new EDSystem(newSystemIndex, ownSystem);

					// add it to merged data
					m_Systems[(int)DataSource.Data_Merged].Add(newSystem);

					// now go and get the stations
					CopyStationsForNewSystem(newSystem);
				}

				if (SystemCanBeDeleted)
				{
					//

					// delete the system;
					m_Systems[(int)DataSource.Data_Own].Remove(ownSystem);
					ChangedSystems = true;

				}
			}

			return ChangedStations || ChangedSystems;
		}

		/// <summary>
		/// check if the stations of two systems are equal if the system 
		/// exists in both EDDB data and own data
		/// </summary>
		/// <param name="ownSystem">system from own data</param>
		/// <param name="existingEDDNSystem">system from EDDB data</param>
		/// <param name="SystemCanBeDeleted">normally true, except there are differences between at least one stations.
		/// if so the system must be hold as reference. If a own data station is 100% equal (e.g. Commoditynames and other strings
		/// are not casesensitive compared) to the eddb station it will automatically deleted</param>
		private void CheckStations(EDSystem ownSystem, EDSystem existingEDDNSystem, ref bool SystemCanBeDeleted, ref bool StationsChanged)
		{
			// own system can be deleted if the stations are equal, too
			List<EDStation> ownSystemStations = GetStations(ownSystem.Name, DataSource.Data_Own);

			for (int j = 0; j < ownSystemStations.Count(); j++)
			{
				EDStation ownStation = ownSystemStations[j];
				EDStation existingEDDNStation = GetSystemStation(ownSystem.Name, ownSystemStations[j].Name);

				if (existingEDDNStation != null)
				{
					if (existingEDDNStation.EqualsED(ownStation))
					{
						// no favour to hold it anymore
						m_Stations[(int)DataSource.Data_Own].Remove(ownStation);
						StationsChanged = true;
					}
					else
					{
						// copy the values and hold it
						existingEDDNStation.getValues(ownStation);
						SystemCanBeDeleted = false;
					}

				}
				else
				{
					// station is in EDDB not existing, so we'll hold the station
					SystemCanBeDeleted = false;
					int newStationIndex = m_Stations[(int)DataSource.Data_Merged].Max(X => X.Id) + 1;

					m_Stations[(int)DataSource.Data_Merged].Add(new EDStation(newStationIndex, existingEDDNSystem.Id, ownStation));
				}
			}
		}

		/// <summary>
		/// go and get station clones from the own data for a new station
		/// and adds them to the merged data
		/// </summary>
		/// <param name="newSystem">new created system in merged data </param>
		private void CopyStationsForNewSystem(EDSystem newSystem)
		{
			// get the list from own data with the name of the new station 
			// (it's ok because it must be the same name)
			List<EDStation> ownSystemStations = GetStations(newSystem.Name, DataSource.Data_Own);

			// get the gighest index
			int newStationIndex = m_Stations[(int)DataSource.Data_Merged].Max(X => X.Id);

			for (int j = 0; j < ownSystemStations.Count(); j++)
			{
				newStationIndex++;
				m_Stations[(int)DataSource.Data_Merged].Add(new EDStation(newStationIndex, newSystem.Id, ownSystemStations[j]));
			}
		}

		/// <summary>
		/// get all for stationnames a system
		/// </summary>
		/// <param name="systemname"></param>
		/// <returns></returns>
		public string[] GetStationNames(string systemname)
		{
			return GetStations(systemname).Select(s => s.Name).ToArray();
		}

		/// <summary>
		/// get all stations for a system from the main list
		/// </summary>
		/// <param name="systemname"></param>
		/// <returns></returns>
		public IEnumerable<EDStation> GetStations(string systemname)
		{
			return GetStations(systemname, DataSource.Data_Merged);
		}

		/// <summary>
		/// get all stations for a system from a particular list
		/// </summary>
		/// <param name="systemname"></param>
		/// <param name="wantedSource"></param>
		/// <returns></returns>
		private List<EDStation> GetStations(string systemname, DataSource wantedSource)
		{
			List<EDStation> retValue;

				EDSystem SystemData = m_Systems[(int)wantedSource].Find(x => x.Name.Equals(systemname, StringComparison.InvariantCultureIgnoreCase));

			if (SystemData != null)
				retValue = m_Stations[(int)wantedSource].FindAll(x => x.SystemId == SystemData.Id);
			else
				retValue = new List<EDStation>();

			return retValue;
		}

		/// <summary>
		/// check if there's a system with this name
		/// </summary>
		/// <param name="Systemname"></param>
		/// <returns></returns>
		public bool SystemExists(string Systemname)
		{
			return m_Systems[(int)DataSource.Data_Merged].Exists(x => x.Name.Equals(Systemname, StringComparison.InvariantCultureIgnoreCase));
		}

		/// <summary>
		/// get the data of a system
		/// </summary>
		/// <param name="Systemname"></param>
		/// <returns></returns>
		public EDSystem GetSystem(string Systemname)
		{
			return m_Systems[(int)DataSource.Data_Merged].Find(x => x.Name.Equals(Systemname, StringComparison.InvariantCultureIgnoreCase));
		}

		private EDStation GetSystemStation(string systemname, string stationName)
		{
			EDStation wantedStation = null;
			EDSystem wantedSystem = GetSystem(systemname);

			if (wantedSystem != null)
				wantedStation = m_Stations[(int)DataSource.Data_Merged].Find(x => (x.SystemId == wantedSystem.Id) && (x.Name.Equals(stationName)));

			return wantedStation;
		}

		/// <summary>
		/// get coordinates of a system
		/// </summary>
		/// <param name="systemname"></param>
		/// <returns></returns>
		public Point3D GetSystemCoordinates(string systemname)
		{
			Point3D retValue = null;

			if (!String.IsNullOrEmpty(systemname))
			{
				if (!m_cachedLocations.TryGetValue(systemname, out retValue))
				{
					EDSystem mySystem = m_Systems[(int)DataSource.Data_Merged].Find(x => x.Name.Equals(systemname, StringComparison.InvariantCultureIgnoreCase));

					if (mySystem != null)
					{
						retValue = mySystem.SystemCoordinates();
						m_cachedLocations.Add(systemname, retValue);
					}


				}
			}

			return retValue;
		}

		/// <summary>
		/// returns true if there are some unsaved changes in the own system data
		/// </summary>
		private bool ChangedSystems { get; set; }

		/// <summary>
		/// returns true if there are some unsaved changes in the own station data
		/// </summary>
		private bool ChangedStations
		{
			get
			{
				return m_changedStations;
			}
		}

		/// <summary>
		/// calculates the min, max and average market price for supply and demand of each commodity
		/// </summary>
		/// <returns></returns>
		private Dictionary<int, MarketData> CalculateAveragePrices()
		{
			Dictionary<int, MarketData> collectedData = new Dictionary<int, MarketData>();

			foreach (EDStation station in m_Stations[(int)(DataSource.Data_Merged)])
			{
				if (station.EdMarketDatas != null)
					foreach (EDMarketData stationCommodity in station.EdMarketDatas)
					{
						MarketData commodityData;
						if (!collectedData.TryGetValue(stationCommodity.CommodityId, out commodityData))
						{
							// add a new Marketdata-Object
							commodityData = new MarketData { Id = stationCommodity.CommodityId };
							collectedData.Add(commodityData.Id, commodityData);
						}

						if (stationCommodity.Demand != 0)
						{
							if (stationCommodity.BuyPrice > 0)
								commodityData.BuyPricesDemand.Add(stationCommodity.BuyPrice);

							if (stationCommodity.SellPrice > 0)
								commodityData.SellPricesDemand.Add(stationCommodity.SellPrice);

						}

						if (stationCommodity.Supply != 0)
						{
							if (stationCommodity.BuyPrice > 0)
								commodityData.BuyPricesSupply.Add(stationCommodity.BuyPrice);

							if (stationCommodity.SellPrice > 0)
								commodityData.SellPricesSupply.Add(stationCommodity.SellPrice);

						}
					}
			}

			return collectedData;
		}

		/// <summary>
		/// returns the base data of a commodity
		/// </summary>
		/// <param name="CommodityId"></param>
		/// <returns></returns>
		public EDCommoditiesExt GetCommodity(string commodityName)
		{
			return m_Commodities.Find(x => x.Name.Equals(commodityName, StringComparison.InvariantCultureIgnoreCase));
		}

		/// <summary>
		/// returns the base data of a commodity
		/// </summary>
		/// <param name="commodityId"></param>
		/// <returns></returns>
		public EDCommoditiesExt GetCommodity(int commodityId)
		{
			return m_Commodities.Find(x => x.Id == commodityId);
		}

		/// <summary>
		/// calculating the market prices and save them as min and max values for new OCR_ed data,
		/// if "FileName" is not nothing the new data will is saves in this file
		/// 
		/// </summary>
		/// <param name="fileName">name of the file to save to</param>
		private void CalculateNewPriceLimits(string fileName = "")
		{
			Dictionary<int, MarketData> collectedData = CalculateAveragePrices();

			foreach (MarketData Commodity in collectedData.Values)
			{
				EDCommoditiesExt CommodityBasedata = m_Commodities.Find(x => x.Id == Commodity.Id);

				if (CommodityBasedata != null)
				{
					if (Commodity.BuyPricesDemand.Count() > 0)
						CommodityBasedata.PriceWarningLevel_Demand_Buy_Low = Commodity.BuyPricesDemand.Min();
					else
						CommodityBasedata.PriceWarningLevel_Demand_Buy_Low = -1;

					if (Commodity.BuyPricesDemand.Count() > 0)
						CommodityBasedata.PriceWarningLevel_Demand_Buy_High = Commodity.BuyPricesDemand.Max();
					else
						CommodityBasedata.PriceWarningLevel_Demand_Buy_High = -1;

					if (Commodity.BuyPricesSupply.Count() > 0)
						CommodityBasedata.PriceWarningLevel_Supply_Buy_Low = Commodity.BuyPricesSupply.Min();
					else
						CommodityBasedata.PriceWarningLevel_Supply_Buy_Low = -1;

					if (Commodity.BuyPricesSupply.Count() > 0)
						CommodityBasedata.PriceWarningLevel_Supply_Buy_High = Commodity.BuyPricesSupply.Max();
					else
						CommodityBasedata.PriceWarningLevel_Supply_Buy_High = -1;

					if (Commodity.BuyPricesDemand.Count() > 0)
						CommodityBasedata.PriceWarningLevel_Demand_Sell_Low = Commodity.SellPricesDemand.Min();
					else
						CommodityBasedata.PriceWarningLevel_Demand_Sell_Low = -1;

					if (Commodity.SellPricesDemand.Count() > 0)
						CommodityBasedata.PriceWarningLevel_Demand_Sell_High = Commodity.SellPricesDemand.Max();
					else
						CommodityBasedata.PriceWarningLevel_Demand_Sell_High = -1;

					if (Commodity.SellPricesSupply.Count() > 0)
						CommodityBasedata.PriceWarningLevel_Supply_Sell_Low = Commodity.SellPricesSupply.Min();
					else
						CommodityBasedata.PriceWarningLevel_Supply_Sell_Low = -1;

					if (Commodity.SellPricesSupply.Count() > 0)
						CommodityBasedata.PriceWarningLevel_Supply_Sell_High = Commodity.SellPricesSupply.Max();
					else
						CommodityBasedata.PriceWarningLevel_Supply_Sell_High = -1;
				}
				else
				{
					Debug.Print("STOP");
				}

				//if (CommodityBasedata.Name == "Palladium")
				//    Debug.Print("STOP, doppelt belegt  " + CommodityBasedata.Name);
				//    Debug.Print("STOP");

				//Debug.Print("");
				//Debug.Print("");
				//Debug.Print(CommodityBasedata.Name + " :");
				//Debug.Print("Demand Buy Min \t\t" + Commodity.BuyPrices_Demand.Min().ToString("F0"));
				//Debug.Print("Demand Buy Average\t" + Commodity.BuyPrices_Demand.Average().ToString("F0") + " (" + Commodity.BuyPrices_Demand.Count() + " values)");
				//Debug.Print("Demand Buy Max\t\t" + Commodity.BuyPrices_Demand.Max().ToString("F0"));
				//Debug.Print("");
				//Debug.Print("Demand Sell Min\t\t" + Commodity.SellPrices_Demand.Min().ToString("F0"));
				//Debug.Print("Demand Sell Average\t" + Commodity.SellPrices_Demand.Average().ToString("F0") + " (" + Commodity.SellPrices_Demand.Count() + " values)");
				//Debug.Print("Demand Sell Max\t\t" + Commodity.SellPrices_Demand.Max().ToString("F0"));
				//Debug.Print("");
				//Debug.Print("Supply Buy Min\t\t" + Commodity.BuyPrices_Supply.Min().ToString("F0"));
				//Debug.Print("Supply Buy Average\t" + Commodity.BuyPrices_Supply.Average().ToString("F0") + " (" + Commodity.BuyPrices_Supply.Count() + " values)");
				//Debug.Print("Supply Buy Max\t\t" + Commodity.BuyPrices_Supply.Max().ToString("F0"));
				//Debug.Print("");
				//Debug.Print("Supply Sell Min\t\t" + Commodity.SellPrices_Supply.Min().ToString("F0"));
				//Debug.Print("Supply Sell Average\t" + Commodity.SellPrices_Supply.Average().ToString("F0") + " (" + Commodity.SellPrices_Supply.Count() + " values)");
				//Debug.Print("Supply Sell Max\t\t" + Commodity.SellPrices_Supply.Max().ToString("F0"));
			}

			if (!String.IsNullOrEmpty(fileName))
				SaveRNCommodityData(fileName, true);
		}

		/// <summary>
		/// loads the commodity data from the files
		/// </summary>
		/// <param name="eddbCommodityDatafile"></param>
		/// <param name="RNCommodityDatafile"></param>
		/// <param name="checkOnly"></param>
		private bool LoadCommodityData(string eddbCommodityDatafile, string RNCommodityDatafile, bool checkOnly = false)
		{
			bool notExisting = false;
			List<EDCommoditiesWarningLevels> rnCommodities;
			if (checkOnly)
				if (File.Exists(RNCommodityDatafile))
					return true;
				else
					return false;

			if (File.Exists(RNCommodityDatafile))
			{
				rnCommodities = JsonConvert.DeserializeObject<List<EDCommoditiesWarningLevels>>(File.ReadAllText(RNCommodityDatafile));
			}
			else
			{
				notExisting = true;
				rnCommodities = new List<EDCommoditiesWarningLevels>();
			}

			List<EDCommodities> eddbCommodities = JsonConvert.DeserializeObject<List<EDCommodities>>(File.ReadAllText(eddbCommodityDatafile));
			m_Commodities = EDCommoditiesExt.mergeCommodityData(eddbCommodities, rnCommodities);

			if (notExisting)
			{
				CalculateNewPriceLimits();
			}
			SaveRNCommodityData(RNCommodityDatafile, true);
			return true;
		}

		public void SetCommodities(IEnumerable<Commodity> commodities)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// saves the RN-specific commodity data to a file
		/// </summary>
		/// <param name="File">json-file to save</param>
		/// <param name="Stationtype"></param>
		public void SaveRNCommodityData(string filename, bool backupOldFile)
		{
			List<EDCommoditiesWarningLevels> warningLevels = EDCommoditiesExt.extractWarningLevels(m_Commodities);

			string newFile, backupFile;


			newFile = String.Format("{0}_new{1}", Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)), Path.GetExtension(filename));
			backupFile = String.Format("{0}_bak{1}", Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)), Path.GetExtension(filename));

			File.WriteAllText(newFile, JsonConvert.SerializeObject(warningLevels));
			// we delete the current file not until the new file is written without errors
			RotateSaveFiles(filename, newFile, backupFile, backupOldFile);
		}

		/// <summary>
		/// changes or adds a system to the "own" list and to the merged list
		/// EDDB basedata will not be changed
		/// </summary>
		/// <param name="currentSystemdata">systemdata to be added</param>
		/// <param name="oldSystemName"></param>
		public void ChangeAddSystem(EDSystem currentSystemdata, string oldSystemName = null)
		{
			EDSystem System;
			List<EDSystem> ownSystems = GetSystems(DataSource.Data_Own);
			int newSystemIndex;

			if (String.IsNullOrEmpty(oldSystemName.Trim()))
				oldSystemName = currentSystemdata.Name;

			if (!oldSystemName.Equals(currentSystemdata.Name))
			{
				// changing system name
				var existing = GetSystems(DataSource.Data_EDDB).Find(x => x.Name.Equals(oldSystemName, StringComparison.InvariantCultureIgnoreCase));
				if (existing != null)
					throw new Exception("It's not allowed to rename a EDDB system");
			}

			// 1st put the new values into our local list
			System = ownSystems.Find(x => x.Name.Equals(oldSystemName, StringComparison.CurrentCultureIgnoreCase));
			if (System != null)
			{
				// copy new values into existing system
				System.getValues(currentSystemdata);
			}
			else
			{
				// add as a new system to own data
				newSystemIndex = 0;

				if (ownSystems.Count > 0)
					newSystemIndex = ownSystems.Max(X => X.Id) + 1;

				ownSystems.Add(new EDSystem(newSystemIndex, currentSystemdata));

			}

			// 2nd put the new values into our merged list
			List<EDSystem> mergedSystems = GetSystems(DataSource.Data_Merged);

			System = mergedSystems.Find(x => x.Name.Equals(oldSystemName, StringComparison.CurrentCultureIgnoreCase));
			if (System != null)
			{
				// copy new values into existing system
				System.getValues(currentSystemdata);
			}
			else
			{
				// add as a new system to own data
				newSystemIndex = 0;

				if (mergedSystems.Count > 0)
					newSystemIndex = mergedSystems.Max(X => X.Id) + 1;
				mergedSystems.Add(new EDSystem(newSystemIndex, currentSystemdata));
			}

			if (m_cachedLocations.ContainsKey(oldSystemName))
				m_cachedLocations.Remove(oldSystemName);

			SaveStationData(OWN_STATIONS_DATAFILE, DataSource.Data_Own, true);
			SaveSystemData(OWN_SYSTEMS_DATAFILE, DataSource.Data_Own, true);
		}

		private List<EDSystem> GetSystems(DataSource dataSource)
		{
			return m_Systems[(int)dataSource];
		}

		private List<EDStation> GetStations(DataSource dataSource)
		{
			return m_Stations[(int)dataSource];
		}

		/// <summary>
		/// changes or adds a station to the "own" list and to the merged list
		/// EDDB basedata will not be changed
		/// </summary>
		/// <param name="systemname"></param>
		/// <param name="currentStationdata">station data to be added</param>
		/// <param name="oldStationName"></param>
		public void ChangeAddStation(string systemname, EDStation currentStationdata, string oldStationName = null)
		{
			EDSystem System;
			EDStation Station;
			int newStationIndex;

			if (String.IsNullOrEmpty(oldStationName.Trim()))
				oldStationName = currentStationdata.Name;

			List<EDSystem> ownSystems = GetSystems(DataSource.Data_Own);
			List<EDStation> ownStations = GetStations(DataSource.Data_Own);

			List<EDSystem> mergedSystems = GetSystems(DataSource.Data_Merged);
			List<EDStation> mergedStations = GetStations(DataSource.Data_Merged);

			// 1st put the new values into our local list
			System = ownSystems.Find(x => x.Name.Equals(systemname, StringComparison.CurrentCultureIgnoreCase));
			if (System == null)
			{
				// own system is not existing, look for a EDDN system
				System = mergedSystems.Find(x => x.Name.Equals(systemname, StringComparison.CurrentCultureIgnoreCase));

				if (System == null)
					throw new Exception("System in merged list required but not existing");

				// get a new local system id 
				int newSystemIndex = 0;
				if (m_Systems[(int)DataSource.Data_Own].Count > 0)
					newSystemIndex = m_Systems[(int)DataSource.Data_Own].Max(X => X.Id) + 1;

				// and add the EDDN system as a new system to the local list
				System = new EDSystem(newSystemIndex, System);
				ownSystems.Add(System);

				// get a new station index
				newStationIndex = 0;
				if (ownStations.Count > 0)
					newStationIndex = ownStations.Max(X => X.Id) + 1;

				// add the new station in the local station dictionary
				ownStations.Add(new EDStation(newStationIndex, newSystemIndex, currentStationdata));
			}
			else
			{
				// the system is existing in the own dictionary 
				Station = ownStations.Find(x => (x.Name.Equals(oldStationName, StringComparison.CurrentCultureIgnoreCase)) &&
																		(x.SystemId == System.Id));
				if (Station != null)
				{
					// station is already existing, copy new values into existing Station
					Station.getValues(currentStationdata);
				}
				else
				{
					// station is not existing, get a new station index
					newStationIndex = 0;
					if (ownStations.Count > 0)
						newStationIndex = ownStations.Max(X => X.Id) + 1;

					// add the new station in the local station dictionary
					ownStations.Add(new EDStation(newStationIndex, System.Id, currentStationdata));
				}
			}

			// 1st put the new values into the merged list
			System = mergedSystems.Find(x => x.Name.Equals(systemname, StringComparison.CurrentCultureIgnoreCase));
			if (System == null)
			{
				// system is not exiting in merged list
				System = ownSystems.Find(x => x.Name.Equals(systemname, StringComparison.CurrentCultureIgnoreCase));

				if (System == null)
					throw new Exception("System in own list required but not existing");

				// get a new merged system id 
				int newSystemIndex = m_Systems[(int)DataSource.Data_Merged].Max(X => X.Id) + 1;

				// and add system to the merged list
				System = new EDSystem(newSystemIndex, System);
				mergedSystems.Add(System);

				// get a new station index
				newStationIndex = 0;
				if (mergedStations.Count > 0)
					newStationIndex = mergedStations.Max(X => X.Id) + 1;

				// add the new station in the local station dictionary
				mergedStations.Add(new EDStation(newStationIndex, newSystemIndex, currentStationdata));
			}
			else
			{
				// the system is existing in the merged dictionary 
				Station = mergedStations.Find(x => (x.Name.Equals(oldStationName, StringComparison.CurrentCultureIgnoreCase)) &&
																		(x.SystemId == System.Id));
				if (Station != null)
				{
					// station is already existing, copy new values into existing Station
					Station.getValues(currentStationdata);
				}
				else
				{
					// station is not existing, get a new station index
					newStationIndex = 0;
					if (mergedStations.Count > 0)
						newStationIndex = mergedStations.Max(X => X.Id) + 1;

					// add the new station in the merged station dictionary
					mergedStations.Add(new EDStation(newStationIndex, System.Id, currentStationdata));
				}
			}

			if (m_cachedStationDistances.ContainsKey(oldStationName))
				m_cachedStationDistances.Remove(oldStationName);

			SaveStationData(OWN_STATIONS_DATAFILE, DataSource.Data_Own, true);
			SaveSystemData(@"./Data/Systems_own.json", DataSource.Data_Own, true);
		}

		/// <summary>
		/// using the direct EDDB format 
		/// (see http://eddb.io/api)
		/// </summary>
		public virtual void ImportSystemLocations()
		{
			// read file into a string and deserialize JSON to a type
			try
			{
				EventBus.InitializationStart("create milkyway...");
				DownloadDataFiles();
				// 1. load the EDDN data
				{
					bool needPriceCalculation = !LoadCommodityData(EDDB_COMMODITIES_DATAFILE, REGULATEDNOISE_COMMODITIES_DATAFILE, true);

					if (needPriceCalculation || ApplicationContext.RegulatedNoiseSettings.LoadStationsJSON)
					{
						EventBus.InitializationStart("loading stations from <stations.json> (calculation of plausibility limits required)");
						LoadStationData(EDDB_STATIONS_FULL_DATAFILE, DataSource.Data_EDDB, false);
						EventBus.InitializationCompleted("loading stations from <stations.json> (calculation of plausibility limits required)");
						EventBus.InitializationProgress("(" + GetStations(DataSource.Data_EDDB).Count + " stations loaded)");
					}
					else
					{
						// look which stations-file we can get
						if (File.Exists(EDDB_STATIONS_LITE_DATAFILE))
						{
							EventBus.InitializationStart("loading stations from <stations_lite.json>");
							LoadStationData(EDDB_STATIONS_LITE_DATAFILE, DataSource.Data_EDDB, false);
							EventBus.InitializationCompleted("loading stations from <stations_lite.json>");
							EventBus.InitializationProgress("(" + GetStations(DataSource.Data_EDDB).Count + " stations loaded)");
						}
						else
						{
							EventBus.InitializationStart("loading stations from <stations.json>");
							LoadStationData(EDDB_STATIONS_FULL_DATAFILE, DataSource.Data_EDDB, false);
							EventBus.InitializationCompleted("loading stations from <stations.json>");
							EventBus.InitializationProgress("(" + GetStations(DataSource.Data_EDDB).Count + " stations loaded)");
						}
					}

					// load the systems
					EventBus.InitializationStart("...loading systems from <systems.json>...");
					LoadSystemData(EDDB_SYSTEMS_DATAFILE, DataSource.Data_EDDB, false);
					EventBus.InitializationCompleted("loading systems from <systems.json>");
					EventBus.InitializationProgress("(" + GetSystems(DataSource.Data_EDDB).Count + " systems loaded)");
				}

				// 2. load own local data
				EventBus.InitializationStart("loading own stations from <stations_own.json>");
				LoadStationData(OWN_STATIONS_DATAFILE, DataSource.Data_Own, true);
				EventBus.InitializationCompleted("loading stations from <stations_own.json>");
				EventBus.InitializationProgress("(" + GetStations(DataSource.Data_Own).Count + " stations loaded)");

				EventBus.InitializationStart("loading own systems from <systems_own.json>");
				LoadSystemData(OWN_SYSTEMS_DATAFILE, DataSource.Data_Own, true);
				EventBus.InitializationCompleted("loading own systems from <systems_own.json>)");
				EventBus.InitializationProgress(GetSystems(DataSource.Data_Own).Count + " systems loaded)");

				EventBus.InitializationStart("merging data");
				if (MergeData())
				{
					SaveStationData(OWN_STATIONS_DATAFILE, DataSource.Data_Own, true);
					SaveSystemData(OWN_SYSTEMS_DATAFILE, DataSource.Data_Own, true);
				}
				EventBus.InitializationCompleted("merging data");
				EventBus.InitializationStart("loading commodity data from <commodities.json>");
				LoadCommodityData(EDDB_COMMODITIES_DATAFILE, REGULATEDNOISE_COMMODITIES_DATAFILE);
				EventBus.InitializationCompleted("loading commodity data from <commodities.json>");
				CalculateAveragePrices();
				EventBus.InitializationProgress("create milkyway...<OK>");
			}
			catch (Exception ex)
			{
				throw new InitializationException("Error while reading system and station data", ex);
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
			if (!File.Exists(EDDB_STATIONS_FULL_DATAFILE))
			{
				tasks.Add(Task.Run(() => DownloadDataFile(new Uri(EDDB_STATIONS_FULL_URL), EDDB_STATIONS_FULL_DATAFILE,
					 "eddb stations full data")));
			}
			if (!File.Exists(EDDB_STATIONS_LITE_DATAFILE))
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

		private class MarketData
		{
			public MarketData()
			{
				Id = -1;
				BuyPricesDemand = new List<int>();
				BuyPricesSupply = new List<int>();
				SellPricesDemand = new List<int>();
				SellPricesSupply = new List<int>();
			}

			public int Id;
			public readonly List<int> BuyPricesDemand;
			public readonly List<int> BuyPricesSupply;
			public readonly List<int> SellPricesDemand;
			public readonly List<int> SellPricesSupply;
		}

		public PlausibilityState IsImplausible(MarketDataRow marketData, bool simpleEDDNCheck)
		{
			EDCommoditiesExt commodityData =
				 GetCommodity(
                    ApplicationContext.CommoditiesLocalisation.TranslateInEnglish(marketData.CommodityName));

			if (marketData.CommodityName == "Panik")
				Debug.Print("STOP");

			PlausibilityState plausibility = new PlausibilityState(true);

			if (commodityData != null)
			{
				if (marketData.SupplyLevel.HasValue && marketData.DemandLevel.HasValue)
				{
					// demand AND supply !?
					plausibility = new PlausibilityState(false, "both demand and supply");
				}
				else if ((marketData.SellPrice <= 0) && (marketData.BuyPrice <= 0))
				{
					// both on 0 is not plausible
					plausibility = new PlausibilityState(false, "nor sell, nor buy price");
				}
				else if (marketData.SupplyLevel.HasValue || (simpleEDDNCheck && (marketData.Stock > 0)))
				{
					if (marketData.BuyPrice <= 0)
					{
						plausibility = new PlausibilityState(false, "buy price not provided when demand available");
					}
					// check supply data             
					else if (((commodityData.PriceWarningLevel_Supply_Sell_Low >= 0) &&
						  (marketData.SellPrice < commodityData.PriceWarningLevel_Supply_Sell_Low)) ||
						 ((commodityData.PriceWarningLevel_Supply_Sell_High >= 0) &&
						  (marketData.SellPrice > commodityData.PriceWarningLevel_Supply_Sell_High)))
					{
						// sell price is out of range
						plausibility = new PlausibilityState(false, "sell price out of supply prices warn level "
							 + marketData.SellPrice
							 + " [" + commodityData.PriceWarningLevel_Supply_Sell_Low + "," + commodityData.PriceWarningLevel_Supply_Sell_High + "]");
					}
					else if (((commodityData.PriceWarningLevel_Supply_Buy_Low >= 0) &&
						  (marketData.BuyPrice < commodityData.PriceWarningLevel_Supply_Buy_Low)) ||
						 ((commodityData.PriceWarningLevel_Supply_Buy_High >= 0) &&
						  (marketData.BuyPrice > commodityData.PriceWarningLevel_Supply_Buy_High)))
					{
						// buy price is out of range
						plausibility = new PlausibilityState(false, "buy price out of supply prices warn level "
																				  + marketData.SellPrice
																				  + " [" +
																				  commodityData.PriceWarningLevel_Supply_Buy_Low +
																				  "," +
																				  commodityData.PriceWarningLevel_Supply_Buy_High +
																				  "]");
					}
					if (marketData.Stock <= 0)
					{
						// no supply quantity
						plausibility = new PlausibilityState(false, "supply not provided");
					}
				}
				else if (marketData.DemandLevel.HasValue || (simpleEDDNCheck && (marketData.Demand > 0)))
				{
					// check demand data
					if (marketData.SellPrice <= 0)
					{
						// at least the sell price must be present
						plausibility = new PlausibilityState(false, "sell price not provided when supply available");
					}
					else if (((commodityData.PriceWarningLevel_Demand_Sell_Low >= 0) &&
						  (marketData.SellPrice < commodityData.PriceWarningLevel_Demand_Sell_Low)) ||
						 ((commodityData.PriceWarningLevel_Demand_Sell_High >= 0) &&
						  (marketData.SellPrice > commodityData.PriceWarningLevel_Demand_Sell_High)))
					{
						// buy price is out of range
						plausibility = new PlausibilityState(false, "sell price out of demand prices warn level "
																				  + marketData.SellPrice
																				  + " [" +
																				  commodityData.PriceWarningLevel_Demand_Sell_Low +
																				  "," +
																				  commodityData.PriceWarningLevel_Demand_Sell_High +
																				  "]");
					}
					else if (marketData.BuyPrice > 0 && (((commodityData.PriceWarningLevel_Demand_Buy_Low >= 0) &&
						  (marketData.BuyPrice < commodityData.PriceWarningLevel_Demand_Buy_Low)) ||
						 ((commodityData.PriceWarningLevel_Demand_Buy_High >= 0) &&
						  (marketData.BuyPrice > commodityData.PriceWarningLevel_Demand_Buy_High))))
					{
						// buy price is out of range
						plausibility = new PlausibilityState(false, "buy price out of supply prices warn level "
																				  + marketData.BuyPrice
																				  + " [" +
																				  commodityData.PriceWarningLevel_Demand_Buy_Low +
																				  "," +
																				  commodityData.PriceWarningLevel_Demand_Buy_High +
																				  "]");
					}

					if (marketData.Demand <= 0)
					{
						// no demand quantity
						plausibility = new PlausibilityState(false, "demand not provided");
					}
				}
				else
				{
					// nothing ?!
					plausibility = new PlausibilityState(false, "nor demand,nor supply provided");
				}
			}
            else
            {
	            plausibility = new PlausibilityState(false, "unknown commodity");
            }
			return plausibility;
		}

		public double DistanceInLightYears(string system1, string system2)
		{
			double distance;
			string systemPairKey = GetSystemDistanceKey(system1, system2);
			if (!_systemDistances.TryGetValue(systemPairKey, out distance))
			{
				distance = SystemDistance(GetSystemCoordinates(system1), GetSystemCoordinates(system2));
				_systemDistances.Add(systemPairKey, distance);
			}
			return distance;
		}

		private string GetSystemDistanceKey(string system1, string system2)
		{
			if (String.IsNullOrWhiteSpace(system1)) throw new ArgumentException("system1");
			if (String.IsNullOrWhiteSpace(system2)) throw new ArgumentException("system2");
			return String.Compare(system1, system2, StringComparison.InvariantCultureIgnoreCase) > 0 ? system2.Trim().ToUpper() + "_" + system1.Trim().ToUpper() : system1.Trim().ToUpper() + "_" + system2.Trim().ToUpper();
		}

		private static double SystemDistance(Point3D currentSystemLocation, Point3D remoteSystemLocation)
		{
			if (currentSystemLocation == null || remoteSystemLocation == null) return NO_DISTANCE;
			double xDelta = currentSystemLocation.X - remoteSystemLocation.X;
			double yDelta = currentSystemLocation.Y - remoteSystemLocation.Y;
			double zDelta = currentSystemLocation.Z - remoteSystemLocation.Z;
			return Math.Sqrt(Math.Pow(xDelta, 2) + Math.Pow(yDelta, 2) + Math.Pow(zDelta, 2));
		}
	}
}
