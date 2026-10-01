using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Engine;

namespace AnimusForge;

internal static class LordEncounterRedirectGuard
{
	internal static float SuppressUntil;

	internal static void SuppressForSeconds(float seconds)
	{
		float applicationTime = Time.ApplicationTime;
		float num = applicationTime + ((seconds < 0f) ? 0f : seconds);
		if (num > SuppressUntil)
		{
			SuppressUntil = num;
		}
	}

	internal static void Clear()
	{
		SuppressUntil = 0f;
	}

	internal static bool IsSuppressed()
	{
		if (Time.ApplicationTime >= SuppressUntil)
		{
			return false;
		}
		try
		{
			bool isMeetingActive = MeetingBattleRuntime.IsMeetingActive;
			bool flag = PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null;
			bool flag2 = PlayerEncounter.CampaignBattleResult != null;
			if (!isMeetingActive && !flag && !flag2)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			if (PlayerEncounter.Current == null)
			{
				return false;
			}
			PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
			if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		return true;
	}
}
