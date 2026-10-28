using System;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace Helpers;

public static class MiscHelper
{
	public static XmlDocument LoadXmlFile(string path)
	{
		Debug.Print("opening " + path);
		XmlDocument xmlDocument = new XmlDocument();
		StreamReader streamReader = new StreamReader(path);
		string xml = streamReader.ReadToEnd();
		xmlDocument.LoadXml(xml);
		streamReader.Close();
		return xmlDocument;
	}

	public static string GenerateCampaignId(int length)
	{
		Random random = new Random((int)(DateTime.Now.Ticks & 0xFFFF));
		char[] array = new char[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[random.Next("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".Length)];
		}
		string text = new string(array);
		Debug.Print("Campaign id: " + text, 1, Debug.DebugColor.Green);
		return text;
	}
}
