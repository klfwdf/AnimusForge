using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

public partial class SiegeAiInterventionBehavior
{
	private static void ApplySettlementOwnershipBySiege(Hero capturer, Settlement settlement)
	{
		ChangeOwnerOfSettlementAction.ApplyBySiege(capturer, capturer, settlement);
	}

	private static void ApplySettlementOwnershipByDefault(Hero owner, Settlement settlement)
	{
		ChangeOwnerOfSettlementAction.ApplyByDefault(owner, settlement);
	}

	private static void ConfirmSettlementOwnershipAssignment(Settlement settlement)
	{
		settlement.Town.IsOwnerUnassigned = false;
	}

	private static void ApplyNativeSettlementAftermath(
		MobileParty attackerParty,
		Settlement settlement,
		SiegeAftermathAction.SiegeAftermath aftermath,
		Clan previousOwner,
		Dictionary<MobileParty, float> contributions)
	{
		SiegeAftermathAction.ApplyAftermath(attackerParty, settlement, aftermath, previousOwner, contributions);
	}

	private static void ApplySettlementCulture(Settlement settlement, CultureObject culture, string source)
	{
		GcczSettlementCulturePersistenceBehavior.ApplyAndRemember(settlement, culture, source);
	}

	private static void ApplyHeroCulture(Hero hero, CultureObject culture)
	{
		hero.Culture = culture;
	}

	private static void ClearNotablePowerForReplacement(Hero notable, float power)
	{
		notable.AddPower(-power);
	}

	private static Hero CreateReplacementNotable(Occupation occupation, Settlement settlement)
	{
		return HeroCreator.CreateNotable(occupation, settlement);
	}

	private static void PlaceReplacementNotable(Hero notable, Settlement settlement)
	{
		EnterSettlementAction.ApplyForCharacterOnly(notable, settlement);
	}

	private static void KillInterventionNotableByBattle(Hero notable)
	{
		KillCharacterAction.ApplyByBattle(notable, Hero.MainHero, true);
	}

	private static void KillInterventionNotableByMurder(Hero notable)
	{
		KillCharacterAction.ApplyByMurder(notable, Hero.MainHero, false);
	}

	private static void RemoveInterventionNotable(Hero notable)
	{
		KillCharacterAction.ApplyByRemove(notable, false, true);
	}
}
