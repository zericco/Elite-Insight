using System;

namespace Elite.Insight.Core.DomainModel
{
	public class ValidationEventArgs : EventArgs
	{
		public readonly PlausibilityState PlausibilityState;

		public ValidationEventArgs(PlausibilityState plausibilityState)
		{
			PlausibilityState = plausibilityState;
		}
	}
}