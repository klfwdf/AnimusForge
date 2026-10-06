using System.Collections.Generic;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCTroopSacrificeModel : TroopSacrificeModel
{
	private const int MinNumberOfShipsForSacrificeShips = 2;

	public override int BreakOutArmyLeaderRelationPenalty => base.BaseModel.BreakOutArmyLeaderRelationPenalty;

	public override int BreakOutArmyMemberRelationPenalty => base.BaseModel.BreakOutArmyMemberRelationPenalty;

	public override ExplainedNumber GetLostTroopCountForBreakingInBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent)
	{
		ExplainedNumber lostTroopCountForBreakingInBesiegedSettlement = base.BaseModel.GetLostTroopCountForBreakingInBesiegedSettlement(party, siegeEvent);
		Hero perkOwnerHero = null;
		if (party.HasPerk(NavalPerks.Shipmaster.GhostShip, out perkOwnerHero))
		{
			lostTroopCountForBreakingInBesiegedSettlement.AddFactor(NavalPerks.Shipmaster.GhostShip.PrimaryBonus * -1f, NavalPerks.Shipmaster.GhostShip.Name);
		}
		return lostTroopCountForBreakingInBesiegedSettlement;
	}

	public override ExplainedNumber GetLostTroopCountForBreakingOutOfBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent, bool isBreakingOutFromPort)
	{
		ExplainedNumber lostTroopCountForBreakingOutOfBesiegedSettlement = base.BaseModel.GetLostTroopCountForBreakingOutOfBesiegedSettlement(party, siegeEvent, isBreakingOutFromPort);
		Hero perkOwnerHero = null;
		if (isBreakingOutFromPort && party.HasPerk(NavalPerks.Shipmaster.GhostShip, out perkOwnerHero))
		{
			lostTroopCountForBreakingOutOfBesiegedSettlement.AddFactor(NavalPerks.Shipmaster.GhostShip.PrimaryBonus * -1f, NavalPerks.Shipmaster.GhostShip.Name);
		}
		return lostTroopCountForBreakingOutOfBesiegedSettlement;
	}

	public override int GetNumberOfTroopsSacrificedForTryingToGetAway(BattleSideEnum battleSide, MapEvent mapEvent)
	{
		return base.BaseModel.GetNumberOfTroopsSacrificedForTryingToGetAway(battleSide, mapEvent);
	}

	private static bool CanPlayerSideTryToGetAwayWithTheirShipStats(out float totalDamageToApply)
	{
		totalDamageToApply = 0f;
		BattleSideEnum playerSide = PlayerEncounter.Current.PlayerSide;
		MapEvent battle = PlayerEncounter.Battle;
		float num = 0f;
		foreach (MapEventParty item in battle.PartiesOnSide(playerSide))
		{
			foreach (Ship ship in item.Ships)
			{
				num += ship.HitPoints;
			}
		}
		float num2 = 0f;
		foreach (MapEventParty item2 in battle.PartiesOnSide(playerSide.GetOppositeSide()))
		{
			foreach (Ship ship2 in item2.Ships)
			{
				num2 += ship2.HitPoints;
			}
		}
		float a = num2 / num;
		totalDamageToApply = num * MathF.Pow(MathF.Min(a, 3f), 1.3f) * 0.1f;
		if (totalDamageToApply > 0f)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(totalDamageToApply);
			SkillHelper.AddSkillBonusForParty(NavalSkillEffects.ShipDamageReduction, MobileParty.MainParty, ref explainedNumber);
			float num3 = explainedNumber.ResultNumber;
			Hero perkOwnerHero = null;
			if (MobileParty.MainParty.HasPerk(NavalPerks.Shipmaster.GhostShip, out perkOwnerHero))
			{
				num3 -= num3 * 0.5f;
			}
			ExplainedNumber explainedNumber2 = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateBaseSpeed(MobileParty.MainParty);
			PartyBase leaderParty = battle.GetLeaderParty(playerSide.GetOppositeSide());
			ExplainedNumber explainedNumber3 = Campaign.Current.Models.PartySpeedCalculatingModel.CalculateBaseSpeed(leaderParty.MobileParty);
			if (explainedNumber2.ResultNumber > explainedNumber3.ResultNumber)
			{
				float num4 = MBMath.ClampFloat(explainedNumber2.ResultNumber / explainedNumber3.ResultNumber, 1f, 5f) * 0.1f;
				num3 -= num3 * num4;
			}
			totalDamageToApply = num3;
		}
		return totalDamageToApply < num;
	}

	public override void GetShipsToSacrificeForTryingToGetAway(BattleSideEnum playerBattleSide, MapEvent mapEvent, out MBList<Ship> shipsToCapture, out Ship shipToTakeDamage, out float damageToApplyForLastShip)
	{
		damageToApplyForLastShip = float.MinValue;
		shipsToCapture = new MBList<Ship>();
		shipToTakeDamage = null;
		MBReadOnlyList<MapEventParty> mBReadOnlyList = mapEvent.PartiesOnSide(playerBattleSide);
		mapEvent.RecalculateStrengthOfSides();
		List<Ship> list = new List<Ship>();
		foreach (MapEventParty item in mBReadOnlyList)
		{
			foreach (Ship ship in item.Ships)
			{
				list.Add(ship);
			}
		}
		if (CanPlayerSideTryToGetAwayWithTheirShipStats(out var totalDamageToApply))
		{
			float maxHitPoints = list.MaxBy((Ship x) => x.MaxHitPoints).MaxHitPoints;
			if (totalDamageToApply <= list.MinBy((Ship x) => x.HitPoints).HitPoints)
			{
				shipsToCapture.Add(list.MinBy((Ship x) => x.HitPoints));
				return;
			}
			while (totalDamageToApply > 0f)
			{
				Ship shipToSacrifice = GetShipToSacrifice(maxHitPoints, list);
				if (totalDamageToApply < shipToSacrifice.HitPoints)
				{
					shipToTakeDamage = shipToSacrifice;
					damageToApplyForLastShip = totalDamageToApply;
					totalDamageToApply = 0f;
					break;
				}
				shipsToCapture.Add(shipToSacrifice);
				totalDamageToApply -= shipToSacrifice.HitPoints;
				list.Remove(shipToSacrifice);
			}
		}
		else
		{
			Debug.FailedAssert("This can't be possible anymore (Should already handled in previous menu)", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\GameComponents\\NavalDLCTroopSacrificeModel.cs", "GetShipsToSacrificeForTryingToGetAway", 198);
		}
	}

	private static Ship GetShipToSacrifice(float maxHitPointScore, List<Ship> shipsToSacrifice)
	{
		Dictionary<PartyBase, int> partyShipCounts = new Dictionary<PartyBase, int>();
		foreach (Ship item in shipsToSacrifice)
		{
			if (partyShipCounts.TryGetValue(item.Owner, out var _))
			{
				partyShipCounts[item.Owner]++;
			}
			else
			{
				partyShipCounts.Add(item.Owner, 1);
			}
		}
		int maxOwnedShipCount = partyShipCounts.MaxBy((KeyValuePair<PartyBase, int> x) => x.Value).Value;
		return shipsToSacrifice.MinBy((Ship x) => GetShipSacrificeScore(x, maxOwnedShipCount, partyShipCounts[x.Owner], maxHitPointScore));
	}

	private static float GetShipSacrificeScore(Ship shipToConsider, int maxOwnedShipCount, int ownerCurrentShipCount, float maxHitPointScore)
	{
		float hitPoints = shipToConsider.HitPoints;
		hitPoints += (float)(maxOwnedShipCount - ownerCurrentShipCount) * maxHitPointScore;
		if (shipToConsider.Owner.MobileParty.LeaderHero.IsKingdomLeader)
		{
			hitPoints += 50000f;
		}
		else if (shipToConsider.Owner.MobileParty.LeaderHero.IsClanLeader)
		{
			hitPoints += 20000f;
		}
		return hitPoints;
	}

	public override bool CanPlayerGetAwayFromEncounter(out TextObject explanation)
	{
		if (!base.BaseModel.CanPlayerGetAwayFromEncounter(out explanation))
		{
			return false;
		}
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			int num = MobileParty.MainParty.Ships.Count;
			if (MobileParty.MainParty.Army != null && (MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty || MobileParty.MainParty.AttachedTo != null))
			{
				foreach (MobileParty attachedParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
				{
					num += attachedParty.Ships.Count;
				}
			}
			if (num < 2 || !CanPlayerSideTryToGetAwayWithTheirShipStats(out var _))
			{
				explanation = new TextObject("{=uafBbokT}You don't have enough room on your surviving ships to escape.");
				return false;
			}
		}
		return true;
	}
}
