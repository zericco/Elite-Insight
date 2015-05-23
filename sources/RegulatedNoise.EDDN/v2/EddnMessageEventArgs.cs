#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 23.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using RegulatedNoise.Core.DataProviders.Eddn.v2;

namespace RegulatedNoise.EDDN.v2
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