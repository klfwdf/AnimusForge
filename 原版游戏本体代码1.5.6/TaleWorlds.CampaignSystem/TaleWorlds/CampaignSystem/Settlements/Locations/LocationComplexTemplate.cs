using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Settlements.Locations;

public sealed class LocationComplexTemplate : MBObjectBase
{
	public List<Location> Locations = new List<Location>();

	public List<KeyValuePair<string, string>> Passages = new List<KeyValuePair<string, string>>();

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "Location")
			{
				if (childNode.Attributes == null)
				{
					throw new TWXmlLoadException("node.Attributes != null");
				}
				string value = childNode.Attributes["id"].Value;
				TextObject textObject = new TextObject(childNode.Attributes["name"].Value);
				string[] sceneNames = new string[4]
				{
					(childNode.Attributes["scene_name"] != null) ? childNode.Attributes["scene_name"].Value : "",
					(childNode.Attributes["scene_name_1"] != null) ? childNode.Attributes["scene_name_1"].Value : "",
					(childNode.Attributes["scene_name_2"] != null) ? childNode.Attributes["scene_name_2"].Value : "",
					(childNode.Attributes["scene_name_3"] != null) ? childNode.Attributes["scene_name_3"].Value : ""
				};
				int prosperityMax = int.Parse(childNode.Attributes["max_prosperity"].Value);
				bool isIndoor = bool.Parse(childNode.Attributes["indoor"].Value);
				bool canBeReserved = childNode.Attributes["can_be_reserved"] != null && bool.Parse(childNode.Attributes["can_be_reserved"].Value);
				string innerText = childNode.Attributes["player_can_enter"].InnerText;
				string innerText2 = childNode.Attributes["player_can_see"].InnerText;
				string innerText3 = childNode.Attributes["ai_can_exit"].InnerText;
				string innerText4 = childNode.Attributes["ai_can_enter"].InnerText;
				Locations.Add(new Location(value, textObject, textObject, prosperityMax, isIndoor, canBeReserved, innerText, innerText2, innerText3, innerText4, sceneNames, null));
			}
			if (!(childNode.Name == "Passages"))
			{
				continue;
			}
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				if (childNode2.Name == "Passage")
				{
					if (childNode2.Attributes == null)
					{
						throw new TWXmlLoadException("node.Attributes != null");
					}
					string value2 = childNode2.Attributes["location_1"].Value;
					string value3 = childNode2.Attributes["location_2"].Value;
					Passages.Add(new KeyValuePair<string, string>(value2, value3));
				}
			}
		}
		foreach (KeyValuePair<string, string> passage in Passages)
		{
			_ = passage;
		}
	}
}
