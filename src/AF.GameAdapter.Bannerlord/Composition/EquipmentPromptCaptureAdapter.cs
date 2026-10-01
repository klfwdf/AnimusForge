using System;using System.Collections.Generic;using System.Linq;using System.Text;using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.CampaignSystem.Settlements;using TaleWorlds.Core;using TaleWorlds.MountAndBlade;
namespace AnimusForge;
internal sealed class HeroEquipmentPromptLivePort {
 internal delegate bool ResolveContextQuery(Hero hero,out bool civilian);
 internal ResolveContextQuery ResolveContext;
 internal Func<Hero,EquipmentIndex,bool,ItemObject> GetItem;
}
internal static class EquipmentPromptCaptureAdapter {
internal static bool IsWeaponEquipmentIndexForPrompt(EquipmentIndex index)
	{
		return index == EquipmentIndex.WeaponItemBeginSlot || index == EquipmentIndex.Weapon1 || index == EquipmentIndex.Weapon2 || index == EquipmentIndex.Weapon3;
	}
 internal static void AddEquipmentSummaryItemForPrompt(Dictionary<string,int> counts,Dictionary<string,string> names,EquipmentIndex index,ItemObject item)
 {
  if(counts==null||names==null||item==null)return;
  // Capture in original ID -> fallback slot -> name order; no engine object leaves here.
  string id=(item.StringId??"").Trim();string slot=string.IsNullOrWhiteSpace(id)?index.ToString():"";
  string name=(item.Name?.ToString()??"").Trim();
  PersonaIntroTextRules.AddEquipmentItem(counts,names,id,slot,name);
 }
internal static string BuildHeroEquipmentSummaryForPrompt(Hero hero, HeroEquipmentPromptLivePort port, int maxEntries = 8)
	{
		if (hero == null)
		{
			return "未知";
		}
		bool useCivilianEquipment = false;
		port.ResolveContext(hero, out useCivilianEquipment);
		string text = PersonaIntroTextRules.GetEquipmentContextLabelForPrompt(useCivilianEquipment);
		try
		{
			Dictionary<string, int> wornCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> wornNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, int> weaponCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> weaponNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			EquipmentIndex[] array = new EquipmentIndex[9]
			{
				EquipmentIndex.NumAllWeaponSlots,
				EquipmentIndex.Body,
				EquipmentIndex.Leg,
				EquipmentIndex.Gloves,
				EquipmentIndex.Cape,
				EquipmentIndex.WeaponItemBeginSlot,
				EquipmentIndex.Weapon1,
				EquipmentIndex.Weapon2,
				EquipmentIndex.Weapon3
			};
			EquipmentIndex[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				EquipmentIndex index = array2[i];
				ItemObject item = port.GetItem(hero, index, useCivilianEquipment);
				if (item != null)
				{
					AddEquipmentSummaryItemForPrompt(IsWeaponEquipmentIndexForPrompt(index) ? weaponCounts : wornCounts, IsWeaponEquipmentIndexForPrompt(index) ? weaponNames : wornNames, index, item);
				}
			}
			List<string> wornItems = PersonaIntroTextRules.BuildEquipmentSummaryItemLinesForPrompt(wornCounts, wornNames, maxEntries);
			List<string> weaponItems = PersonaIntroTextRules.BuildEquipmentSummaryItemLinesForPrompt(weaponCounts, weaponNames, maxEntries);
			return PersonaIntroTextRules.ComposeEquipmentSummary(text, wornItems, weaponItems);
		}
		catch
		{
			return text + "：赤身裸体";
		}
	}
internal static string BuildNonHeroEquipmentSummaryForPrompt(NpcDataPacket npc, int maxEntries = 8)
	{
		if (npc == null)
		{
			return "未知";
		}
		Agent agent = null;
		try
		{
			agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
		}
		catch
		{
			agent = null;
		}
		if (agent == null || !agent.IsActive())
		{
			return "未知";
		}
		bool useCivilianEquipment = false;
		try
		{
			Mission current = Mission.Current;
			if (current != null)
			{
				useCivilianEquipment = current.DoesMissionRequireCivilianEquipment;
			}
			else
			{
				Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
				useCivilianEquipment = settlement != null && !settlement.IsVillage;
			}
		}
		catch
		{
			useCivilianEquipment = false;
		}
		string contextLabel = useCivilianEquipment ? "常服" : "战斗装";
		try
		{
			Dictionary<string, int> wornCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> wornNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, int> weaponCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, string> weaponNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			EquipmentIndex[] array = new EquipmentIndex[9]
			{
				EquipmentIndex.NumAllWeaponSlots,
				EquipmentIndex.Body,
				EquipmentIndex.Leg,
				EquipmentIndex.Gloves,
				EquipmentIndex.Cape,
				EquipmentIndex.WeaponItemBeginSlot,
				EquipmentIndex.Weapon1,
				EquipmentIndex.Weapon2,
				EquipmentIndex.Weapon3
			};
			EquipmentIndex[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				EquipmentIndex equipmentIndex = array2[i];
				ItemObject itemObject = null;
				try
				{
					itemObject = agent.SpawnEquipment[equipmentIndex].Item;
				}
				catch
				{
					itemObject = null;
				}
				if (itemObject == null)
				{
					try
					{
						itemObject = agent.Equipment[equipmentIndex].Item;
					}
					catch
					{
						itemObject = null;
					}
				}
				if (itemObject == null)
				{
					continue;
				}
				AddEquipmentSummaryItemForPrompt(IsWeaponEquipmentIndexForPrompt(equipmentIndex) ? weaponCounts : wornCounts, IsWeaponEquipmentIndexForPrompt(equipmentIndex) ? weaponNames : wornNames, equipmentIndex, itemObject);
			}
			List<string> wornItems = PersonaIntroTextRules.BuildEquipmentSummaryItemLinesForPrompt(wornCounts, wornNames, maxEntries);
			List<string> weaponItems = PersonaIntroTextRules.BuildEquipmentSummaryItemLinesForPrompt(weaponCounts, weaponNames, maxEntries);
			return PersonaIntroTextRules.ComposeEquipmentSummary(contextLabel, wornItems, weaponItems);
		}
		catch
		{
			return contextLabel + "：赤身裸体";
		}
	}
}
