#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 23.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegulatedNoise.EDDN.v1;

namespace RegulatedNoise.Test.EDDN
{
	[TestClass]
	public class EddnServiceTest
	{
		[TestMethod]
		public void foo()
		{
			using (var eddn = new EddnService())
			{
				eddn.OnMessageReceived += (sender, args) => Debug.WriteLine(args.Message.ToJson());
				eddn.Subscribe();
				Thread.Sleep(200000);
			}
		}
	}
}