using System;

namespace TaleWorlds.ObjectSystem;

public class ObjectSystemException : Exception
{
	internal ObjectSystemException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	internal ObjectSystemException(string message)
		: base(message)
	{
	}

	internal ObjectSystemException()
	{
	}
}
