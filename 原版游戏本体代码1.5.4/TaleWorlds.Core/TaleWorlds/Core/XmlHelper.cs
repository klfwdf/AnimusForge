using System;
using System.Globalization;
using System.Xml;

namespace TaleWorlds.Core;

public static class XmlHelper
{
	public static int ReadInt(XmlNode node, string str)
	{
		XmlAttribute xmlAttribute = node.Attributes[str];
		if (xmlAttribute == null)
		{
			return 0;
		}
		return int.Parse(xmlAttribute.Value);
	}

	public static void ReadInt(ref int val, XmlNode node, string str)
	{
		XmlAttribute xmlAttribute = node.Attributes[str];
		if (xmlAttribute != null)
		{
			val = int.Parse(xmlAttribute.Value);
		}
	}

	public static float ReadFloat(XmlNode node, string str, float defaultValue = 0f)
	{
		XmlAttribute xmlAttribute = node.Attributes[str];
		if (xmlAttribute == null)
		{
			return defaultValue;
		}
		return float.Parse(xmlAttribute.Value);
	}

	public static string ReadString(XmlNode node, string str)
	{
		XmlAttribute xmlAttribute = node.Attributes[str];
		if (xmlAttribute == null)
		{
			return "";
		}
		return xmlAttribute.Value;
	}

	public static void ReadHexCode(ref uint val, XmlNode node, string str)
	{
		XmlAttribute xmlAttribute = node.Attributes[str];
		if (xmlAttribute != null)
		{
			string value = xmlAttribute.Value;
			value = value.Substring(2);
			val = uint.Parse(value, NumberStyles.HexNumber);
		}
	}

	public static bool ReadBool(XmlNode node, string str)
	{
		XmlAttribute xmlAttribute = node.Attributes[str];
		if (xmlAttribute != null)
		{
			return Convert.ToBoolean(xmlAttribute.InnerText);
		}
		return false;
	}
}
