using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x02000230 RID: 560
	public class ConversationAnimationManager
	{
		// Token: 0x17000864 RID: 2148
		// (get) Token: 0x060021E5 RID: 8677 RVA: 0x00094CB0 File Offset: 0x00092EB0
		// (set) Token: 0x060021E6 RID: 8678 RVA: 0x00094CB8 File Offset: 0x00092EB8
		public Dictionary<string, ConversationAnimData> ConversationAnims { get; private set; }

		// Token: 0x060021E7 RID: 8679 RVA: 0x00094CC1 File Offset: 0x00092EC1
		public ConversationAnimationManager()
		{
			this.ConversationAnims = new Dictionary<string, ConversationAnimData>();
			this.LoadConversationAnimData(ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/conversation_animations.xml");
		}

		// Token: 0x060021E8 RID: 8680 RVA: 0x00094CF0 File Offset: 0x00092EF0
		private void LoadConversationAnimData(string xmlPath)
		{
			XmlDocument doc = this.LoadXmlFile(xmlPath);
			this.LoadFromXml(doc);
		}

		// Token: 0x060021E9 RID: 8681 RVA: 0x00094D0C File Offset: 0x00092F0C
		private XmlDocument LoadXmlFile(string path)
		{
			Debug.Print("opening " + path, 0, Debug.DebugColor.White, 17592186044416UL);
			XmlDocument xmlDocument = new XmlDocument();
			StreamReader streamReader = new StreamReader(path);
			string xml = streamReader.ReadToEnd();
			xmlDocument.LoadXml(xml);
			streamReader.Close();
			return xmlDocument;
		}

		// Token: 0x060021EA RID: 8682 RVA: 0x00094D58 File Offset: 0x00092F58
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
			foreach (object obj in doc.DocumentElement.SelectNodes("IdleAnim"))
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Attributes != null)
				{
					KeyValuePair<string, ConversationAnimData> keyValuePair = new KeyValuePair<string, ConversationAnimData>(xmlNode.Attributes["id"].Value, new ConversationAnimData());
					keyValuePair.Value.IdleAnimStart = xmlNode.Attributes["action_id_1"].Value;
					keyValuePair.Value.IdleAnimLoop = xmlNode.Attributes["action_id_2"].Value;
					keyValuePair.Value.FamilyType = 0;
					XmlAttribute xmlAttribute = xmlNode.Attributes["family_type"];
					int familyType;
					if (xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.Value) && int.TryParse(xmlAttribute.Value, out familyType))
					{
						keyValuePair.Value.FamilyType = familyType;
					}
					keyValuePair.Value.MountFamilyType = 0;
					XmlAttribute xmlAttribute2 = xmlNode.Attributes["mount_family_type"];
					int mountFamilyType;
					if (xmlAttribute2 != null && !string.IsNullOrEmpty(xmlAttribute2.Value) && int.TryParse(xmlAttribute2.Value, out mountFamilyType))
					{
						keyValuePair.Value.MountFamilyType = mountFamilyType;
					}
					foreach (object obj2 in xmlNode.ChildNodes)
					{
						XmlNode xmlNode2 = (XmlNode)obj2;
						if (xmlNode2.Name == "Reactions")
						{
							foreach (object obj3 in xmlNode2.ChildNodes)
							{
								XmlNode xmlNode3 = (XmlNode)obj3;
								if (xmlNode3.Name == "Reaction" && xmlNode3.Attributes["id"] != null && xmlNode3.Attributes["action_id"] != null)
								{
									keyValuePair.Value.Reactions.Add(xmlNode3.Attributes["id"].Value, xmlNode3.Attributes["action_id"].Value);
								}
							}
						}
					}
					this.ConversationAnims.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}
		}
	}
}
