#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 23.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using NUnit.Framework;
using RegulatedNoise.EDDN.v1;

namespace RegulatedNoise.Test.EDDN.v1
{
	[TestFixture]
	public class EddnListenerTest
	{
		[Test]
		//[Ignore()]
		public void i_can_receive_eddn_message()
		{
			int messageCount = 5;
			using (var eddn = new EddnListener())
			{
				var waiter = new ManualResetEvent(false);
				eddn.OnMessageReceived += (sender, args) =>
				{
					Debug.WriteLine(args.Message.ToJson());
					--messageCount;
					if (messageCount == 0) waiter.Set();
				};
				eddn.Start();
				waiter.WaitOne(TimeSpan.FromMinutes(2));
			}
		}

		[Test]
		public void foo()
		{
			byte[] read = new byte[] {120, 156};
			foreach (char c in Encoding.ASCII.GetChars(read))
			{
				Debug.Write(c);
			}
		}
	}
}