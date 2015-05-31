#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 25.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Elite.Insight.Core;
using Elite.Insight.Core.DomainModel;
using Elite.Insight.Core.Helpers;
using Elite.Insight.DataProviders;
using Elite.Insight.DataProviders.Eddb;
using Elite.Insight.Test.DataProviders;
using NUnit.Framework;

namespace Elite.Insight.Test.Db
{
	[TestFixture]
	public class InsightDbTest
	{
		private const string TEST_DB_PATH = "elite-insight.test.db";

		[Test]
		[Ignore]
		public void i_can_insert_systems()
		{
			var db = new InsightDb(TEST_DB_PATH);
			DataModel model = new DataModel(new TestLocalizer(), new TestValidator());
			EddbDataProvider eddb = new EddbDataProvider();
			eddb.ImportSystems(model.StarMap);
			//eddb.ImportData(model, false, ImportMode.Import);
			////CsvDataProvider.RetrieveMarketData(new FileInfo("Autosave.csv"), model);
			db.StoreSystems(model.StarMap);
			using (var cx = db.NewConnection())
			{
				DisplaySystems(cx);
			}
		}

		[Test]
		[Ignore]
		public void i_can_initialize_db_from_eddb()
		{
			var db = new InsightDb(TEST_DB_PATH);
			DataModel model = new DataModel(new TestLocalizer(), new TestValidator());
			EddbDataProvider eddb = new EddbDataProvider();
			eddb.DownloadDataFiles();
			eddb.ImportSystems(model.StarMap);
			eddb.ImportStations(model.StarMap);
			eddb.ImportCommodities(model.Commodities);
			////CsvDataProvider.RetrieveMarketData(new FileInfo("Autosave.csv"), model);
			db.StoreSystems(model.StarMap);
			db.StoreStations(model.StarMap.Stations);
			db.StoreCommodities(model.Commodities);
		}

		[Test]
		public void i_can_load_systems()
		{
			var db = new InsightDb(TEST_DB_PATH);
			DataModel model = new DataModel(new TestLocalizer(), new TestValidator());
			db.LoadModel(model, 50);
			Debug.WriteLine(model.StarMap.Count +" system(s) loaded");
			foreach (StarSystem starSystem in model.StarMap)
			{
				Debug.WriteLine(starSystem);
			}
		}

		[Test]
		public void i_can_load_market_data()
		{
			var db = new InsightDb(TEST_DB_PATH);
			using (var cx = db.NewConnection())
			{
				using (var cmd = cx.CreateCommand())
				{
					cmd.CommandText = "select max(lastUpdate) from marketdata";
					//var parameter = cmd.CreateParameter();
					//parameter.ParameterName = "@lastUpdate";
					//parameter.Value = DateTime.Today.AddDays(-25).ToUnixTimestamp();
					//cmd.Parameters.Add(parameter);
					Debug.WriteLine(UnixTimeStamp.ToDateTime((long) cmd.ExecuteScalar()));
				}
			}
		}

		[Test]
		[Ignore]
		public void i_can_insert_commodities()
		{
			var db = new InsightDb(TEST_DB_PATH);
			DataModel model = new DataModel(new TestLocalizer(), new TestValidator());
			EddbDataProvider eddb = new EddbDataProvider();
			eddb.ImportCommodities(model.Commodities);
			//eddb.ImportData(model, false, ImportMode.Import);
			////CsvDataProvider.RetrieveMarketData(new FileInfo("Autosave.csv"), model);
			db.StoreCommodities(model.Commodities);
			using (var cx = db.NewConnection())
			{
				DisplayCommodities(cx);
			}
		}

		[Test]
		[Ignore]
		public void i_can_insert_stations()
		{
			var db = new InsightDb(TEST_DB_PATH);
			//using (var cx = db.NewConnection())
			//{
			//	using (var cmd = cx.CreateCommand())
			//	{
			//		cmd.CommandText = "drop table stations";
			//		cmd.ExecuteNonQuery();
			//	}
			//	using (var cmd = cx.CreateCommand())
			//	{
			//		cmd.CommandText = "drop table marketdata";
			//		cmd.ExecuteNonQuery();
			//	}
			//}
			//db.InitDb();
			//DataModel model = new DataModel(new TestLocalizer(), new TestValidator());
			//EddbDataProvider eddb = new EddbDataProvider();
			//eddb.ImportSystems(model.StarMap);
			//eddb.ImportStations(model.StarMap);
			//eddb.ImportData(model, false, ImportMode.Import);
			////CsvDataProvider.RetrieveMarketData(new FileInfo("Autosave.csv"), model);
			//db.StoreStations(model.StarMap.Stations);
			using (var cx = db.NewConnection())
			{
				DisplayStations(cx);
			}
		}

