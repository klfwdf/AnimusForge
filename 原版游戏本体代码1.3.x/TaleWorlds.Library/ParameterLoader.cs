using System;
using System.IO;
using System.Xml;

namespace TaleWorlds.Library
{
	// Token: 0x02000078 RID: 120
	public class ParameterLoader
	{
		// Token: 0x0600044C RID: 1100 RVA: 0x0000F240 File Offset: 0x0000D440
		public static ParameterContainer LoadParametersFromClientProfile(string configurationName)
		{
			ParameterContainer parameterContainer = new ParameterContainer();
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(VirtualFolders.GetFileContent(BasePath.Name + "Parameters/ClientProfile.xml", null));
			string innerText = xmlDocument.ChildNodes[0].Attributes["Value"].InnerText;
			ParameterLoader.LoadParametersInto(string.Concat(new string[] { "ClientProfiles/", innerText, "/", configurationName, ".xml" }), parameterContainer);
			return parameterContainer;
		}

		// Token: 0x0600044D RID: 1101 RVA: 0x0000F2C8 File Offset: 0x0000D4C8
		public static void LoadParametersInto(string fileFullName, ParameterContainer parameters)
		{
			XmlDocument xmlDocument = new XmlDocument();
			string filePath = BasePath.Name + "Parameters/" + fileFullName;
			xmlDocument.LoadXml(VirtualFolders.GetFileContent(filePath, null));
			foreach (object obj in xmlDocument.FirstChild.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Name == "Parameters")
				{
					XmlAttributeCollection attributes = xmlNode.Attributes;
					string text;
					if (attributes == null)
					{
						text = null;
					}
					else
					{
						XmlAttribute xmlAttribute = attributes["Platforms"];
						text = ((xmlAttribute != null) ? xmlAttribute.InnerText : null);
					}
					string text2 = text;
					if (!string.IsNullOrWhiteSpace(text2))
					{
						if (text2.Split(new char[] { ',' }).FindIndex((string p) => p.Trim().Equals(string.Concat(ApplicationPlatform.CurrentPlatform))) < 0)
						{
							continue;
						}
					}
					foreach (object obj2 in xmlNode.ChildNodes)
					{
						XmlNode xmlNode2 = (XmlNode)obj2;
						if (xmlNode2.NodeType != XmlNodeType.Comment)
						{
							string innerText = xmlNode2.Attributes["Name"].InnerText;
							string text3;
							string value;
							string text4;
							if (ParameterLoader.TryGetFromFile(xmlNode2, out text3))
							{
								value = text3;
							}
							else if (ParameterLoader.TryGetFromEnvironment(xmlNode2, out text4))
							{
								value = text4;
							}
							else if (xmlNode2.Attributes["DefaultValue"] != null)
							{
								value = xmlNode2.Attributes["DefaultValue"].InnerText;
							}
							else
							{
								value = xmlNode2.Attributes["Value"].InnerText;
							}
							parameters.AddParameter(innerText, value, true);
						}
					}
				}
			}
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x0000F4CC File Offset: 0x0000D6CC
		private static bool TryGetFromFile(XmlNode node, out string value)
		{
			value = "";
			XmlAttributeCollection attributes = node.Attributes;
			if (((attributes != null) ? attributes["LoadFromFile"] : null) != null && node.Attributes["LoadFromFile"].InnerText.ToLower() == "true")
			{
				string innerText = node.Attributes["File"].InnerText;
				if (File.Exists(innerText))
				{
					string text = File.ReadAllText(innerText);
					value = text;
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600044F RID: 1103 RVA: 0x0000F54C File Offset: 0x0000D74C
		private static bool TryGetFromEnvironment(XmlNode node, out string value)
		{
			value = "";
			XmlAttributeCollection attributes = node.Attributes;
			if (((attributes != null) ? attributes["GetFromEnvironment"] : null) != null && node.Attributes["GetFromEnvironment"].InnerText.ToLower() == "true")
			{
				string innerText = node.Attributes["Name"].InnerText;
				string environmentVariable = Environment.GetEnvironmentVariable(innerText);
				if (string.IsNullOrEmpty(environmentVariable))
				{
					environmentVariable = Environment.GetEnvironmentVariable(ParameterLoader.GetAltEnvironmentVariableName(innerText));
				}
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					value = environmentVariable;
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000450 RID: 1104 RVA: 0x0000F5DE File Offset: 0x0000D7DE
		private static string GetAltEnvironmentVariableName(string name)
		{
			return name.Replace(".", "_").Replace(":", "__");
		}
	}
}
