using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Library;

public class TWException : ApplicationException
{
	public TWException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	public TWException(string message)
		: base(message)
	{
	}

	public TWException()
	{
	}

	public TWException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
