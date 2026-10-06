using System;
using System.Linq;
using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCPartySpeedCalculationModel : PartySpeedModel
{
	private const float RiverBonus = 0.5f;

	private const float OpenSeaBonus = 0.448f;

	private const int PartyFleetSizeThreshold = 3;

	private const int RaftStateSpeed = 4;

	private const float DisorganizedEffect = -0.4f;

	private const float WindDeadZoneThresholdInDegrees = 60f;

	private const float OverburdenedEffect = -1f;

	private const float MaximumNavalSpeed = 10f;

	private static readonly TextObject _textOverburdened = new TextObject("{=xgO3cCgR}Overburdened");

	private static readonly TextObject _textOverFleetSize = new TextObject("{=D3OvWCpp}Over fleet size");

	private static readonly TextObject _textDisorganized = new TextObject("{=JuwBb2Yg}Disorganized");

	private static readonly TextObject _textShallowDraftPenalty = new TextObject("{=RU7pNBts}Shallow Draft");

	private static readonly TextObject _openSeaEffect = new TextObject("{=KzEFMlfZ}Open Sea");

	private static readonly TextObject _riverEffect = new TextObject("{=UvIsHvrt}River");

	private static readonly TextObject _windEffect = new TextObject("{=lJDeXyt1}Wind");

	private static readonly TextObject _gunnarEffect = new TextObject("{=LSVGrpMr}Gunnar's Skill");

	private readonly TextObject _cultureEffect = GameTexts.FindText("str_culture");

	public override float BaseSpeed => base.BaseModel.BaseSpeed;

	public override float MinimumSpeed => base.BaseModel.MinimumSpeed;

	public override ExplainedNumber CalculateBaseSpeed(MobileParty party, bool includeDescriptions = false, int additionalTroopOnFootCount = 0, int additionalTroopOnHorseCount = 0)
	{
		if (party.IsCurrentlyAtSea)
		{
			return CalculateNavalBaseSpeed(party, includeDescriptions);
		}
		return base.BaseModel.CalculateBaseSpeed(party, includeDescriptions, additionalTroopOnFootCount, additionalTroopOnHorseCount);
	}

	private ExplainedNumber CalculateNavalBaseSpeed(MobileParty mobileParty, bool includeDescriptions = false)
	{
		if (!mobileParty.Ships.Any())
		{
			return new ExplainedNumber(4f, includeDescriptions);
		}
		float totalShipSpeed = 0f;
		float minimumShipSpeed = float.MaxValue;
		int neededSkeletalCrew = 0;
		int num = mobileParty.MemberRoster.TotalManCount;
		float num2 = mobileParty.TotalWeightCarried;
		int num3 = mobileParty.Ships.Count;
		int maximumCrewLimit = 0;
		GetMobilePartyShipSpeedData(mobileParty, ref neededSkeletalCrew, ref maximumCrewLimit, ref totalShipSpeed, ref minimumShipSpeed);
		if (mobileParty.AttachedParties.Count != 0)
		{
			foreach (MobileParty attachedParty in mobileParty.AttachedParties)
			{
				num3 += attachedParty.Ships.Count;
				num += attachedParty.MemberRoster.TotalManCount;
				num2 += attachedParty.TotalWeightCarried;
				GetMobilePartyShipSpeedData(attachedParty, ref neededSkeletalCrew, ref maximumCrewLimit, ref totalShipSpeed, ref minimumShipSpeed);
			}
		}
		float baseNumber = (totalShipSpeed / (float)num3 + minimumShipSpeed) * 0.5f;
		ExplainedNumber bonuses = new ExplainedNumber(baseNumber, includeDescriptions);
		if (mobileParty.IsFishingParty())
		{
			Settlement bound = mobileParty.VillagerPartyComponent.Village.Bound;
			PerkHelper.AddPerkBonusForTown(NavalPerks.Shipmaster.GhostShip, bound.Town, isPrimaryBonus: false, ref bonuses);
		}
		ExplainedNumber stat = new ExplainedNumber(neededSkeletalCrew);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.FleetCommander, mobileParty, isPrimaryBonus: false, ref stat);
		neededSkeletalCrew = stat.RoundedResultNumber;
		int num4 = Campaign.Current.Models.PartySpeedCalculatingModel.GetSkeletalCrewCount(mobileParty);
		foreach (MobileParty attachedParty2 in mobileParty.AttachedParties)
		{
			num4 += Campaign.Current.Models.PartySpeedCalculatingModel.GetSkeletalCrewCount(attachedParty2);
		}
		if (num4 < neededSkeletalCrew)
		{
			float underSkeletalCrewEffect = GetUnderSkeletalCrewEffect(num4, neededSkeletalCrew);
			TextObject textObject = null;
			if (includeDescriptions)
			{
				textObject = new TextObject("{=4LlzFaUa}Undermanned ({AVAILABLE_CREW}/{NEEDED_CREW})");
				textObject.SetTextVariable("AVAILABLE_CREW", num4);
				textObject.SetTextVariable("NEEDED_CREW", neededSkeletalCrew);
			}
			bonuses.AddFactor(underSkeletalCrewEffect, textObject);
		}
		if (num > maximumCrewLimit)
		{
			float overCrewSizeEffect = GetOverCrewSizeEffect(num, maximumCrewLimit);
			TextObject textObject2 = null;
			if (includeDescriptions)
			{
				textObject2 = new TextObject("{=X8V6b6mC}Overmanned ({AVAILABLE_CREW}/{NEEDED_CREW})");
				textObject2.SetTextVariable("AVAILABLE_CREW", num);
				textObject2.SetTextVariable("NEEDED_CREW", maximumCrewLimit);
			}
			bonuses.AddFactor(overCrewSizeEffect, textObject2);
		}
		int num5 = (int)Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(mobileParty, mobileParty.IsCurrentlyAtSea).ResultNumber;
		if (num2 > (float)num5)
		{
			ExplainedNumber overburdenedEffect = GetOverburdenedEffect(mobileParty, num2 - (float)num5, num5, includeDescriptions);
			bonuses.AddFromExplainedNumber(overburdenedEffect, _textOverburdened);
		}
		if (num3 > 3)
		{
			int num6 = num3 - 3;
			float num7 = 0.5f;
			float num8 = 0.2f / (1f + (float)Math.Exp((0f - num7) * ((float)num6 - 3f)));
			Hero perkOwnerHero = null;
			if (mobileParty.HasPerk(NavalPerks.Shipmaster.ShoreMaster, out perkOwnerHero, checkSecondaryRole: true))
			{
				num8 *= 1f + NavalPerks.Shipmaster.ShoreMaster.SecondaryBonus;
			}
			Hero perkOwnerHero2 = null;
			if (mobileParty.HasPerk(NavalPerks.Shipmaster.FleetCommander, out perkOwnerHero2))
			{
				num8 *= 1f + NavalPerks.Shipmaster.FleetCommander.PrimaryBonus;
			}
			bonuses.AddFactor(0f - num8, _textOverFleetSize);
		}
		if (mobileParty.IsDisorganized)
		{
			bonuses.AddFactor(-0.4f, _textDisorganized);
		}
		bonuses.LimitMin(MinimumSpeed);
		return bonuses;
	}

	public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)
	{
		ExplainedNumber result = base.BaseModel.CalculateFinalSpeed(mobileParty, finalSpeed);
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace);
		if (mobileParty.IsCurrentlyAtSea)
		{
			switch (faceTerrainType)
			{
			case TerrainType.OpenSea:
				result.AddFactor(0.448f, _openSeaEffect);
				break;
			case TerrainType.River:
				result.AddFactor(0.5f, _riverEffect);
				break;
			}
			if (mobileParty.Ships.Count > 0)
			{
				float num = 0f;
				foreach (Ship ship in mobileParty.Ships)
				{
					if (ship.ShipHull.CanNavigateShallowWater)
					{
						num = ((faceTerrainType != TerrainType.CoastalSea && faceTerrainType != TerrainType.River && faceTerrainType != TerrainType.UnderBridge) ? (num - ship.GetCampaignSpeed() * 0.066f) : (num + ship.GetCampaignSpeed() * 0.066f));
					}
				}
				result.Add(num / (float)mobileParty.Ships.Count, _textShallowDraftPenalty);
			}
			if (!mobileParty.IsInNavalAutoTravel)
			{
				Hero perkOwnerHero = null;
				if ((faceTerrainType == TerrainType.River || faceTerrainType == TerrainType.CoastalSea || faceTerrainType == TerrainType.UnderBridge) && mobileParty.HasPerk(NavalPerks.Shipmaster.RiverRaider, out perkOwnerHero))
				{
					result.AddFactor(-0.448f * NavalPerks.Shipmaster.RiverRaider.PrimaryBonus, NavalPerks.Shipmaster.RiverRaider.Name);
				}
				if (faceTerrainType == TerrainType.River || faceTerrainType == TerrainType.CoastalSea || faceTerrainType == TerrainType.UnderBridge)
				{
					FeatHelper.ApplyCultureFeat(mobileParty.Party, NavalCulturalFeats.NordShipMovementFeat, ref result);
				}
				SkillHelper.AddSkillBonusForParty(NavalSkillEffects.WindBonus, mobileParty, ref result);
				PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.OldSaltsTouch, mobileParty, isPrimaryBonus: true, ref result);
				PerkHelper.AddPerkBonusForParty(NavalPerks.Shipmaster.FavorableTide, mobileParty, isPrimaryBonus: true, ref result);
				if (mobileParty.IsMainParty && NavalStorylineData.IsNavalStoryLineActive())
				{
					result.Add(1f, _gunnarEffect);
				}
			}
			float num2 = CalculateWindBoostForParty(mobileParty);
			result.AddFactor(num2 * (1f + result.SumOfFactors), _windEffect);
			result.LimitMax(10f);
		}
		return result;
	}

	private float CalculateWindBoostForParty(MobileParty mobileParty)
	{
		Vec2 windForPosition = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(mobileParty.Position);
		float num = TaleWorlds.Library.MathF.Abs(mobileParty.Bearing.RotationInRadians - windForPosition.RotationInRadians) * 57.29578f;
		if (windForPosition.Length > 0f)
		{
			if (num < 120f)
			{
				float num2 = MBMath.ClampFloat(MBMath.Map(num, 0f, 120f, windForPosition.Length, 0f) * 1.5f, 0f, 1.5f);
				Hero perkOwnerHero = null;
				if (!mobileParty.IsInNavalAutoTravel && mobileParty.HasPerk(NavalPerks.Shipmaster.FairWinds, out perkOwnerHero))
				{
					num2 += NavalPerks.Shipmaster.FairWinds.PrimaryBonus;
				}
				return num2;
			}
			float result = 0f;
			Hero perkOwnerHero2 = null;
			if (!mobileParty.IsInNavalAutoTravel && mobileParty.HasPerk(NavalPerks.Shipmaster.ShockAndAwe, out perkOwnerHero2, checkSecondaryRole: true))
			{
				result = NavalPerks.Shipmaster.ShockAndAwe.SecondaryBonus;
			}
			return result;
		}
		return 0f;
	}

	private ExplainedNumber GetOverburdenedEffect(MobileParty party, float extraWeightCarried, int partyCapacity, bool includeDescriptions)
	{
		ExplainedNumber stat = new ExplainedNumber(-1f * (extraWeightCarried / (float)partyCapacity), includeDescriptions);
		PerkHelper.AddPerkBonusForParty(NavalPerks.Boatswain.VeteransWisdom, party, isPrimaryBonus: false, ref stat);
		return stat;
	}

	public override int GetSkeletalCrewCount(MobileParty party)
	{
		if (party == null)
		{
			return 0;
		}
		int num = party.MemberRoster.TotalManCount;
		Hero perkOwnerHero = null;
		if (party.HasPerk(NavalPerks.Shipmaster.ChainToOars, out perkOwnerHero, checkSecondaryRole: true))
		{
			int totalManCount = party.PrisonRoster.TotalManCount;
			num += totalManCount;
		}
		return num;
	}

	private void GetMobilePartyShipSpeedData(MobileParty mobileParty, ref int neededSkeletalCrew, ref int maximumCrewLimit, ref float totalShipSpeed, ref float minimumShipSpeed)
	{
		foreach (Ship ship in mobileParty.Ships)
		{
			neededSkeletalCrew += ship.SkeletalCrewCapacity;
			maximumCrewLimit += ship.TotalCrewCapacity;
			float campaignSpeed = ship.GetCampaignSpeed();
			totalShipSpeed += campaignSpeed;
			if (campaignSpeed < minimumShipSpeed)
			{
				minimumShipSpeed = campaignSpeed;
			}
		}
	}

	private float GetOverCrewSizeEffect(int totalMenCount, int maxCrewSize)
	{
		return 1f / ((float)totalMenCount / (float)maxCrewSize) - 1f;
	}

	private float GetUnderSkeletalCrewEffect(float totalManCount, float neededSkeletalCrew)
	{
		float num = totalManCount / neededSkeletalCrew;
		return (0f - (1f - num)) * 0.4f;
	}
}
