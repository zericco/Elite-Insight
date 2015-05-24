#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 24.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RegulatedNoise.Annotations;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.Core.Messaging;
using ZeroMQ;

namespace RegulatedNoise.EDDN.v1
{
	public class EddnPublisher : IDisposable
	{
		private const string POST_URL = "http://eddn-gateway.elite-markets.net:8080/upload/";
		private const string EDDN_SCHEMA_URL = "http://schemas.elite-markets.net/eddn/commodity/1";
		private const string EDDN_SCHEMA_TEST_URL = EDDN_SCHEMA_URL + "/test";
		private const string SOFTWARE_NAME = "SpaceFindED";
		private readonly BlockingCollection<EddnMessage.MessageContent> _pendingMessage;
		private Task _publishTask;
		private readonly object _startingtask = new object();

		public bool UseTestSchema { get; set; }

		public string CurrentUser { get; set; }

		public string CurrentVersion { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether [test mode].
		/// </summary>
		/// <value>
		///   <c>true</c> if [test mode]; otherwise, <c>false</c>.
		/// </value>
		public bool TestMode { get; set; }

		public EddnPublisher()
		{
			_pendingMessage = new BlockingCollection<EddnMessage.MessageContent>();
			EventBus.InitializationStart("initializing Eddn publisher");
		}

		public void Publish(MarketDataRow commodityData)
		{
			if (commodityData == null)
			{
				throw new ArgumentNullException("commodityData");
			}
			if (_publishTask == null)
			{
				lock (_startingtask)
				{
					if (_publishTask == null)
						_publishTask = Task.Factory.StartNew(PublishToEddn,
							new CancellationToken(),
							TaskCreationOptions.LongRunning,
							TaskScheduler.Default);
				}
			}
			Debug.Print("Items in Queue : " + _pendingMessage.Count);
			_pendingMessage.Add(ToMessageContent(commodityData));
			Debug.Print("Items in Queue : " + _pendingMessage.Count + ", added : " + commodityData);
		}

		private void PublishToEddn()
		{
			foreach (EddnMessage.MessageContent messageContent in _pendingMessage.GetConsumingEnumerable())
			{
				try
				{
					Publish(messageContent);
				}
				catch (Exception ex)
				{
					Trace.TraceError("eddn publication failure: " + ex);
				}
			}
		}

		private static EddnMessage.MessageContent ToMessageContent(MarketDataRow commodityData)
		{
			return new EddnMessage.MessageContent()
			{
				CommodityName = commodityData.CommodityName
				,BuyPrice = commodityData.BuyPrice > 0 ? (int?) commodityData.BuyPrice : null
				,Demand = commodityData.Demand
				,DemandLevel = commodityData.DemandLevel
				,SellPrice = commodityData.SellPrice
				,StationName = commodityData.StationName
				,Supply = commodityData.Stock
				,SupplyLevel = commodityData.SupplyLevel
				,SystemName = commodityData.SystemName
				,Timestamp = commodityData.SampleDate.ToUniversalTime()
			};
		}

		private void Publish(EddnMessage.MessageContent rowToPost)
		{
			Debug.Print("eddn send : " + rowToPost);
			var eddnMessage = new EddnMessage
			{
				Header = new EddnMessage.HeaderContent()
				{
					SoftwareName = SOFTWARE_NAME
					,SoftwareVersion = CurrentVersion
					,UploaderId = CurrentUser
				}
				,Message = rowToPost,
				SchemaRef = UseTestSchema ? EDDN_SCHEMA_TEST_URL : EDDN_SCHEMA_URL
			};
			SendToEddn(eddnMessage.ToJson());
		}

		private void SendToEddn(string json)
		{
			if (TestMode)
			{
				Debug.WriteLine("sending to eddn: " + json);
				return;
			}
			using (var client = new WebClient())
			{
				try
				{
					client.UploadString(POST_URL, "POST", json);
				}
				catch (WebException ex)
				{
					Trace.TraceError("Error uploading Json: " + ex, true);
					using (WebResponse response = ex.Response)
					{
						using (Stream data = response.GetResponseStream())
						{
							if (data != null)
							{
								StreamReader sr = new StreamReader(data);
								EventBus.Alert(sr.ReadToEnd(), "Error while uploading to EDDN");
							}
						}
					}
				}
			}
		}

		private class EddnPublisherStatisticCollection : KeyedCollection<string, EddnPublisherVersionStats>
		{
			protected override string GetKeyForItem(EddnPublisherVersionStats item)
			{
				return item.Publisher;
			}

			public bool TryGetValue(string publisher, out EddnPublisherVersionStats stats)
			{
				if (Dictionary != null)
				{
					return Dictionary.TryGetValue(publisher, out stats);
				}
				else
				{
					stats = null;
					return false;
				}
			}
		}

		public void Dispose()
		{
			_pendingMessage.CompleteAdding();
		}
	}
}