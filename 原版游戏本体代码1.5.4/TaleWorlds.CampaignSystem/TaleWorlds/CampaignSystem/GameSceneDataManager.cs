using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public class GameSceneDataManager
{
	private MBList<SingleplayerBattleSceneData> _singleplayerBattleScenes;

	private MBList<ConversationSceneData> _conversationScenes;

	private MBList<MeetingSceneData> _meetingScenes;

	private const TerrainType DefaultTerrain = TerrainType.Plain;

	private const ForestDensity DefaultForestDensity = ForestDensity.None;

	public static GameSceneDataManager Instance { get; private set; }

	public MBReadOnlyList<SingleplayerBattleSceneData> SingleplayerBattleScenes => _singleplayerBattleScenes;

	public MBReadOnlyList<ConversationSceneData> ConversationScenes => _conversationScenes;

	public MBReadOnlyList<MeetingSceneData> MeetingScenes => _meetingScenes;

	public GameSceneDataManager()
	{
		_singleplayerBattleScenes = new MBList<SingleplayerBattleSceneData>();
		_conversationScenes = new MBList<ConversationSceneData>();
		_meetingScenes = new MBList<MeetingSceneData>();
	}

	internal static void Initialize()
	{
		Instance = new GameSceneDataManager();
	}

	internal static void Destroy()
	{
		Instance = null;
	}

	public void LoadSPBattleScenes(string path)
	{
		XmlDocument doc = LoadXmlFile(path);
		LoadSPBattleScenes(doc);
	}

	public void LoadConversationScenes(string path)
	{
		XmlDocument doc = LoadXmlFile(path);
		LoadConversationScenes(doc);
	}

	public void LoadMeetingScenes(string path)
	{
		XmlDocument doc = LoadXmlFile(path);
		LoadMeetingScenes(doc);
	}

	private XmlDocument LoadXmlFile(string path)
	{
		Debug.Print("opening " + path);
		XmlDocument xmlDocument = new XmlDocument();
		StreamReader streamReader = new StreamReader(path);
		string xml = streamReader.ReadToEnd();
		xmlDocument.LoadXml(xml);
		streamReader.Close();
		return xmlDocument;
	}

	private void LoadSPBattleScenes(XmlDocument doc)
	{
		Debug.Print("loading sp_battles.xml");
		if (doc.ChildNodes.Count <= 1)
		{
			throw new TWXmlLoadException("Incorrect XML document format. XML document must have at least 2 child nodes.");
		}
		XmlNode xmlNode = doc.ChildNodes[1];
		if (xmlNode.Name != "SPBattleScenes")
		{
			throw new TWXmlLoadException("Incorrect XML document format. Root node's name must be SPBattleScenes.");
		}
		if (!(xmlNode.Name == "SPBattleScenes"))
		{
			return;
		}
		foreach (XmlNode childNode in xmlNode.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Comment)
			{
				continue;
			}
			string sceneID = null;
			List<int> list = new List<int>();
			TerrainType result = TerrainType.Plain;
			ForestDensity result2 = ForestDensity.None;
			bool result3 = false;
			for (int i = 0; i < childNode.Attributes.Count; i++)
			{
				if (childNode.Attributes[i].Name == "id")
				{
					sceneID = childNode.Attributes[i].InnerText;
				}
				else if (childNode.Attributes[i].Name == "map_indices")
				{
					string[] array = childNode.Attributes[i].InnerText.Replace(" ", "").Split(new char[1] { ',' });
					foreach (string s in array)
					{
						list.Add(int.Parse(s));
					}
				}
				else if (childNode.Attributes[i].Name == "terrain")
				{
					if (!Enum.TryParse<TerrainType>(childNode.Attributes[i].InnerText, out result))
					{
						result = TerrainType.Plain;
					}
				}
				else if (childNode.Attributes[i].Name == "forest_density")
				{
					char[] array2 = childNode.Attributes[i].InnerText.ToLower().ToCharArray();
					array2[0] = char.ToUpper(array2[0]);
					if (!Enum.TryParse<ForestDensity>(new string(array2), out result2))
					{
						result2 = ForestDensity.None;
					}
				}
				else if (childNode.Attributes[i].Name == "is_naval")
				{
					bool.TryParse(childNode.Attributes[i].Value, out result3);
				}
			}
			XmlNodeList childNodes = childNode.ChildNodes;
			List<TerrainType> list2 = new List<TerrainType>();
			foreach (XmlNode item in childNodes)
			{
				if (item.NodeType == XmlNodeType.Comment || !(item.Name == "TerrainTypes"))
				{
					continue;
				}
				foreach (XmlNode childNode2 in item.ChildNodes)
				{
					if (childNode2.Name == "TerrainType" && Enum.TryParse<TerrainType>(childNode2.Attributes["name"].InnerText, out var result4) && !list2.Contains(result4))
					{
						list2.Add(result4);
					}
				}
			}
			_singleplayerBattleScenes.Add(new SingleplayerBattleSceneData(sceneID, result, list2, result2, list, result3));
		}
	}

	private void LoadConversationScenes(XmlDocument doc)
	{
		Debug.Print("loading conversation_scenes.xml");
		if (doc.ChildNodes.Count <= 1)
		{
			throw new TWXmlLoadException("Incorrect XML document format. XML document must have at least 2 child nodes.");
		}
		XmlNode xmlNode = doc.ChildNodes[1];
		if (xmlNode.Name != "ConversationScenes")
		{
			throw new TWXmlLoadException("Incorrect XML document format. Root node's name must be ConversationScenes.");
		}
		if (!(xmlNode.Name == "ConversationScenes"))
		{
			return;
		}
		foreach (XmlNode childNode in xmlNode.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Comment)
			{
				continue;
			}
			string sceneID = null;
			TerrainType result = TerrainType.Plain;
			ForestDensity result2 = ForestDensity.None;
			for (int i = 0; i < childNode.Attributes.Count; i++)
			{
				if (childNode.Attributes[i].Name == "id")
				{
					sceneID = childNode.Attributes[i].InnerText;
				}
				else if (childNode.Attributes[i].Name == "terrain")
				{
					if (!Enum.TryParse<TerrainType>(childNode.Attributes[i].InnerText, out result))
					{
						result = TerrainType.Plain;
					}
				}
				else if (childNode.Attributes[i].Name == "forest_density")
				{
					char[] array = childNode.Attributes[i].InnerText.ToLower().ToCharArray();
					array[0] = char.ToUpper(array[0]);
					if (!Enum.TryParse<ForestDensity>(new string(array), out result2))
					{
						result2 = ForestDensity.None;
					}
				}
			}
			XmlNodeList childNodes = childNode.ChildNodes;
			List<TerrainType> list = new List<TerrainType>();
			foreach (XmlNode item in childNodes)
			{
				if (item.NodeType == XmlNodeType.Comment || !(item.Name == "flags"))
				{
					continue;
				}
				foreach (XmlNode childNode2 in item.ChildNodes)
				{
					if (childNode2.NodeType != XmlNodeType.Comment && childNode2.Attributes["name"].InnerText == "TerrainType" && Enum.TryParse<TerrainType>(childNode2.Attributes["value"].InnerText, out var result3) && !list.Contains(result3))
					{
						list.Add(result3);
					}
				}
			}
			_conversationScenes.Add(new ConversationSceneData(sceneID, result, list, result2));
		}
	}

	private void LoadMeetingScenes(XmlDocument doc)
	{
		Debug.Print("loading meeting_scenes.xml");
		if (doc.ChildNodes.Count <= 1)
		{
			throw new TWXmlLoadException("Incorrect XML document format. XML document must have at least 2 child nodes.");
		}
		XmlNode xmlNode = doc.ChildNodes[1];
		if (xmlNode.Name != "MeetingScenes")
		{
			throw new TWXmlLoadException("Incorrect XML document format. Root node's name must be MeetingScenes.");
		}
		if (!(xmlNode.Name == "MeetingScenes"))
		{
			return;
		}
		foreach (XmlNode childNode in xmlNode.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Comment)
			{
				continue;
			}
			string sceneID = null;
			string cultureString = null;
			for (int i = 0; i < childNode.Attributes.Count; i++)
			{
				if (childNode.Attributes[i].Name == "id")
				{
					sceneID = childNode.Attributes[i].InnerText;
				}
				if (childNode.Attributes[i].Name == "culture")
				{
					cultureString = childNode.Attributes[i].InnerText.Split(new char[1] { '.' })[1];
				}
			}
			_meetingScenes.Add(new MeetingSceneData(sceneID, cultureString));
		}
	}
}
