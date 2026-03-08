using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003D4 RID: 980
	public class CampaignBattleRecoveryBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003A5A RID: 14938 RVA: 0x000F1534 File Offset: 0x000EF734
		public override void RegisterEvents()
		{
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
			CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.DailyTickParty));
		}

		// Token: 0x06003A5B RID: 14939 RVA: 0x000F1564 File Offset: 0x000EF764
		private void DailyTickParty(MobileParty party)
		{
			if (!party.IsCurrentlyAtSea && MBRandom.RandomFloat < DefaultPerks.Medicine.Veterinarian.PrimaryBonus && party.HasPerk(DefaultPerks.Medicine.Veterinarian, false))
			{
				ItemModifier @object = MBObjectManager.Instance.GetObject<ItemModifier>("lame_horse");
				int num = MBRandom.RandomInt(party.ItemRoster.Count);
				for (int i = num; i < party.ItemRoster.Count + num; i++)
				{
					int index = i % party.ItemRoster.Count;
					ItemObject itemAtIndex = party.ItemRoster.GetItemAtIndex(index);
					ItemRosterElement elementCopyAtIndex = party.ItemRoster.GetElementCopyAtIndex(index);
					if (elementCopyAtIndex.EquipmentElement.ItemModifier == @object)
					{
						party.ItemRoster.AddToCounts(elementCopyAtIndex.EquipmentElement, -1);
						party.ItemRoster.Add(new ItemRosterElement(itemAtIndex, 1, null));
						return;
					}
				}
			}
		}

		// Token: 0x06003A5C RID: 14940 RVA: 0x000F1640 File Offset: 0x000EF840
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003A5D RID: 14941 RVA: 0x000F1642 File Offset: 0x000EF842
		private void OnMapEventEnded(MapEvent mapEvent)
		{
			this.CheckRecoveryForMapEventSide(mapEvent.AttackerSide);
			this.CheckRecoveryForMapEventSide(mapEvent.DefenderSide);
		}

		// Token: 0x06003A5E RID: 14942 RVA: 0x000F165C File Offset: 0x000EF85C
		private void CheckRecoveryForMapEventSide(MapEventSide mapEventSide)
		{
			if (mapEventSide.MapEvent.EventType == MapEvent.BattleTypes.FieldBattle || mapEventSide.MapEvent.EventType == MapEvent.BattleTypes.Siege || mapEventSide.MapEvent.EventType == MapEvent.BattleTypes.SiegeOutside)
			{
				foreach (MapEventParty mapEventParty in mapEventSide.Parties)
				{
					PartyBase party = mapEventParty.Party;
					if (party.IsMobile)
					{
						MobileParty mobileParty = party.MobileParty;
						foreach (TroopRosterElement troopRosterElement in mapEventParty.WoundedInBattle.GetTroopRoster())
						{
							int index = mapEventParty.WoundedInBattle.FindIndexOfTroop(troopRosterElement.Character);
							int elementNumber = mapEventParty.WoundedInBattle.GetElementNumber(index);
							if (mobileParty.HasPerk(DefaultPerks.Medicine.BattleHardened, false))
							{
								float num = DefaultPerks.Medicine.BattleHardened.PrimaryBonus;
								if (mobileParty.IsCurrentlyAtSea)
								{
									num *= 0.5f;
								}
								this.GiveTroopXp(troopRosterElement, elementNumber, party, MathF.Round(num));
							}
						}
						foreach (TroopRosterElement troopRosterElement2 in mapEventParty.DiedInBattle.GetTroopRoster())
						{
							int index2 = mapEventParty.DiedInBattle.FindIndexOfTroop(troopRosterElement2.Character);
							int elementNumber2 = mapEventParty.DiedInBattle.GetElementNumber(index2);
							if (!mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Medicine.Veterinarian, false) && troopRosterElement2.Character.IsMounted)
							{
								this.RecoverMountWithChance(troopRosterElement2, elementNumber2, party);
							}
						}
					}
				}
			}
		}

		// Token: 0x06003A5F RID: 14943 RVA: 0x000F1854 File Offset: 0x000EFA54
		private void RecoverMountWithChance(TroopRosterElement troopRosterElement, int count, PartyBase party)
		{
			EquipmentElement equipmentElement = troopRosterElement.Character.Equipment[10];
			if (equipmentElement.Item != null)
			{
				for (int i = 0; i < count; i++)
				{
					if (MBRandom.RandomFloat < DefaultPerks.Medicine.Veterinarian.SecondaryBonus)
					{
						party.ItemRoster.AddToCounts(equipmentElement.Item, 1);
					}
				}
			}
		}

		// Token: 0x06003A60 RID: 14944 RVA: 0x000F18AE File Offset: 0x000EFAAE
		private void GiveTroopXp(TroopRosterElement troopRosterElement, int count, PartyBase partyBase, int xp)
		{
			partyBase.MemberRoster.AddXpToTroop(troopRosterElement.Character, xp * count);
		}
	}
}
