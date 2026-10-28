using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem.Conversation;

public class ConversationAnimationManager
{
	public Dictionary<string, ConversationAnimData> ConversationAnims { get; private set; }

	public ConversationAnimationManager()
	{
		ConversationAnims = new Dictionary<string, ConversationAnimData>();
		LoadConversationAnimData(ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/conversation_animations.xml");
	}

	private void LoadConversationAnimData(string xmlPath)
	{
		XmlDocument doc = LoadXmlFile(xmlPath);
		LoadFromXml(doc);
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

	private void LoadFromXml(XmlDocument doc)
	{
		if (doc.ChildNodes.Count <= 1)
		{
			throw new TWXmlLoadException("Incorrect XML document format.");
		}
		if (doc.ChildNodes[1].Name != "ConversationAnimations")
		{
			throw new TWXmlLoadException("Incorrect XML document format.");
		}
		foreach (XmlNode item in doc.DocumentElement.SelectNodes("IdleAnim"))
		{
			if (item.Attributes == null)
			{
				continue;
			}
			KeyValuePair<string, ConversationAnimData> keyValuePair = new KeyValuePair<string, ConversationAnimData>(item.Attributes["id"].Value, new ConversationAnimData());
			keyValuePair.Value.IdleAnimStart = item.Attributes["action_id_1"].Value;
			keyValuePair.Value.IdleAnimLoop = item.Attributes["action_id_2"].Value;
			keyValuePair.Value.FamilyType = 0;
			XmlAttribute xmlAttribute = item.Attributes["family_type"];
			if (xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.Value) && int.TryParse(xmlAttribute.Value, out var result))
			{
				keyValuePair.Value.FamilyType = result;
			}
			keyValuePair.Value.MountFamilyType = 0;
			XmlAttribute xmlAttribute2 = item.Attributes["mount_family_type"];
			if (xmlAttribute2 != null && !string.IsNullOrEmpty(xmlAttribute2.Value) && int.TryParse(xmlAttribute2.Value, out var result2))
			{
				keyValuePair.Value.MountFamilyType = result2;
			}
			foreach (XmlNode childNode in item.ChildNodes)
			{
				if (!(childNode.Name == "Reactions"))
				{
					continue;
				}
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					if (childNode2.Name == "Reaction" && childNode2.Attributes["id"] != null && childNode2.Attributes["action_id"] != null)
					{
						keyValuePair.Value.Reactions.Add(childNode2.Attributes["id"].Value, childNode2.Attributes["action_id"].Value);
					}
				}
			}
			ConversationAnims.Add(keyValuePair.Key, keyValuePair.Value);
		}
	}
}
