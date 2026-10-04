using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AnimusForge;

public partial class MyBehavior
{
	// Vanilla LeaveWithRebellion + CreateKingdom make a new rebel kingdom fight the old kingdom AND every enemy of the old kingdom.
	// Only the war against the old kingdom is the rebellion itself, so the inherited wars are ended once, right at creation.
	private static void PacifyInheritedRebelWars(Kingdom rebel, Kingdom home)
	{
		try
		{
			if (rebel == null || rebel.IsEliminated) return;
			int ended = 0;
			foreach (IFaction other in rebel.FactionsAtWarWith.ToList())
			{
				if (other == null || other == home) continue;
				if (TryMakePeaceQuietly(rebel, other)) ended++;
			}
			Logger.Log("KingdomRebellion", "[inherited_wars_pacified] rebel=" + GetKingdomId(rebel) + " home=" + GetKingdomId(home) + " ended=" + ended + " remainingWars=" + rebel.FactionsAtWarWith.Count);
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomRebellion", "[WARN] inherited_wars_pacify failed rebel=" + GetKingdomId(rebel) + " error=" + ex.Message);
		}
	}

	// Independent clans left over from a settled civil war make peace with everyone they are not permanently at war with,
	// so they are not hunted while they look for a kingdom to serve.
	internal static void MakeCivilWarClansPeacefulForExternal(IEnumerable<Clan> clans, string reason)
	{
		try
		{
			foreach (Clan clan in (clans ?? Enumerable.Empty<Clan>()).ToList())
			{
				if (clan == null || clan.IsEliminated || clan.Kingdom != null || clan == Clan.PlayerClan) continue;
				int ended = 0;
				foreach (IFaction other in clan.FactionsAtWarWith.ToList())
				{
					if (TryMakePeaceQuietly(clan, other)) ended++;
				}
				if (ended > 0) Logger.Log("KingdomCivilWar", "[clan_peace] reason=" + (reason ?? "") + " clan=" + (clan.StringId ?? "") + " ended=" + ended);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomCivilWar", "[WARN] clan_peace failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	// True only while this file makes peace, so the civil-war grievance listener does not count it as a crown-imposed peace.
	internal static bool QuietPeaceActive;

	private static bool TryMakePeaceQuietly(IFaction first, IFaction second)
	{
		try
		{
			if (first == null || second == null || first == second || first.IsEliminated || second.IsEliminated || !first.IsAtWarWith(second)) return false;
			if (Campaign.Current?.Models?.DiplomacyModel?.IsAtConstantWar(first, second) == true) return false;
			QuietPeaceActive = true;
			try { MakePeaceAction.Apply(first, second); } finally { QuietPeaceActive = false; }
			return !first.IsAtWarWith(second);
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomRebellion", "[WARN] quiet peace failed error=" + ex.Message);
			return false;
		}
	}
}
