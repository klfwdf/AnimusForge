using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Direct-to-inventory castle armament settlement. It does not use the town market loot
/// roster or the town loot screen flags.
/// </summary>
internal static class CastleAftermathArmamentRuntimeBridge
{
	internal static CastleAftermathArmamentResult ReceiveSelectedRegularArmaments(string source)
	{
		TroopRoster selected = CastleAftermathRuntimeBridge.GetSelectedPrisonerRosterSnapshot();
		ItemRoster target = PartyBase.MainParty?.ItemRoster;
		if (selected == null || target == null)
		{
			return new CastleAftermathArmamentResult(0, 0, 0);
		}

		int itemCount = 0;
		int stackKinds = 0;
		int gold = 0;
		var kinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (TroopRosterElement element in selected.GetTroopRoster().ToList())
		{
			CharacterObject character = element.Character;
			if (character == null || character.IsHero || element.Number <= 0)
			{
				continue;
			}
			Equipment equipment = character.FirstBattleEquipment;
			if (equipment == null)
			{
				continue;
			}
			float dropRatio = MathF.Min(0.30f, 0.10f + Math.Max(0, character.Tier) * 0.025f);
			for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumEquipmentSetSlots; slot++)
			{
				EquipmentElement equipmentElement = equipment[slot];
				ItemObject item = equipmentElement.Item;
				if (!CanTransfer(equipmentElement))
				{
					continue;
				}
				int amount = Math.Min(element.Number, Math.Max(0, (int)MathF.Round(element.Number * dropRatio)));
				if (amount <= 0 && element.Number >= 4)
				{
					amount = 1;
				}
				if (amount <= 0)
				{
					continue;
				}
				target.AddToCounts(equipmentElement, amount);
				itemCount += amount;
				if (kinds.Add(item.StringId ?? item.Name?.ToString() ?? "item"))
				{
					stackKinds++;
				}
			}
			gold += Math.Max(0, character.Tier) * Math.Max(0, element.Number) * 3;
		}
		if (gold > 0 && Hero.MainHero != null)
		{
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, disableNotification: true);
		}
		Logger.Log("CastleAftermath", "Received regular prisoner armaments directly. Items=" + itemCount
			+ ", Kinds=" + stackKinds + ", Gold=" + gold + ", Source=" + (source ?? "N/A"));
		return new CastleAftermathArmamentResult(itemCount, stackKinds, gold);
	}

	internal static CastleAftermathArmamentResult ReceiveLordArmaments(Hero lord, string source)
	{
		ItemRoster target = PartyBase.MainParty?.ItemRoster;
		Equipment equipment = lord?.BattleEquipment;
		if (target == null || equipment == null)
		{
			return new CastleAftermathArmamentResult(0, 0, 0);
		}
		int count = 0;
		var kinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumEquipmentSetSlots; slot++)
		{
			EquipmentElement element = equipment[slot];
			if (!CanTransfer(element))
			{
				continue;
			}
			target.AddToCounts(element, 1);
			equipment[slot] = EquipmentElement.Invalid;
			count++;
			kinds.Add(element.Item.StringId ?? element.Item.Name?.ToString() ?? "item");
		}
		Logger.Log("CastleAftermath", "Received captured lord armaments directly. Hero="
			+ (lord?.StringId ?? "N/A") + ", Items=" + count + ", Source=" + (source ?? "N/A"));
		return new CastleAftermathArmamentResult(count, kinds.Count, 0);
	}

	private static bool CanTransfer(EquipmentElement element)
	{
		ItemObject item = element.Item;
		return item != null
			&& !element.IsQuestItem
			&& !item.IsBannerItem
			&& item.ItemType != ItemObject.ItemTypeEnum.Horse
			&& item.ItemType != ItemObject.ItemTypeEnum.HorseHarness;
	}
}

internal sealed class CastleAftermathArmamentResult
{
	internal CastleAftermathArmamentResult(int itemCount, int stackKinds, int gold)
	{
		ItemCount = Math.Max(0, itemCount);
		StackKinds = Math.Max(0, stackKinds);
		Gold = Math.Max(0, gold);
	}

	internal int ItemCount { get; }
	internal int StackKinds { get; }
	internal int Gold { get; }
}
