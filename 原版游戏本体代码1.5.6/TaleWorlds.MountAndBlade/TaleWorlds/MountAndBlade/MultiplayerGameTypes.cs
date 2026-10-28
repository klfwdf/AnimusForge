using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade;

public static class MultiplayerGameTypes
{
	private static Dictionary<string, MultiplayerGameTypeInfo> _multiplayerGameTypeInfos;

	public static void Initialize()
	{
		CreateGameTypeInformations();
		LoadMultiplayerSceneInformations();
	}

	public static bool CheckGameTypeInfoExists(string gameType)
	{
		return _multiplayerGameTypeInfos.ContainsKey(gameType);
	}

	public static MultiplayerGameTypeInfo GetGameTypeInfo(string gameType)
	{
		if (_multiplayerGameTypeInfos.ContainsKey(gameType))
		{
			return _multiplayerGameTypeInfos[gameType];
		}
		Debug.Print("Cannot find game type:" + gameType);
		return null;
	}

	private static void LoadMultiplayerSceneInformations()
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/Multiplayer/MultiplayerScenes.xml");
		foreach (XmlNode childNode in xmlDocument.ChildNodes)
		{
			if (childNode.NodeType != XmlNodeType.Element || !(childNode.Name == "MultiplayerScenes"))
			{
				continue;
			}
			{
				foreach (XmlNode item in childNode)
				{
					if (item.NodeType == XmlNodeType.Comment)
					{
						continue;
					}
					string innerText = item.Attributes["name"].InnerText;
					foreach (XmlNode childNode2 in item.ChildNodes)
					{
						if (childNode2.NodeType != XmlNodeType.Comment)
						{
							string innerText2 = childNode2.Attributes["name"].InnerText;
							if (_multiplayerGameTypeInfos.ContainsKey(innerText2))
							{
								_multiplayerGameTypeInfos[innerText2].Scenes.Add(innerText);
							}
						}
					}
				}
				break;
			}
		}
	}

	private static void CreateGameTypeInformations()
	{
		_multiplayerGameTypeInfos = new Dictionary<string, MultiplayerGameTypeInfo>();
		foreach (MultiplayerGameTypeInfo multiplayerGameType in Module.CurrentModule.GetMultiplayerGameTypes())
		{
			_multiplayerGameTypeInfos.Add(multiplayerGameType.GameType, multiplayerGameType);
		}
	}
}
