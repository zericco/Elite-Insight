#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 23.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using RegulatedNoise.Core.DataProviders.Eddn.v1;

namespace RegulatedNoise.EDDN.v1
{
	public class EddnMessageEventArgs : EventArgs
	{
		public readonly EddnMessage Message;

		public EddnMessageEventArgs(EddnMessage message)
		{
			Message = message;
		}

		public override string ToString()
		{
			return Message == null ? "<null>" : Message.ToString();
		}
	}
}