using System;
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

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200015E RID: 350
	public class DefaultTroopSacrificeModel : TroopSacrificeModel
	{
		// Token: 0x170006D5 RID: 1749
		// (get) Token: 0x06001AB5 RID: 6837 RVA: 0x00088DDB File Offset: 0x00086FDB
		public override int BreakOutArmyLeaderRelationPenalty
		{
			get
			{
				return -5;
			}
		}

		// Token: 0x170006D6 RID: 1750
		// (get) Token: 0x06001AB6 RID: 6838 RVA: 0x00088DDF File Offset: 0x00086FDF
		public override int BreakOutArmyMemberRelationPenalty
		{
			get
			{
				return -1;
			}
		}

		// Token: 0x06001AB7 RID: 6839 RVA: 0x00088DE2 File Offset: 0x00086FE2
		public override ExplainedNumber GetLostTroopCountForBreakingInBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent)
		{
			return this.GetLostTroopCount(party, siegeEvent, party.IsTargetingPort && party.IsCurrentlyAtSea);
		}

		// Token: 0x06001AB8 RID: 6840 RVA: 0x00088DFD File Offset: 0x00086FFD
		public override ExplainedNumber GetLostTroopCountForBreakingOutOfBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent, bool isBreakingOutFromPort)
		{
			return this.GetLostTroopCount(party, siegeEvent, isBreakingOutFromPort);
		}

		// Token: 0x06001AB9 RID: 6841 RVA: 0x00088E08 File Offset: 0x00087008
		public override int GetNumberOfTroopsSacrificedForTryingToGetAway(BattleSideEnum playerBattleSide, MapEvent mapEvent)
		{
			mapEvent.RecalculateStrengthOfSides();
			MapEventSide mapEventSide = mapEvent.GetMapEventSide(playerBattleSide);
			float num = mapEvent.StrengthOfSide[(int)playerBattleSide] + 1f;
			float a = mapEvent.StrengthOfSide[(int)playerBattleSide.GetOppositeSide()] / num;
			int num2 = PartyBase.MainParty.NumberOfRegularMembers;
			if (MobileParty.MainParty.Army != null)
			{
				foreach (MobileParty mobileParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
				{
					num2 += mobileParty.Party.NumberOfRegularMembers;
				}
			}
			int num3 = mapEventSide.CountTroops((FlattenedTroopRosterElement x) => x.State == RosterTroopState.Active && !x.Troop.IsHero);
			float baseNumber = (float)num2 * MathF.Pow(MathF.Min(a, 3f), 1.3f) * 0.1f + 5f;
			ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber, false, null);
			SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.TacticsTroopSacrificeReduction, CharacterObject.PlayerCharacter, ref explainedNumber);
			explainedNumber = new ExplainedNumber((float)MathF.Max(1, MathF.Round(explainedNumber.ResultNumber)), false, null);
			if (!MobileParty.MainParty.IsCurrentlyAtSea)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.SwiftRegroup, MobileParty.MainParty, false, ref explainedNumber, false);
			}
			if (explainedNumber.ResultNumber <= (float)num3)
			{
				return MathF.Round(explainedNumber.ResultNumber);
			}
			return -1;
		}

		// Token: 0x06001ABA RID: 6842 RVA: 0x00088F78 File Offset: 0x00087178
		private ExplainedNumber GetLostTroopCount(MobileParty party, SiegeEvent siegeEvent, bool isFromPort)
		{
			if (isFromPort && !siegeEvent.IsBlockadeActive)
			{
				return new ExplainedNumber(0f, false, null);
			}
			int num = 5;
			float num2 = 0f;
			foreach (PartyBase partyBase in siegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege))
			{
				num2 += (isFromPort ? partyBase.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.SeaBattle) : partyBase.GetCustomStrength(BattleSideEnum.Attacker, MapEvent.PowerCalculationContext.PlainBattle));
			}
			float num3;
			int num4;
			if (party.Army != null && party.Army.LeaderParty == party)
			{
				num3 = (isFromPort ? party.Army.LeaderParty.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.SeaBattle) : party.Army.LeaderParty.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.PlainBattle));
				foreach (MobileParty mobileParty in party.Army.LeaderParty.AttachedParties)
				{
					num3 += (isFromPort ? mobileParty.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.SeaBattle) : mobileParty.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.PlainBattle));
				}
				num4 = party.Army.TotalRegularCount;
			}
			else
			{
				num3 = (isFromPort ? party.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.SeaBattle) : party.Party.GetCustomStrength(BattleSideEnum.Defender, MapEvent.PowerCalculationContext.PlainBattle));
				num4 = party.MemberRoster.TotalRegulars;
			}
			float num5 = MathF.Clamp(0.12f * MathF.Pow((num2 + 1f) / (num3 + 1f), 0.25f), 0.12f, 0.24f);
			ExplainedNumber result = new ExplainedNumber(num5 * (float)num4, false, null);
			SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.TacticsTroopSacrificeReduction, CharacterObject.PlayerCharacter, ref result);
			result = new ExplainedNumber((float)(num + (int)result.ResultNumber), false, null);
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Tactics.Improviser, MobileParty.MainParty, false, ref result, isFromPort);
			return result;
		}

		// Token: 0x06001ABB RID: 6843 RVA: 0x00089178 File Offset: 0x00087378
		public override bool CanPlayerGetAwayFromEncounter(out TextObject explanation)
		{
			explanation = TextObject.GetEmpty();
			int num = PartyBase.MainParty.NumberOfHealthyMembers - PartyBase.MainParty.MemberRoster.TotalHeroes;
			if (MobileParty.MainParty.Army != null && (MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty || MobileParty.MainParty.AttachedTo != null))
			{
				foreach (MobileParty mobileParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
				{
					num += mobileParty.Party.NumberOfHealthyMembers - mobileParty.Party.MemberRoster.TotalHeroes;
				}
			}
			if (num <= 8 || Campaign.Current.Models.TroopSacrificeModel.GetNumberOfTroopsSacrificedForTryingToGetAway(PlayerEncounter.Current.PlayerSide, PlayerEncounter.Battle) == -1)
			{
				explanation = new TextObject("{=MTbOGRCF}You don't have enough men!", null);
				return false;
			}
			return true;
		}

		// Token: 0x06001ABC RID: 6844 RVA: 0x0008927C File Offset: 0x0008747C
		public override void GetShipsToSacrificeForTryingToGetAway(BattleSideEnum playerBattleSide, MapEvent mapEvent, out MBList<Ship> shipsToCapture, out Ship shipToTakeDamage, out float damageToApplyForLastShip)
		{
			shipsToCapture = new MBList<Ship>();
			shipToTakeDamage = null;
			damageToApplyForLastShip = 0f;
		}

		// Token: 0x040008F1 RID: 2289
		public const int MinimumNumberOfTroopsRequiredForGetAway = 8;
	}
}
