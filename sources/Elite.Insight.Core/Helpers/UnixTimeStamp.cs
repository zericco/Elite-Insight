#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 22.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;

namespace Elite.Insight.Core.Helpers
{
	public static class UnixTimeStamp
	{
		private static DateTime _unixTimeStampToDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

		public static DateTime ToDateTime(long unixTimeStamp)
		{
			// Unix timestamp is seconds past epoch
			return _unixTimeStampToDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
		}

		public static long ToUnixTimestamp(this DateTime dateTime)
		{
			return (long)(dateTime.ToUniversalTime() - _unixTimeStampToDateTime).TotalSeconds;
		}
	}
}