using System;
using System.Xml;

namespace TaleWorlds.MountAndBlade.Diamond;

internal class ItemInnerData
{
	internal string TypeId { get; private set; }

	internal ItemType Type { get; private set; }

	internal int Price { get; private set; }

	internal void Deserialize(XmlNode node)
	{
		TypeId = node.Attributes["id"].Value;
		Price = ((node.Attributes["value"] != null) ? int.Parse(node.Attributes["value"].Value) : 0);
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (!(childNode.Name == "flags"))
			{
				continue;
			}
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				if (childNode2.Name == "flag" && childNode2.Attributes["name"].Value == "type")
				{
					string value = childNode2.Attributes["value"].Value;
					Type = (ItemType)Enum.Parse(typeof(ItemType), value, ignoreCase: true);
				}
			}
		}
	}
}
