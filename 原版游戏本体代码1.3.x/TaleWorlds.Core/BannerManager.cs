using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x02000018 RID: 24
	public class BannerManager
	{
		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000F8 RID: 248 RVA: 0x00004BA2 File Offset: 0x00002DA2
		// (set) Token: 0x060000F9 RID: 249 RVA: 0x00004BA9 File Offset: 0x00002DA9
		public static BannerManager Instance { get; private set; }

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000FA RID: 250 RVA: 0x00004BB1 File Offset: 0x00002DB1
		public MBReadOnlyList<BannerIconGroup> BannerIconGroups
		{
			get
			{
				return this._bannerIconGroups;
			}
		}

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060000FB RID: 251 RVA: 0x00004BB9 File Offset: 0x00002DB9
		// (set) Token: 0x060000FC RID: 252 RVA: 0x00004BC1 File Offset: 0x00002DC1
		public int BaseBackgroundId { get; private set; }

		// Token: 0x060000FD RID: 253 RVA: 0x00004BCA File Offset: 0x00002DCA
		private BannerManager()
		{
			this._bannerIconGroups = new MBList<BannerIconGroup>();
			this._colorPalette = new Dictionary<int, BannerColor>();
			this._cultureColorPalette = new Dictionary<BasicCultureObject, List<BannerColor>>();
			this.ReadOnlyColorPalette = this._colorPalette.GetReadOnlyDictionary<int, BannerColor>();
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00004C04 File Offset: 0x00002E04
		public static void Initialize()
		{
			if (BannerManager.Instance == null)
			{
				BannerManager.Instance = new BannerManager();
			}
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00004C17 File Offset: 0x00002E17
		public static void ResetAndLoad()
		{
			BannerManager.Instance._bannerIconGroups = new MBList<BannerIconGroup>();
			BannerManager.Instance._colorPalette = new Dictionary<int, BannerColor>();
			BannerManager.Instance._cultureColorPalette = new Dictionary<BasicCultureObject, List<BannerColor>>();
			BannerManager.Instance.LoadBannerIcons();
		}

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000100 RID: 256 RVA: 0x00004C50 File Offset: 0x00002E50
		private static MBReadOnlyDictionary<int, BannerColor> ColorPalette
		{
			get
			{
				return BannerManager.Instance.ReadOnlyColorPalette;
			}
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00004C5C File Offset: 0x00002E5C
		public static uint GetColor(int id)
		{
			BannerColor bannerColor;
			if (BannerManager.ColorPalette.TryGetValue(id, out bannerColor))
			{
				return bannerColor.Color;
			}
			return 3735928559U;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00004C88 File Offset: 0x00002E88
		public static int GetColorId(uint color)
		{
			foreach (KeyValuePair<int, BannerColor> keyValuePair in BannerManager.ColorPalette)
			{
				if (keyValuePair.Value.Color == color)
				{
					return keyValuePair.Key;
				}
			}
			return -1;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00004CF4 File Offset: 0x00002EF4
		public int GetRandomColorId(MBFastRandom random)
		{
			return BannerManager.ColorPalette.ElementAt(random.Next(BannerManager.ColorPalette.Count<KeyValuePair<int, BannerColor>>())).Key;
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00004D24 File Offset: 0x00002F24
		public BannerIconData GetIconDataFromIconId(int id)
		{
			using (List<BannerIconGroup>.Enumerator enumerator = this._bannerIconGroups.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					BannerIconData result;
					if (enumerator.Current.AllIcons.TryGetValue(id, out result))
					{
						return result;
					}
				}
			}
			return default(BannerIconData);
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00004D90 File Offset: 0x00002F90
		public int GetRandomBackgroundId(MBFastRandom random)
		{
			int num = random.Next(0, this._availablePatternCount);
			foreach (BannerIconGroup bannerIconGroup in this.BannerIconGroups)
			{
				if (bannerIconGroup.IsPattern)
				{
					if (num < bannerIconGroup.AllBackgrounds.Count)
					{
						return bannerIconGroup.AllBackgrounds.ElementAt(num).Key;
					}
					num -= bannerIconGroup.AllBackgrounds.Count;
				}
			}
			return -1;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00004E2C File Offset: 0x0000302C
		public int GetRandomBannerIconId(MBFastRandom random)
		{
			int num = random.Next(0, this._availableIconCount);
			foreach (BannerIconGroup bannerIconGroup in this.BannerIconGroups)
			{
				if (!bannerIconGroup.IsPattern)
				{
					if (num < bannerIconGroup.AvailableIcons.Count)
					{
						return bannerIconGroup.AvailableIcons.ElementAt(num).Key;
					}
					num -= bannerIconGroup.AvailableIcons.Count;
				}
			}
			return -1;
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00004EC8 File Offset: 0x000030C8
		public string GetBackgroundMeshName(int id)
		{
			foreach (BannerIconGroup bannerIconGroup in this.BannerIconGroups)
			{
				if (bannerIconGroup.IsPattern && bannerIconGroup.AllBackgrounds.ContainsKey(id))
				{
					return bannerIconGroup.AllBackgrounds[id];
				}
			}
			return null;
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00004F3C File Offset: 0x0000313C
		public string GetIconSourceTextureName(int id)
		{
			foreach (BannerIconGroup bannerIconGroup in this.BannerIconGroups)
			{
				if (!bannerIconGroup.IsPattern && bannerIconGroup.AllBackgrounds.ContainsKey(id))
				{
					return bannerIconGroup.AllBackgrounds[id];
				}
			}
			return null;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00004FB0 File Offset: 0x000031B0
		public void SetBaseBackgroundId(int id)
		{
			this.BaseBackgroundId = id;
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00004FB9 File Offset: 0x000031B9
		public void SetCultureColors(BasicCultureObject culture, List<BannerColor> color)
		{
			if (!this._cultureColorPalette.ContainsKey(culture))
			{
				this._cultureColorPalette[culture] = color;
				return;
			}
			Debug.FailedAssert("Culture colors already set", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BannerManager.cs", "SetCultureColors", 200);
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00004FF0 File Offset: 0x000031F0
		public void LoadBannerIcons()
		{
			Game game = Game.Current;
			bool ignoreGameTypeInclusionCheck = false;
			string gameType = "";
			if (game != null)
			{
				ignoreGameTypeInclusionCheck = game.GameType.IsDevelopment;
				gameType = game.GameType.GetType().Name;
			}
			XmlDocument mergedXmlForManaged = MBObjectManager.GetMergedXmlForManaged("BannerIcons", false, ignoreGameTypeInclusionCheck, gameType);
			this.LoadBannerIconsFromXml(mergedXmlForManaged);
		}

		// Token: 0x0600010C RID: 268 RVA: 0x00005040 File Offset: 0x00003240
		public void LoadBannerIcons(string xmlPath)
		{
			XmlDocument doc = this.LoadXmlFile(xmlPath);
			this.LoadBannerIconsFromXml(doc);
		}

		// Token: 0x0600010D RID: 269 RVA: 0x0000505C File Offset: 0x0000325C
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

		// Token: 0x0600010E RID: 270 RVA: 0x000050A8 File Offset: 0x000032A8
		private void LoadBannerIconsFromXml(XmlDocument doc)
		{
			Debug.Print("loading banner_icons.xml:", 0, Debug.DebugColor.White, 17592186044416UL);
			XmlNodeList elementsByTagName = doc.GetElementsByTagName("base");
			if (elementsByTagName.Count != 1)
			{
				throw new TWXmlLoadException("Incorrect XML document format.");
			}
			XmlNode xmlNode = elementsByTagName[0].ChildNodes[0];
			if (xmlNode.Name != "BannerIconData")
			{
				throw new TWXmlLoadException("Incorrect XML document format.");
			}
			if (xmlNode.Name == "BannerIconData")
			{
				foreach (object obj in xmlNode.ChildNodes)
				{
					XmlNode xmlNode2 = (XmlNode)obj;
					if (xmlNode2.Name == "BannerIconGroup")
					{
						BannerIconGroup bannerIconGroup = new BannerIconGroup();
						bannerIconGroup.Deserialize(xmlNode2, this._bannerIconGroups);
						BannerIconGroup bannerIconGroup3 = this._bannerIconGroups.FirstOrDefault((BannerIconGroup x) => x.Id == bannerIconGroup.Id);
						if (bannerIconGroup3 == null)
						{
							this._bannerIconGroups.Add(bannerIconGroup);
						}
						else
						{
							bannerIconGroup3.Merge(bannerIconGroup);
						}
					}
					if (xmlNode2.Name == "BannerColors")
					{
						foreach (object obj2 in xmlNode2.ChildNodes)
						{
							XmlNode xmlNode3 = (XmlNode)obj2;
							if (xmlNode3.Name == "Color")
							{
								int key = Convert.ToInt32(xmlNode3.Attributes["id"].Value);
								if (!this._colorPalette.ContainsKey(key))
								{
									uint color = Convert.ToUInt32(xmlNode3.Attributes["hex"].Value, 16);
									XmlAttribute xmlAttribute = xmlNode3.Attributes["player_can_choose_for_sigil"];
									bool playerCanChooseForSigil = Convert.ToBoolean(((xmlAttribute != null) ? xmlAttribute.Value : null) ?? "false");
									XmlAttribute xmlAttribute2 = xmlNode3.Attributes["player_can_choose_for_background"];
									bool playerCanChooseForBackground = Convert.ToBoolean(((xmlAttribute2 != null) ? xmlAttribute2.Value : null) ?? "false");
									this._colorPalette.Add(key, new BannerColor(color, playerCanChooseForSigil, playerCanChooseForBackground));
								}
							}
						}
					}
				}
			}
			this._availablePatternCount = 0;
			this._availableIconCount = 0;
			foreach (BannerIconGroup bannerIconGroup2 in this._bannerIconGroups)
			{
				if (bannerIconGroup2.IsPattern)
				{
					this._availablePatternCount += bannerIconGroup2.AllBackgrounds.Count;
				}
				else
				{
					this._availableIconCount += bannerIconGroup2.AvailableIcons.Count;
				}
			}
		}

		// Token: 0x04000120 RID: 288
		public const int DarkRed = 1;

		// Token: 0x04000121 RID: 289
		public const int Green = 120;

		// Token: 0x04000122 RID: 290
		public const int Blue = 119;

		// Token: 0x04000123 RID: 291
		public const int Purple = 4;

		// Token: 0x04000124 RID: 292
		public const int DarkPurple = 6;

		// Token: 0x04000125 RID: 293
		public const int Orange = 9;

		// Token: 0x04000126 RID: 294
		public const int DarkBlue = 12;

		// Token: 0x04000127 RID: 295
		public const int Red = 118;

		// Token: 0x04000128 RID: 296
		public const int Yellow = 121;

		// Token: 0x0400012A RID: 298
		public MBReadOnlyDictionary<int, BannerColor> ReadOnlyColorPalette;

		// Token: 0x0400012B RID: 299
		private Dictionary<BasicCultureObject, List<BannerColor>> _cultureColorPalette;

		// Token: 0x0400012C RID: 300
		private Dictionary<int, BannerColor> _colorPalette;

		// Token: 0x0400012D RID: 301
		private MBList<BannerIconGroup> _bannerIconGroups;

		// Token: 0x0400012F RID: 303
		private int _availablePatternCount;

		// Token: 0x04000130 RID: 304
		private int _availableIconCount;
	}
}
