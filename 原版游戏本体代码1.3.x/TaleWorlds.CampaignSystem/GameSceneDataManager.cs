using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200008C RID: 140
	public class GameSceneDataManager
	{
		// Token: 0x170004DD RID: 1245
		// (get) Token: 0x06001231 RID: 4657 RVA: 0x00052B39 File Offset: 0x00050D39
		// (set) Token: 0x06001232 RID: 4658 RVA: 0x00052B40 File Offset: 0x00050D40
		public static GameSceneDataManager Instance { get; private set; }

		// Token: 0x170004DE RID: 1246
		// (get) Token: 0x06001233 RID: 4659 RVA: 0x00052B48 File Offset: 0x00050D48
		public MBReadOnlyList<SingleplayerBattleSceneData> SingleplayerBattleScenes
		{
			get
			{
				return this._singleplayerBattleScenes;
			}
		}

		// Token: 0x170004DF RID: 1247
		// (get) Token: 0x06001234 RID: 4660 RVA: 0x00052B50 File Offset: 0x00050D50
		public MBReadOnlyList<ConversationSceneData> ConversationScenes
		{
			get
			{
				return this._conversationScenes;
			}
		}

		// Token: 0x170004E0 RID: 1248
		// (get) Token: 0x06001235 RID: 4661 RVA: 0x00052B58 File Offset: 0x00050D58
		public MBReadOnlyList<MeetingSceneData> MeetingScenes
		{
			get
			{
				return this._meetingScenes;
			}
		}

		// Token: 0x06001236 RID: 4662 RVA: 0x00052B60 File Offset: 0x00050D60
		public GameSceneDataManager()
		{
			this._singleplayerBattleScenes = new MBList<SingleplayerBattleSceneData>();
			this._conversationScenes = new MBList<ConversationSceneData>();
			this._meetingScenes = new MBList<MeetingSceneData>();
		}

		// Token: 0x06001237 RID: 4663 RVA: 0x00052B89 File Offset: 0x00050D89
		internal static void Initialize()
		{
			GameSceneDataManager.Instance = new GameSceneDataManager();
		}

		// Token: 0x06001238 RID: 4664 RVA: 0x00052B95 File Offset: 0x00050D95
		internal static void Destroy()
		{
			GameSceneDataManager.Instance = null;
		}

		// Token: 0x06001239 RID: 4665 RVA: 0x00052BA0 File Offset: 0x00050DA0
		public void LoadSPBattleScenes(string path)
		{
			XmlDocument doc = this.LoadXmlFile(path);
			this.LoadSPBattleScenes(doc);
		}

		// Token: 0x0600123A RID: 4666 RVA: 0x00052BBC File Offset: 0x00050DBC
		public void LoadConversationScenes(string path)
		{
			XmlDocument doc = this.LoadXmlFile(path);
			this.LoadConversationScenes(doc);
		}

		// Token: 0x0600123B RID: 4667 RVA: 0x00052BD8 File Offset: 0x00050DD8
		public void LoadMeetingScenes(string path)
		{
			XmlDocument doc = this.LoadXmlFile(path);
			this.LoadMeetingScenes(doc);
		}

		// Token: 0x0600123C RID: 4668 RVA: 0x00052BF4 File Offset: 0x00050DF4
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

		// Token: 0x0600123D RID: 4669 RVA: 0x00052C40 File Offset: 0x00050E40
		private void LoadSPBattleScenes(XmlDocument doc)
		{
			Debug.Print("loading sp_battles.xml", 0, Debug.DebugColor.White, 17592186044416UL);
			if (doc.ChildNodes.Count <= 1)
			{
				throw new TWXmlLoadException("Incorrect XML document format. XML document must have at least 2 child nodes.");
			}
			XmlNode xmlNode = doc.ChildNodes[1];
			if (xmlNode.Name != "SPBattleScenes")
			{
				throw new TWXmlLoadException("Incorrect XML document format. Root node's name must be SPBattleScenes.");
			}
			if (xmlNode.Name == "SPBattleScenes")
			{
				foreach (object obj in xmlNode.ChildNodes)
				{
					XmlNode xmlNode2 = (XmlNode)obj;
					if (xmlNode2.NodeType != XmlNodeType.Comment)
					{
						string sceneID = null;
						List<int> list = new List<int>();
						TerrainType terrain = TerrainType.Plain;
						ForestDensity forestDensity = ForestDensity.None;
						bool isNaval = false;
						for (int i = 0; i < xmlNode2.Attributes.Count; i++)
						{
							if (xmlNode2.Attributes[i].Name == "id")
							{
								sceneID = xmlNode2.Attributes[i].InnerText;
							}
							else if (xmlNode2.Attributes[i].Name == "map_indices")
							{
								foreach (string s in xmlNode2.Attributes[i].InnerText.Replace(" ", "").Split(new char[] { ',' }))
								{
									list.Add(int.Parse(s));
								}
							}
							else if (xmlNode2.Attributes[i].Name == "terrain")
							{
								if (!Enum.TryParse<TerrainType>(xmlNode2.Attributes[i].InnerText, out terrain))
								{
									terrain = TerrainType.Plain;
								}
							}
							else if (xmlNode2.Attributes[i].Name == "forest_density")
							{
								char[] array2 = xmlNode2.Attributes[i].InnerText.ToLower().ToCharArray();
								array2[0] = char.ToUpper(array2[0]);
								if (!Enum.TryParse<ForestDensity>(new string(array2), out forestDensity))
								{
									forestDensity = ForestDensity.None;
								}
							}
							else if (xmlNode2.Attributes[i].Name == "is_naval")
							{
								bool.TryParse(xmlNode2.Attributes[i].Value, out isNaval);
							}
						}
						XmlNodeList childNodes = xmlNode2.ChildNodes;
						List<TerrainType> list2 = new List<TerrainType>();
						foreach (object obj2 in childNodes)
						{
							XmlNode xmlNode3 = (XmlNode)obj2;
							if (xmlNode3.NodeType != XmlNodeType.Comment && xmlNode3.Name == "TerrainTypes")
							{
								foreach (object obj3 in xmlNode3.ChildNodes)
								{
									XmlNode xmlNode4 = (XmlNode)obj3;
									TerrainType item;
									if (xmlNode4.Name == "TerrainType" && Enum.TryParse<TerrainType>(xmlNode4.Attributes["name"].InnerText, out item) && !list2.Contains(item))
									{
										list2.Add(item);
									}
								}
							}
						}
						this._singleplayerBattleScenes.Add(new SingleplayerBattleSceneData(sceneID, terrain, list2, forestDensity, list, isNaval));
					}
				}
			}
		}

		// Token: 0x0600123E RID: 4670 RVA: 0x00053020 File Offset: 0x00051220
		private void LoadConversationScenes(XmlDocument doc)
		{
			Debug.Print("loading conversation_scenes.xml", 0, Debug.DebugColor.White, 17592186044416UL);
			if (doc.ChildNodes.Count <= 1)
			{
				throw new TWXmlLoadException("Incorrect XML document format. XML document must have at least 2 child nodes.");
			}
			XmlNode xmlNode = doc.ChildNodes[1];
			if (xmlNode.Name != "ConversationScenes")
			{
				throw new TWXmlLoadException("Incorrect XML document format. Root node's name must be ConversationScenes.");
			}
			if (xmlNode.Name == "ConversationScenes")
			{
				foreach (object obj in xmlNode.ChildNodes)
				{
					XmlNode xmlNode2 = (XmlNode)obj;
					if (xmlNode2.NodeType != XmlNodeType.Comment)
					{
						string sceneID = null;
						TerrainType terrain = TerrainType.Plain;
						ForestDensity forestDensity = ForestDensity.None;
						for (int i = 0; i < xmlNode2.Attributes.Count; i++)
						{
							if (xmlNode2.Attributes[i].Name == "id")
							{
								sceneID = xmlNode2.Attributes[i].InnerText;
							}
							else if (xmlNode2.Attributes[i].Name == "terrain")
							{
								if (!Enum.TryParse<TerrainType>(xmlNode2.Attributes[i].InnerText, out terrain))
								{
									terrain = TerrainType.Plain;
								}
							}
							else if (xmlNode2.Attributes[i].Name == "forest_density")
							{
								char[] array = xmlNode2.Attributes[i].InnerText.ToLower().ToCharArray();
								array[0] = char.ToUpper(array[0]);
								if (!Enum.TryParse<ForestDensity>(new string(array), out forestDensity))
								{
									forestDensity = ForestDensity.None;
								}
							}
						}
						XmlNodeList childNodes = xmlNode2.ChildNodes;
						List<TerrainType> list = new List<TerrainType>();
						foreach (object obj2 in childNodes)
						{
							XmlNode xmlNode3 = (XmlNode)obj2;
							if (xmlNode3.NodeType != XmlNodeType.Comment && xmlNode3.Name == "flags")
							{
								foreach (object obj3 in xmlNode3.ChildNodes)
								{
									XmlNode xmlNode4 = (XmlNode)obj3;
									TerrainType item;
									if (xmlNode4.NodeType != XmlNodeType.Comment && xmlNode4.Attributes["name"].InnerText == "TerrainType" && Enum.TryParse<TerrainType>(xmlNode4.Attributes["value"].InnerText, out item) && !list.Contains(item))
									{
										list.Add(item);
									}
								}
							}
						}
						this._conversationScenes.Add(new ConversationSceneData(sceneID, terrain, list, forestDensity));
					}
				}
			}
		}

		// Token: 0x0600123F RID: 4671 RVA: 0x0005334C File Offset: 0x0005154C
		private void LoadMeetingScenes(XmlDocument doc)
		{
			Debug.Print("loading meeting_scenes.xml", 0, Debug.DebugColor.White, 17592186044416UL);
			if (doc.ChildNodes.Count <= 1)
			{
				throw new TWXmlLoadException("Incorrect XML document format. XML document must have at least 2 child nodes.");
			}
			XmlNode xmlNode = doc.ChildNodes[1];
			if (xmlNode.Name != "MeetingScenes")
			{
				throw new TWXmlLoadException("Incorrect XML document format. Root node's name must be MeetingScenes.");
			}
			if (xmlNode.Name == "MeetingScenes")
			{
				foreach (object obj in xmlNode.ChildNodes)
				{
					XmlNode xmlNode2 = (XmlNode)obj;
					if (xmlNode2.NodeType != XmlNodeType.Comment)
					{
						string sceneID = null;
						string cultureString = null;
						for (int i = 0; i < xmlNode2.Attributes.Count; i++)
						{
							if (xmlNode2.Attributes[i].Name == "id")
							{
								sceneID = xmlNode2.Attributes[i].InnerText;
							}
							if (xmlNode2.Attributes[i].Name == "culture")
							{
								cultureString = xmlNode2.Attributes[i].InnerText.Split(new char[] { '.' })[1];
							}
						}
						this._meetingScenes.Add(new MeetingSceneData(sceneID, cultureString));
					}
				}
			}
		}

		// Token: 0x040005FD RID: 1533
		private MBList<SingleplayerBattleSceneData> _singleplayerBattleScenes;

		// Token: 0x040005FE RID: 1534
		private MBList<ConversationSceneData> _conversationScenes;

		// Token: 0x040005FF RID: 1535
		private MBList<MeetingSceneData> _meetingScenes;

		// Token: 0x04000600 RID: 1536
		private const TerrainType DefaultTerrain = TerrainType.Plain;

		// Token: 0x04000601 RID: 1537
		private const ForestDensity DefaultForestDensity = ForestDensity.None;
	}
}
