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
using System.Diagnostics;
using System.IO;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.Core.EliteInteractions;
using RegulatedNoise.Core.Messaging;
using RegulatedNoise.Enums_and_Utility_Classes;

namespace RegulatedNoise
{
	internal static class ApplicationContext
	{
		public const string LOGS_PATH = "Logs";

		static ApplicationContext()
		{
			Trace.UseGlobalLock = false;
#if(DEBUG)
			Trace.Listeners.Add(new TextWriterTraceListener(Path.Combine(LOGS_PATH, "RegulatedNoise.log")) { Name = "RegulatedNoise" });
#else
			Trace.Listeners.Add(new TextWriterTraceListener(Path.Combine(LOGS_PATH, "RegulatedNoise-" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "-" + Guid.NewGuid() + ".log")) { Name = "RegulatedNoise" });
#endif			
			Trace.AutoFlush = true;
			Trace.TraceInformation("Application context set up");
		
			_settings = new Lazy<RegulatedNoiseSettings>(() =>
			{
				var settings = RegulatedNoiseSettings.LoadSettings();
				settings.PropertyChanged += (sender, args) => settings.Save();
				return settings;
			});
			_commoditiesLocalisation = new Lazy<dsCommodities>(() =>
			{
				var localizer = new dsCommodities();
				localizer.ReadXml(
					RegulatedNoiseSettings.COMMODITIES_LOCALISATION_FILEPATH);
				return localizer;
			});
			_eddn = new Lazy<Eddn>(() =>
			{
				EventBus.Start("prepare EDDN interface");
				var eddn = new Eddn(CommoditiesLocalisation, RegulatedNoiseSettings);
				Trace.TraceInformation("  - EDDN object created");
				if (RegulatedNoiseSettings.StartListeningEddnOnLoad)
				{
					EventBus.Start("subscribing to EDDN");
					Eddn.Subscribe();
					EventBus.Completed("now listening to EDDN");
				}
				EventBus.Completed("prepare EDDN interface");
				return eddn;
			});
			_eliteLogFilesScanner = new Lazy<LogFilesScanner>(() => new LogFilesScanner(RegulatedNoiseSettings.ProductsPath));
			_model = new Lazy<DataModel>(() => new DataModel(CommoditiesLocalisation, new MarketDataValidator()));
		}

		private static readonly Lazy<dsCommodities> _commoditiesLocalisation;
		private static readonly Lazy<Eddn> _eddn;
		private static readonly Lazy<LogFilesScanner> _eliteLogFilesScanner;
		private static readonly Lazy<DataModel> _model;
		private static readonly Lazy<RegulatedNoiseSettings> _settings;

		public static RegulatedNoiseSettings RegulatedNoiseSettings
		{
			get
			{
				return _settings.Value;
			}
		}

		public static dsCommodities CommoditiesLocalisation
		{
			get
			{
				return _commoditiesLocalisation.Value;
			}
		}

		public static Eddn Eddn
		{
			get
			{
				return _eddn.Value;
			}
		}

		public static LogFilesScanner EliteLogFilesScanner
		{
			get
			{
				return _eliteLogFilesScanner.Value;
			}
		}

		public static DataModel Model
		{
			get
			{
				return _model.Value;
			}
		}
	}
}