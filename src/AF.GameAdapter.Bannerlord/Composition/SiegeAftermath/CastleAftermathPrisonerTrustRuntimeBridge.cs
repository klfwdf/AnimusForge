using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;

namespace AnimusForge;

/// <summary>
/// Castle-only persistence for ordinary defeated-garrison trust. Lords remain on AF's
/// personal trust system, so this bridge never shadows or rewrites AF hero trust.
/// </summary>
internal static class CastleAftermathPrisonerTrustRuntimeBridge
{
	private static Dictionary<string, int> _regularTrustByTroopId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	internal static void SyncData(IDataStore dataStore)
	{
		dataStore?.SyncData("_gcczCastleRegularPrisonerTrustByTroopId_v1", ref _regularTrustByTroopId);
		_regularTrustByTroopId ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	}

	internal static void ClearForNewGame()
	{
		_regularTrustByTroopId.Clear();
	}

	internal static int GetSpeakerTrust(
		SiegeCastleActionSpeakerRole role,
		CharacterObject character,
		Hero hero)
	{
		if (role == SiegeCastleActionSpeakerRole.CapturedLord)
		{
			hero ??= character?.HeroObject;
			try
			{
				return hero != null && RewardSystemBehavior.Instance != null
					? SiegeCastlePrisonerTrustProfile.Clamp(RewardSystemBehavior.Instance.GetEffectiveTrust(hero))
					: SiegeCastlePrisonerTrustProfile.DefaultDefeatedGarrisonTrust;
			}
			catch
			{
				return SiegeCastlePrisonerTrustProfile.DefaultDefeatedGarrisonTrust;
			}
		}
		return GetRegularTrust(character);
	}

	internal static int GetRegularTrust(CharacterObject character)
	{
		string key = character?.StringId;
		return !string.IsNullOrWhiteSpace(key) && _regularTrustByTroopId.TryGetValue(key, out int trust)
			? SiegeCastlePrisonerTrustProfile.Clamp(trust)
			: SiegeCastlePrisonerTrustProfile.DefaultDefeatedGarrisonTrust;
	}

	internal static int AdjustSelectedRegularTrust(int delta, string reason)
	{
		if (delta == 0)
		{
			return 0;
		}
		TroopRoster roster = CastleAftermathRuntimeBridge.GetSelectedPrisonerRosterSnapshot();
		return AdjustRegularTrustForRoster(roster, delta, reason);
	}

	internal static int AdjustRegularTrustForRoster(TroopRoster roster, int delta, string reason)
	{
		if (roster == null || delta == 0)
		{
			return 0;
		}
		int changed = 0;
		foreach (TroopRosterElement element in roster.GetTroopRoster().ToList())
		{
			CharacterObject character = element.Character;
			string key = character?.StringId;
			if (character == null || character.IsHero || element.Number <= 0 || string.IsNullOrWhiteSpace(key))
			{
				continue;
			}
			int current = GetRegularTrust(character);
			_regularTrustByTroopId[key] = SiegeCastlePrisonerTrustProfile.Clamp(current + delta);
			changed++;
		}
		Logger.Log("CastleAftermath", "Adjusted castle regular prisoner trust. Delta=" + delta
			+ ", TroopTypes=" + changed + ", Reason=" + (reason ?? "N/A"));
		return changed;
	}

	internal static bool AdjustLordTrust(Hero hero, int delta, string reason)
	{
		if (hero == null || delta == 0 || RewardSystemBehavior.Instance == null)
		{
			return false;
		}
		try
		{
			RewardSystemBehavior.Instance.AdjustPersonalTrustWholeDeltaForExternal(
				hero,
				delta,
				"gccz_castle_" + (reason ?? "prisoner_action"));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Adjust captured lord AF trust failed. Hero=" + (hero.StringId ?? "N/A")
				+ ", Error=" + ex.Message);
			return false;
		}
	}
}
