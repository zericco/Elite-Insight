#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 19.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RegulatedNoise.Core.Tracing;

namespace RegulatedNoise.Core.Test
{
	[TestClass]
	public class LogEnvetSourceTest
	{
		[TestMethod]
		public void ShouldValidateEventSource()
		{
			EventSourceAnalyzer.InspectAll(Log.Debug);
		} 
	}
}