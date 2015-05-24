using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RegulatedNoise.Annotations;
using RegulatedNoise.Core.Messaging;
using ZeroMQ;

namespace RegulatedNoise.EDDN
{
	public abstract class EddnListenerBase : IDisposable
	{
		private bool _disposed;
		private readonly object _listeningStateChange = new object();
		private bool _listening;
		private bool _saveMessagesToFile;
		private Task _listenTask;
		protected string EddnListenUrl { get; set; }

		protected EddnListenerBase(string eddnListenUrl)
		{
			if (eddnListenUrl == null)
			{
				throw new ArgumentNullException("eddnListenUrl");
			}
			EventBus.Start("initializing Eddn listener");
			EddnListenUrl = eddnListenUrl;
		}

		private const int BLOCKING_TIME = 15;

		public string OutputFilePath { get; set; }

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

		public void Stop()
		{
			lock (_listeningStateChange)
			{
				Listening = false;
			}
		}


		public void Start()
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
					socket.Connect(EddnListenUrl);
					while (!_disposed && Listening)
					{
						socket.ReceiveTimeout = TimeSpan.FromSeconds(BLOCKING_TIME);
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
				}
			}
		}

		protected abstract void ReadFrame(ZFrame frame);

		public virtual void Dispose()
		{
			_disposed = true;
			if (_listenTask != null)
			{
				_listenTask.Wait(TimeSpan.FromSeconds(BLOCKING_TIME + 2));
				_listenTask = null;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
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

		protected void SaveToFile(string message)
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
	}
}