using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class FontFactory
{
	private Language _currentLangugage;

	private readonly Dictionary<string, Font> _bitmapFonts;

	private readonly ResourceDepot _resourceDepot;

	private readonly Dictionary<string, Language> _fontLanguageMap;

	private SpriteData _latestSpriteData;

	public Language DefaultLanguage { get; private set; }

	public Language CurrentLanguage
	{
		get
		{
			if (_currentLangugage != null)
			{
				return _currentLangugage;
			}
			if (DefaultLanguage != null)
			{
				Debug.Print("Couldn't find language in language map: " + _currentLangugage?.LanguageID);
				Debug.FailedAssert("Couldn't find language in language map: " + _currentLangugage?.LanguageID, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "CurrentLanguage", 26);
				_currentLangugage = DefaultLanguage;
				return _currentLangugage;
			}
			if (_fontLanguageMap.TryGetValue("English", out var value))
			{
				Debug.Print("Couldn't find default language(" + (DefaultLanguage?.LanguageID ?? "INVALID") + ") in language map.");
				Debug.FailedAssert("Couldn't find default language(" + (DefaultLanguage?.LanguageID ?? "INVALID") + ") in language map.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "CurrentLanguage", 35);
				DefaultLanguage = value;
				_currentLangugage = value;
				return _currentLangugage;
			}
			Debug.Print("Couldn't find English language in language map.");
			Debug.FailedAssert("Couldn't find English language in language map.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "CurrentLanguage", 45);
			DefaultLanguage = _fontLanguageMap.FirstOrDefault().Value;
			_currentLangugage = DefaultLanguage;
			if (_currentLangugage == null)
			{
				Debug.Print("There are no languages in language map");
				Debug.FailedAssert("There are no languages in language map", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "CurrentLanguage", 54);
			}
			return _currentLangugage;
		}
		private set
		{
			if (value != _currentLangugage)
			{
				_currentLangugage = value;
			}
		}
	}

	public Font DefaultFont => CurrentLanguage.DefaultFont;

	public FontFactory(ResourceDepot resourceDepot)
	{
		_resourceDepot = resourceDepot;
		_bitmapFonts = new Dictionary<string, Font>();
		_fontLanguageMap = new Dictionary<string, Language>();
		_resourceDepot.OnResourceChange += OnResourceChange;
	}

	private void OnResourceChange()
	{
		CheckForUpdates();
	}

	public void LoadAllFonts(SpriteData spriteData)
	{
		string[] files = _resourceDepot.GetFiles("Fonts", ".fnt");
		foreach (string path in files)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
			TryAddFontDefinition(Path.GetDirectoryName(path) + "/", fileNameWithoutExtension, spriteData);
		}
		files = _resourceDepot.GetFiles("Fonts", ".xml");
		foreach (string text in files)
		{
			if (Path.GetFileNameWithoutExtension(text).EndsWith("Languages"))
			{
				try
				{
					LoadLocalizationValues(text);
				}
				catch (Exception)
				{
					Debug.FailedAssert("Failed to load language at path: " + text, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "LoadAllFonts", 128);
				}
			}
		}
		if (DefaultLanguage == null && _fontLanguageMap.TryGetValue("English", out var value))
		{
			DefaultLanguage = value;
			CurrentLanguage = DefaultLanguage;
		}
		_latestSpriteData = spriteData;
	}

	public bool TryAddFontDefinition(string fontPath, string fontName, SpriteData spriteData)
	{
		Font font = new Font(fontName);
		string path = fontPath + fontName + ".fnt";
		bool num = font.TryLoadFontFromPath(path, spriteData);
		if (num)
		{
			_bitmapFonts.Add(fontName, font);
		}
		return num;
	}

	public void LoadLocalizationValues(string sourceXMLPath)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(sourceXMLPath);
		XmlElement xmlElement = xmlDocument["Languages"];
		_ = xmlElement.Attributes["DefaultLanguage"]?.InnerText;
		foreach (XmlNode item in xmlElement)
		{
			if (item.NodeType == XmlNodeType.Element && item.Name == "Language")
			{
				Language language = Language.CreateFrom(item, this);
				if (_fontLanguageMap.TryGetValue(language.LanguageID, out var _))
				{
					_fontLanguageMap[language.LanguageID] = language;
				}
				else
				{
					_fontLanguageMap.Add(language.LanguageID, language);
				}
			}
		}
		string text = xmlElement.Attributes["DefaultLanguage"]?.InnerText;
		if (!string.IsNullOrEmpty(text) && _fontLanguageMap.TryGetValue(text, out var value2))
		{
			DefaultLanguage = value2;
			CurrentLanguage = DefaultLanguage;
			return;
		}
		Debug.FailedAssert("DefaultLanguage cannot be found in the dictionary.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "LoadLocalizationValues", 200);
		if (_fontLanguageMap.TryGetValue("English", out value2))
		{
			DefaultLanguage = value2;
			CurrentLanguage = DefaultLanguage;
		}
		else
		{
			Debug.FailedAssert("English cannot be found in the dictionary.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "LoadLocalizationValues", 209);
		}
	}

	public Font GetFont(string fontName)
	{
		if (_bitmapFonts.ContainsKey(fontName))
		{
			return _bitmapFonts[fontName];
		}
		return DefaultFont;
	}

	public IEnumerable<Font> GetFonts()
	{
		return _bitmapFonts.Values;
	}

	public string GetFontName(Font font)
	{
		return _bitmapFonts.FirstOrDefault((KeyValuePair<string, Font> f) => f.Value == font).Key;
	}

	public Font GetMappedFontForLocalization(string englishFontName)
	{
		if (string.IsNullOrEmpty(englishFontName))
		{
			return DefaultFont;
		}
		if (DefaultLanguage != CurrentLanguage && CurrentLanguage != null && CurrentLanguage.FontMapHasKey(englishFontName))
		{
			return CurrentLanguage.GetMappedFont(englishFontName);
		}
		return GetFont(englishFontName);
	}

	public void OnLanguageChange(string newLanguageCode)
	{
		if (CurrentLanguage?.LanguageID != newLanguageCode)
		{
			if (!string.IsNullOrEmpty(newLanguageCode) && _fontLanguageMap.TryGetValue(newLanguageCode, out var value))
			{
				CurrentLanguage = value;
			}
			else
			{
				Debug.FailedAssert(newLanguageCode + " doesn't exist in the dictionary!", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\FontFactory.cs", "OnLanguageChange", 260);
			}
		}
	}

	public Font GetUsableFontForCharacter(int characterCode)
	{
		for (int i = 0; i < _fontLanguageMap.Values.Count; i++)
		{
			if (_fontLanguageMap.ElementAt(i).Value.DefaultFont.Characters.ContainsKey(characterCode))
			{
				return _fontLanguageMap.ElementAt(i).Value.DefaultFont;
			}
		}
		return null;
	}

	public void CheckForUpdates()
	{
		_ = CurrentLanguage?.LanguageID;
		DefaultLanguage = null;
		CurrentLanguage = null;
		_bitmapFonts.Clear();
		_fontLanguageMap.Clear();
		LoadAllFonts(_latestSpriteData);
		Language language = null;
		if (language != null)
		{
			CurrentLanguage = language;
		}
	}
}
