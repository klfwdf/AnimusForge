using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CampaignBattleRecoveryBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickParty);
	}

	private void DailyTickParty(MobileParty party)
	{
		if (party.IsCurrentlyAtSea || !(MBRandom.RandomFloat < DefaultPerks.Medicine.Veterinarian.PrimaryBonus) || !party.HasPerk(DefaultPerks.Medicine.Veterinarian))
		{
			return;
		}
		ItemModifier itemModifier = MBObjectManager.Instance.GetObject<ItemModifier>("lame_horse");
		int num = MBRandom.RandomInt(party.ItemRoster.Count);
		for (int i = num; i < party.ItemRoster.Count + num; i++)
		{
			int index = i % party.ItemRoster.Count;
			ItemObject itemAtIndex = party.ItemRoster.GetItemAtIndex(index);
			ItemRosterElement elementCopyAtIndex = party.ItemRoster.GetElementCopyAtIndex(index);
			if (elementCopyAtIndex.EquipmentElement.ItemModifier == itemModifier)
			{
				party.ItemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement, -1);
				party.ItemRoster.Add(new ItemRosterElement(itemAtIndex, 1));
				break;
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		CheckRecoveryForMapEventSide(mapEvent.AttackerSide);
		CheckRecoveryForMapEventSide(mapEvent.DefenderSide);
	}

	private void CheckRecoveryForMapEventSide(MapEventSide mapEventSide)
	{
		if (mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.FieldBattle && mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.Siege && mapEventSide.MapEvent.EventType != MapEvent.BattleTypes.SiegeOutside)
		{
			return;
		}
		foreach (MapEventParty party2 in mapEventSide.Parties)
		{
			PartyBase party = party2.Party;
			if (!party.IsMobile)
			{
				continue;
			}
			MobileParty mobileParty = party.MobileParty;
			foreach (TroopRosterElement item in party2.WoundedInBattle.GetTroopRoster())
			{
				int index = party2.WoundedInBattle.FindIndexOfTroop(item.Character);
				int elementNumber = party2.WoundedInBattle.GetElementNumber(index);
				if (mobileParty.HasPerk(DefaultPerks.Medicine.BattleHardened))
				{
					float num = DefaultPerks.Medicine.BattleHardened.PrimaryBonus;
					if (mobileParty.IsCurrentlyAtSea)
					{
						num *= 0.5f;
					}
					GiveTroopXp(item, elementNumber, party, MathF.Round(num));
				}
			}
			foreach (TroopRosterElement item2 in party2.DiedInBattle.GetTroopRoster())
			{
				int index2 = party2.DiedInBattle.FindIndexOfTroop(item2.Character);
				int elementNumber2 = party2.DiedInBattle.GetElementNumber(index2);
				if (!mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Medicine.Veterinarian) && item2.Character.IsMounted)
				{
					RecoverMountWithChance(item2, elementNumber2, party);
				}
			}
		}
	}

	private void RecoverMountWithChance(TroopRosterElement troopRosterElement, int count, PartyBase party)
	{
		EquipmentElement equipmentElement = troopRosterElement.Character.Equipment[10];
		if (equipmentElement.Item == null)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			if (MBRandom.RandomFloat < DefaultPerks.Medicine.Veterinarian.SecondaryBonus)
			{
				party.ItemRoster.AddToCounts(equipmentElement.Item, 1);
			}
		}
	}

	private void GiveTroopXp(TroopRosterElement troopRosterElement, int count, PartyBase partyBase, int xp)
	{
		partyBase.MemberRoster.AddXpToTroop(troopRosterElement.Character, xp * count);
	}
}
