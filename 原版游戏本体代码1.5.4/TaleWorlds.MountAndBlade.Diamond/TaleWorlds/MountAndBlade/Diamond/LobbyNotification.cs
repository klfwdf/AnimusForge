using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class LobbyNotification
{
	public const string BadgeIdParameterName = "badge_id";

	public const string FriendRequesterParameterName = "friend_requester";

	public int Id { get; set; }

	public NotificationType Type { get; set; }

	public DateTime Date { get; set; }

	public string Message { get; set; }

	public Dictionary<string, string> Parameters { get; set; }

	public LobbyNotification()
	{
	}

	public LobbyNotification(NotificationType type, DateTime date, string message)
	{
		Id = -1;
		Type = type;
		Date = date;
		Message = message;
		Parameters = new Dictionary<string, string>();
	}

	public LobbyNotification(int id, NotificationType type, DateTime date, string message, string serializedParameters)
	{
		Id = id;
		Type = type;
		Date = date;
		Message = message;
		try
		{
			Parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(serializedParameters);
		}
		catch (Exception)
		{
			Parameters = new Dictionary<string, string>();
		}
	}

	public string GetParametersAsString()
	{
		string result = "{}";
		try
		{
			result = JsonConvert.SerializeObject(Parameters, Formatting.None);
		}
		catch (Exception)
		{
		}
		return result;
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
