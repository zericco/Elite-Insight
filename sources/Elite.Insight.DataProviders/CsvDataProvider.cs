#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 24.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Diagnostics;
using System.IO;
using Elite.Insight.Core.DomainModel;
using Elite.Insight.Core.Messaging;

namespace Elite.Insight.DataProviders
{
	public class CsvDataProvider
	{
		private readonly FileInfo _filepath;

		public CsvDataProvider(FileInfo filepath)
		{
			_filepath = filepath;
			if (filepath == null)
			{
				throw new ArgumentNullException("filepath");
			}
			if (!filepath.Exists) throw new FileNotFoundException("csv file does not exist", filepath.FullName);
		}

		public static void RetrieveMarketData(FileInfo filepath, DataModel model)
		{
			new CsvDataProvider(filepath).RetrieveMarketData(model);
		}

		public void RetrieveMarketData(DataModel model)
		{
			int correlationId = EventBus.Start("parsing csv data");
			using (var reader = new StreamReader(File.OpenRead(_filepath.FullName)))
			{
				string header = reader.ReadLine();
				if (header != null && !header.StartsWith("System;"))
				{
					EventBus.Alert("Error: '" + _filepath.FullName + "' is unreadable or in an old format.  Skipping...");
				}
				else
				{
					int lineCount = 1; //header
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();
						try
						{
							++lineCount;
							model.GalacticMarket.Update(MarketDataRow.ReadCsv(line));
							EventBus.Progress("parsing csv data", lineCount, lineCount + 10);
						}
						catch (Exception ex)
						{
							Trace.TraceError("unable to parse csv row: " + line + Environment.NewLine + ex);
						}
					}
				}
			}
			EventBus.Completed("parsing csv data");
		}
	}
}