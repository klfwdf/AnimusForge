using System;

namespace AnimusForge;

public sealed class PreprocessFormatException : Exception
{
	public PreprocessFormatException(string message)
		: base(message)
	{
	}
}
