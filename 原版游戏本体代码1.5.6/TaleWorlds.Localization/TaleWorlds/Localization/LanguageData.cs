using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.Localization;

internal class LanguageData
{
	public readonly string StringId;

	private readonly MBList<string> _xmlPaths;

	private readonly Dictionary<string, string> _voiceXmlPathsAndModulePaths;

	public static MBReadOnlyList<LanguageData> All => _all;

	private static MBList<LanguageData> _all { get; } = new MBList<LanguageData>();

	public string Title { get; private set; }

	public string TextProcessor { get; private set; }

	public string[] SupportedIsoCodes { get; private set; }

	public string SubtitleExtension { get; private set; }

	public bool IsUnderDevelopment { get; private set; }

	public MBReadOnlyList<string> XmlPaths => _xmlPaths;

	public IReadOnlyDictionary<string, string> VoiceXmlPathsAndModulePaths => _voiceXmlPathsAndModulePaths;

	public bool IsValid { get; private set; }

	public LanguageData(string stringId)
	{
		StringId = stringId;
		IsUnderDevelopment = true;
		SupportedIsoCodes = new string[0];
		_xmlPaths = new MBList<string>();
		_voiceXmlPathsAndModulePaths = new Dictionary<string, string>();
		IsValid = false;
	}

	public void InitializeDefault(string title, string[] supportedIsoCodes, string subtitleExtension, string textProcessor, bool isUnderDevelopment)
	{
		Title = title;
		TextProcessor = textProcessor;
		SubtitleExtension = subtitleExtension;
		SupportedIsoCodes = supportedIsoCodes;
		IsUnderDevelopment = isUnderDevelopment;
		IsValid = SupportedIsoCodes.Length != 0;
	}

	public static void Clear()
	{
		_all.Clear();
	}

	public static LanguageData GetLanguageData(string stringId)
	{
		foreach (LanguageData item in All)
		{
			if (item.StringId == stringId)
			{
				return item;
			}
		}
		return null;
	}

	public static int GetLanguageDataIndex(string stringId)
	{
		for (int i = 0; i < _all.Count; i++)
		{
			if (_all[i].StringId == stringId)
			{
				return i;
			}
		}
		return -1;
	}

	private void Deserialize(XmlNode node, string modulePath)
	{
		if (node.Attributes == null)
		{
			throw new TWXmlLoadException("LanguageData node does not have any Attributes!");
		}
		string text = node.Attributes["name"]?.Value;
		if (!string.IsNullOrEmpty(text))
		{
			Title = text;
		}
		string text2 = node.Attributes["subtitle_extension"]?.Value;
		if (!string.IsNullOrEmpty(text2))
		{
			SubtitleExtension = text2;
		}
		string text3 = node.Attributes["supported_iso"]?.Value;
		if (text3 != null)
		{
			string[] second = text3.Split(new char[1] { ',' });
			SupportedIsoCodes = new List<string>(SupportedIsoCodes).Union(second).ToArray();
		}
		string textProcessor = node.Attributes["text_processor"]?.Value;
		if (!string.IsNullOrEmpty(text))
		{
			TextProcessor = textProcessor;
		}
		XmlAttribute xmlAttribute = node.Attributes["under_development"];
		if (xmlAttribute != null)
		{
			bool.TryParse(xmlAttribute.Value, out var result);
			IsUnderDevelopment = result;
		}
		IsValid = SupportedIsoCodes.Length != 0;
		if (!node.HasChildNodes)
		{
			return;
		}
		for (node = node.FirstChild; node != null; node = node.NextSibling)
		{
			if (node.Name == "LanguageFile" && node.NodeType != XmlNodeType.Comment && node.Attributes != null)
			{
				string text4 = node.Attributes["xml_path"]?.Value;
				if (!string.IsNullOrEmpty(text4) && !_xmlPaths.Contains(text4))
				{
					_xmlPaths.Add(modulePath + "/" + text4);
				}
			}
			if (node.Name == "VoiceFile" && node.NodeType != XmlNodeType.Comment && node.Attributes != null)
			{
				string text5 = node.Attributes["xml_path"]?.Value;
				string key = modulePath + "/" + text5;
				if (!string.IsNullOrEmpty(text5) && !_voiceXmlPathsAndModulePaths.ContainsKey(key))
				{
					_voiceXmlPathsAndModulePaths.Add(key, modulePath);
				}
			}
		}
	}

	public static void LoadFromXml(XmlDocument doc, string modulePath)
	{
		Debug.Print("Loading localized text xml: " + doc.Name);
		if (!doc.HasChildNodes)
		{
			return;
		}
		for (XmlNode xmlNode = doc.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (xmlNode.Name == "LanguageData" && xmlNode.NodeType != XmlNodeType.Comment && xmlNode.Attributes != null)
			{
				string text = xmlNode.Attributes["id"]?.Value;
				if (!string.IsNullOrEmpty(text))
				{
					LanguageData languageData = GetLanguageData(text);
					if (languageData == null)
					{
						languageData = new LanguageData(text);
						_all.Add(languageData);
					}
					languageData.Deserialize(xmlNode, modulePath);
				}
			}
		}
	}

	public static void LoadTestData(LanguageData data)
	{
		int languageDataIndex = GetLanguageDataIndex(data.StringId);
		if (languageDataIndex == -1)
		{
			_all.Add(data);
		}
		else
		{
			_all[languageDataIndex] = data;
		}
	}
}
