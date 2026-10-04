using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// Clans that fought in a civil war can end up independent and landless (expelled leader, fiefs confiscated or lost).
// Vanilla then destroys them after 28 days, which players see as a family "going missing".
// They get a grace period instead: no vanilla discontinuation and peace with everyone, so they can take service again.
// Written once per settled war; read only when vanilla asks (settlement/kingdom change or a daily clan tick), never per frame.
internal sealed partial class KingdomCivilWarOwner
{
	internal const int ClanProtectionDays = 112;

	internal bool IsClanProtected(Clan clan)
	{
		return clan != null && _storage.ProtectedClanUntilDay.TryGetValue(clan.StringId ?? "", out int until) && CivilWarWorld.CurrentDay() < until;
	}

	internal void ProtectClans(IEnumerable<Clan> clans)
	{
		if (clans == null) return;
		int day = CivilWarWorld.CurrentDay();
		// Expired entries are dropped here so the map stays bounded by the clans of recent wars.
		foreach (string stale in _storage.ProtectedClanUntilDay.Where(x => x.Value <= day).Select(x => x.Key).ToList()) _storage.ProtectedClanUntilDay.Remove(stale);
		foreach (Clan clan in clans)
		{
			if (clan == null || clan.IsEliminated || string.IsNullOrWhiteSpace(clan.StringId) || clan == Clan.PlayerClan) continue;
			_storage.ProtectedClanUntilDay[clan.StringId] = Math.Max(day + ClanProtectionDays, _storage.ProtectedClanUntilDay.TryGetValue(clan.StringId, out int current) ? current : 0);
		}
	}

	// Called once when a faction's war ends (FinishFaction). Only clans that ended up without a kingdom need shelter:
	// clans that returned home are safe, and the expelled or stranded ones would otherwise be destroyed after 28 days.
	private void ProtectWarClans(KingdomCivilWarFactionState faction)
	{
		if (faction?.WarClanIds == null || faction.WarClanIds.Count == 0) return;
		List<Clan> clans = faction.WarClanIds.Select(CivilWarWorld.FindClan).Where(x => x != null && !x.IsEliminated && x.Kingdom == null && x != Clan.PlayerClan).ToList();
		if (clans.Count == 0) return;
		ProtectClans(clans);
		Host.MakeClansPeaceful(clans, "civil_war_resolved:" + faction.Id);
	}
}
