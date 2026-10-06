using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Inventory;

public class TransferCommandResult
{
	public Equipment ResultSideEquipment => ResultSide switch
	{
		InventoryLogic.InventorySide.CivilianEquipment => TransferCharacter?.FirstCivilianEquipment, 
		InventoryLogic.InventorySide.BattleEquipment => TransferCharacter?.FirstBattleEquipment, 
		InventoryLogic.InventorySide.StealthEquipment => TransferCharacter?.FirstStealthEquipment, 
		_ => null, 
	};

	public CharacterObject TransferCharacter { get; private set; }

	public InventoryLogic.InventorySide ResultSide { get; private set; }

	public ItemRosterElement EffectedItemRosterElement { get; private set; }

	public int EffectedNumber { get; private set; }

	public int FinalNumber { get; private set; }

	public EquipmentIndex EffectedEquipmentIndex { get; private set; }

	public TransferCommandResult()
	{
	}

	public TransferCommandResult(InventoryLogic.InventorySide resultSide, ItemRosterElement effectedItemRosterElement, int effectedNumber, int finalNumber, EquipmentIndex effectedEquipmentIndex, CharacterObject transferCharacter)
	{
		ResultSide = resultSide;
		EffectedItemRosterElement = effectedItemRosterElement;
		EffectedNumber = effectedNumber;
		FinalNumber = finalNumber;
		EffectedEquipmentIndex = effectedEquipmentIndex;
		TransferCharacter = transferCharacter;
	}
}
