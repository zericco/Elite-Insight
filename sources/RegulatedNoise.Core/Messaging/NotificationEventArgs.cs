﻿using System;

namespace RegulatedNoise.Core.Messaging
{
	public class NotificationEventArgs : EventArgs
	{
		public enum EventType
		{
			InitializationStart,
			InitializationProgress,
			InitializationCompleted,
			Information,
			Request,
			FileRequest,
		    Alert
		}

		public readonly EventType Event;

		public readonly string Message;

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