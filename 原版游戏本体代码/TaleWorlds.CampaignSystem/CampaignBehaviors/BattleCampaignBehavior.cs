using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003D2 RID: 978
	public class BattleCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003A49 RID: 14921 RVA: 0x000F0CF3 File Offset: 0x000EEEF3
		public override void RegisterEvents()
		{
			CampaignEvents.OnHeroCombatHitEvent.AddNonSerializedListener(this, new Action<CharacterObject, CharacterObject, PartyBase, WeaponComponentData, bool, int>(BattleCampaignBehavior.OnHeroCombatHit));
			CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, new Action<PartyBase, ItemRoster>(BattleCampaignBehavior.OnCollectLootItems));
		}

		// Token: 0x06003A4A RID: 14922 RVA: 0x000F0D24 File Offset: 0x000EEF24
		private static void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
		{
			if (winnerParty.IsMobile && winnerParty.MobileParty.HasPerk(DefaultPerks.Engineering.Metallurgy, false))
			{
				foreach (ItemRosterElement itemRosterElement in gainedLoots.ToMBList<ItemRosterElement>())
				{
					ItemModifier itemModifier = itemRosterElement.EquipmentElement.ItemModifier;
					if (itemModifier != null && itemModifier.PriceMultiplier < 1f)
					{
						for (int i = 0; i < itemRosterElement.Amount; i++)
						{
							int num = 0;
							if (MBRandom.RandomFloat < DefaultPerks.Engineering.Metallurgy.PrimaryBonus)
							{
								num++;
							}
							gainedLoots.AddToCounts(itemRosterElement.EquipmentElement.Item, -num);
							ItemRosterElement itemRosterElement2 = new ItemRosterElement(itemRosterElement.EquipmentElement.Item, num, null);
							gainedLoots.Add(itemRosterElement2);
						}
					}
				}
			}
		}

		// Token: 0x06003A4B RID: 14923 RVA: 0x000F0E28 File Offset: 0x000EF028
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003A4C RID: 14924 RVA: 0x000F0E2C File Offset: 0x000EF02C
		private static void OnHeroCombatHit(CharacterObject attacker, CharacterObject attacked, PartyBase party, WeaponComponentData attackerWeapon, bool isFatal, int xpGained)
		{
			if (isFatal && attackerWeapon != null && party.MemberRoster.TotalRegulars > 0 && BattleCampaignBehavior.IsWeaponSuitableToGetBaptisedInBloodPerkBonus(attackerWeapon) && attacker.HeroObject.GetPerkValue(DefaultPerks.TwoHanded.BaptisedInBlood))
			{
				for (int i = 0; i < party.MemberRoster.Count; i++)
				{
					TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(i);
					if (!elementCopyAtIndex.Character.IsHero && elementCopyAtIndex.Character.IsInfantry)
					{
						party.MemberRoster.AddXpToTroopAtIndex(i, (int)DefaultPerks.TwoHanded.BaptisedInBlood.PrimaryBonus * elementCopyAtIndex.Number);
					}
				}
			}
		}

		// Token: 0x06003A4D RID: 14925 RVA: 0x000F0ECA File Offset: 0x000EF0CA
		private static bool IsWeaponSuitableToGetBaptisedInBloodPerkBonus(WeaponComponentData attackerWeapon)
		{
			return attackerWeapon.WeaponClass == WeaponClass.TwoHandedSword || attackerWeapon.WeaponClass == WeaponClass.TwoHandedPolearm || attackerWeapon.WeaponClass == WeaponClass.TwoHandedAxe || attackerWeapon.WeaponClass == WeaponClass.TwoHandedMace;
		}
	}
}
