#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 19.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Diagnostics.Tracing;

namespace RegulatedNoise.Core.Tracing
{
	[EventSource(Name="RegulatedNoise")]
	public class Log: EventSource
	{
		private readonly int _eventId;
		private const int DEBUG_ID = 1000;
		private const int INFO_ID = 1001;
		private const int WARN_ID = 1002;
		private const int ERROR_ID = 1003;
		private const int FATAL_ID = 1004;

		private Log(int eventId)
		{
			_eventId = eventId;
		}

		private static readonly Lazy<Log> _debug = new Lazy<Log>(() => new Log(DEBUG_ID));
		public static Log Debug { get { return _debug.Value; } }

		private static readonly Lazy<Log> _info = new Lazy<Log>(() => new Log(INFO_ID));
		public static Log Info { get { return _info.Value; } }

		private static readonly Lazy<Log> _warn = new Lazy<Log>(() => new Log(WARN_ID));
		public static Log Warn { get { return _warn.Value; } }

		private static readonly Lazy<Log> _error = new Lazy<Log>(() => new Log(ERROR_ID));
		public static Log Error { get { return _error.Value; } }

		private static readonly Lazy<Log> _fatal = new Lazy<Log>(() => new Log(FATAL_ID));
		public static Log Fatal { get { return _fatal.Value; } }

		//[Event(1, Level = EventLevel.Verbose)]
		//public void WriteLine(string format, params object[] args)
		//{
		//	WriteEvent(_eventId, String.Format(format, args));
		//}

		[Event(1, Level = EventLevel.Verbose)]
		public void WriteLine(string message)
		{
			WriteEvent(_eventId, message);
		}
	}
}