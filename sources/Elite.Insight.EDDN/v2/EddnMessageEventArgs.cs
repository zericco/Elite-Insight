#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 23.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;

namespace Elite.Insight.EDDN.v2
{
	public class EddnMessageEventArgs : EventArgs
	{
		public readonly EddnMessage Message;

		public EddnMessageEventArgs(EddnMessage message)
		{
			Message = message;
		}
	}
}