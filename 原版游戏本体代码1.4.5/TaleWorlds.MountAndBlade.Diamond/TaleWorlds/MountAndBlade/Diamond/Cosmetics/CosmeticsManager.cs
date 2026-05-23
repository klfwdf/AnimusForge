using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;

namespace TaleWorlds.MountAndBlade.Diamond.Cosmetics;

public static class CosmeticsManager
{
	public enum CosmeticRarity
	{
		Default,
		Common,
		Rare,
		Unique
	}

	public enum CosmeticType
	{
		Clothing,
		Frame,
		Sigil,
		Taunt
	}

	private static MBReadOnlyList<CosmeticElement> _cosmeticElementList;

	private static Dictionary<string, CosmeticElement> _cosmeticElementsLookup;

	public static MBReadOnlyList<CosmeticElement> CosmeticElementsList => _cosmeticElementList;

	static CosmeticsManager()
	{
		_cosmeticElementList = new MBReadOnlyList<CosmeticElement>();
		_cosmeticElementsLookup = new Dictionary<string, CosmeticElement>();
		LoadFromXml(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/mpcosmetics.xml");
	}

	public static CosmeticElement GetCosmeticElement(string cosmeticId)
	{
		if (_cosmeticElementsLookup.TryGetValue(cosmeticId, out var value))
		{
			return value;
		}
		return null;
	}

	public static void LoadFromXml(string path)
	{
		XmlDocument xmlDocument = new XmlDocument();
		StreamReader streamReader = new StreamReader(path);
		streamReader.ReadToEnd();
		xmlDocument.Load(path);
		streamReader.Close();
		_cosmeticElementsLookup.Clear();
		MBList<CosmeticElement> mBList = new MBList<CosmeticElement>();
		foreach (XmlNode childNode in xmlDocument.ChildNodes)
		{
			if (!(childNode.Name == "Cosmetics"))
			{
				continue;
			}
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				if (!(childNode2.Name == "Cosmetic"))
				{
					continue;
				}
				string value = childNode2.Attributes["id"].Value;
				CosmeticType cosmeticType = CosmeticType.Clothing;
				string value2 = childNode2.Attributes["type"].Value;
				switch (value2)
				{
				case "Clothing":
					cosmeticType = CosmeticType.Clothing;
					break;
				case "Frame":
					cosmeticType = CosmeticType.Frame;
					break;
				case "Sigil":
					cosmeticType = CosmeticType.Sigil;
					break;
				case "Taunt":
					cosmeticType = CosmeticType.Taunt;
					break;
				default:
					Debug.FailedAssert("Invalid cosmetic type: " + value2, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Cosmetics\\CosmeticsManager.cs", "LoadFromXml", 103);
					break;
				}
				CosmeticRarity rarity = CosmeticRarity.Common;
				string value3 = childNode2.Attributes["rarity"].Value;
				switch (value3)
				{
				case "Common":
					rarity = CosmeticRarity.Common;
					break;
				case "Rare":
					rarity = CosmeticRarity.Rare;
					break;
				case "Unique":
					rarity = CosmeticRarity.Unique;
					break;
				default:
					Debug.FailedAssert("Invalid cosmetic rarity: " + value3, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Cosmetics\\CosmeticsManager.cs", "LoadFromXml", 123);
					break;
				}
				int cost = int.Parse(childNode2.Attributes["cost"].Value);
				switch (cosmeticType)
				{
				case CosmeticType.Clothing:
				{
					List<string> list = new List<string>();
					List<Tuple<string, string>> list2 = new List<Tuple<string, string>>();
					foreach (XmlNode childNode3 in childNode2.ChildNodes)
					{
						if (!(childNode3.Name == "Replace"))
						{
							continue;
						}
						foreach (XmlNode childNode4 in childNode3.ChildNodes)
						{
							if (childNode4.Name == "Item")
							{
								list.Add(childNode4.Attributes.Item(0).Value);
							}
							else if (childNode4.Name == "Itemless")
							{
								list2.Add(Tuple.Create(childNode4.Attributes.Item(0).Value, childNode4.Attributes.Item(1).Value));
							}
						}
					}
					mBList.Add(new ClothingCosmeticElement(value, rarity, cost, list, list2));
					break;
				}
				case CosmeticType.Frame:
					mBList.Add(new CosmeticElement(value, rarity, cost, cosmeticType));
					break;
				case CosmeticType.Sigil:
				{
					string bannerCode = childNode2.Attributes?["banner_code"]?.Value;
					mBList.Add(new SigilCosmeticElement(value, rarity, cost, bannerCode));
					break;
				}
				case CosmeticType.Taunt:
				{
					string name = childNode2.Attributes?["name"]?.Value;
					TauntCosmeticElement item = new TauntCosmeticElement(-1, value, rarity, cost, name);
					mBList.Add(item);
					break;
				}
				}
			}
		}
		_cosmeticElementsLookup = new Dictionary<string, CosmeticElement>();
		foreach (CosmeticElement item2 in mBList)
		{
			_cosmeticElementsLookup[item2.Id] = item2;
		}
		_cosmeticElementList = mBList;
	}

	private static bool CheckForCosmeticsListDuplicatesDebug()
	{
		for (int i = 0; i < _cosmeticElementList.Count; i++)
		{
			for (int j = i + 1; j < _cosmeticElementList.Count; j++)
			{
				if (_cosmeticElementList[i].Id == _cosmeticElementList[j].Id)
				{
					Debug.FailedAssert(_cosmeticElementList[i].Id + " has more than one entry.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Cosmetics\\CosmeticsManager.cs", "CheckForCosmeticsListDuplicatesDebug", 200);
					return false;
				}
			}
		}
		return true;
	}
}
