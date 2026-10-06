using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization.TextProcessor;

namespace TaleWorlds.Localization;

public static class LocalizedTextManager
{
	public const string LanguageDataFileName = "language_data";

	public const string DefaultEnglishLanguageId = "English";

	private static readonly Dictionary<string, string> _gameTextDictionary = new Dictionary<string, string>();

	public static string GetTranslatedText(string languageId, string id)
	{
		if (_gameTextDictionary.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public static List<string> GetLanguageIds(bool developmentMode)
	{
		List<string> list = new List<string>();
		foreach (LanguageData item in LanguageData.All)
		{
			bool flag = developmentMode || !item.IsUnderDevelopment;
			if ((item?.IsValid ?? false) && flag)
			{
				list.Add(item.StringId);
			}
		}
		return list;
	}

	public static string GetLanguageTitle(string id)
	{
		LanguageData languageData = LanguageData.GetLanguageData(id);
		if (languageData != null)
		{
			return languageData.Title;
		}
		return LanguageData.GetLanguageData("English").Title;
	}

	public static LanguageSpecificTextProcessor CreateTextProcessorForLanguage(string id)
	{
		LanguageData languageData = LanguageData.GetLanguageData(id);
		if (languageData != null && languageData.TextProcessor != null)
		{
			Type type = Type.GetType(languageData.TextProcessor);
			if (type == null)
			{
				Debug.FailedAssert("Can't find the type: " + languageData.TextProcessor, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\LocalizedTextManager.cs", "CreateTextProcessorForLanguage", 71);
				return new DefaultTextProcessor();
			}
			return (LanguageSpecificTextProcessor)Activator.CreateInstance(type);
		}
		return new DefaultTextProcessor();
	}

	public static void AddLanguageTest(string id, string processor)
	{
		LanguageData languageData = new LanguageData(id);
		languageData.InitializeDefault(id, new string[1] { id }, id, processor, isUnderDevelopment: false);
		LanguageData.LoadTestData(languageData);
	}

	public static int GetLanguageIndex(string id)
	{
		int languageDataIndex = LanguageData.GetLanguageDataIndex(id);
		if (languageDataIndex == -1)
		{
			languageDataIndex = LanguageData.GetLanguageDataIndex("English");
		}
		return languageDataIndex;
	}

	public static void LoadLocalizationXmls(string[] loadedModules)
	{
		LanguageData.Clear();
		for (int i = 0; i < loadedModules.Length; i++)
		{
			string text = loadedModules[i] + "/ModuleData/Languages";
			if (!Directory.Exists(text))
			{
				continue;
			}
			string[] array;
			try
			{
				array = Directory.GetFiles(text, "language_data.xml", SearchOption.AllDirectories);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Exception occurred in LoadLocalizationXmls: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\LocalizedTextManager.cs", "LoadLocalizationXmls", 119);
				array = new string[0];
			}
			string[] array2 = array;
			for (int j = 0; j < array2.Length; j++)
			{
				XmlDocument xmlDocument = LoadXmlFile(array2[j]);
				if (xmlDocument != null)
				{
					LanguageData.LoadFromXml(xmlDocument, text);
				}
			}
		}
	}

	public static void AddLocalizationXml(string newModule)
	{
		string text = newModule + "/ModuleData/Languages";
		if (!Directory.Exists(text))
		{
			return;
		}
		string[] array;
		try
		{
			array = Directory.GetFiles(text, "language_data.xml", SearchOption.AllDirectories);
		}
		catch (Exception ex)
		{
			Debug.FailedAssert("Exception occurred in LoadLocalizationXmls: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\LocalizedTextManager.cs", "AddLocalizationXml", 147);
			array = new string[0];
		}
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			XmlDocument xmlDocument = LoadXmlFile(array2[i]);
			if (xmlDocument != null)
			{
				LanguageData.LoadFromXml(xmlDocument, text);
			}
		}
	}

	public static string GetDateFormattedByLanguage(string languageCode, DateTime dateTime)
	{
		string shortDatePattern = GetCultureInfo(languageCode).DateTimeFormat.ShortDatePattern;
		return dateTime.ToString(shortDatePattern);
	}

	public static string GetTimeFormattedByLanguage(string languageCode, DateTime dateTime)
	{
		string shortTimePattern = GetCultureInfo(languageCode).DateTimeFormat.ShortTimePattern;
		return dateTime.ToString(shortTimePattern);
	}

	public static string GetSubtitleExtensionOfLanguage(string languageId)
	{
		return GetLanguageData(languageId).SubtitleExtension;
	}

	public static string GetLocalizationCodeOfISOLanguageCode(string isoLanguageCode)
	{
		foreach (LanguageData item in LanguageData.All)
		{
			string[] supportedIsoCodes = item.SupportedIsoCodes;
			for (int i = 0; i < supportedIsoCodes.Length; i++)
			{
				if (string.Equals(supportedIsoCodes[i], isoLanguageCode, StringComparison.InvariantCultureIgnoreCase))
				{
					return item.StringId;
				}
			}
		}
		Debug.FailedAssert("Undefined language code " + isoLanguageCode, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\LocalizedTextManager.cs", "GetLocalizationCodeOfISOLanguageCode", 202);
		return "English";
	}

	private static CultureInfo GetCultureInfo(string languageId)
	{
		LanguageData languageData = GetLanguageData(languageId);
		CultureInfo result = CultureInfo.InvariantCulture;
		if (languageData.SupportedIsoCodes != null && languageData.SupportedIsoCodes.Length != 0)
		{
			result = new CultureInfo(languageData.SupportedIsoCodes[0]);
		}
		return result;
	}

	private static LanguageData GetLanguageData(string languageId)
	{
		LanguageData languageData = LanguageData.GetLanguageData(languageId);
		if (languageData == null || !languageData.IsValid)
		{
			languageData = LanguageData.GetLanguageData("English");
			Debug.FailedAssert("Undefined language code: " + languageId, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\LocalizedTextManager.cs", "GetLanguageData", 225);
		}
		return languageData;
	}

	private static XmlDocument LoadXmlFile(string path)
	{
		try
		{
			Debug.Print("opening " + path);
			XmlDocument xmlDocument = new XmlDocument();
			StreamReader streamReader = new StreamReader(path);
			string xml = streamReader.ReadToEnd();
			xmlDocument.LoadXml(xml);
			streamReader.Close();
			return xmlDocument;
		}
		catch
		{
			Debug.FailedAssert("Could not parse: " + path, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\LocalizedTextManager.cs", "LoadXmlFile", 245);
		}
		return null;
	}

	internal static void LoadLanguage(string languageId)
	{
		_gameTextDictionary.Clear();
		LanguageData languageData = LanguageData.GetLanguageData(languageId);
		if (languageData != null)
		{
			LoadLanguage(languageData);
		}
	}

	private static void LoadLanguage(LanguageData language)
	{
		MBTextManager.ResetFunctions();
		string stringId = language.StringId;
		bool flag = stringId != "English";
		foreach (string xmlPath in language.XmlPaths)
		{
			XmlDocument xmlDocument = LoadXmlFile(xmlPath);
			if (xmlDocument == null)
			{
				continue;
			}
			for (XmlNode xmlNode = xmlDocument.ChildNodes[1].FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
			{
				if (xmlNode.Name == "strings" && xmlNode.HasChildNodes)
				{
					if (flag)
					{
						for (XmlNode xmlNode2 = xmlNode.FirstChild; xmlNode2 != null; xmlNode2 = xmlNode2.NextSibling)
						{
							if (xmlNode2.Name == "string" && xmlNode2.NodeType != XmlNodeType.Comment)
							{
								DeserializeStrings(xmlNode2, stringId);
							}
						}
					}
				}
				else if (xmlNode.Name == "functions" && xmlNode.HasChildNodes)
				{
					for (XmlNode xmlNode3 = xmlNode.FirstChild; xmlNode3 != null; xmlNode3 = xmlNode3.NextSibling)
					{
						if (xmlNode3.Name == "function" && xmlNode3.NodeType != XmlNodeType.Comment)
						{
							string value = xmlNode3.Attributes["functionName"].Value;
							string value2 = xmlNode3.Attributes["functionBody"].Value;
							MBTextManager.SetFunction(value, value2);
						}
					}
				}
			}
		}
		Debug.Print("Loading localized text xml.");
	}

	private static void DeserializeStrings(XmlNode node, string languageId)
	{
		if (node.Attributes == null)
		{
			throw new TWXmlLoadException("Node attributes are null!");
		}
		string value = node.Attributes["id"].Value;
		string value2 = node.Attributes["text"].Value;
		_gameTextDictionary[value] = value2;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("change_language", "localization")]
	public static string ChangeLanguage(List<string> strings)
	{
		if (strings.Count != 1)
		{
			return "Format is \"localization.change_language [LanguageCode/LanguageName/ISOCode]\".";
		}
		string value = strings[0];
		int activeTextLanguageIndex = MBTextManager.GetActiveTextLanguageIndex();
		string text = null;
		foreach (string languageId in GetLanguageIds(developmentMode: true))
		{
			if (GetLanguageTitle(languageId).Equals(value, StringComparison.OrdinalIgnoreCase) || GetSubtitleExtensionOfLanguage(languageId).Contains(value))
			{
				text = languageId;
				break;
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			return "cant find the language in current configuration.";
		}
		if (GetLanguageIndex(text) == activeTextLanguageIndex)
		{
			return "Same language";
		}
		MBTextManager.ChangeLanguage(text);
		return "New language is " + text;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("reload_texts", "localization")]
	public static string ReloadTexts(List<string> strings)
	{
		LoadLanguage(MBTextManager.ActiveTextLanguage);
		return "OK";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("check_for_errors", "localization")]
	public static string CheckValidity(List<string> strings)
	{
		if (File.Exists("faulty_translation_lines.txt"))
		{
			File.Delete("faulty_translation_lines.txt");
		}
		bool flag = false;
		foreach (string languageId in GetLanguageIds(developmentMode: false))
		{
			MBTextManager.ChangeLanguage(languageId);
			Write("Testing Language: " + MBTextManager.ActiveTextLanguage + "\n\n");
			foreach (KeyValuePair<string, string> item in _gameTextDictionary)
			{
				string key = item.Key;
				string value = item.Value;
				string errorLine;
				bool num = CheckValidity(key, value, out errorLine);
				if (num)
				{
					Write(errorLine);
				}
				flag = num || flag;
			}
			Write("\nTesting Language: " + MBTextManager.ActiveTextLanguage + "\n\n");
		}
		if (!flag)
		{
			return "No errors are found.";
		}
		return "Errors are written into 'faulty_translation_lines.txt' file in the binary folder.";
		static void Write(string s)
		{
			File.AppendAllText("faulty_translation_lines.txt", s, Encoding.Unicode);
		}
	}

	public static bool CheckValidity(string id, string text, out string errorLine)
	{
		errorLine = null;
		bool flag = false;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < text.Length; i++)
		{
			switch (text[i])
			{
			case '{':
				num++;
				break;
			case '}':
				num2++;
				break;
			}
		}
		int num3 = 0;
		int num4 = 0;
		string text2 = text;
		while (true)
		{
			int num5 = text2.IndexOf("{?");
			if (num5 == -1)
			{
				break;
			}
			num5 = TaleWorlds.Library.MathF.Min(num5 + 1, text2.Length - 1);
			text2 = text2.Substring(num5);
			if (text2.Length > 2 && text2[1] != '}')
			{
				num3++;
			}
		}
		string text3 = text;
		while (true)
		{
			int num6 = text3.IndexOf("{\\?}");
			if (num6 == -1)
			{
				break;
			}
			num4++;
			num6 = TaleWorlds.Library.MathF.Min(num6 + 1, text.Length - 1);
			text3 = text3.Substring(num6);
		}
		if (num != num2)
		{
			errorLine = $"{id} | {text}\n";
			flag = true;
		}
		else if (num3 != num4)
		{
			errorLine = $"{id} | {text}\n";
			flag = true;
		}
		else if (!flag)
		{
			try
			{
				MBTextManager.ProcessTextToString(new TextObject("{=" + id + "}" + GetTranslatedText(MBTextManager.ActiveTextLanguage, id)), shouldClear: true);
			}
			catch
			{
				errorLine = $"{id} | {text}\n";
			}
		}
		return flag;
	}
}
