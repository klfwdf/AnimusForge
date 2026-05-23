using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.Diamond.ClientApplication
{
	// Token: 0x02000043 RID: 67
	public class ClientApplicationConfiguration
	{
		// Token: 0x17000050 RID: 80
		// (get) Token: 0x06000184 RID: 388 RVA: 0x00004E3A File Offset: 0x0000303A
		// (set) Token: 0x06000185 RID: 389 RVA: 0x00004E42 File Offset: 0x00003042
		public string Name { get; set; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000186 RID: 390 RVA: 0x00004E4B File Offset: 0x0000304B
		// (set) Token: 0x06000187 RID: 391 RVA: 0x00004E53 File Offset: 0x00003053
		public string InheritFrom { get; set; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000188 RID: 392 RVA: 0x00004E5C File Offset: 0x0000305C
		// (set) Token: 0x06000189 RID: 393 RVA: 0x00004E64 File Offset: 0x00003064
		public string[] Clients { get; set; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x0600018A RID: 394 RVA: 0x00004E6D File Offset: 0x0000306D
		// (set) Token: 0x0600018B RID: 395 RVA: 0x00004E75 File Offset: 0x00003075
		public SessionProviderType SessionProviderType { get; set; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x0600018C RID: 396 RVA: 0x00004E7E File Offset: 0x0000307E
		// (set) Token: 0x0600018D RID: 397 RVA: 0x00004E86 File Offset: 0x00003086
		public ParameterContainer Parameters { get; set; }

		// Token: 0x0600018E RID: 398 RVA: 0x00004E8F File Offset: 0x0000308F
		public ClientApplicationConfiguration()
		{
			this.Name = "NewlyCreated";
			this.InheritFrom = "";
			this.Clients = new string[0];
			this.Parameters = new ParameterContainer();
		}

		// Token: 0x0600018F RID: 399 RVA: 0x00004EC4 File Offset: 0x000030C4
		private void FillFromBase(ClientApplicationConfiguration baseConfiguration)
		{
			this.SessionProviderType = baseConfiguration.SessionProviderType;
			this.Parameters = baseConfiguration.Parameters.Clone();
		}

		// Token: 0x06000190 RID: 400 RVA: 0x00004EE4 File Offset: 0x000030E4
		public static string GetDefaultConfigurationFromFile()
		{
			XmlDocument xmlDocument = new XmlDocument();
			string fileContent = VirtualFolders.GetFileContent(BasePath.Name + "Parameters/ClientProfile.xml", null);
			if (fileContent == "")
			{
				return "";
			}
			xmlDocument.LoadXml(fileContent);
			return xmlDocument.ChildNodes[0].Attributes["Value"].InnerText;
		}

		// Token: 0x06000191 RID: 401 RVA: 0x00004F47 File Offset: 0x00003147
		public static void SetDefaultConfigurationCategory(string category)
		{
			ClientApplicationConfiguration._defaultConfigurationCategory = category;
		}

		// Token: 0x06000192 RID: 402 RVA: 0x00004F4F File Offset: 0x0000314F
		public void FillFrom(string configurationName)
		{
			if (string.IsNullOrEmpty(ClientApplicationConfiguration._defaultConfigurationCategory))
			{
				ClientApplicationConfiguration._defaultConfigurationCategory = ClientApplicationConfiguration.GetDefaultConfigurationFromFile();
			}
			this.FillFrom(ClientApplicationConfiguration._defaultConfigurationCategory, configurationName);
		}

		// Token: 0x06000193 RID: 403 RVA: 0x00004F74 File Offset: 0x00003174
		public void FillFrom(string configurationCategory, string configurationName)
		{
			XmlDocument xmlDocument = new XmlDocument();
			if (configurationCategory == "")
			{
				return;
			}
			string fileContent = VirtualFolders.GetFileContent(string.Concat(new string[]
			{
				BasePath.Name,
				"Parameters/ClientProfiles/",
				configurationCategory,
				"/",
				configurationName,
				".xml"
			}), null);
			if (fileContent == "")
			{
				return;
			}
			xmlDocument.LoadXml(fileContent);
			this.Name = Path.GetFileNameWithoutExtension(configurationName);
			XmlNode firstChild = xmlDocument.FirstChild;
			if (firstChild.Attributes != null && firstChild.Attributes["InheritFrom"] != null)
			{
				this.InheritFrom = firstChild.Attributes["InheritFrom"].InnerText;
				ClientApplicationConfiguration clientApplicationConfiguration = new ClientApplicationConfiguration();
				clientApplicationConfiguration.FillFrom(configurationCategory, this.InheritFrom);
				this.FillFromBase(clientApplicationConfiguration);
			}
			ParameterLoader.LoadParametersInto(string.Concat(new string[] { "ClientProfiles/", configurationCategory, "/", configurationName, ".xml" }), this.Parameters);
			foreach (object obj in firstChild.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Name == "SessionProvider")
				{
					string innerText = xmlNode.Attributes["Type"].InnerText;
					this.SessionProviderType = (SessionProviderType)Enum.Parse(typeof(SessionProviderType), innerText);
				}
				else if (xmlNode.Name == "Clients")
				{
					List<string> list = new List<string>();
					foreach (object obj2 in xmlNode.ChildNodes)
					{
						string innerText2 = ((XmlNode)obj2).Attributes["Type"].InnerText;
						list.Add(innerText2);
					}
					this.Clients = list.ToArray();
				}
				else
				{
					xmlNode.Name == "Parameters";
				}
			}
		}

		// Token: 0x04000090 RID: 144
		private static string _defaultConfigurationCategory = "";
	}
}
