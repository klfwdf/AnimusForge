using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultMapVisibilityModel : MapVisibilityModel
{
	private const float PartySpottingDifficultyInForests = 0.3f;

	public override float MaximumSeeingRange()
	{
		return 60f;
	}

	public override float GetPartySeeingRangeBase(MobileParty party)
	{
		if (!Campaign.Current.IsNight)
		{
			return 12f;
		}
		return 6f;
	}

	public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
	{
		float partySeeingRangeBase = Campaign.Current.Models.MapVisibilityModel.GetPartySeeingRangeBase(party);
		ExplainedNumber explainedNumber = new ExplainedNumber(partySeeingRangeBase, includeDescriptions);
		SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.TrackingSpottingDistance, party, ref explainedNumber);
		PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.EagleEye, party, isPrimaryBonus: false, ref explainedNumber);
		Hero effectiveScout = party.EffectiveScout;
		if (effectiveScout != null)
		{
			TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
			if (faceTerrainType == TerrainType.Forest && PartyBaseHelper.HasFeat(party.Party, DefaultCulturalFeats.BattanianForestSpeedFeat))
			{
				explainedNumber.AddFactor(0.15f, GameTexts.FindText("str_culture"));
			}
			if (!party.IsCurrentlyAtSea)
			{
				if ((faceTerrainType == TerrainType.Plain || faceTerrainType == TerrainType.Steppe) && effectiveScout.GetPerkValue(DefaultPerks.Scouting.WaterDiviner))
				{
					explainedNumber.AddFactor(DefaultPerks.Scouting.WaterDiviner.PrimaryBonus, DefaultPerks.Scouting.WaterDiviner.Name);
				}
				if (Campaign.Current.IsNight)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.NightRunner, party, isPrimaryBonus: false, ref explainedNumber);
				}
				else
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.DayTraveler, party, isPrimaryBonus: false, ref explainedNumber);
				}
			}
			if (!party.IsMoving && party.StationaryStartTime.ElapsedHoursUntilNow >= 1f)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.VantagePoint, party, isPrimaryBonus: true, ref explainedNumber);
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
					explainedNumber.AddFactor(DefaultPerks.Scouting.MountedScouts.PrimaryBonus, DefaultPerks.Scouting.MountedScouts.Name);
				}
			}
		}
		explainedNumber.LimitMax(Campaign.Current.Models.MapVisibilityModel.MaximumSeeingRange(), new TextObject("{=6qv6Hdww}Limit"));
		return explainedNumber;
	}

	public override float GetPartySpottingRatioForMainPartySeeingRange(MobileParty party)
	{
		float num = 1f;
		if (Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace) == TerrainType.Forest)
		{
			float num2 = -0.3f;
			Hero perkOwnerHero = null;
			if (MobileParty.MainParty.HasPerk(DefaultPerks.Scouting.KeenSight, out perkOwnerHero))
			{
				num2 += num2 * DefaultPerks.Scouting.KeenSight.PrimaryBonus;
			}
			num += num2;
		}
		int num3 = ((party.Army != null && party.Army.LeaderParty == party) ? party.Army.TotalManCount : party.MemberRoster.TotalManCount);
		return MBMath.ClampFloat(1.1f - 0.5f * TaleWorlds.Library.MathF.Pow(System.MathF.E, (float)(-num3) / 200f), 0f, 1f) * num;
	}

	public override float GetHideoutSpottingDistance()
	{
		float num = MobileParty.MainParty.SeeingRange * 1.2f;
		Hero perkOwnerHero = null;
		if (MobileParty.MainParty.HasPerk(DefaultPerks.Scouting.RumourNetwork, out perkOwnerHero, checkSecondaryRole: true))
		{
			return num * (1f + DefaultPerks.Scouting.RumourNetwork.SecondaryBonus);
		}
		return num;
	}

	public override void GetMobilePartyVisibilityAndInspectedState(MobileParty mobileParty, Vec2[] points, float seeingRange, out bool isVisible, out bool isInspected)
	{
		isVisible = false;
		isInspected = false;
		Vec2 point;
		if ((mobileParty.SiegeEvent != null && mobileParty.SiegeEvent.BesiegedSettlement.IsInspected) || (mobileParty.MapEvent != null && mobileParty.MapEvent.IsRaid && mobileParty.MapEvent.MapEventSettlement.IsInspected))
		{
			isVisible = true;
			isInspected = true;
		}
		else if (mobileParty.CurrentSettlement != null)
		{
			isVisible = false;
			isInspected = false;
		}
		else if (TryGetBestPoint(points, mobileParty.Position.ToVec2(), seeingRange, out point))
		{
			CalculateMobilePartyVisibilityAndInspected(point, mobileParty, out isVisible, out isInspected, seeingRange);
		}
	}

	public override void GetSettlementInspectedState(Settlement settlement, Vec2[] points, float seeingRange, out bool isInspected, out bool isDistanceDependent)
	{
		isInspected = false;
		isDistanceDependent = false;
		if (TryGetBestPoint(points, settlement.GatePosition.ToVec2(), seeingRange, out var point) || (settlement.HasPort && TryGetBestPoint(points, settlement.PortPosition.ToVec2(), seeingRange, out point)))
		{
			isInspected = CalculateSettlementInspected(point, settlement, seeingRange);
			isDistanceDependent = true;
		}
	}

	public override bool IsVisibilityPersistent(MobileParty mobileParty)
	{
		if (Campaign.Current.TrueSight)
		{
			return true;
		}
		if (PlayerCaptivity.CaptorParty != null && PlayerCaptivity.CaptorParty == mobileParty.Party)
		{
			return true;
		}
		if (!mobileParty.IsActive || mobileParty.IsGarrison || mobileParty.IsMilitia)
		{
			return true;
		}
		return false;
	}

	private bool CalculateSettlementInspected(Vec2 fromPosition, IMapPoint mapPoint, float mainPartySeeingRange)
	{
		return CalculateVisibilityRangeOfMapPoint(fromPosition, mapPoint, mainPartySeeingRange) <= 1.5f;
	}

	private float CalculateVisibilityRangeOfMapPoint(Vec2 fromPosition, IMapPoint mapPoint, float mainPartySeeingRange)
	{
		Vec2 vec = fromPosition - mapPoint.Position.ToVec2();
		return vec.Length / mainPartySeeingRange;
	}

	private bool TryGetBestPoint(Vec2[] points, Vec2 targetPosition, float seeingRange, out Vec2 point)
	{
		point = default(Vec2);
		Vec2 vec = Vec2.Invalid;
		float num = float.MaxValue;
		for (int i = 0; i < points.Length; i++)
		{
			float num2 = targetPosition.DistanceSquared(points[i]);
			if (num2 < num)
			{
				num = num2;
				vec = points[i];
			}
		}
		if (num < seeingRange * seeingRange)
		{
			point = vec;
			return true;
		}
		return false;
	}

	private void CalculateMobilePartyVisibilityAndInspected(Vec2 fromPosition, MobileParty mobileParty, out bool isVisible, out bool isInspected, float mainPartySeeingRange)
	{
		isInspected = false;
		isVisible = false;
		if (mobileParty.Army != null && mobileParty.Army.LeaderParty.AttachedParties.IndexOf(mobileParty) >= 0)
		{
			isVisible = mobileParty.Army.LeaderParty.IsVisible;
			return;
		}
		float num = CalculateVisibilityRangeOfMapPoint(fromPosition, mobileParty, mainPartySeeingRange);
		if (num < 1f)
		{
			float partySpottingRatioForMainPartySeeingRange = Campaign.Current.Models.MapVisibilityModel.GetPartySpottingRatioForMainPartySeeingRange(mobileParty);
			isVisible = mobileParty.IsActive && num <= partySpottingRatioForMainPartySeeingRange;
			if (isVisible)
			{
				isInspected = true;
			}
		}
	}
}
