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
using System.IO.Compression;
using ZeroMQ;

namespace RegulatedNoise.EDDN.v1
{
	public class EddnListener: EddnListenerBase
	{
		private const string LISTEN_URL = "tcp://eddn-relay.elite-markets.net:9500";
		public event EventHandler<EddnMessageEventArgs> OnMessageReceived;

		public EddnListener() : base(LISTEN_URL)
		{
		}

		protected override void ReadFrame(ZFrame frame)
		{
			try
			{
				Debug.WriteLine("header: " + frame.ReadInt16());
				//frame.Seek(2, SeekOrigin.Begin); // skipping topic?
				string message;
				using (var unzipper = new DeflateStream(frame, CompressionMode.Decompress))
				{
					using (var sr = new StreamReader(unzipper))
					{
						message = sr.ReadToEnd();
					}
				}
				EddnMessage eddnMessage;
				try
				{
					eddnMessage = EddnMessage.ReadJson(message);
				}
				catch (Exception ex)
				{
					Trace.TraceError("unable to parse message " + Environment.NewLine + message + Environment.NewLine + ex);
					eddnMessage = new EddnMessage
					{
						RawText = message
					};
				}
				RaiseMessageReceived(eddnMessage);
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
		protected void RaiseMessageReceived(EddnMessage message)
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
	}
}