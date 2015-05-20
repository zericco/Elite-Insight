#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 06.05.2015
// ///
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RegulatedNoise.Core;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.EDDB_Data;
using RegulatedNoise.EliteInteractions;
using RegulatedNoise.Enums_and_Utility_Classes;

namespace RegulatedNoise
{
	internal static class ApplicationContext
	{
		public const string LOGS_PATH = "Logs";

		static ApplicationContext()
		{
			Trace.UseGlobalLock = false;
			Trace.Listeners.Add(new TextWriterTraceListener(Path.Combine(LOGS_PATH, "RegulatedNoise-" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "-" + Guid.NewGuid() + ".log")) { Name = "RegulatedNoise" });
			Trace.AutoFlush = true;
			Trace.TraceInformation("Application context set up");
		}

		private static RegulatedNoiseSettings _settings;
		public static RegulatedNoiseSettings RegulatedNoiseSettings
		{
			get
			{
				if (_settings == null)
				{
					_settings = RegulatedNoiseSettings.LoadSettings();
					_settings.PropertyChanged += (sender, args) => _settings.Save();
				}
				return _settings;
			}
		}

		private static EDMilkyway _milkyway;
		private static dsCommodities _commoditiesLocalisation;
        private static Eddn _eddn;
		private static GalacticMarket _galacticMarket;
		private static LogFilesScanner _eliteLogFilesScanner;
		private static DataModel _model;

		public static EDMilkyway Milkyway
		{
			get
			{
				if (_milkyway == null)
				{
					_milkyway = new EDMilkyway();
					_milkyway.ImportSystemLocations();
					Trace.TraceInformation("  - system locations imported");
				}
				return _milkyway;
			}
		}

		public static dsCommodities CommoditiesLocalisation
		{
			get
			{
				if (_commoditiesLocalisation == null)
				{
					_commoditiesLocalisation = new dsCommodities();
					_commoditiesLocalisation.ReadXml(RegulatedNoiseSettings.COMMODITIES_LOCALISATION_FILEPATH);
				}
				return _commoditiesLocalisation;
			}
		}

        public static Eddn Eddn
		{
			get
			{
				if (_eddn == null)
				{
					EventBus.InitializationStart("prepare EDDN interface");
                    _eddn = new Eddn(CommoditiesLocalisation, RegulatedNoiseSettings);
					Trace.TraceInformation("  - EDDN object created");
					if (RegulatedNoiseSettings.StartListeningEddnOnLoad)
					{
						EventBus.InitializationStart("subscribing to EDDN");
						Eddn.Subscribe();
						EventBus.InitializationCompleted("now listening to EDDN");
					}
					EventBus.InitializationCompleted("prepare EDDN interface");

				}
				return _eddn;
			}
		}

		public static GalacticMarket GalacticMarket
		{
			get
			{
				if (_galacticMarket == null)
					_galacticMarket = new GalacticMarket();
				return _galacticMarket;
			}
			set { _galacticMarket = value; }
		}

		public static LogFilesScanner EliteLogFilesScanner
		{
			get
			{
				if (_eliteLogFilesScanner == null)
				{
					_eliteLogFilesScanner = new LogFilesScanner(RegulatedNoiseSettings);
				}
				return _eliteLogFilesScanner;
			}
			set { _eliteLogFilesScanner = value; }
		}

		public static DataModel Model
		{
			get
			{
				if (_model == null)
					_model = new DataModel(new dsCommodities(), new MarketDataValidator());
				return _model;
			}
			set { _model = value; }
		}
	}

	internal interface IStarMap
	{
		PlausibilityState IsImplausible(MarketDataRow marketDataRow, bool simpleCheck);
		IEnumerable<Commodity> CloneCommodities();
		double DistanceInLightYears(string originSystem, string targetSystem);
		void SetCommodities(IEnumerable<Commodity> commodities);
		void SaveRNCommodityData(string jsonFilepath, bool backupOldFile);
		string[] GetStationNames(string system);
		int? GetStationDistance(string systemName, string stationName);
		bool SystemExists(string systemName);
		
		IEnumerable<Station> GetStations();
	
		IEnumerable<Station> GetStations(string systemName);
		IEnumerable<StarSystem> GetSystems();
		
		StarSystem GetSystem(string systemname);
		Station GetStation(string systemname, string stationname);
		
		void ChangeAddSystem(StarSystem existing, string actual);
		void ChangeAddStation(string currentSystemName, Station currentStationdata, string oldStationName);
	}
}