using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>Pitch eligibility. Runs only when the player asks to camp.</summary>
internal static class FieldCampRules
{
	private const float NearSettlementDistance = 3f;

	internal static bool CanPitch(out TextObject reason)
	{
		reason = null;
		MobileParty main = MobileParty.MainParty;
		if (Campaign.Current == null || main == null || Hero.MainHero == null || Mission.Current != null)
		{
			reason = FieldCampText.BlockedState();
			return false;
		}
		if (Campaign.Current.ConversationManager?.IsConversationInProgress == true
			|| PlayerEncounter.Current != null || MapEvent.PlayerMapEvent != null || main.MapEvent != null)
		{
			reason = FieldCampText.BlockedEncounter();
			return false;
		}
		if (Hero.MainHero.IsPrisoner || main.CurrentSettlement != null)
		{
			reason = FieldCampText.BlockedState();
			return false;
		}
		if (main.BesiegerCamp != null || main.SiegeEvent != null || main.BesiegedSettlement != null)
		{
			reason = FieldCampText.BlockedSiege();
			return false;
		}
		Army army = main.Army;
		if (army != null && army.LeaderParty != main)
		{
			reason = FieldCampText.BlockedArmyMember();
			return false;
		}
		if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(main))
		{
			reason = FieldCampText.BlockedSea();
			return false;
		}
		Settlement nearby = FindSettlementWithin(main.Position.ToVec2(), NearSettlementDistance);
		if (nearby != null)
		{
			reason = FieldCampText.BlockedSettlement(nearby);
			return false;
		}
		return true;
	}

	// Straight-line scan of ~1k settlements; only runs on a camp request, never per tick.
	private static Settlement FindSettlementWithin(Vec2 position, float distance)
	{
		float limitSquared = distance * distance;
		foreach (Settlement settlement in Settlement.All)
		{
			if (settlement == null || settlement.IsHideout || !(settlement.IsTown || settlement.IsCastle || settlement.IsVillage))
			{
				continue;
			}
			if (settlement.GatePosition.ToVec2().DistanceSquared(position) < limitSquared)
			{
				return settlement;
			}
		}
		return null;
	}
}
