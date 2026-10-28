using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class AlternativeEquipmentEffect : MPOnSpawnPerkEffect
{
	protected static string StringType = "AlternativeEquipmentOnSpawn";

	private EquipmentElement _item;

	private EquipmentIndex _index;

	protected AlternativeEquipmentEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.Deserialize(node);
		_item = default(EquipmentElement);
		_index = EquipmentIndex.None;
		XmlNode xmlNode = node?.Attributes?["item"];
		if (xmlNode != null)
		{
			ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(xmlNode.Value);
			_item = new EquipmentElement(item);
		}
		XmlNode xmlNode2 = node?.Attributes?["slot"];
		if (xmlNode2 != null)
		{
			_index = Equipment.GetEquipmentIndexFromOldEquipmentIndexName(xmlNode2.Value);
		}
	}

	public override List<(EquipmentIndex, EquipmentElement)> GetAlternativeEquipments(bool isPlayer, List<(EquipmentIndex, EquipmentElement)> alternativeEquipments, bool getAll)
	{
		if (EffectTarget == Target.Any || (isPlayer ? (EffectTarget == Target.Player) : (EffectTarget == Target.Troops)))
		{
			if (alternativeEquipments == null)
			{
				alternativeEquipments = new List<(EquipmentIndex, EquipmentElement)> { (_index, _item) };
			}
			else
			{
				alternativeEquipments.Add((_index, _item));
			}
		}
		return alternativeEquipments;
	}
}
