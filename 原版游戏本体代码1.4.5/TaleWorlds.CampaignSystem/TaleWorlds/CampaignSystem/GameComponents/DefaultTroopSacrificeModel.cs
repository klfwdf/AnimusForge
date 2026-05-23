using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultTroopSacrificeModel : TroopSacrificeModel
{
	public const int MinimumNumberOfTroopsRequiredForGetAway = 8;

	public override int BreakOutArmyLeaderRelationPenalty => -5;

	public override int BreakOutArmyMemberRelationPenalty => -1;

	public override ExplainedNumber GetLostTroopCountForBreakingInBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent)
	{
		return GetLostTroopCount(party, siegeEvent, party.IsTargetingPort && party.IsCurrentlyAtSea);
	}

	public override ExplainedNumber GetLostTroopCountForBreakingOutOfBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent, bool isBreakingOutFromPort)
	{
		return GetLostTroopCount(party, siegeEvent, isBreakingOutFromPort);
	}

	public override int GetNumberOfTroopsSacrificedForTryingToGetAway(BattleSideEnum playerBattleSide, MapEvent mapEvent)
	{
		mapEvent.RecalculateStrengthOfSides();
		MapEventSide mapEventSide = mapEvent.GetMapEventSide(playerBattleSide);
		float num = mapEvent.StrengthOfSide[(int)playerBattleSide] + 1f;
		float a = mapEvent.StrengthOfSide[(int)playerBattleSide.GetOppositeSide()] / num;
		int num2 = PartyBase.MainParty.NumberOfRegularMembers;
		if (MobileParty.MainParty.Army != null)
		{
			foreach (MobileParty attachedParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
			{
				num2 += attachedParty.Party.NumberOfRegularMembers;
			}
		}
		int num3 = mapEventSide.CountTroops((FlattenedTroopRosterElement x) => x.State == RosterTroopState.Active && !x.Troop.IsHero);
		float baseNumber = (float)num2 * MathF.Pow(MathF.Min(a, 3f), 1.3f) * 0.1f + 5f;
		ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber);
		SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.TacticsTroopSacrificeReduction, CharacterObject.PlayerCharacter, ref explainedNumber);
		explainedNumber = new ExplainedNumber(MathF.Max(1, MathF.Round(explainedNumber.ResultNumber)));
		if (!MobileParty.MainParty.IsCurrentlyAtSea)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.SwiftRegroup, MobileParty.MainParty, isPrimaryBonus: false, ref explainedNumber);
		}
		if (explainedNumber.ResultNumber <= (float)num3)
		{
			return MathF.Round(explainedNumber.ResultNumber);
		}
		return -1;
	}

	private ExplainedNumber GetLostTroopCount(MobileParty party, SiegeEvent siegeEvent, bool isFromPort)
	{
		if (isFromPort && !siegeEvent.IsBlockadeActive)
		{
			return new ExplainedNumber(0f, includeDescriptions: false, null);
		}
		int num = 5;
		float num2 = 0f;
		foreach (PartyBase item in siegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType())
		{
			num2 += (isFromPort ? item.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.SeaBattle) : item.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.PlainBattle));
		}
		float num3;
		int num4;
		if (party.Army != null && party.Army.LeaderParty == party)
		{
			num3 = (isFromPort ? party.Army.LeaderParty.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.SeaBattle) : party.Army.LeaderParty.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.PlainBattle));
			foreach (MobileParty attachedParty in party.Army.LeaderParty.AttachedParties)
			{
				num3 += (isFromPort ? attachedParty.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.SeaBattle) : attachedParty.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.PlainBattle));
			}
			num4 = party.Army.TotalRegularCount;
		}
		else
		{
			num3 = (isFromPort ? party.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.SeaBattle) : party.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.PlainBattle));
			num4 = party.MemberRoster.TotalRegulars;
		}
		float num5 = MathF.Clamp(0.12f * MathF.Pow((num2 + 1f) / (num3 + 1f), 0.25f), 0.12f, 0.24f);
		ExplainedNumber explainedNumber = new ExplainedNumber(num5 * (float)num4);
		SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.TacticsTroopSacrificeReduction, CharacterObject.PlayerCharacter, ref explainedNumber);
		explainedNumber = new ExplainedNumber(num + (int)explainedNumber.ResultNumber);
		PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Improviser, MobileParty.MainParty, isPrimaryBonus: false, ref explainedNumber, isFromPort);
		return explainedNumber;
	}

	public override bool CanPlayerGetAwayFromEncounter(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		int num = PartyBase.MainParty.NumberOfHealthyMembers - PartyBase.MainParty.MemberRoster.TotalHeroes;
		if (MobileParty.MainParty.Army != null && (MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty || MobileParty.MainParty.AttachedTo != null))
		{
			foreach (MobileParty attachedParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
			{
				num += attachedParty.Party.NumberOfHealthyMembers - attachedParty.Party.MemberRoster.TotalHeroes;
			}
		}
		if (num <= 8 || Campaign.Current.Models.TroopSacrificeModel.GetNumberOfTroopsSacrificedForTryingToGetAway(PlayerEncounter.Current.PlayerSide, PlayerEncounter.Battle) == -1)
		{
			explanation = new TextObject("{=MTbOGRCF}You don't have enough men!");
			return false;
		}
		return true;
	}

	public override void GetShipsToSacrificeForTryingToGetAway(BattleSideEnum playerBattleSide, MapEvent mapEvent, out MBList<Ship> shipsToCapture, out Ship shipToTakeDamage, out float damageToApplyForLastShip)
	{
		shipsToCapture = new MBList<Ship>();
		shipToTakeDamage = null;
		damageToApplyForLastShip = 0f;
	}
}
