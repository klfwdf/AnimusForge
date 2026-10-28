using System;

namespace TaleWorlds.Localization;

public class LocalizationException : Exception
{
	public LocalizationException()
	{
	}

	public LocalizationException(string message)
		: base(message)
	{
	}

	public LocalizationException(string message, Exception inner)
		: base(message, inner)
	{
	}
}
