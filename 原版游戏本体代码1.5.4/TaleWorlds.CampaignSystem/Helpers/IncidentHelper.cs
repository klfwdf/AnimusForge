using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace Helpers;

public static class IncidentHelper
{
	public static T GetSeededRandomElement<T>(List<T> list, long seed)
	{
		if (list == null || list.Count == 0)
		{
			return default(T);
		}
		return list[MobileParty.MainParty.RandomIntWithSeed((uint)seed, list.Count)];
	}

	public static T GetSeededRandomElement<T>(MBList<T> list, long seed)
	{
		if (list == null || list.Count == 0)
		{
			return default(T);
		}
		return list[MobileParty.MainParty.RandomIntWithSeed((uint)seed, list.Count)];
	}

	public static T GetSeededRandomElement<T>(MBReadOnlyList<T> list, long seed)
	{
		if (list == null || list.Count == 0)
		{
			return default(T);
		}
		return list[MobileParty.MainParty.RandomIntWithSeed((uint)seed, list.Count)];
	}
}
