using System;
using System.IO;
using System.Xml;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Credits;

public class CreditsVM : ViewModel
{
	public CreditsItemVM _rootItem;

	private InputKeyItemVM _exitKey;

	private string _exitText;

	[DataSourceProperty]
	public CreditsItemVM RootItem
	{
		get
		{
			return _rootItem;
		}
		set
		{
			if (value != _rootItem)
			{
				_rootItem = value;
				OnPropertyChangedWithValue(value, "RootItem");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ExitKey
	{
		get
		{
			return _exitKey;
		}
		set
		{
			if (value != _exitKey)
			{
				_exitKey = value;
				OnPropertyChangedWithValue(value, "ExitKey");
			}
		}
	}

	[DataSourceProperty]
	public string ExitText
	{
		get
		{
			return _exitText;
		}
		set
		{
			if (value != _exitText)
			{
				_exitText = value;
				OnPropertyChangedWithValue(value, "ExitText");
			}
		}
	}

	public CreditsVM()
	{
		ExitKey = InputKeyItemVM.CreateFromHotKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"), isConsoleOnly: false);
		ExitText = new TextObject("{=exitMenuOption}Exit").ToString();
	}

	private static CreditsItemVM CreateFromFile(string path)
	{
		CreditsItemVM result = null;
		try
		{
			if (File.Exists(path))
			{
				XmlDocument xmlDocument = new XmlDocument();
				XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
				xmlReaderSettings.IgnoreComments = true;
				using (XmlReader reader = XmlReader.Create(new StreamReader(path), xmlReaderSettings))
				{
					xmlDocument.Load(reader);
				}
				XmlNode xmlNode = null;
				for (int i = 0; i < xmlDocument.ChildNodes.Count; i++)
				{
					XmlNode xmlNode2 = xmlDocument.ChildNodes.Item(i);
					if (xmlNode2.NodeType == XmlNodeType.Element && xmlNode2.Name == "Credits")
					{
						xmlNode = xmlNode2;
						break;
					}
				}
				if (xmlNode != null)
				{
					result = CreateItem(xmlNode);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.Print("Could not load Credits xml from " + path + ". Exception: " + ex.Message);
			result = null;
		}
		return result;
	}

	public void FillFromFile(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return;
			}
			XmlDocument xmlDocument = new XmlDocument();
			XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
			xmlReaderSettings.IgnoreComments = true;
			using (XmlReader reader = XmlReader.Create(new StreamReader(path), xmlReaderSettings))
			{
				xmlDocument.Load(reader);
			}
			XmlNode xmlNode = null;
			for (int i = 0; i < xmlDocument.ChildNodes.Count; i++)
			{
				XmlNode xmlNode2 = xmlDocument.ChildNodes.Item(i);
				if (xmlNode2.NodeType == XmlNodeType.Element && xmlNode2.Name == "Credits")
				{
					xmlNode = xmlNode2;
					break;
				}
			}
			if (xmlNode != null)
			{
				CreditsItemVM rootItem = CreateItem(xmlNode);
				_rootItem = rootItem;
			}
		}
		catch (Exception ex)
		{
			Debug.Print("Could not load Credits xml. Exception: " + ex.Message);
		}
	}

	private static CreditsItemVM CreateItem(XmlNode node)
	{
		CreditsItemVM creditsItemVM = null;
		if (node.Name.ToLower() == "LoadFromFile".ToLower())
		{
			string value = node.Attributes["Name"].Value;
			string text = "";
			if (node.Attributes["PlatformSpecific"] != null && node.Attributes["PlatformSpecific"].Value.ToLower() == "true")
			{
				text = ((!ApplicationPlatform.IsPlatformConsole()) ? "PC" : "Console");
			}
			if (node.Attributes["ConsoleSpecific"] != null && node.Attributes["ConsoleSpecific"].Value.ToLower() == "true")
			{
				text = ((ApplicationPlatform.CurrentPlatform == Platform.Durango) ? "XBox" : ((ApplicationPlatform.CurrentPlatform != Platform.Orbis) ? "PC" : "PlayStation"));
			}
			creditsItemVM = CreateFromFile(string.Concat(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/", value, text, ".xml"));
		}
		else
		{
			creditsItemVM = new CreditsItemVM();
			creditsItemVM.Type = node.Name;
			if (node.Attributes["Text"] != null)
			{
				creditsItemVM.Text = new TextObject(node.Attributes["Text"].Value).ToString();
			}
			else
			{
				creditsItemVM.Text = "";
			}
			foreach (XmlNode childNode in node.ChildNodes)
			{
				CreditsItemVM item = CreateItem(childNode);
				creditsItemVM.Items.Add(item);
			}
		}
		return creditsItemVM;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ExitKey.OnFinalize();
	}
}
