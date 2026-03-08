using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.Settlements.Locations
{
	// Token: 0x020003C6 RID: 966
	public sealed class LocationComplexTemplate : MBObjectBase
	{
		// Token: 0x06003959 RID: 14681 RVA: 0x000E9310 File Offset: 0x000E7510
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Name == "Location")
				{
					if (xmlNode.Attributes == null)
					{
						throw new TWXmlLoadException("node.Attributes != null");
					}
					string value = xmlNode.Attributes["id"].Value;
					TextObject textObject = new TextObject(xmlNode.Attributes["name"].Value, null);
					string[] sceneNames = new string[]
					{
						(xmlNode.Attributes["scene_name"] != null) ? xmlNode.Attributes["scene_name"].Value : "",
						(xmlNode.Attributes["scene_name_1"] != null) ? xmlNode.Attributes["scene_name_1"].Value : "",
						(xmlNode.Attributes["scene_name_2"] != null) ? xmlNode.Attributes["scene_name_2"].Value : "",
						(xmlNode.Attributes["scene_name_3"] != null) ? xmlNode.Attributes["scene_name_3"].Value : ""
					};
					int prosperityMax = int.Parse(xmlNode.Attributes["max_prosperity"].Value);
					bool isIndoor = bool.Parse(xmlNode.Attributes["indoor"].Value);
					bool canBeReserved = xmlNode.Attributes["can_be_reserved"] != null && bool.Parse(xmlNode.Attributes["can_be_reserved"].Value);
					string innerText = xmlNode.Attributes["player_can_enter"].InnerText;
					string innerText2 = xmlNode.Attributes["player_can_see"].InnerText;
					string innerText3 = xmlNode.Attributes["ai_can_exit"].InnerText;
					string innerText4 = xmlNode.Attributes["ai_can_enter"].InnerText;
					this.Locations.Add(new Location(value, textObject, textObject, prosperityMax, isIndoor, canBeReserved, innerText, innerText2, innerText3, innerText4, sceneNames, null));
				}
				if (xmlNode.Name == "Passages")
				{
					foreach (object obj2 in xmlNode.ChildNodes)
					{
						XmlNode xmlNode2 = (XmlNode)obj2;
						if (xmlNode2.Name == "Passage")
						{
							if (xmlNode2.Attributes == null)
							{
								throw new TWXmlLoadException("node.Attributes != null");
							}
							string value2 = xmlNode2.Attributes["location_1"].Value;
							string value3 = xmlNode2.Attributes["location_2"].Value;
							this.Passages.Add(new KeyValuePair<string, string>(value2, value3));
						}
					}
				}
			}
			foreach (KeyValuePair<string, string> keyValuePair in this.Passages)
			{
			}
		}

		// Token: 0x040011AD RID: 4525
		public List<Location> Locations = new List<Location>();

		// Token: 0x040011AE RID: 4526
		public List<KeyValuePair<string, string>> Passages = new List<KeyValuePair<string, string>>();
	}
}
