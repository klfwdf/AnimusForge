using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.Diamond;

internal class ItemList
{
	private static Dictionary<string, ItemInnerData> _items;

	static ItemList()
	{
		_items = new Dictionary<string, ItemInnerData>();
		XmlDocument xmlDocument = new XmlDocument();
		string filename = ModuleHelper.GetModuleFullPath("Native") + "ModuleData/mpitems.xml";
		if (ConfigurationManager.GetAppSettings("MultiplayerItemsFileName") != null)
		{
			filename = ConfigurationManager.GetAppSettings("MultiplayerItemsFileName");
		}
		xmlDocument.Load(filename);
		XmlNode xmlNode = xmlDocument.SelectSingleNode("Items");
		if (xmlNode == null)
		{
			throw new Exception("'Items' node is not defined in mpitems.xml");
		}
		Debug.Print("---" + xmlNode.Name);
		foreach (XmlNode childNode in xmlNode.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Element)
			{
				ItemInnerData itemInnerData = new ItemInnerData();
				itemInnerData.Deserialize(childNode);
				Debug.Print(itemInnerData.TypeId);
				if (!_items.ContainsKey(itemInnerData.TypeId))
				{
					_items.Add(itemInnerData.TypeId, itemInnerData);
				}
				else
				{
					Debug.Print("--- Item type id already exists, check mpitems.xml for item type Id:" + itemInnerData.TypeId);
				}
			}
		}
	}

	internal static ItemType GetItemTypeOf(string typeId)
	{
		return _items[typeId].Type;
	}

	internal static bool IsItemValid(string itemId, string modifierId)
	{
		return _items.ContainsKey(itemId);
	}

	internal static int GetPriceOf(string itemId, string modifierId)
	{
		return _items[itemId].Price;
	}
}
