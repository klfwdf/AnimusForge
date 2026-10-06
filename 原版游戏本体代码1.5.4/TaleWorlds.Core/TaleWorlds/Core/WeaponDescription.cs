using System;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class WeaponDescription : MBObjectBase
{
	public bool UseCenterOfMassAsHandBase;

	private MBList<CraftingPiece> _availablePieces;

	public WeaponClass WeaponClass { get; private set; }

	public WeaponFlags WeaponFlags { get; private set; }

	public string ItemUsageFeatures { get; private set; }

	public bool RotatedInHand { get; private set; }

	public bool IsHiddenFromUI { get; set; }

	public MBReadOnlyList<CraftingPiece> AvailablePieces => _availablePieces;

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		WeaponClass = ((node.Attributes["weapon_class"] != null) ? ((WeaponClass)Enum.Parse(typeof(WeaponClass), node.Attributes["weapon_class"].Value)) : WeaponClass.Undefined);
		ItemUsageFeatures = ((node.Attributes["item_usage_features"] != null) ? node.Attributes["item_usage_features"].Value : "");
		RotatedInHand = XmlHelper.ReadBool(node, "rotated_in_hand");
		UseCenterOfMassAsHandBase = XmlHelper.ReadBool(node, "use_center_of_mass_as_hand_base");
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "WeaponFlags")
			{
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					WeaponFlags |= (WeaponFlags)Enum.Parse(typeof(WeaponFlags), childNode2.Attributes["value"].Value);
				}
			}
			else
			{
				if (!(childNode.Name == "AvailablePieces"))
				{
					continue;
				}
				_availablePieces = new MBList<CraftingPiece>();
				foreach (XmlNode childNode3 in childNode.ChildNodes)
				{
					if (childNode3.NodeType == XmlNodeType.Element)
					{
						string value = childNode3.Attributes["id"].Value;
						CraftingPiece craftingPiece = MBObjectManager.Instance.GetObject<CraftingPiece>(value);
						if (craftingPiece != null)
						{
							_availablePieces.Add(craftingPiece);
						}
					}
				}
			}
		}
	}
}
