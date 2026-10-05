using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

// A main hero left behind in some prison roster while the player is free makes native battle looting call
// EndCaptivityInternal on a null captor: it adds the main hero to the party again, throws, and the map event
// retries every tick. Runs only on that anomalous path and once per session load.
internal static class PlayerCaptivityGhostSafePatch
{
	private static bool _patched;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched || harmony == null)
		{
			return;
		}
		_patched = true;
		try
		{
			var applyInternal = AccessTools.Method(typeof(EndCaptivityAction), "ApplyInternal");
			if (applyInternal != null)
			{
				harmony.Patch(applyInternal, prefix: new HarmonyMethod(typeof(PlayerCaptivityGhostSafePatch), nameof(EndCaptivityApplyInternalPrefix)));
			}
			var endCaptivityInternal = AccessTools.Method(typeof(PlayerCaptivity), "EndCaptivityInternal");
			if (endCaptivityInternal != null)
			{
				harmony.Patch(endCaptivityInternal, prefix: new HarmonyMethod(typeof(PlayerCaptivityGhostSafePatch), nameof(EndCaptivityInternalPrefix)));
			}
			Logger.Log("CaptivitySafety", "Ghost main hero release guard applied. applyInternal=" + (applyInternal != null) + " endCaptivityInternal=" + (endCaptivityInternal != null));
		}
		catch (Exception ex)
		{
			Logger.Log("CaptivitySafety", "Failed to apply ghost main hero release guard: " + ex.Message);
		}
	}

	public static bool EndCaptivityApplyInternalPrefix(Hero prisoner)
	{
		try
		{
			if (prisoner == null || prisoner != Hero.MainHero || IsPlayerCaptive())
			{
				return true;
			}
			int removed = RemoveGhostMainHeroFromPrisonRosters();
			Logger.Log("CaptivitySafety", "Skipped release of a free main hero. ghostEntriesRemoved=" + removed);
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static bool EndCaptivityInternalPrefix()
	{
		try
		{
			if (Hero.MainHero == null || IsPlayerCaptive())
			{
				return true;
			}
			Logger.Log("CaptivitySafety", "Skipped PlayerCaptivity.EndCaptivity while the player is not captive.");
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static void RepairMainHeroRosters(string reason)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			MobileParty mainParty = MobileParty.MainParty;
			if (mainHero == null || mainParty == null || IsPlayerCaptive())
			{
				return;
			}
			int ghosts = RemoveGhostMainHeroFromPrisonRosters();
			int extra = TrimMainHeroMemberCount(mainParty.MemberRoster, mainHero.CharacterObject);
			if (ghosts > 0 || extra > 0)
			{
				Logger.Log("CaptivitySafety", "Repaired main hero rosters reason=" + (reason ?? "") + " ghostPrisonEntries=" + ghosts + " duplicateMemberEntries=" + extra);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CaptivitySafety", "Repair main hero rosters failed reason=" + (reason ?? "") + ": " + ex.Message);
		}
	}

	private static bool IsPlayerCaptive()
	{
		return PlayerCaptivity.IsCaptive || Hero.MainHero.IsPrisoner;
	}

	private static int TrimMainHeroMemberCount(TroopRoster roster, CharacterObject character)
	{
		if (roster == null || character == null)
		{
			return 0;
		}
		int index = roster.FindIndexOfTroop(character);
		int count = index >= 0 ? roster.GetElementNumber(index) : 0;
		if (count <= 1)
		{
			return 0;
		}
		// Removing hero copies clears PartyBelongedTo, so drop the whole stack and re-add one like native HeroSpawn does.
		roster.AddToCounts(character, -count, insertAtFront: false, woundedCount: -roster.GetElementWoundedNumber(index), xpChange: 0, removeDepleted: true, index: -1);
		roster.AddToCounts(character, 1, insertAtFront: true, woundedCount: 0, xpChange: 0, removeDepleted: true, index: -1);
		return count - 1;
	}

	private static int RemoveGhostMainHeroFromPrisonRosters()
	{
		CharacterObject character = Hero.MainHero?.CharacterObject;
		if (character == null)
		{
			return 0;
		}
		int removed = 0;
		foreach (MobileParty party in MobileParty.All)
		{
			removed += RemoveAllFromRoster(party?.Party?.PrisonRoster, character);
		}
		foreach (Settlement settlement in Settlement.All)
		{
			removed += RemoveAllFromRoster(settlement?.Party?.PrisonRoster, character);
		}
		return removed;
	}

	private static int RemoveAllFromRoster(TroopRoster roster, CharacterObject character)
	{
		if (roster == null)
		{
			return 0;
		}
		int index = roster.FindIndexOfTroop(character);
		int count = index >= 0 ? roster.GetElementNumber(index) : 0;
		if (count <= 0)
		{
			return 0;
		}
		roster.RemoveTroop(character, count);
		return count;
	}
}
