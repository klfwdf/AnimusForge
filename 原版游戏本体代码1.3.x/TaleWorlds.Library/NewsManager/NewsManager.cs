using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace TaleWorlds.Library.NewsManager
{
	// Token: 0x020000AA RID: 170
	public class NewsManager
	{
		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x06000669 RID: 1641 RVA: 0x000166A2 File Offset: 0x000148A2
		public MBReadOnlyList<NewsItem> NewsItems
		{
			get
			{
				return this._newsItems;
			}
		}

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x0600066A RID: 1642 RVA: 0x000166AA File Offset: 0x000148AA
		// (set) Token: 0x0600066B RID: 1643 RVA: 0x000166B2 File Offset: 0x000148B2
		public bool IsInPreviewMode { get; private set; }

		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x0600066C RID: 1644 RVA: 0x000166BB File Offset: 0x000148BB
		// (set) Token: 0x0600066D RID: 1645 RVA: 0x000166C3 File Offset: 0x000148C3
		public string LocalizationID { get; private set; }

		// Token: 0x0600066E RID: 1646 RVA: 0x000166CC File Offset: 0x000148CC
		public NewsManager()
		{
			this._newsItems = new MBList<NewsItem>();
			this.UpdateConfigSettings();
		}

		// Token: 0x0600066F RID: 1647 RVA: 0x000166EC File Offset: 0x000148EC
		public async Task<MBReadOnlyList<NewsItem>> GetNewsItems(bool forceRefresh)
		{
			await this.UpdateNewsItems(forceRefresh);
			return this.NewsItems;
		}

		// Token: 0x06000670 RID: 1648 RVA: 0x00016739 File Offset: 0x00014939
		public void SetNewsSourceURL(string url)
		{
			this._newsSourceURL = url;
		}

		// Token: 0x06000671 RID: 1649 RVA: 0x00016744 File Offset: 0x00014944
		public async Task UpdateNewsItems(bool forceRefresh)
		{
			if (ApplicationPlatform.CurrentPlatform != Platform.Durango && ApplicationPlatform.CurrentPlatform != Platform.GDKDesktop)
			{
				if (this._isNewsItemCacheDirty || forceRefresh)
				{
					try
					{
						if (Uri.IsWellFormedUriString(this._newsSourceURL, UriKind.Absolute))
						{
							this._newsItems = await NewsManager.DeserializeObjectAsync<MBList<NewsItem>>(await HttpHelper.DownloadStringTaskAsync(this._newsSourceURL));
						}
						else
						{
							Debug.FailedAssert("News file doesn't exist", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\NewsSystem\\NewsManager.cs", "UpdateNewsItems", 73);
						}
					}
					catch (Exception)
					{
					}
					this._isNewsItemCacheDirty = false;
				}
			}
		}

		// Token: 0x06000672 RID: 1650 RVA: 0x00016794 File Offset: 0x00014994
		public static Task<T> DeserializeObjectAsync<T>(string json)
		{
			Task<T> result;
			try
			{
				using (new StringReader(json))
				{
					result = Task.FromResult<T>(JsonConvert.DeserializeObject<T>(json));
				}
			}
			catch (Exception ex)
			{
				Debug.Print(ex.Message, 0, Debug.DebugColor.White, 17592186044416UL);
				result = Task.FromResult<T>(default(T));
			}
			return result;
		}

		// Token: 0x06000673 RID: 1651 RVA: 0x00016804 File Offset: 0x00014A04
		private void UpdateConfigSettings()
		{
			this._configPath = this.GetConfigXMLPath();
			this.IsInPreviewMode = false;
			this.LocalizationID = "en";
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(this._configPath);
				this.IsInPreviewMode = this.GetIsInPreviewMode(xmlDocument);
				this.LocalizationID = this.GetLocalizationCode(xmlDocument);
			}
			catch (Exception ex)
			{
				Debug.Print(ex.Message, 0, Debug.DebugColor.White, 17592186044416UL);
			}
		}

		// Token: 0x06000674 RID: 1652 RVA: 0x00016888 File Offset: 0x00014A88
		private bool GetIsInPreviewMode(XmlDocument configDocument)
		{
			return configDocument != null && configDocument.HasChildNodes && bool.Parse(configDocument.ChildNodes[0].SelectSingleNode("UsePreviewLink").Attributes["Value"].InnerText);
		}

		// Token: 0x06000675 RID: 1653 RVA: 0x000168C6 File Offset: 0x00014AC6
		private string GetLocalizationCode(XmlDocument configDocument)
		{
			if (configDocument != null && configDocument.HasChildNodes)
			{
				return configDocument.ChildNodes[0].SelectSingleNode("LocalizationID").Attributes["Value"].InnerText;
			}
			return "en";
		}

		// Token: 0x06000676 RID: 1654 RVA: 0x00016904 File Offset: 0x00014B04
		public void UpdateLocalizationID(string localizationID)
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(this._configPath);
			if (xmlDocument.HasChildNodes)
			{
				xmlDocument.ChildNodes[0].SelectSingleNode("LocalizationID").Attributes["Value"].Value = localizationID;
			}
			xmlDocument.Save(this._configPath);
		}

		// Token: 0x06000677 RID: 1655 RVA: 0x00016964 File Offset: 0x00014B64
		private PlatformFilePath GetConfigXMLPath()
		{
			PlatformDirectoryPath folderPath = new PlatformDirectoryPath(PlatformFileType.User, "Configs");
			PlatformFilePath platformFilePath = new PlatformFilePath(folderPath, "NewsFeedConfig.xml");
			bool flag = FileHelper.FileExists(platformFilePath);
			bool flag2 = true;
			if (flag)
			{
				XmlDocument xmlDocument = new XmlDocument();
				try
				{
					xmlDocument.Load(platformFilePath);
					flag2 = xmlDocument.HasChildNodes && xmlDocument.FirstChild.HasChildNodes;
				}
				catch (Exception ex)
				{
					Debug.Print(ex.Message, 0, Debug.DebugColor.White, 17592186044416UL);
					flag2 = false;
				}
			}
			if (!flag || !flag2)
			{
				try
				{
					XmlDocument xmlDocument2 = new XmlDocument();
					XmlNode xmlNode = xmlDocument2.CreateElement("Root");
					xmlDocument2.AppendChild(xmlNode);
					((XmlElement)xmlNode.AppendChild(xmlDocument2.CreateElement("LocalizationID"))).SetAttribute("Value", "en");
					((XmlElement)xmlNode.AppendChild(xmlDocument2.CreateElement("UsePreviewLink"))).SetAttribute("Value", "False");
					xmlDocument2.Save(platformFilePath);
				}
				catch (Exception ex2)
				{
					Debug.Print(ex2.Message, 0, Debug.DebugColor.White, 17592186044416UL);
				}
			}
			return platformFilePath;
		}

		// Token: 0x06000678 RID: 1656 RVA: 0x00016A94 File Offset: 0x00014C94
		public void OnFinalize()
		{
			MBList<NewsItem> newsItems = this._newsItems;
			if (newsItems != null)
			{
				newsItems.Clear();
			}
			this._newsItems = null;
			this.LocalizationID = null;
		}

		// Token: 0x040001E5 RID: 485
		private string _newsSourceURL;

		// Token: 0x040001E6 RID: 486
		private MBList<NewsItem> _newsItems;

		// Token: 0x040001E7 RID: 487
		private bool _isNewsItemCacheDirty = true;

		// Token: 0x040001EA RID: 490
		private PlatformFilePath _configPath;

		// Token: 0x040001EB RID: 491
		private const string DataFolder = "Configs";

		// Token: 0x040001EC RID: 492
		private const string FileName = "NewsFeedConfig.xml";
	}
}
