using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000127 RID: 295
	public class DefaultMapVisibilityModel : MapVisibilityModel
	{
		// Token: 0x06001860 RID: 6240 RVA: 0x000750AB File Offset: 0x000732AB
		public override float MaximumSeeingRange()
		{
			return 60f;
		}

		// Token: 0x06001861 RID: 6241 RVA: 0x000750B2 File Offset: 0x000732B2
		public override float GetPartySpottingRangeBase(MobileParty party)
		{
			if (!Campaign.Current.IsNight)
			{
				return 12f;
			}
			return 6f;
		}

		// Token: 0x06001862 RID: 6242 RVA: 0x000750CC File Offset: 0x000732CC
		public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
		{
			float partySpottingRangeBase = Campaign.Current.Models.MapVisibilityModel.GetPartySpottingRangeBase(party);
			ExplainedNumber result = new ExplainedNumber(partySpottingRangeBase, includeDescriptions, null);
			TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
			SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.TrackingSpottingDistance, party, ref result);
			if (!party.IsCurrentlyAtSea)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.EagleEye, party, false, ref result, false);
			}
			Hero effectiveScout = party.EffectiveScout;
			if (effectiveScout != null)
			{
				if (faceTerrainType == TerrainType.Forest && PartyBaseHelper.HasFeat(party.Party, DefaultCulturalFeats.BattanianForestSpeedFeat))
				{
					result.AddFactor(0.15f, GameTexts.FindText("str_culture", null));
				}
				if (!party.IsCurrentlyAtSea)
				{
					if ((faceTerrainType == TerrainType.Plain || faceTerrainType == TerrainType.Steppe) && effectiveScout.GetPerkValue(DefaultPerks.Scouting.WaterDiviner))
					{
						result.AddFactor(DefaultPerks.Scouting.WaterDiviner.PrimaryBonus, DefaultPerks.Scouting.WaterDiviner.Name);
					}
					if (Campaign.Current.IsNight)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.NightRunner, party, false, ref result, false);
					}
					else
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.DayTraveler, party, false, ref result, false);
					}
				}
				if (!party.IsMoving && !party.IsCurrentlyAtSea && party.StationaryStartTime.ElapsedHoursUntilNow >= 1f)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.VantagePoint, party, false, ref result, false);
				}
				if (effectiveScout.GetPerkValue(DefaultPerks.Scouting.MountedScouts) && !party.IsCurrentlyAtSea)
				{
					float num = 0f;
					for (int i = 0; i < party.MemberRoster.Count; i++)
					{
						if (party.MemberRoster.GetCharacterAtIndex(i).DefaultFormationClass.Equals(FormationClass.Cavalry))
						{
							num += (float)party.MemberRoster.GetElementNumber(i);
						}
					}
					if (num / (float)party.MemberRoster.TotalManCount >= 0.5f)
					{
						result.AddFactor(DefaultPerks.Scouting.MountedScouts.PrimaryBonus, DefaultPerks.Scouting.MountedScouts.Name);
					}
				}
			}
			return result;
		}

		// Token: 0x06001863 RID: 6243 RVA: 0x000752B0 File Offset: 0x000734B0
		public override float GetPartyRelativeInspectionRange(IMapPoint party)
		{
			return 0.5f;
		}

		// Token: 0x06001864 RID: 6244 RVA: 0x000752B8 File Offset: 0x000734B8
		public override float GetPartySpottingDifficulty(MobileParty spottingParty, MobileParty party)
		{
			float num = 1f;
			if (party != null && spottingParty != null && Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace) == TerrainType.Forest)
			{
				float num2 = 0.3f;
				if (spottingParty.HasPerk(DefaultPerks.Scouting.KeenSight, false))
				{
					num2 += num2 * DefaultPerks.Scouting.KeenSight.PrimaryBonus;
				}
				num += num2;
			}
			return (1f / MathF.Pow((float)(party.Party.NumberOfAllMembers + party.Party.NumberOfPrisoners + 2) * 0.2f, 0.6f) + 0.94f) * num;
		}

		// Token: 0x06001865 RID: 6245 RVA: 0x00075348 File Offset: 0x00073548
		public override float GetHideoutSpottingDistance()
		{
			if (MobileParty.MainParty.HasPerk(DefaultPerks.Scouting.RumourNetwork, true))
			{
				return MobileParty.MainParty.SeeingRange * 1.2f * (1f + DefaultPerks.Scouting.RumourNetwork.SecondaryBonus);
			}
			return MobileParty.MainParty.SeeingRange * 1.2f;
		}

		// Token: 0x040007E6 RID: 2022
		private const float PartySpottingDifficultyInForests = 0.3f;
	}
}
