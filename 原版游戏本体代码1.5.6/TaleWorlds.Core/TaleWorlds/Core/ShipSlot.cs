using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class ShipSlot : MBObjectBase
{
	private MBList<ShipUpgradePiece> _matchingPieces;

	public string TypeId { get; private set; }

	public string MainPrefabId { get; private set; }

	public MBReadOnlyList<ShipUpgradePiece> MatchingPieces => _matchingPieces;

	public ShipSlot()
	{
		_matchingPieces = new MBList<ShipUpgradePiece>();
	}

	public override void AfterRegister()
	{
		base.AfterRegister();
		Initialize();
		base.IsReady = true;
	}

	public void AddMatchingPiece(ShipUpgradePiece upgradePiece)
	{
		if (!_matchingPieces.Contains(upgradePiece))
		{
			_matchingPieces.Add(upgradePiece);
		}
	}

	public TextObject GetSlotTypeName()
	{
		return GameTexts.FindText("str_ship_slot_type", TypeId);
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		MainPrefabId = node.Attributes["prefab_id"]?.Value ?? base.StringId;
		_ = TypeId;
		TypeId = node.Attributes["type_id"]?.Value ?? base.StringId;
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (!childNode.Name.Equals("ShipUpgradePieces"))
			{
				continue;
			}
			foreach (XmlNode childNode2 in childNode.ChildNodes)
			{
				if (childNode2.Name.Equals("ShipUpgradePiece"))
				{
					string value = childNode2.Attributes["id"].Value;
					ShipUpgradePiece shipUpgradePiece = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(value);
					AddMatchingPiece(shipUpgradePiece);
					shipUpgradePiece.AddTargetSlot(this);
				}
			}
		}
	}
}
