using System;

namespace RegulatedNoise.Enums_and_Utility_Classes
{
    static class UnixTimeStamp
    {
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp )
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0,DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds( unixTimeStamp ).ToUniversalTime();
            return dtDateTime;
        }

        public static long  DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1).ToUniversalTime()).TotalSeconds;
        }
    }
}
