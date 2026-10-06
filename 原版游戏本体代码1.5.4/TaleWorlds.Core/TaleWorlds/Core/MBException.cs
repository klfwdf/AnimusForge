using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Core;

public class MBException : ApplicationException
{
	public MBException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	public MBException(string message)
		: base(message)
	{
	}

	public MBException()
	{
	}

	public MBException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
