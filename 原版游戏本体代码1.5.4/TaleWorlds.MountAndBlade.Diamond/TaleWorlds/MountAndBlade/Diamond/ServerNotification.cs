using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ServerNotification
{
	public ServerNotificationType Type { get; }

	public string Message { get; }

	public ServerNotification(ServerNotificationType type, string message)
	{
		Type = type;
		Message = message;
	}

	public TextObject GetTextObjectOfMessage()
	{
		if (!GameTexts.TryGetText(Message, out var textObject))
		{
			return new TextObject("{=!}" + Message);
		}
		return textObject;
	}
}
