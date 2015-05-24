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
using RegulatedNoise.Core.DomainModel;
using RegulatedNoise.Core.Messaging;
using ZeroMQ;

namespace RegulatedNoise.EDDN.v1
{
	public class EddnService : IDisposable
	{
		public event EventHandler<EddnMessageEventArgs> OnMessageReceived
		{
			add { Listener.OnMessageReceived += value; }
			remove { Listener.OnMessageReceived -= value; }
		}

		protected readonly EddnPublisherStatisticCollection _publicationStats;

		public IEnumerable<EddnPublisherVersionStats> Publications
		{
			get {  return _publicationStats; }
		}

		protected EddnPublisher Publisher { get; set; }

		protected EddnListener Listener { get; set; }

		public bool Listening { get { return Listener.Listening; } }

		public EddnService()
		{
			Publisher = new EddnPublisher();
			Listener = new EddnListener();
			_publicationStats = new EddnPublisherStatisticCollection();
			Listener.OnMessageReceived += _publicationStats.UpdateStats;
		}

		public void StartListening()
		{
			Listener.Start();
		}

		public void StopListening()
		{
			Listener.Stop();
		}

		public void Publish(MarketDataRow marketData)
		{
			Publisher.Publish(marketData);
		}

		public virtual void Dispose()
		{
			Listener.Dispose();
			Publisher.Dispose();
		}
	}
}