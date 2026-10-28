using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.Localization;

public static class LocalizedVoiceManager
{
	private static readonly Dictionary<string, VoiceObject> _voiceObjectDictionary = new Dictionary<string, VoiceObject>();

	public static VoiceObject GetLocalizedVoice(string id)
	{
		if (_voiceObjectDictionary.TryGetValue(id, out var value))
		{
			return value;
		}
		Debug.Print("Voice object for text id is not found: " + id, 0, Debug.DebugColor.Green);
		return null;
	}

	public static List<string> GetVoiceLanguageIds()
	{
		List<string> list = new List<string>();
		foreach (LanguageData item in LanguageData.All)
		{
			if (item != null && item.IsValid && item.VoiceXmlPathsAndModulePaths.Count > 0)
			{
				list.Add(item.StringId);
			}
		}
		return list;
	}

	internal static void LoadLanguage(string languageId)
	{
		_voiceObjectDictionary.Clear();
		LanguageData languageData = LanguageData.GetLanguageData(languageId);
		if (languageData != null)
		{
			LoadLanguage(languageData);
		}
	}

	private static XmlDocument LoadXmlFile(string xmlPath)
	{
		try
		{
			Debug.Print("opening " + xmlPath);
			XmlDocument xmlDocument = new XmlDocument();
			StreamReader streamReader = new StreamReader(xmlPath);
			string xml = streamReader.ReadToEnd();
			xmlDocument.LoadXml(xml);
			streamReader.Close();
			return xmlDocument;
		}
		catch
		{
			Debug.FailedAssert("Could not parse: " + xmlPath, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Localization\\LocalizedVoiceManager.cs", "LoadXmlFile", 70);
		}
		return null;
	}

	private static void LoadLanguage(LanguageData language)
	{
		foreach (KeyValuePair<string, string> voiceXmlPathsAndModulePath in language.VoiceXmlPathsAndModulePaths)
		{
			XmlDocument xmlDocument = LoadXmlFile(voiceXmlPathsAndModulePath.Key);
			if (xmlDocument == null)
			{
				continue;
			}
			XmlNode xmlNode = null;
			foreach (XmlNode childNode in xmlDocument.DocumentElement.ChildNodes)
			{
				if (childNode.Name == "VoiceOvers")
				{
					xmlNode = childNode;
					break;
				}
			}
			foreach (XmlNode childNode2 in xmlNode.ChildNodes)
			{
				if (childNode2.Name == "VoiceOver")
				{
					string innerText = childNode2.Attributes["id"].InnerText;
					if (_voiceObjectDictionary.ContainsKey(innerText))
					{
						_voiceObjectDictionary[innerText].AddVoicePaths(childNode2, voiceXmlPathsAndModulePath.Value);
						continue;
					}
					VoiceObject value = VoiceObject.Deserialize(childNode2, voiceXmlPathsAndModulePath.Value);
					_voiceObjectDictionary.Add(innerText, value);
				}
			}
		}
	}
}
