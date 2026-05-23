using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000021 RID: 33
	public class FontFactory
	{
		// Token: 0x170000CC RID: 204
		// (get) Token: 0x060002C1 RID: 705 RVA: 0x0000DCF2 File Offset: 0x0000BEF2
		// (set) Token: 0x060002C2 RID: 706 RVA: 0x0000DCFA File Offset: 0x0000BEFA
		public Language DefaultLanguage { get; private set; }

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x060002C3 RID: 707 RVA: 0x0000DD04 File Offset: 0x0000BF04
		// (set) Token: 0x060002C4 RID: 708 RVA: 0x0000DEC2 File Offset: 0x0000C0C2
		public Language CurrentLanguage
		{
			get
			{
				if (this._currentLangugage != null)
				{
					return this._currentLangugage;
				}
				if (this.DefaultLanguage != null)
				{
					string str = "Couldn't find language in language map: ";
					Language currentLangugage = this._currentLangugage;
					Debug.Print(str + ((currentLangugage != null) ? currentLangugage.LanguageID : null), 0, Debug.DebugColor.White, 17592186044416UL);
					string str2 = "Couldn't find language in language map: ";
					Language currentLangugage2 = this._currentLangugage;
					Debug.FailedAssert(str2 + ((currentLangugage2 != null) ? currentLangugage2.LanguageID : null), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "CurrentLanguage", 26);
					this._currentLangugage = this.DefaultLanguage;
					return this._currentLangugage;
				}
				Language language;
				if (this._fontLanguageMap.TryGetValue("English", out language))
				{
					string str3 = "Couldn't find default language(";
					Language defaultLanguage = this.DefaultLanguage;
					Debug.Print(str3 + (((defaultLanguage != null) ? defaultLanguage.LanguageID : null) ?? "INVALID") + ") in language map.", 0, Debug.DebugColor.White, 17592186044416UL);
					string str4 = "Couldn't find default language(";
					Language defaultLanguage2 = this.DefaultLanguage;
					Debug.FailedAssert(str4 + (((defaultLanguage2 != null) ? defaultLanguage2.LanguageID : null) ?? "INVALID") + ") in language map.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "CurrentLanguage", 35);
					this.DefaultLanguage = language;
					this._currentLangugage = language;
					return this._currentLangugage;
				}
				Debug.Print("Couldn't find English language in language map.", 0, Debug.DebugColor.White, 17592186044416UL);
				Debug.FailedAssert("Couldn't find English language in language map.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "CurrentLanguage", 45);
				this.DefaultLanguage = this._fontLanguageMap.FirstOrDefault<KeyValuePair<string, Language>>().Value;
				this._currentLangugage = this.DefaultLanguage;
				if (this._currentLangugage == null)
				{
					Debug.Print("There are no languages in language map", 0, Debug.DebugColor.White, 17592186044416UL);
					Debug.FailedAssert("There are no languages in language map", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "CurrentLanguage", 54);
				}
				return this._currentLangugage;
			}
			private set
			{
				if (value != this._currentLangugage)
				{
					this._currentLangugage = value;
				}
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x060002C5 RID: 709 RVA: 0x0000DED4 File Offset: 0x0000C0D4
		public Font DefaultFont
		{
			get
			{
				return this.CurrentLanguage.DefaultFont;
			}
		}

		// Token: 0x060002C6 RID: 710 RVA: 0x0000DEE1 File Offset: 0x0000C0E1
		public FontFactory(ResourceDepot resourceDepot)
		{
			this._resourceDepot = resourceDepot;
			this._bitmapFonts = new Dictionary<string, Font>();
			this._fontLanguageMap = new Dictionary<string, Language>();
			this._resourceDepot.OnResourceChange += this.OnResourceChange;
		}

		// Token: 0x060002C7 RID: 711 RVA: 0x0000DF1D File Offset: 0x0000C11D
		private void OnResourceChange()
		{
			this.CheckForUpdates();
		}

		// Token: 0x060002C8 RID: 712 RVA: 0x0000DF28 File Offset: 0x0000C128
		public void LoadAllFonts(SpriteData spriteData)
		{
			foreach (string path in this._resourceDepot.GetFiles("Fonts", ".fnt", false))
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
				this.TryAddFontDefinition(Path.GetDirectoryName(path) + "/", fileNameWithoutExtension, spriteData);
			}
			foreach (string text in this._resourceDepot.GetFiles("Fonts", ".xml", false))
			{
				if (Path.GetFileNameWithoutExtension(text).EndsWith("Languages"))
				{
					try
					{
						this.LoadLocalizationValues(text);
					}
					catch (Exception)
					{
						Debug.FailedAssert("Failed to load language at path: " + text, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "LoadAllFonts", 128);
					}
				}
			}
			Language defaultLanguage;
			if (this.DefaultLanguage == null && this._fontLanguageMap.TryGetValue("English", out defaultLanguage))
			{
				this.DefaultLanguage = defaultLanguage;
				this.CurrentLanguage = this.DefaultLanguage;
			}
			this._latestSpriteData = spriteData;
		}

		// Token: 0x060002C9 RID: 713 RVA: 0x0000E030 File Offset: 0x0000C230
		public bool TryAddFontDefinition(string fontPath, string fontName, SpriteData spriteData)
		{
			Font font = new Font(fontName);
			string path = fontPath + fontName + ".fnt";
			bool flag = font.TryLoadFontFromPath(path, spriteData);
			if (flag)
			{
				this._bitmapFonts.Add(fontName, font);
			}
			return flag;
		}

		// Token: 0x060002CA RID: 714 RVA: 0x0000E06C File Offset: 0x0000C26C
		public void LoadLocalizationValues(string sourceXMLPath)
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(sourceXMLPath);
			XmlElement xmlElement = xmlDocument["Languages"];
			XmlAttribute xmlAttribute = xmlElement.Attributes["DefaultLanguage"];
			if (xmlAttribute != null)
			{
				string innerText = xmlAttribute.InnerText;
			}
			foreach (object obj in xmlElement)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.NodeType == XmlNodeType.Element && xmlNode.Name == "Language")
				{
					Language language = Language.CreateFrom(xmlNode, this);
					Language language2;
					if (this._fontLanguageMap.TryGetValue(language.LanguageID, out language2))
					{
						this._fontLanguageMap[language.LanguageID] = language;
					}
					else
					{
						this._fontLanguageMap.Add(language.LanguageID, language);
					}
				}
			}
			XmlAttribute xmlAttribute2 = xmlElement.Attributes["DefaultLanguage"];
			string text = ((xmlAttribute2 != null) ? xmlAttribute2.InnerText : null);
			Language defaultLanguage;
			if (!string.IsNullOrEmpty(text) && this._fontLanguageMap.TryGetValue(text, out defaultLanguage))
			{
				this.DefaultLanguage = defaultLanguage;
				this.CurrentLanguage = this.DefaultLanguage;
				return;
			}
			Debug.FailedAssert("DefaultLanguage cannot be found in the dictionary.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "LoadLocalizationValues", 200);
			if (this._fontLanguageMap.TryGetValue("English", out defaultLanguage))
			{
				this.DefaultLanguage = defaultLanguage;
				this.CurrentLanguage = this.DefaultLanguage;
				return;
			}
			Debug.FailedAssert("English cannot be found in the dictionary.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "LoadLocalizationValues", 209);
		}

		// Token: 0x060002CB RID: 715 RVA: 0x0000E1FC File Offset: 0x0000C3FC
		public Font GetFont(string fontName)
		{
			if (this._bitmapFonts.ContainsKey(fontName))
			{
				return this._bitmapFonts[fontName];
			}
			return this.DefaultFont;
		}

		// Token: 0x060002CC RID: 716 RVA: 0x0000E21F File Offset: 0x0000C41F
		public IEnumerable<Font> GetFonts()
		{
			return this._bitmapFonts.Values;
		}

		// Token: 0x060002CD RID: 717 RVA: 0x0000E22C File Offset: 0x0000C42C
		public string GetFontName(Font font)
		{
			return this._bitmapFonts.FirstOrDefault((KeyValuePair<string, Font> f) => f.Value == font).Key;
		}

		// Token: 0x060002CE RID: 718 RVA: 0x0000E268 File Offset: 0x0000C468
		public Font GetMappedFontForLocalization(string englishFontName)
		{
			if (string.IsNullOrEmpty(englishFontName))
			{
				return this.DefaultFont;
			}
			if (this.DefaultLanguage != this.CurrentLanguage && this.CurrentLanguage != null && this.CurrentLanguage.FontMapHasKey(englishFontName))
			{
				return this.CurrentLanguage.GetMappedFont(englishFontName);
			}
			return this.GetFont(englishFontName);
		}

		// Token: 0x060002CF RID: 719 RVA: 0x0000E2BC File Offset: 0x0000C4BC
		public void OnLanguageChange(string newLanguageCode)
		{
			Language currentLanguage = this.CurrentLanguage;
			if (((currentLanguage != null) ? currentLanguage.LanguageID : null) != newLanguageCode)
			{
				Language currentLanguage2;
				if (!string.IsNullOrEmpty(newLanguageCode) && this._fontLanguageMap.TryGetValue(newLanguageCode, out currentLanguage2))
				{
					this.CurrentLanguage = currentLanguage2;
					return;
				}
				Debug.FailedAssert(newLanguageCode + " doesn't exist in the dictionary!", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "OnLanguageChange", 260);
			}
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x0000E324 File Offset: 0x0000C524
		public Font GetUsableFontForCharacter(int characterCode)
		{
			for (int i = 0; i < this._fontLanguageMap.Values.Count; i++)
			{
				if (this._fontLanguageMap.ElementAt(i).Value.DefaultFont.Characters.ContainsKey(characterCode))
				{
					return this._fontLanguageMap.ElementAt(i).Value.DefaultFont;
				}
			}
			return null;
		}

		// Token: 0x060002D1 RID: 721 RVA: 0x0000E390 File Offset: 0x0000C590
		public void CheckForUpdates()
		{
			Language currentLanguage = this.CurrentLanguage;
			if (currentLanguage != null)
			{
				string languageID = currentLanguage.LanguageID;
			}
			this.DefaultLanguage = null;
			this.CurrentLanguage = null;
			this._bitmapFonts.Clear();
			this._fontLanguageMap.Clear();
			this.LoadAllFonts(this._latestSpriteData);
			Language language = null;
			if (language != null)
			{
				this.CurrentLanguage = language;
			}
		}

		// Token: 0x04000164 RID: 356
		private Language _currentLangugage;

		// Token: 0x04000165 RID: 357
		private readonly Dictionary<string, Font> _bitmapFonts;

		// Token: 0x04000166 RID: 358
		private readonly ResourceDepot _resourceDepot;

		// Token: 0x04000167 RID: 359
		private readonly Dictionary<string, Language> _fontLanguageMap;

		// Token: 0x04000168 RID: 360
		private SpriteData _latestSpriteData;
	}
}
