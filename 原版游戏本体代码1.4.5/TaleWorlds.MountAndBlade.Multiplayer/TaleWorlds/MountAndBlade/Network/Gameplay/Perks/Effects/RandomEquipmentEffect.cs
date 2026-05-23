using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class RandomEquipmentEffect : MPRandomOnSpawnPerkEffect
{
	protected static string StringType = "RandomEquipmentOnSpawn";

	private MBList<List<(EquipmentIndex, EquipmentElement)>> _groups;

	protected RandomEquipmentEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.Deserialize(node);
		_groups = new MBList<List<(EquipmentIndex, EquipmentElement)>>();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.NodeType == XmlNodeType.Comment || childNode.NodeType == XmlNodeType.SignificantWhitespace || !(childNode.Name == "Group"))
			{
				continue;
			}
			List<(EquipmentIndex, EquipmentElement)> list = new List<(EquipmentIndex, EquipmentElement)>();
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				if (childNode2.NodeType != XmlNodeType.Comment && childNode2.NodeType != XmlNodeType.SignificantWhitespace)
				{
					EquipmentElement item = default(EquipmentElement);
					EquipmentIndex item2 = EquipmentIndex.None;
					XmlAttribute xmlAttribute = childNode2.Attributes?["item"];
					if (xmlAttribute != null)
					{
						ItemObject item3 = MBObjectManager.Instance.GetObject<ItemObject>(xmlAttribute.Value);
						item = new EquipmentElement(item3);
					}
					XmlAttribute xmlAttribute2 = childNode2.Attributes?["slot"];
					if (xmlAttribute2 != null)
					{
						item2 = Equipment.GetEquipmentIndexFromOldEquipmentIndexName(xmlAttribute2.Value);
					}
					list.Add((item2, item));
				}
			}
			if (list.Count > 0)
			{
				_groups.Add(list);
			}
		}
	}

	public override List<(EquipmentIndex, EquipmentElement)> GetAlternativeEquipments(bool isPlayer, List<(EquipmentIndex, EquipmentElement)> alternativeEquipments, bool getAll)
	{
		if (getAll)
		{
			foreach (List<(EquipmentIndex, EquipmentElement)> group in _groups)
			{
				if (EffectTarget == Target.Any || (isPlayer ? (EffectTarget == Target.Player) : (EffectTarget == Target.Troops)))
				{
					if (alternativeEquipments == null)
					{
						alternativeEquipments = new List<(EquipmentIndex, EquipmentElement)>(group);
					}
					else
					{
						alternativeEquipments.AddRange(group);
					}
				}
			}
		}
		else if (EffectTarget == Target.Any || (isPlayer ? (EffectTarget == Target.Player) : (EffectTarget == Target.Troops)))
		{
			if (alternativeEquipments == null)
			{
				alternativeEquipments = new List<(EquipmentIndex, EquipmentElement)>(_groups.GetRandomElement());
			}
			else
			{
				alternativeEquipments.AddRange(_groups.GetRandomElement());
			}
		}
		return alternativeEquipments;
	}
}
