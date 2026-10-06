using System;

namespace TaleWorlds.Diamond;

public class MessageDescription : Attribute
{
	public string To { get; private set; }

	public string From { get; private set; }

	public bool EndSessionOnFail { get; private set; }

	public MessageDescription(string from, string to, bool endSessionOnFail = true)
	{
		From = from;
		To = to;
		EndSessionOnFail = endSessionOnFail;
	}
}
