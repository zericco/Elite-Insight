using System;
using System.Diagnostics;
using System.Threading;

namespace RegulatedNoise.Core.Messaging
{
	public static class EventBus
	{
		private static int _nextId;

		public static int GetNextEventId()
		{
			return Interlocked.Increment(ref _nextId);
		}

		public static event EventHandler<NotificationEventArgs> OnNotificationEvent;

		public static int Start(string message, int total = 0)
		{
			int nextEventId = GetNextEventId();
			RaiseNotificationEvent(new NotificationEventArgs(message, NotificationEventArgs.EventType.Start)
			{
				TotalProgress = total
				,CorrelationId = nextEventId
			});
			return nextEventId;
		}

		public static int Progress(string message, int actual, int total, int correlationId = -1)
		{
			RaiseNotificationEvent(new NotificationEventArgs(message, NotificationEventArgs.EventType.Progress)
			{
				ActualProgress = actual
				,TotalProgress = total
				,CorrelationId = correlationId
			});
			return correlationId;
		}

		public static int Completed(string message, int correlationId = -1)
		{
			RaiseNotificationEvent(new NotificationEventArgs(message, NotificationEventArgs.EventType.Completed)
			{
				CorrelationId = correlationId
			});
			return correlationId;
		}

		public static void Information(string message, string title = null)
		{
			RaiseNotificationEvent(new NotificationEventArgs(message, NotificationEventArgs.EventType.Information) { Title = title });
		}

        public static void Alert(string message, string title = null)
        {
            RaiseNotificationEvent(new NotificationEventArgs(message, NotificationEventArgs.EventType.Alert) { Title = title });
        }

		public static bool Request(string message, string title = null)
		{
			return !RaiseNotificationEvent(new NotificationEventArgs(message, NotificationEventArgs.EventType.Request) { Title = title }).Cancel;
		}

		public static string FileRequest(string message, string title = null)
		{
			return
				RaiseNotificationEvent(new NotificationEventArgs(message, NotificationEventArgs.EventType.FileRequest)
				{
					Title = title
				}).Response;
		}

		private static NotificationEventArgs RaiseNotificationEvent(NotificationEventArgs notificationEventArgs)
		{
			var handler = OnNotificationEvent;
			if (handler != null)
			{
				try
				{
					handler(null, notificationEventArgs);
				}
				catch (Exception ex)
				{
					Trace.TraceError("event notification failure: " + ex);
				}
			}
			return notificationEventArgs;
		}
	}
}