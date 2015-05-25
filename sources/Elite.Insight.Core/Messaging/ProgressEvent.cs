#region file header
// ////////////////////////////////////////////////////////////////////
// ///
// ///  
// /// 24.05.2015
// ///
// ////////////////////////////////////////////////////////////////////
#endregion

using System;

namespace Elite.Insight.Core.Messaging
{
	public struct ProgressEvent
	{
		public readonly string Text;

		public readonly int Actual;

		public readonly int Total;

		public ProgressEvent(string text, int actual, int total)
			: this()
		{
			Text = text;
			Actual = actual;
			Total = total;
		}
	}

	public class NopProgress : IProgress<ProgressEvent>
	{
		private static readonly Lazy<NopProgress> _instance;

		static NopProgress()
		{
			_instance = new Lazy<NopProgress>(() => new NopProgress());
		}

		public static IProgress<ProgressEvent> Instance { get { return _instance.Value; } }
		
		private NopProgress()
		{
		}

		public void Report(ProgressEvent value)
		{
		}
	}
}