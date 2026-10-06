using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.View.Scripts;

public class PopupSceneSwitchItemSequence : PopupSceneSequence
{
	public enum BodyPartIndex
	{
		None,
		Weapon0,
		Weapon1,
		Weapon2,
		Weapon3,
		ExtraWeaponSlot,
		Head,
		Body,
		Leg,
		Gloves,
		Cape,
		Horse,
		HorseHarness
	}

	public string InitialItem;

	public string PositiveItem;

	public string NegativeItem;

	public BodyPartIndex InitialBodyPart;

	public BodyPartIndex PositiveBodyPart;

	public BodyPartIndex NegativeBodyPart;

	public override void OnInitialState()
	{
		AttachItem(InitialItem, InitialBodyPart);
	}

	public override void OnPositiveState()
	{
		AttachItem(PositiveItem, PositiveBodyPart);
	}

	public override void OnNegativeState()
	{
		AttachItem(NegativeItem, NegativeBodyPart);
	}

	private EquipmentIndex StringToEquipmentIndex(BodyPartIndex part)
	{
		return part switch
		{
			BodyPartIndex.None => EquipmentIndex.None, 
			BodyPartIndex.Weapon0 => EquipmentIndex.WeaponItemBeginSlot, 
			BodyPartIndex.Weapon1 => EquipmentIndex.Weapon1, 
			BodyPartIndex.Weapon2 => EquipmentIndex.Weapon2, 
			BodyPartIndex.Weapon3 => EquipmentIndex.Weapon3, 
			BodyPartIndex.ExtraWeaponSlot => EquipmentIndex.ExtraWeaponSlot, 
			BodyPartIndex.Head => EquipmentIndex.NumAllWeaponSlots, 
			BodyPartIndex.Body => EquipmentIndex.Body, 
			BodyPartIndex.Leg => EquipmentIndex.Leg, 
			BodyPartIndex.Gloves => EquipmentIndex.Gloves, 
			BodyPartIndex.Cape => EquipmentIndex.Cape, 
			BodyPartIndex.Horse => EquipmentIndex.ArmorItemEndSlot, 
			BodyPartIndex.HorseHarness => EquipmentIndex.HorseHarness, 
			_ => EquipmentIndex.None, 
		};
	}

	private void AttachItem(string itemName, BodyPartIndex bodyPart)
	{
		if (_agentVisuals == null)
		{
			return;
		}
		EquipmentIndex equipmentIndex = StringToEquipmentIndex(bodyPart);
		if (equipmentIndex != EquipmentIndex.None)
		{
			AgentVisualsData copyAgentVisualsData = _agentVisuals.GetCopyAgentVisualsData();
			Equipment equipment = _agentVisuals.GetEquipment().Clone();
			if (itemName == "")
			{
				equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, default(EquipmentElement));
			}
			else
			{
				equipment.AddEquipmentToSlotWithoutAgent(equipmentIndex, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>(itemName)));
			}
			copyAgentVisualsData.RightWieldedItemIndex(0).LeftWieldedItemIndex(-1).Equipment(equipment);
			_agentVisuals.Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData);
		}
	}
}
