using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Diamond;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Diamond;

public class LobbyClientConnectResult
{
	public bool Connected { get; private set; }

	public TextObject Error { get; private set; }

	public LobbyClientConnectResult(bool connected, TextObject error)
	{
		Connected = connected;
		Error = error;
	}

	public static LobbyClientConnectResult FromServerConnectResult(string errorCode, Dictionary<string, string> parameters)
	{
		TextObject textObject = GameTexts.FindText("str_login_error", errorCode);
		if (textObject == null)
		{
			Debug.FailedAssert("Error text is not handled: " + errorCode, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\LobbyClient.cs", "FromServerConnectResult", 2207);
			textObject = new TextObject("{=tzQxtv27}Unknown error.");
		}
		else if (parameters != null)
		{
			foreach (string key in parameters.Keys)
			{
				if (key == "BANREASON")
				{
					if (parameters[key].StartsWith("Custom:"))
					{
						textObject.SetTextVariable(key, parameters[key].Substring("Custom:".Length));
						continue;
					}
					TextObject textObject2 = GameTexts.FindText("str_ban_reason", parameters[key]);
					textObject.SetTextVariable(key, textObject2.ToString());
				}
				else if (key == "ACCESSERROR")
				{
					TextObject textObject3 = GameTexts.FindText("str_access_error", parameters[key]);
					textObject.SetTextVariable(key, textObject3.ToString());
				}
				else
				{
					textObject.SetTextVariable(key, parameters[key]);
				}
			}
		}
		return new LobbyClientConnectResult(errorCode == LoginErrorCode.None.ToString(), textObject);
	}
}
