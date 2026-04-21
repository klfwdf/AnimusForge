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
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Invalid comparison between Unknown and I4
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
			if ((int)encounterState != 0 && (int)encounterState != 1)
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
