using System;
using System.Globalization;
using System.Xml;

namespace TaleWorlds.Core
{
	// Token: 0x020000E1 RID: 225
	public static class XmlHelper
	{
		// Token: 0x06000B72 RID: 2930 RVA: 0x00024E20 File Offset: 0x00023020
		public static int ReadInt(XmlNode node, string str)
		{
			XmlAttribute xmlAttribute = node.Attributes[str];
			if (xmlAttribute == null)
			{
				return 0;
			}
			return int.Parse(xmlAttribute.Value);
		}

		// Token: 0x06000B73 RID: 2931 RVA: 0x00024E4C File Offset: 0x0002304C
		public static void ReadInt(ref int val, XmlNode node, string str)
		{
			XmlAttribute xmlAttribute = node.Attributes[str];
			if (xmlAttribute != null)
			{
				val = int.Parse(xmlAttribute.Value);
			}
		}

		// Token: 0x06000B74 RID: 2932 RVA: 0x00024E78 File Offset: 0x00023078
		public static float ReadFloat(XmlNode node, string str, float defaultValue = 0f)
		{
			XmlAttribute xmlAttribute = node.Attributes[str];
			if (xmlAttribute == null)
			{
				return defaultValue;
			}
			return float.Parse(xmlAttribute.Value);
		}

		// Token: 0x06000B75 RID: 2933 RVA: 0x00024EA4 File Offset: 0x000230A4
		public static string ReadString(XmlNode node, string str)
		{
			XmlAttribute xmlAttribute = node.Attributes[str];
			if (xmlAttribute == null)
			{
				return "";
			}
			return xmlAttribute.Value;
		}

		// Token: 0x06000B76 RID: 2934 RVA: 0x00024ED0 File Offset: 0x000230D0
		public static void ReadHexCode(ref uint val, XmlNode node, string str)
		{
			XmlAttribute xmlAttribute = node.Attributes[str];
			if (xmlAttribute != null)
			{
				string text = xmlAttribute.Value;
				text = text.Substring(2);
				val = uint.Parse(text, NumberStyles.HexNumber);
			}
		}

		// Token: 0x06000B77 RID: 2935 RVA: 0x00024F0C File Offset: 0x0002310C
		public static bool ReadBool(XmlNode node, string str)
		{
			XmlAttribute xmlAttribute = node.Attributes[str];
			return xmlAttribute != null && Convert.ToBoolean(xmlAttribute.InnerText);
		}
	}
}
