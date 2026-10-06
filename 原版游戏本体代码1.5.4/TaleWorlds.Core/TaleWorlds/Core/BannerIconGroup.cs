using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public class BannerIconGroup
{
	public MBReadOnlyDictionary<int, BannerIconData> AllIcons;

	public MBReadOnlyDictionary<int, string> AllBackgrounds;

	public MBReadOnlyDictionary<int, BannerIconData> AvailableIcons;

	private Dictionary<int, BannerIconData> _allIcons;

	private Dictionary<int, string> _allBackgrounds;

	private Dictionary<int, BannerIconData> _availableIcons;

	public TextObject Name { get; private set; }

	public bool IsPattern { get; private set; }

	public int Id { get; private set; }

	internal BannerIconGroup()
	{
	}

	public void Deserialize(XmlNode xmlNode, MBList<BannerIconGroup> previouslyAddedGroups)
	{
		_allIcons = new Dictionary<int, BannerIconData>();
		_availableIcons = new Dictionary<int, BannerIconData>();
		_allBackgrounds = new Dictionary<int, string>();
		AllIcons = new MBReadOnlyDictionary<int, BannerIconData>(_allIcons);
		AvailableIcons = new MBReadOnlyDictionary<int, BannerIconData>(_availableIcons);
		AllBackgrounds = new MBReadOnlyDictionary<int, string>(_allBackgrounds);
		Id = Convert.ToInt32(xmlNode.Attributes["id"].Value);
		Name = new TextObject(xmlNode.Attributes["name"].Value);
		IsPattern = Convert.ToBoolean(xmlNode.Attributes["is_pattern"].Value);
		foreach (XmlNode childNode in xmlNode.ChildNodes)
		{
			if (childNode.Name == "Icon")
			{
				int id = Convert.ToInt32(childNode.Attributes["id"].Value);
				string value = childNode.Attributes["material_name"].Value;
				int textureIndex = int.Parse(childNode.Attributes["texture_index"].Value);
				if (!_allIcons.ContainsKey(id) && !previouslyAddedGroups.Any((BannerIconGroup x) => x.AllIcons.ContainsKey(id)))
				{
					_allIcons.Add(id, new BannerIconData(value, textureIndex));
					if (childNode.Attributes["is_reserved"] == null || !Convert.ToBoolean(childNode.Attributes["is_reserved"].Value))
					{
						_availableIcons.Add(id, new BannerIconData(value, textureIndex));
					}
				}
			}
			else if (childNode.Name == "Background")
			{
				int id2 = Convert.ToInt32(childNode.Attributes["id"].Value);
				string value2 = childNode.Attributes["mesh_name"].Value;
				if (childNode.Attributes["is_base_background"] != null && Convert.ToBoolean(childNode.Attributes["is_base_background"].Value))
				{
					BannerManager.Instance.SetBaseBackgroundId(id2);
				}
				if (!_allBackgrounds.ContainsKey(id2) && !previouslyAddedGroups.Any((BannerIconGroup x) => x.AllBackgrounds.ContainsKey(id2)))
				{
					_allBackgrounds.Add(id2, value2);
				}
			}
		}
	}

	public void Merge(BannerIconGroup otherGroup)
	{
		foreach (KeyValuePair<int, BannerIconData> allIcon in otherGroup._allIcons)
		{
			if (!_allIcons.ContainsKey(allIcon.Key))
			{
				_allIcons.Add(allIcon.Key, allIcon.Value);
			}
		}
		foreach (KeyValuePair<int, string> allBackground in otherGroup._allBackgrounds)
		{
			if (!_allBackgrounds.ContainsKey(allBackground.Key))
			{
				_allBackgrounds.Add(allBackground.Key, allBackground.Value);
			}
		}
		foreach (KeyValuePair<int, BannerIconData> availableIcon in otherGroup._availableIcons)
		{
			if (!_availableIcons.ContainsKey(availableIcon.Key))
			{
				_availableIcons.Add(availableIcon.Key, availableIcon.Value);
			}
		}
	}
}
