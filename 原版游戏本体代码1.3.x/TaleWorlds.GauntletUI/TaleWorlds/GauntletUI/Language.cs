using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000022 RID: 34
	public class Language : ILanguage
	{
		// Token: 0x170000CF RID: 207
		// (get) Token: 0x060002D2 RID: 722 RVA: 0x0000E3EB File Offset: 0x0000C5EB
		// (set) Token: 0x060002D3 RID: 723 RVA: 0x0000E3F3 File Offset: 0x0000C5F3
		public char[] ForbiddenStartOfLineCharacters { get; private set; }

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x060002D4 RID: 724 RVA: 0x0000E3FC File Offset: 0x0000C5FC
		// (set) Token: 0x060002D5 RID: 725 RVA: 0x0000E404 File Offset: 0x0000C604
		public char[] ForbiddenEndOfLineCharacters { get; private set; }

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x060002D6 RID: 726 RVA: 0x0000E40D File Offset: 0x0000C60D
		// (set) Token: 0x060002D7 RID: 727 RVA: 0x0000E415 File Offset: 0x0000C615
		public string LanguageID { get; private set; }

		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x060002D8 RID: 728 RVA: 0x0000E41E File Offset: 0x0000C61E
		// (set) Token: 0x060002D9 RID: 729 RVA: 0x0000E426 File Offset: 0x0000C626
		public string DefaultFontName { get; private set; }

		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x060002DA RID: 730 RVA: 0x0000E42F File Offset: 0x0000C62F
		// (set) Token: 0x060002DB RID: 731 RVA: 0x0000E437 File Offset: 0x0000C637
		public bool DoesFontRequireSpaceForNewline { get; private set; } = true;

		// Token: 0x170000D4 RID: 212
		// (get) Token: 0x060002DC RID: 732 RVA: 0x0000E440 File Offset: 0x0000C640
		// (set) Token: 0x060002DD RID: 733 RVA: 0x0000E448 File Offset: 0x0000C648
		public Font DefaultFont { get; private set; }

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x060002DE RID: 734 RVA: 0x0000E451 File Offset: 0x0000C651
		// (set) Token: 0x060002DF RID: 735 RVA: 0x0000E459 File Offset: 0x0000C659
		public char LineSeperatorChar { get; private set; }

		// Token: 0x060002E0 RID: 736 RVA: 0x0000E462 File Offset: 0x0000C662
		public bool FontMapHasKey(string keyFontName)
		{
			return this._fontMap.ContainsKey(keyFontName);
		}

		// Token: 0x060002E1 RID: 737 RVA: 0x0000E470 File Offset: 0x0000C670
		public Font GetMappedFont(string keyFontName)
		{
			return this._fontMap[keyFontName];
		}

		// Token: 0x060002E2 RID: 738 RVA: 0x0000E47E File Offset: 0x0000C67E
		private Language()
		{
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x0000E498 File Offset: 0x0000C698
		public static Language CreateFrom(XmlNode languageNode, FontFactory fontFactory)
		{
			Language language = new Language
			{
				LanguageID = languageNode.Attributes["id"].InnerText
			};
			Language language2 = language;
			XmlAttribute xmlAttribute = languageNode.Attributes["DefaultFont"];
			language2.DefaultFontName = ((xmlAttribute != null) ? xmlAttribute.InnerText : null) ?? "Galahad";
			Language language3 = language;
			XmlAttribute xmlAttribute2 = languageNode.Attributes["LineSeperatorChar"];
			language3.LineSeperatorChar = ((xmlAttribute2 != null) ? xmlAttribute2.InnerText[0] : '-');
			language.DefaultFont = fontFactory.GetFont(language.DefaultFontName);
			foreach (object obj in languageNode.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.NodeType == XmlNodeType.Element)
				{
					if (xmlNode.Name == "Map")
					{
						string innerText = xmlNode.Attributes["From"].InnerText;
						string innerText2 = xmlNode.Attributes["To"].InnerText;
						language._fontMap.Add(innerText, fontFactory.GetFont(innerText2));
					}
					else if (xmlNode.Name == "NewlineDoesntRequireSpace")
					{
						language.DoesFontRequireSpaceForNewline = false;
					}
					else if (xmlNode.Name == "ForbiddenStartOfLineCharacters")
					{
						Language language4 = language;
						XmlAttribute xmlAttribute3 = xmlNode.Attributes["Characters"];
						language4.ForbiddenStartOfLineCharacters = ((xmlAttribute3 != null) ? xmlAttribute3.InnerText.ToCharArray() : null);
					}
					else if (xmlNode.Name == "ForbiddenEndOfLineCharacters")
					{
						Language language5 = language;
						XmlAttribute xmlAttribute4 = xmlNode.Attributes["Characters"];
						language5.ForbiddenEndOfLineCharacters = ((xmlAttribute4 != null) ? xmlAttribute4.InnerText.ToCharArray() : null);
					}
				}
			}
			return language;
		}

		// Token: 0x060002E4 RID: 740 RVA: 0x0000E67C File Offset: 0x0000C87C
		IEnumerable<char> ILanguage.GetForbiddenStartOfLineCharacters()
		{
			return this.ForbiddenStartOfLineCharacters;
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x0000E684 File Offset: 0x0000C884
		IEnumerable<char> ILanguage.GetForbiddenEndOfLineCharacters()
		{
			return this.ForbiddenEndOfLineCharacters;
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x0000E68C File Offset: 0x0000C88C
		bool ILanguage.IsCharacterForbiddenAtStartOfLine(char character)
		{
			if (this.ForbiddenStartOfLineCharacters == null || this.ForbiddenStartOfLineCharacters.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < this.ForbiddenStartOfLineCharacters.Length; i++)
			{
				if (this.ForbiddenStartOfLineCharacters[i] == character)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060002E7 RID: 743 RVA: 0x0000E6D0 File Offset: 0x0000C8D0
		bool ILanguage.IsCharacterForbiddenAtEndOfLine(char character)
		{
			if (this.ForbiddenEndOfLineCharacters == null || this.ForbiddenEndOfLineCharacters.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < this.ForbiddenEndOfLineCharacters.Length; i++)
			{
				if (this.ForbiddenEndOfLineCharacters[i] == character)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x0000E711 File Offset: 0x0000C911
		string ILanguage.GetLanguageID()
		{
			return this.LanguageID;
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x0000E719 File Offset: 0x0000C919
		string ILanguage.GetDefaultFontName()
		{
			return this.DefaultFontName;
		}

		// Token: 0x060002EA RID: 746 RVA: 0x0000E721 File Offset: 0x0000C921
		Font ILanguage.GetDefaultFont()
		{
			return this.DefaultFont;
		}

		// Token: 0x060002EB RID: 747 RVA: 0x0000E729 File Offset: 0x0000C929
		char ILanguage.GetLineSeperatorChar()
		{
			return this.LineSeperatorChar;
		}

		// Token: 0x060002EC RID: 748 RVA: 0x0000E731 File Offset: 0x0000C931
		bool ILanguage.DoesLanguageRequireSpaceForNewline()
		{
			return this.DoesFontRequireSpaceForNewline;
		}

		// Token: 0x060002ED RID: 749 RVA: 0x0000E739 File Offset: 0x0000C939
		bool ILanguage.FontMapHasKey(string keyFontName)
		{
			return this._fontMap.ContainsKey(keyFontName);
		}

		// Token: 0x060002EE RID: 750 RVA: 0x0000E747 File Offset: 0x0000C947
		Font ILanguage.GetMappedFont(string keyFontName)
		{
			return this._fontMap[keyFontName];
		}

		// Token: 0x04000170 RID: 368
		private readonly Dictionary<string, Font> _fontMap = new Dictionary<string, Font>();
	}
}
