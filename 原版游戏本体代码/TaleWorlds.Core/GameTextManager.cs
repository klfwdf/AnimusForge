using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x02000077 RID: 119
	public class GameTextManager
	{
		// Token: 0x06000831 RID: 2097 RVA: 0x0001B161 File Offset: 0x00019361
		public GameTextManager()
		{
			this._gameTexts = new Dictionary<string, GameText>();
		}

		// Token: 0x06000832 RID: 2098 RVA: 0x0001B174 File Offset: 0x00019374
		public GameText GetGameText(string id)
		{
			GameText result;
			if (this._gameTexts.TryGetValue(id, out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x06000833 RID: 2099 RVA: 0x0001B194 File Offset: 0x00019394
		public GameText AddGameText(string id)
		{
			GameText gameText;
			if (!this._gameTexts.TryGetValue(id, out gameText))
			{
				gameText = new GameText(id);
				this._gameTexts.Add(gameText.Id, gameText);
			}
			return gameText;
		}

		// Token: 0x06000834 RID: 2100 RVA: 0x0001B1CC File Offset: 0x000193CC
		public bool TryGetText(string id, string variation, out TextObject text)
		{
			text = null;
			GameText gameText;
			this._gameTexts.TryGetValue(id, out gameText);
			if (gameText != null)
			{
				if (variation == null)
				{
					text = gameText.DefaultText;
				}
				else
				{
					text = gameText.GetVariation(variation);
				}
				if (text != null)
				{
					text = text.CopyTextObject();
					text.AddIDToValue(id);
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000835 RID: 2101 RVA: 0x0001B224 File Offset: 0x00019424
		public TextObject FindText(string id, string variation = null)
		{
			TextObject result;
			if (this.TryGetText(id, variation, out result))
			{
				return result;
			}
			TextObject result2;
			if (variation == null)
			{
				result2 = new TextObject("{=!}ERROR: Text with id " + id + " doesn't exist!", null);
			}
			else
			{
				result2 = new TextObject("{=!}ERROR: Text with id " + id + " doesn't exist! Variation: " + variation, null);
			}
			return result2;
		}

		// Token: 0x06000836 RID: 2102 RVA: 0x0001B274 File Offset: 0x00019474
		public IEnumerable<TextObject> FindAllTextVariations(string id)
		{
			GameText gameText;
			this._gameTexts.TryGetValue(id, out gameText);
			if (gameText != null)
			{
				foreach (GameText.GameTextVariation gameTextVariation in gameText.Variations)
				{
					yield return gameTextVariation.Text;
				}
				IEnumerator<GameText.GameTextVariation> enumerator = null;
			}
			yield break;
			yield break;
		}

		// Token: 0x06000837 RID: 2103 RVA: 0x0001B28C File Offset: 0x0001948C
		public void LoadGameTexts()
		{
			Game game = Game.Current;
			bool ignoreGameTypeInclusionCheck = false;
			string gameType = "";
			if (game != null)
			{
				ignoreGameTypeInclusionCheck = game.GameType.IsDevelopment;
				gameType = game.GameType.GetType().Name;
			}
			XmlDocument mergedXmlForManaged = MBObjectManager.GetMergedXmlForManaged("GameText", false, ignoreGameTypeInclusionCheck, gameType);
			try
			{
				this.LoadFromXML(mergedXmlForManaged);
			}
			catch (Exception)
			{
			}
		}

		// Token: 0x06000838 RID: 2104 RVA: 0x0001B2F4 File Offset: 0x000194F4
		public void LoadDefaultTexts()
		{
			try
			{
				List<string> list = new List<string>();
				foreach (ModuleInfo moduleInfo in ModuleHelper.GetModules(null))
				{
					string text = moduleInfo.FolderPath + "/ModuleData/global_strings.xml";
					if (File.Exists(text))
					{
						list.Add(text);
					}
				}
				list.Add(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/consoles.xml");
				foreach (string text2 in list)
				{
					Debug.Print("opening " + text2, 0, Debug.DebugColor.White, 17592186044416UL);
					XmlDocument xmlDocument = new XmlDocument();
					StreamReader streamReader = new StreamReader(text2);
					string xml = streamReader.ReadToEnd();
					xmlDocument.LoadXml(xml);
					streamReader.Close();
					this.LoadFromXML(xmlDocument);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		// Token: 0x06000839 RID: 2105 RVA: 0x0001B410 File Offset: 0x00019610
		private void LoadFromXML(XmlDocument doc)
		{
			XmlNode xmlNode = null;
			for (int i = 0; i < doc.ChildNodes.Count; i++)
			{
				XmlNode xmlNode2 = doc.ChildNodes[i];
				if (xmlNode2.NodeType != XmlNodeType.Comment && xmlNode2.Name == "strings" && xmlNode2.ChildNodes.Count > 0)
				{
					xmlNode = xmlNode2.ChildNodes[0];
					IL_1FF:
					while (xmlNode != null)
					{
						try
						{
							if (xmlNode.Name == "string" && xmlNode.NodeType != XmlNodeType.Comment)
							{
								if (xmlNode.Attributes == null)
								{
									throw new TWXmlLoadException("Node attributes are null.");
								}
								string[] array = xmlNode.Attributes["id"].Value.Split(new char[] { '.' });
								string id = array[0];
								GameText gameText = this.AddGameText(id);
								string variationId = "";
								if (array.Length > 1)
								{
									variationId = array[1];
								}
								TextObject textObject = new TextObject(xmlNode.Attributes["text"].Value, null);
								List<GameTextManager.ChoiceTag> list = new List<GameTextManager.ChoiceTag>();
								foreach (object obj in xmlNode.ChildNodes)
								{
									XmlNode xmlNode3 = (XmlNode)obj;
									if (xmlNode3.Name == "tags")
									{
										XmlNodeList childNodes = xmlNode3.ChildNodes;
										for (int j = 0; j < childNodes.Count; j++)
										{
											XmlAttributeCollection attributes = childNodes[j].Attributes;
											if (attributes != null)
											{
												int weight = 1;
												if (attributes["weight"] != null)
												{
													int.TryParse(attributes["weight"].Value, out weight);
												}
												GameTextManager.ChoiceTag item = new GameTextManager.ChoiceTag(attributes["tag_name"].Value, weight);
												list.Add(item);
											}
										}
									}
								}
								textObject.CacheTokens();
								gameText.AddVariationWithId(variationId, textObject, list);
							}
						}
						catch (Exception)
						{
						}
						finally
						{
							xmlNode = xmlNode.NextSibling;
						}
					}
					return;
				}
			}
			goto IL_1FF;
		}

		// Token: 0x0400041C RID: 1052
		private readonly Dictionary<string, GameText> _gameTexts;

		// Token: 0x0200011A RID: 282
		public struct ChoiceTag
		{
			// Token: 0x170003F7 RID: 1015
			// (get) Token: 0x06000BED RID: 3053 RVA: 0x00026047 File Offset: 0x00024247
			// (set) Token: 0x06000BEE RID: 3054 RVA: 0x0002604F File Offset: 0x0002424F
			public string TagName { get; private set; }

			// Token: 0x170003F8 RID: 1016
			// (get) Token: 0x06000BEF RID: 3055 RVA: 0x00026058 File Offset: 0x00024258
			// (set) Token: 0x06000BF0 RID: 3056 RVA: 0x00026060 File Offset: 0x00024260
			public uint Weight { get; private set; }

			// Token: 0x170003F9 RID: 1017
			// (get) Token: 0x06000BF1 RID: 3057 RVA: 0x00026069 File Offset: 0x00024269
			// (set) Token: 0x06000BF2 RID: 3058 RVA: 0x00026071 File Offset: 0x00024271
			public bool IsTagReversed { get; private set; }

			// Token: 0x06000BF3 RID: 3059 RVA: 0x0002607A File Offset: 0x0002427A
			public ChoiceTag(string tagName, int weight)
			{
				this = default(GameTextManager.ChoiceTag);
				this.TagName = tagName;
				this.Weight = (uint)MathF.Abs(weight);
				this.IsTagReversed = weight < 0;
			}
		}
	}
}
