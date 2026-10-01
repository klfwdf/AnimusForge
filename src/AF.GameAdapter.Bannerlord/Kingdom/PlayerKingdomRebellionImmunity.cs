using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public static class PlayerKingdomRebellionImmunity
{
	public static bool ShouldProtectKingdom(Kingdom kingdom)
	{
		try
		{
			return DuelSettings.IsPlayerKingdomRebellionImmunityEnabled() && IsPlayerRulingKingdom(kingdom);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPlayerRulingKingdom(Kingdom kingdom)
	{
		if (kingdom == null || kingdom.IsEliminated)
		{
			return false;
		}
		Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
		Hero mainHero = Hero.MainHero;
		if (playerClan != null && kingdom.RulingClan == playerClan)
		{
			return true;
		}
		if (mainHero != null && kingdom.Leader == mainHero)
		{
			return true;
		}
		return mainHero != null && mainHero.IsFactionLeader && mainHero.MapFaction == kingdom;
	}
}
