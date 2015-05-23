#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 23.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
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
using RegulatedNoise.Core.DataProviders.Eddn.v1;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.Core.Messaging;
using ZeroMQ;

namespace RegulatedNoise.EDDN.v1
{
	public class EddnService : IDisposable, INotifyPropertyChanged
	{
		private const string POST_URL = "http://eddn-gateway.elite-markets.net:8080/upload/";
		private const string LISTEN_URL = "tcp://eddn-relay.elite-markets.net:9500";
		private const int DELAY_BETWEEN_POLL = 1000;
		public const string SOURCENAME = "EDDN";
		public event EventHandler<EddnMessageEventArgs> OnMessageReceived;
		private readonly Queue<EddnMessage.MessageContent> _sendItems;
		private bool _disposed;

		private readonly EddnPublisherStatisticCollection _eddnPublisherStats;

		private readonly object _listeningStateChange = new object();
		private bool _listening;
		private bool _saveMessagesToFile;
		private Task _listenTask;

		public IEnumerable<EddnPublisherVersionStats> PublisherStatistics
		{
			get { return _eddnPublisherStats; }
		}

		public string OutputFilePath { get; set; }

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

		public void UnSubscribe()
		{
			lock (_listeningStateChange)
			{
				Listening = false;
			}
		}

		public bool Listening
		{
			get { return _listening; }
			private set
			{
				if (value == _listening)
					return;
				_listening = value;
				RaisePropertyChanged();
			}
		}

		public bool SaveMessagesToFile
		{
			get { return _saveMessagesToFile; }
			set
			{
				if (value == _saveMessagesToFile)
					return;
				_saveMessagesToFile = value;
				RaisePropertyChanged();
			}
		}

		public EddnService()
		{
			_sendItems = new Queue<EddnMessage.MessageContent>();
			_eddnPublisherStats = new EddnPublisherStatisticCollection();
			EventBus.InitializationStart("initializing Eddn service");
			OnMessageReceived += UpdateStats;
		}

		private void UpdateStats(object sender, EddnMessageEventArgs e)
		{
			var nameAndVersion = (e.Message.Header.SoftwareName + " / " + e.Message.Header.SoftwareVersion);
			EddnPublisherVersionStats stats;
			if (!_eddnPublisherStats.TryGetValue(nameAndVersion, out stats))
			{
				stats = new EddnPublisherVersionStats(nameAndVersion);
				_eddnPublisherStats.Add(stats);
			}
			++stats.MessagesReceived;
		}

		public void Subscribe()
		{
			lock (_listeningStateChange)
			{
				if (Listening)
					return;
				Listening = true;

			}
			_listenTask = Task.Factory.StartNew(ListenToEddn, new CancellationToken(), TaskCreationOptions.LongRunning, TaskScheduler.Default);
			// ReSharper disable once FunctionNeverReturns
		}

		private void ListenToEddn()
		{
			using (var ctx = new ZContext())
			{
				using (var socket = new ZSocket(ctx, ZSocketType.SUB))
				{
					socket.SubscribeAll();
					socket.Connect(LISTEN_URL);
					while (!_disposed && Listening)
					{
						socket.ReceiveTimeout = TimeSpan.FromSeconds(15);
						ReadWithFrame(socket);
						Thread.Sleep(DELAY_BETWEEN_POLL);
					}
				}
			}
		}

		private void ReadWithFrame(ZSocket socket)
		{
			ZError error;
			ZFrame frame = socket.ReceiveFrame(out error);
			if (error == ZError.None)
			{
				using (frame)
				{
					ReadFrame(frame);
				}
			}
		}

		private void ReadFrame(ZFrame frame)
		{
			try
			{
				frame.Seek(2, SeekOrigin.Begin);
				string message;
				using (var unzipper = new DeflateStream(frame, CompressionMode.Decompress))
				{
					using (var sr = new StreamReader(unzipper))
					{
						message = sr.ReadToEnd();
					}
				}
				try
				{
					var eddnMessage = EddnMessage.ReadJson(message);
					eddnMessage.Message.Source = SOURCENAME;
					RaiseMessageReceived(eddnMessage);
				}
				catch (Exception ex)
				{
					Trace.TraceError("unable to parse message " + Environment.NewLine + message + Environment.NewLine +
					                 ex);
					var failedMessage = new EddnMessage
					{
						RawText = message,
						Message = {Source = SOURCENAME}
					};
					RaiseMessageReceived(failedMessage);
				}
				if (SaveMessagesToFile)
				{
					SaveToFile(message);
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError(ex.ToString());
			}
		}

		private void EDDNSender()
		{
			do
			{
				try
				{
					Thread.Sleep(1000);

					while ((_sendItems.Count > 0) && !_disposed)
					{
						Debug.Print("Items in Queue : " + _sendItems.Count);
						PostJsonToEddn(_sendItems.Dequeue());
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError("Error uploading Json: " + ex, true);
				}

			}
			while (!_disposed);

			_sendItems.Clear();
		}

		public void SendToEddn(MarketDataRow commodityData)
		{
			Debug.Print("Items in Queue : " + _sendItems.Count);
			_sendItems.Enqueue(ToMessageContent(commodityData));
			Debug.Print("Items in Queue : " + _sendItems.Count + ", added : " + commodityData);

		}

		private EddnMessage.MessageContent ToMessageContent(MarketDataRow commodityData)
		{
			throw new NotImplementedException();
		}

		private void PostJsonToEddn(EddnMessage.MessageContent rowToPost)
		{
			Debug.Print("eddn send : " + rowToPost);
			var eddnMessage = new EddnMessage()
			{
				Header = new EddnMessage.HeaderContent()
				{
					SoftwareName = "SpaceFindED"
					, SoftwareVersion = CurrentVersion
					, UploaderId = CurrentUser
				}
				, Message = rowToPost
			};
			if (UseTestSchema)
			{
				eddnMessage.SchemaRef = "http://schemas.elite-markets.net/eddn/commodity/1/test";
			}
			else
			{
				eddnMessage.SchemaRef = "http://schemas.elite-markets.net/eddn/commodity/1";
			}

			string commodity = rowToPost.CommodityName;

			if (!String.IsNullOrEmpty(commodity))
			{
				eddnMessage.Message.CommodityName = commodity;
				var json = eddnMessage.ToJson();
				SendToEddn(json);
			}
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

		public void Dispose()
		{
			_disposed = true;
			if (_listenTask != null)
			{
				_listenTask.Wait(TimeSpan.FromMilliseconds(DELAY_BETWEEN_POLL*2));
				_listenTask = null;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
				try
				{
					handler(this, new PropertyChangedEventArgs(propertyName));
				}
				catch (Exception ex)
				{
					Trace.TraceError(propertyName + " notification failed " + ex);
				}
		}

		protected virtual void RaiseMessageReceived(EddnMessage message)
		{
			var handler = OnMessageReceived;
			if (handler != null)
				try
				{
					handler(this, new EddnMessageEventArgs(message));
				}
				catch (Exception exception)
				{
					Trace.TraceError("EDDN message notification failure: " + exception, true);
				}
			;
		}

		private void SaveToFile(string message)
		{
			try
			{
				File.AppendAllText(OutputFilePath, message + Environment.NewLine);
			}
			catch (Exception ex)
			{
				Trace.TraceError("unable to save message to " + OutputFilePath + ": " + ex);
				SaveMessagesToFile = false;
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
	}
}