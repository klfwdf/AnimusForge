using System;

namespace TaleWorlds.TwoDimension;

public class RichTextException : Exception
{
	internal RichTextException(string message)
		: base(message)
	{
	}
}
