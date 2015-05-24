using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.Core.Messaging;

namespace RegulatedNoise.EDDN
{
	public abstract class EddnPublisherBase : IDisposable
	{
		protected const string SOFTWARE_NAME = "SpaceXplorer";

		protected BlockingCollection<string> _pendingMessage;
		protected Task _publishTask;
		protected readonly object _startingtask = new object();
		private bool _disposed;

		public string PostUrl { get; private set; }

		protected EddnPublisherBase(string postUrl)
		{
			PostUrl = postUrl;
			_pendingMessage = new BlockingCollection<string>();
			EventBus.InitializationStart("initializing Eddn publisher");
		}

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

		protected void SendToEddn(string json)
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
					client.UploadString(PostUrl, "POST", json);
				}
				catch (WebException ex)
				{
					Trace.TraceError("Error uploading Json to Eddn: " + ex);
					using (WebResponse response = ex.Response)
					{
						using (Stream data = response.GetResponseStream())
						{
							if (data != null)
							{
								StreamReader sr = new StreamReader(data);
								var eddnError = sr.ReadToEnd();
								Trace.TraceError("Eddn error: " + eddnError);
								EventBus.Alert(eddnError, "Error while uploading to EDDN");
							}
						}
					}
				}
			}
		}

		protected void PublishToEddnTask()
		{
			foreach (string messageContent in _pendingMessage.GetConsumingEnumerable())
			{
				try
				{
					SendToEddn(messageContent);
				}
				catch (Exception ex)
				{
					Trace.TraceError("eddn publication failure: " + ex);
				}
			}
		}

		public void Publish(MarketDataRow commodityData)
		{
			EnqueueMessage(ToMessage(commodityData));
		}

		protected void EnqueueMessage(string message)
		{
			if (_publishTask == null && !_disposed)
			{
				lock (_startingtask)
				{
					if (_publishTask == null && !_disposed)
					{
						_publishTask = Task.Factory.StartNew(PublishToEddnTask,
							new CancellationToken(),
							TaskCreationOptions.LongRunning,
							TaskScheduler.Default);
					}
				}
			}
			_pendingMessage.Add(message);
		}

		protected abstract string ToMessage(MarketDataRow commodityData);

		public virtual void Dispose()
		{
			_disposed = true;
			_pendingMessage.CompleteAdding();
			if (_publishTask != null)
			{
				_publishTask.Wait(TimeSpan.FromSeconds(10));
				_publishTask = null;
			}
		}
	}
}