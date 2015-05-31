using System;
using System.Runtime.Serialization;

namespace Elite.Insight.Core
{
	[Serializable]
	public class MissingDataException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public MissingDataException()
		{
		}

		public MissingDataException(string message) : base(message)
		{
		}

		public MissingDataException(string message, Exception inner) : base(message, inner)
		{
		}

		protected MissingDataException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}