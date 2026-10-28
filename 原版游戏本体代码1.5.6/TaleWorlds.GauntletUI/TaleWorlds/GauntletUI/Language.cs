using System.Collections.Generic;
using System.Xml;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class Language : ILanguage
{
	private readonly Dictionary<string, Font> _fontMap = new Dictionary<string, Font>();

	public char[] ForbiddenStartOfLineCharacters { get; private set; }

	public char[] ForbiddenEndOfLineCharacters { get; private set; }

	public string LanguageID { get; private set; }

	public string DefaultFontName { get; private set; }

	public bool DoesFontRequireSpaceForNewline { get; private set; } = true;

	public Font DefaultFont { get; private set; }

	public char LineSeperatorChar { get; private set; }

	public bool FontMapHasKey(string keyFontName)
	{
		return _fontMap.ContainsKey(keyFontName);
	}

	public Font GetMappedFont(string keyFontName)
	{
		return _fontMap[keyFontName];
	}

	private Language()
	{
	}

	public static Language CreateFrom(XmlNode languageNode, FontFactory fontFactory)
	{
		Language language = new Language
		{
			LanguageID = languageNode.Attributes["id"].InnerText
		};
		language.DefaultFontName = languageNode.Attributes["DefaultFont"]?.InnerText ?? "Galahad";
		language.LineSeperatorChar = languageNode.Attributes["LineSeperatorChar"]?.InnerText[0] ?? '-';
		language.DefaultFont = fontFactory.GetFont(language.DefaultFontName);
		foreach (XmlNode childNode in languageNode.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Element)
			{
				if (childNode.Name == "Map")
				{
					string innerText = childNode.Attributes["From"].InnerText;
					string innerText2 = childNode.Attributes["To"].InnerText;
					language._fontMap.Add(innerText, fontFactory.GetFont(innerText2));
				}
				else if (childNode.Name == "NewlineDoesntRequireSpace")
				{
					language.DoesFontRequireSpaceForNewline = false;
				}
				else if (childNode.Name == "ForbiddenStartOfLineCharacters")
				{
					language.ForbiddenStartOfLineCharacters = childNode.Attributes["Characters"]?.InnerText.ToCharArray();
				}
				else if (childNode.Name == "ForbiddenEndOfLineCharacters")
				{
					language.ForbiddenEndOfLineCharacters = childNode.Attributes["Characters"]?.InnerText.ToCharArray();
				}
			}
		}
		return language;
	}

	IEnumerable<char> ILanguage.GetForbiddenStartOfLineCharacters()
	{
		return ForbiddenStartOfLineCharacters;
	}

	IEnumerable<char> ILanguage.GetForbiddenEndOfLineCharacters()
	{
		return ForbiddenEndOfLineCharacters;
	}

	bool ILanguage.IsCharacterForbiddenAtStartOfLine(char character)
	{
		if (ForbiddenStartOfLineCharacters == null || ForbiddenStartOfLineCharacters.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < ForbiddenStartOfLineCharacters.Length; i++)
		{
			if (ForbiddenStartOfLineCharacters[i] == character)
			{
				return true;
			}
		}
		return false;
	}

	bool ILanguage.IsCharacterForbiddenAtEndOfLine(char character)
	{
		if (ForbiddenEndOfLineCharacters == null || ForbiddenEndOfLineCharacters.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < ForbiddenEndOfLineCharacters.Length; i++)
		{
			if (ForbiddenEndOfLineCharacters[i] == character)
			{
				return true;
			}
		}
		return false;
	}

	string ILanguage.GetLanguageID()
	{
		return LanguageID;
	}

	string ILanguage.GetDefaultFontName()
	{
		return DefaultFontName;
	}

	Font ILanguage.GetDefaultFont()
	{
		return DefaultFont;
	}

	char ILanguage.GetLineSeperatorChar()
	{
		return LineSeperatorChar;
	}

	bool ILanguage.DoesLanguageRequireSpaceForNewline()
	{
		return DoesFontRequireSpaceForNewline;
	}

	bool ILanguage.FontMapHasKey(string keyFontName)
	{
		return _fontMap.ContainsKey(keyFontName);
	}

	Font ILanguage.GetMappedFont(string keyFontName)
	{
		return _fontMap[keyFontName];
	}
}
