using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class MobilePartyTrainingBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTickParty);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
		CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, OnPlayerUpgradedTroops);
	}

	private void OnPlayerUpgradedTroops(CharacterObject troop, CharacterObject upgrade, int number)
	{
		SkillLevelingManager.OnUpgradeTroops(PartyBase.MainParty, troop, upgrade, number);
	}

	private void HourlyTickParty(MobileParty mobileParty)
	{
		if (mobileParty.LeaderHero != null)
		{
			if (mobileParty.BesiegerCamp != null)
			{
				SkillLevelingManager.OnSieging(mobileParty);
			}
			if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty && mobileParty.AttachedParties.Count > 0)
			{
				SkillLevelingManager.OnLeadingArmy(mobileParty);
			}
			if (mobileParty.IsActive)
			{
				WorkSkills(mobileParty);
			}
		}
	}

	private void OnDailyTickParty(MobileParty mobileParty)
	{
		foreach (TroopRosterElement item in mobileParty.MemberRoster.GetTroopRoster())
		{
			ExplainedNumber effectiveDailyExperience = Campaign.Current.Models.PartyTrainingModel.GetEffectiveDailyExperience(mobileParty, item);
			if (!item.Character.IsHero)
			{
				mobileParty.Party.MemberRoster.AddXpToTroop(item.Character, MathF.Round(effectiveDailyExperience.ResultNumber * (float)item.Number));
			}
		}
		if (mobileParty.IsDisbanding || !mobileParty.HasPerk(DefaultPerks.Bow.Trainer))
		{
			return;
		}
		Hero hero = null;
		int num = int.MaxValue;
		foreach (TroopRosterElement item2 in mobileParty.MemberRoster.GetTroopRoster())
		{
			if (item2.Character.IsHero)
			{
				int skillValue = item2.Character.HeroObject.GetSkillValue(DefaultSkills.Bow);
				if (skillValue < num)
				{
					num = skillValue;
					hero = item2.Character.HeroObject;
				}
			}
		}
		hero?.AddSkillXp(DefaultSkills.Bow, DefaultPerks.Bow.Trainer.PrimaryBonus);
	}

	private void CheckScouting(MobileParty mobileParty)
	{
		if (mobileParty.EffectiveScout != null && !mobileParty.IsCurrentlyAtSea)
		{
			TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace);
			if (mobileParty != MobileParty.MainParty)
			{
				SkillLevelingManager.OnAIPartiesTravel(mobileParty.EffectiveScout, mobileParty.IsCaravan, faceTerrainType);
			}
			SkillLevelingManager.OnTraverseTerrain(mobileParty, faceTerrainType);
		}
	}

	private void WorkSkills(MobileParty mobileParty)
	{
		if (mobileParty.IsMoving)
		{
			CheckScouting(mobileParty);
			if (CampaignTime.Now.GetHourOfDay % 4 == 1)
			{
				CheckMovementSkills(mobileParty);
			}
		}
	}

	private void CheckMovementSkills(MobileParty mobileParty)
	{
		if (mobileParty == MobileParty.MainParty)
		{
			if (!mobileParty.IsCurrentlyAtSea)
			{
				foreach (TroopRosterElement item in mobileParty.MemberRoster.GetTroopRoster())
				{
					if (item.Character.IsHero)
					{
						if (item.Character.Equipment.Horse.IsEmpty)
						{
							SkillLevelingManager.OnTravelOnFoot(item.Character.HeroObject, mobileParty._lastCalculatedSpeed);
						}
						else
						{
							SkillLevelingManager.OnTravelOnHorse(item.Character.HeroObject, mobileParty._lastCalculatedSpeed);
						}
					}
				}
				return;
			}
			SkillLevelingManager.OnTravelOnWater(mobileParty, mobileParty._lastCalculatedSpeed);
		}
		else if (mobileParty.LeaderHero != null)
		{
			if (mobileParty.IsCurrentlyAtSea)
			{
				SkillLevelingManager.OnTravelOnWater(mobileParty, mobileParty._lastCalculatedSpeed);
			}
			else if (mobileParty.LeaderHero.CharacterObject.Equipment.Horse.IsEmpty)
			{
				SkillLevelingManager.OnTravelOnFoot(mobileParty.LeaderHero, mobileParty._lastCalculatedSpeed);
			}
			else
			{
				SkillLevelingManager.OnTravelOnHorse(mobileParty.LeaderHero, mobileParty._lastCalculatedSpeed);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
