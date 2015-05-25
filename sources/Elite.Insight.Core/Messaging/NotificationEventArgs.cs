using System;

namespace Elite.Insight.Core.Messaging
{
	public class NotificationEventArgs : EventArgs
	{
		public enum EventType
		{
			Start,
			Progress,
			Completed,
			Information,
			Request,
			FileRequest,
		    Alert
		}

		public readonly EventType Event;

		public readonly string Message;

		public int CorrelationId { get; set; }

		public int ActualProgress { get; set; }

		public int TotalProgress { get; set; }

		public string Title { get; set; }

		public bool Cancel { get; set; }

		public string Response { get; set; }

		public NotificationEventArgs(string message, EventType @event)
		{
			Message = message;
			Event = @event;
			Cancel = true;
		}
	}
}