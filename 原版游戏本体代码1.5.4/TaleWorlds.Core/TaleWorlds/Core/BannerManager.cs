using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class BannerManager
{
	public const int DarkRed = 1;

	public const int Green = 120;

	public const int Blue = 119;

	public const int Purple = 4;

	public const int DarkPurple = 6;

	public const int Orange = 9;

	public const int DarkBlue = 12;

	public const int Red = 118;

	public const int Yellow = 121;

	public MBReadOnlyDictionary<int, BannerColor> ReadOnlyColorPalette;

	private Dictionary<BasicCultureObject, List<BannerColor>> _cultureColorPalette;

	private Dictionary<int, BannerColor> _colorPalette;

	private MBList<BannerIconGroup> _bannerIconGroups;

	private int _availablePatternCount;

	private int _availableIconCount;

	public static BannerManager Instance { get; private set; }

	public MBReadOnlyList<BannerIconGroup> BannerIconGroups => _bannerIconGroups;

	public int BaseBackgroundId { get; private set; }

	private static MBReadOnlyDictionary<int, BannerColor> ColorPalette => Instance.ReadOnlyColorPalette;

	private BannerManager()
	{
		_bannerIconGroups = new MBList<BannerIconGroup>();
		_colorPalette = new Dictionary<int, BannerColor>();
		_cultureColorPalette = new Dictionary<BasicCultureObject, List<BannerColor>>();
		ReadOnlyColorPalette = _colorPalette.GetReadOnlyDictionary();
	}

	public static void Initialize()
	{
		if (Instance == null)
		{
			Instance = new BannerManager();
		}
	}

	public static void ResetAndLoad()
	{
		Instance._bannerIconGroups = new MBList<BannerIconGroup>();
		Instance._colorPalette = new Dictionary<int, BannerColor>();
		Instance._cultureColorPalette = new Dictionary<BasicCultureObject, List<BannerColor>>();
		Instance.LoadBannerIcons();
	}

	public static uint GetColor(int id)
	{
		if (ColorPalette.TryGetValue(id, out var value))
		{
			return value.Color;
		}
		return 3735928559u;
	}

	public static int GetColorId(uint color)
	{
		foreach (KeyValuePair<int, BannerColor> item in ColorPalette)
		{
			if (item.Value.Color == color)
			{
				return item.Key;
			}
		}
		return -1;
	}

	public int GetRandomColorId(MBFastRandom random)
	{
		return ColorPalette.ElementAt(random.Next(ColorPalette.Count())).Key;
	}

	public BannerIconData GetIconDataFromIconId(int id)
	{
		foreach (BannerIconGroup bannerIconGroup in _bannerIconGroups)
		{
			if (bannerIconGroup.AllIcons.TryGetValue(id, out var value))
			{
				return value;
			}
		}
		return default(BannerIconData);
	}

	public int GetRandomBackgroundId(MBFastRandom random)
	{
		int num = random.Next(0, _availablePatternCount);
		foreach (BannerIconGroup bannerIconGroup in BannerIconGroups)
		{
			if (bannerIconGroup.IsPattern)
			{
				if (num < bannerIconGroup.AllBackgrounds.Count)
				{
					return bannerIconGroup.AllBackgrounds.ElementAt(num).Key;
				}
				num -= bannerIconGroup.AllBackgrounds.Count;
			}
		}
		return -1;
	}

	public int GetRandomBannerIconId(MBFastRandom random)
	{
		int num = random.Next(0, _availableIconCount);
		foreach (BannerIconGroup bannerIconGroup in BannerIconGroups)
		{
			if (!bannerIconGroup.IsPattern)
			{
				if (num < bannerIconGroup.AvailableIcons.Count)
				{
					return bannerIconGroup.AvailableIcons.ElementAt(num).Key;
				}
				num -= bannerIconGroup.AvailableIcons.Count;
			}
		}
		return -1;
	}

	public string GetBackgroundMeshName(int id)
	{
		foreach (BannerIconGroup bannerIconGroup in BannerIconGroups)
		{
			if (bannerIconGroup.IsPattern && bannerIconGroup.AllBackgrounds.ContainsKey(id))
			{
				return bannerIconGroup.AllBackgrounds[id];
			}
		}
		return null;
	}

	public string GetIconSourceTextureName(int id)
	{
		foreach (BannerIconGroup bannerIconGroup in BannerIconGroups)
		{
			if (!bannerIconGroup.IsPattern && bannerIconGroup.AllBackgrounds.ContainsKey(id))
			{
				return bannerIconGroup.AllBackgrounds[id];
			}
		}
		return null;
	}

	public void SetBaseBackgroundId(int id)
	{
		BaseBackgroundId = id;
	}

	public void SetCultureColors(BasicCultureObject culture, List<BannerColor> color)
	{
		if (!_cultureColorPalette.ContainsKey(culture))
		{
			_cultureColorPalette[culture] = color;
		}
		else
		{
			Debug.FailedAssert("Culture colors already set", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BannerManager.cs", "SetCultureColors", 200);
		}
	}

	public void LoadBannerIcons()
	{
		Game current = Game.Current;
		bool ignoreGameTypeInclusionCheck = false;
		string gameType = "";
		if (current != null)
		{
			ignoreGameTypeInclusionCheck = current.GameType.IsDevelopment;
			gameType = current.GameType.GetType().Name;
		}
		XmlDocument mergedXmlForManaged = MBObjectManager.GetMergedXmlForManaged("BannerIcons", skipValidation: false, ignoreGameTypeInclusionCheck, gameType);
		LoadBannerIconsFromXml(mergedXmlForManaged);
	}

	public void LoadBannerIcons(string xmlPath)
	{
		XmlDocument doc = LoadXmlFile(xmlPath);
		LoadBannerIconsFromXml(doc);
	}

	private XmlDocument LoadXmlFile(string path)
	{
		Debug.Print("opening " + path);
		XmlDocument xmlDocument = new XmlDocument();
		StreamReader streamReader = new StreamReader(path);
		string xml = streamReader.ReadToEnd();
		xmlDocument.LoadXml(xml);
		streamReader.Close();
		return xmlDocument;
	}

	private void LoadBannerIconsFromXml(XmlDocument doc)
	{
		Debug.Print("loading banner_icons.xml:");
		XmlNodeList elementsByTagName = doc.GetElementsByTagName("base");
		if (elementsByTagName.Count != 1)
		{
			throw new TWXmlLoadException("Incorrect XML document format.");
		}
		XmlNode xmlNode = elementsByTagName[0].ChildNodes[0];
		if (xmlNode.Name != "BannerIconData")
		{
			throw new TWXmlLoadException("Incorrect XML document format.");
		}
		if (xmlNode.Name == "BannerIconData")
		{
			foreach (XmlNode childNode in xmlNode.ChildNodes)
			{
				if (childNode.Name == "BannerIconGroup")
				{
					BannerIconGroup bannerIconGroup = new BannerIconGroup();
					bannerIconGroup.Deserialize(childNode, _bannerIconGroups);
					BannerIconGroup bannerIconGroup2 = _bannerIconGroups.FirstOrDefault((BannerIconGroup x) => x.Id == bannerIconGroup.Id);
					if (bannerIconGroup2 == null)
					{
						_bannerIconGroups.Add(bannerIconGroup);
					}
					else
					{
						bannerIconGroup2.Merge(bannerIconGroup);
					}
				}
				if (!(childNode.Name == "BannerColors"))
				{
					continue;
				}
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					if (childNode2.Name == "Color")
					{
						int key = Convert.ToInt32(childNode2.Attributes["id"].Value);
						if (!_colorPalette.ContainsKey(key))
						{
							uint color = Convert.ToUInt32(childNode2.Attributes["hex"].Value, 16);
							bool playerCanChooseForSigil = Convert.ToBoolean(childNode2.Attributes["player_can_choose_for_sigil"]?.Value ?? "false");
							bool playerCanChooseForBackground = Convert.ToBoolean(childNode2.Attributes["player_can_choose_for_background"]?.Value ?? "false");
							_colorPalette.Add(key, new BannerColor(color, playerCanChooseForSigil, playerCanChooseForBackground));
						}
					}
				}
			}
		}
		_availablePatternCount = 0;
		_availableIconCount = 0;
		foreach (BannerIconGroup bannerIconGroup3 in _bannerIconGroups)
		{
			if (bannerIconGroup3.IsPattern)
			{
				_availablePatternCount += bannerIconGroup3.AllBackgrounds.Count;
			}
			else
			{
				_availableIconCount += bannerIconGroup3.AvailableIcons.Count;
			}
		}
	}
}