		[Test]
		[Ignore]
		public void Normalize()
		{
			var db = new InsightDb(TEST_DB_PATH);
			using (var cx = db.NewConnection())
			{
				using (var cmd = cx.CreateCommand())
				{
					cmd.CommandText = "update stations set ";
					cmd.ExecuteNonQuery();
				}
			}
		}

		[Test]
		[Ignore]
		public void i_can_insert_marketdatas()
		{
			var db = new InsightDb(TEST_DB_PATH);
			//using (var cx = db.NewConnection())
			//{
			//	using (var cmd = cx.CreateCommand())
			//	{
			//		cmd.CommandText = "drop index IF EXISTS marketdata_staion_commodity_idx";
			//		cmd.ExecuteNonQuery();
			//	}
			//	using (var cmd = cx.CreateCommand())
			//	{
			//		cmd.CommandText = "drop index IF EXISTS marketdata_commodity_idx";
			//		cmd.ExecuteNonQuery();
			//	}
			//	using (var cmd = cx.CreateCommand())
			//	{
			//		cmd.CommandText = "drop table IF EXISTS marketdata";
			//		cmd.ExecuteNonQuery();
			//	}
			//}
			//db.InitDb();
			//using (var cx = db.NewConnection())
			//{
			//	DisplayCommodities(cx);
			//	DisplayMarketDatas(cx);
			//}
			DataModel model = new DataModel(new TestLocalizer(), new TestValidator());
			EddbDataProvider eddb = new EddbDataProvider();
			eddb.ImportData(model, false, ImportMode.Import);
			CsvDataProvider.RetrieveMarketData(new FileInfo("Autosave.csv"), model);
			db.StoreMarketData(model.GalacticMarket);
			using (var cx = db.NewConnection())
			{
				DisplayMarketDatas(cx);
			}
		}

		private void DisplayStations(SQLiteConnection cx)
		{
			Display(cx, "SELECT * from stations LIMIT 100");
		}

		private void DisplayMarketDatas(SQLiteConnection cx)
		{
			Display(cx, "SELECT * from marketdata LIMIT 100");
		}

		private static void Display(SQLiteConnection cx, string request)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = request;
				using (var reader = cmd.ExecuteReader())
				{
					Display(reader);
				}
			}
		}

		private static void Display(IDataReader reader)
		{
			bool hasRows = false;
			while (reader.Read())
			{
				hasRows = true;
				for (int i = 0; i < reader.FieldCount; i++)
				{
					Debug.Write(reader[i] + ", ");
				}
				Debug.WriteLine("");
			}
			Debug.WriteIf(!hasRows, "No row.");
		}

		private void DisplayCommodities(SQLiteConnection cx)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = "SELECT id,category,name from commodities order by category,name";
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						Debug.WriteLine("{1} #{0} [{2}]",
							reader[0],
							reader[1],
							reader[2]);
					}
				}
			}
		}

		private long ResetIds(SQLiteConnection cx)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = @"UPDATE constants
												SET nextId = 0";
				return (long)cmd.ExecuteScalar();
			}
		}

		private static void DisplaySystems(SQLiteConnection cx)
		{
			using (var cmd = cx.CreateCommand())
			{
				cmd.CommandText = "SELECT id,name,x,y,z,lastUpdate,datetime(lastUpdate, 'unixepoch') from systems";
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						Debug.WriteLine("{1} #{0} [{2},{3},{4}] {5}: {6}",
							reader[0],
							reader[1],
							reader[2],
							reader[3],
							reader[4],
							reader[5],
							reader[6]);
					}
				}
			}
		}

		//		private static void InsertTestSystems(SQLiteConnection cx)
		//		{
		//			using (var cmd = cx.CreateCommand())
		//			{
		//				var request = new StringBuilder(@"INSERT INTO systems
		//												(id,name,x,y,z,lastUpdate)
		//												VALUES");
		//				int i = 0;
		//				for (; i < 10; ++i)
		//				{
		//					request.AppendLine("(" + i + ",'system_" + i.ToString("00") + "'," + (i + .2).ToString(CultureInfo.InvariantCulture) +
		//											 "," + (i + .3).ToString(CultureInfo.InvariantCulture) + "," +
		//											 (i + .4).ToString(CultureInfo.InvariantCulture) + "," + ToParameter(DateTime.Now) + "),");
		//				}
		//				++i;
		//				request.AppendLine("(" + i + ",'system_" + i.ToString("00") + "'," + (i + .2).ToString(CultureInfo.InvariantCulture) +
		//										 "," + (i + .3).ToString(CultureInfo.InvariantCulture) + "," +
		//										 (i + .4).ToString(CultureInfo.InvariantCulture) + "," + ToParameter(DateTime.Now) + ")");
		//				cmd.CommandText = request.ToString();
		//				Debug.WriteLine(cmd.CommandText);
		//				cmd.ExecuteNonQuery();
		//			}
		//		}
	}
}