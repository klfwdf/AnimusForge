using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000419 RID: 1049
	public class MobilePartyTrainingBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600428C RID: 17036 RVA: 0x001408C8 File Offset: 0x0013EAC8
		public override void RegisterEvents()
		{
			CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.HourlyTickParty));
			CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnDailyTickParty));
			CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, new Action<CharacterObject, CharacterObject, int>(this.OnPlayerUpgradedTroops));
		}

		// Token: 0x0600428D RID: 17037 RVA: 0x0014091A File Offset: 0x0013EB1A
		private void OnPlayerUpgradedTroops(CharacterObject troop, CharacterObject upgrade, int number)
		{
			SkillLevelingManager.OnUpgradeTroops(PartyBase.MainParty, troop, upgrade, number);
		}

		// Token: 0x0600428E RID: 17038 RVA: 0x0014092C File Offset: 0x0013EB2C
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
					this.WorkSkills(mobileParty);
				}
			}
		}

		// Token: 0x0600428F RID: 17039 RVA: 0x00140988 File Offset: 0x0013EB88
		private void OnDailyTickParty(MobileParty mobileParty)
		{
			foreach (TroopRosterElement troopRosterElement in mobileParty.MemberRoster.GetTroopRoster())
			{
				ExplainedNumber effectiveDailyExperience = Campaign.Current.Models.PartyTrainingModel.GetEffectiveDailyExperience(mobileParty, troopRosterElement);
				if (!troopRosterElement.Character.IsHero)
				{
					mobileParty.Party.MemberRoster.AddXpToTroop(troopRosterElement.Character, MathF.Round(effectiveDailyExperience.ResultNumber * (float)troopRosterElement.Number));
				}
			}
			if (!mobileParty.IsDisbanding && mobileParty.HasPerk(DefaultPerks.Bow.Trainer, false))
			{
				Hero hero = null;
				int num = int.MaxValue;
				foreach (TroopRosterElement troopRosterElement2 in mobileParty.MemberRoster.GetTroopRoster())
				{
					if (troopRosterElement2.Character.IsHero)
					{
						int skillValue = troopRosterElement2.Character.HeroObject.GetSkillValue(DefaultSkills.Bow);
						if (skillValue < num)
						{
							num = skillValue;
							hero = troopRosterElement2.Character.HeroObject;
						}
					}
				}
				if (hero != null)
				{
					hero.AddSkillXp(DefaultSkills.Bow, DefaultPerks.Bow.Trainer.PrimaryBonus);
				}
			}
		}

		// Token: 0x06004290 RID: 17040 RVA: 0x00140AE4 File Offset: 0x0013ECE4
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

		// Token: 0x06004291 RID: 17041 RVA: 0x00140B38 File Offset: 0x0013ED38
		private void WorkSkills(MobileParty mobileParty)
		{
			if (mobileParty.IsMoving)
			{
				this.CheckScouting(mobileParty);
				if (CampaignTime.Now.GetHourOfDay % 4 == 1)
				{
					this.CheckMovementSkills(mobileParty);
				}
			}
		}

		// Token: 0x06004292 RID: 17042 RVA: 0x00140B70 File Offset: 0x0013ED70
		private void CheckMovementSkills(MobileParty mobileParty)
		{
			if (mobileParty == MobileParty.MainParty)
			{
				using (List<TroopRosterElement>.Enumerator enumerator = mobileParty.MemberRoster.GetTroopRoster().GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						TroopRosterElement troopRosterElement = enumerator.Current;
						if (troopRosterElement.Character.IsHero)
						{
							if (mobileParty.IsCurrentlyAtSea)
							{
								SkillLevelingManager.OnTravelOnWater(troopRosterElement.Character.HeroObject, mobileParty._lastCalculatedSpeed);
							}
							else if (troopRosterElement.Character.Equipment.Horse.IsEmpty)
							{
								SkillLevelingManager.OnTravelOnFoot(troopRosterElement.Character.HeroObject, mobileParty._lastCalculatedSpeed);
							}
							else
							{
								SkillLevelingManager.OnTravelOnHorse(troopRosterElement.Character.HeroObject, mobileParty._lastCalculatedSpeed);
							}
						}
					}
					return;
				}
			}
			if (mobileParty.LeaderHero != null)
			{
				if (mobileParty.IsCurrentlyAtSea)
				{
					SkillLevelingManager.OnTravelOnWater(mobileParty.LeaderHero, mobileParty._lastCalculatedSpeed);
					return;
				}
				if (mobileParty.LeaderHero.CharacterObject.Equipment.Horse.IsEmpty)
				{
					SkillLevelingManager.OnTravelOnFoot(mobileParty.LeaderHero, mobileParty._lastCalculatedSpeed);
					return;
				}
				SkillLevelingManager.OnTravelOnHorse(mobileParty.LeaderHero, mobileParty._lastCalculatedSpeed);
			}
		}

		// Token: 0x06004293 RID: 17043 RVA: 0x00140CA8 File Offset: 0x0013EEA8
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
