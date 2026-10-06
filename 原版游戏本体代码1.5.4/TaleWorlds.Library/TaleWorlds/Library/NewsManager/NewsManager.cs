using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace TaleWorlds.Library.NewsManager;

public class NewsManager
{
	private string _newsSourceURL;

	private MBList<NewsItem> _newsItems;

	private bool _isNewsItemCacheDirty = true;

	private PlatformFilePath _configPath;

	private const string DataFolder = "Configs";

	private const string FileName = "NewsFeedConfig.xml";

	public MBReadOnlyList<NewsItem> NewsItems => _newsItems;

	public bool IsInPreviewMode { get; private set; }

	public string LocalizationID { get; private set; }

	public NewsManager()
	{
		_newsItems = new MBList<NewsItem>();
		UpdateConfigSettings();
	}

	public async Task<MBReadOnlyList<NewsItem>> GetNewsItems(bool forceRefresh)
	{
		await UpdateNewsItems(forceRefresh);
		return NewsItems;
	}

	public void SetNewsSourceURL(string url)
	{
		_newsSourceURL = url;
	}

	public async Task UpdateNewsItems(bool forceRefresh)
	{
		if (ApplicationPlatform.CurrentPlatform == Platform.Durango || ApplicationPlatform.CurrentPlatform == Platform.GDKDesktop || !(_isNewsItemCacheDirty || forceRefresh))
		{
			return;
		}
		try
		{
			if (Uri.IsWellFormedUriString(_newsSourceURL, UriKind.Absolute))
			{
				_newsItems = await DeserializeObjectAsync<MBList<NewsItem>>(await HttpHelper.DownloadStringTaskAsync(_newsSourceURL));
			}
			else
			{
				Debug.FailedAssert("News file doesn't exist", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\NewsSystem\\NewsManager.cs", "UpdateNewsItems", 73);
			}
		}
		catch (Exception)
		{
		}
		_isNewsItemCacheDirty = false;
	}

	public static Task<T> DeserializeObjectAsync<T>(string json)
	{
		try
		{
			using (new System.IO.StringReader(json))
			{
				return Task.FromResult(JsonConvert.DeserializeObject<T>(json));
			}
		}
		catch (Exception ex)
		{
			Debug.Print(ex.Message);
			return Task.FromResult(default(T));
		}
	}

	private void UpdateConfigSettings()
	{
		_configPath = GetConfigXMLPath();
		IsInPreviewMode = false;
		LocalizationID = "en";
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(_configPath);
			IsInPreviewMode = GetIsInPreviewMode(xmlDocument);
			LocalizationID = GetLocalizationCode(xmlDocument);
		}
		catch (Exception ex)
		{
			Debug.Print(ex.Message);
		}
	}

	private bool GetIsInPreviewMode(XmlDocument configDocument)
	{
		if (configDocument != null && configDocument.HasChildNodes)
		{
			return bool.Parse(configDocument.ChildNodes[0].SelectSingleNode("UsePreviewLink").Attributes["Value"].InnerText);
		}
		return false;
	}

	private string GetLocalizationCode(XmlDocument configDocument)
	{
		if (configDocument != null && configDocument.HasChildNodes)
		{
			return configDocument.ChildNodes[0].SelectSingleNode("LocalizationID").Attributes["Value"].InnerText;
		}
		return "en";
	}

	public void UpdateLocalizationID(string localizationID)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(_configPath);
		if (xmlDocument.HasChildNodes)
		{
			xmlDocument.ChildNodes[0].SelectSingleNode("LocalizationID").Attributes["Value"].Value = localizationID;
		}
		xmlDocument.Save(_configPath);
	}

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
				Debug.Print(ex.Message);
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
				Debug.Print(ex2.Message);
			}
		}
		return platformFilePath;
	}

	public void OnFinalize()
	{
		_newsItems?.Clear();
		_newsItems = null;
		LocalizationID = null;
	}
}
