using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class BattleCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnHeroCombatHitEvent.AddNonSerializedListener(this, OnHeroCombatHit);
		CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, OnCollectLootItems);
	}

	private static void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
	{
		if (!winnerParty.IsMobile || !winnerParty.MobileParty.HasPerk(DefaultPerks.Engineering.Metallurgy))
		{
			return;
		}
		foreach (ItemRosterElement item in gainedLoots.ToMBList())
		{
			ItemModifier itemModifier = item.EquipmentElement.ItemModifier;
			if (itemModifier == null || !(itemModifier.PriceMultiplier < 1f))
			{
				continue;
			}
			for (int i = 0; i < item.Amount; i++)
			{
				if (MBRandom.RandomFloat < DefaultPerks.Engineering.Metallurgy.PrimaryBonus)
				{
					gainedLoots.AddToCounts(item.EquipmentElement, -1);
					ItemRosterElement itemRosterElement = new ItemRosterElement(item.EquipmentElement.Item, 1);
					gainedLoots.Add(itemRosterElement);
				}
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private static void OnHeroCombatHit(CharacterObject attacker, CharacterObject attacked, PartyBase party, WeaponComponentData attackerWeapon, bool isFatal, int xpGained)
	{
		if (!isFatal || attackerWeapon == null || party.MemberRoster.TotalRegulars <= 0 || !IsWeaponSuitableToGetBaptisedInBloodPerkBonus(attackerWeapon) || !attacker.HeroObject.GetPerkValue(DefaultPerks.TwoHanded.BaptisedInBlood))
		{
			return;
		}
		for (int i = 0; i < party.MemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(i);
			if (!elementCopyAtIndex.Character.IsHero && elementCopyAtIndex.Character.IsInfantry)
			{
				party.MemberRoster.AddXpToTroopAtIndex(i, (int)DefaultPerks.TwoHanded.BaptisedInBlood.PrimaryBonus * elementCopyAtIndex.Number);
			}
		}
	}

	private static bool IsWeaponSuitableToGetBaptisedInBloodPerkBonus(WeaponComponentData attackerWeapon)
	{
		if (attackerWeapon.WeaponClass != WeaponClass.TwoHandedSword && attackerWeapon.WeaponClass != WeaponClass.TwoHandedPolearm && attackerWeapon.WeaponClass != WeaponClass.TwoHandedAxe)
		{
			return attackerWeapon.WeaponClass == WeaponClass.TwoHandedMace;
		}
		return true;
	}
}
