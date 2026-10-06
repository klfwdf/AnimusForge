using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map.DistanceCache;

public readonly struct NavigationCacheElement<T> : IEquatable<NavigationCacheElement<T>> where T : ISettlementDataHolder
{
	public readonly T Settlement;

	public readonly bool IsPortUsed;

	public CampaignVec2 PortPosition => Settlement.PortPosition;

	public CampaignVec2 GatePosition => Settlement.GatePosition;

	public string StringId => Settlement.StringId;

	public NavigationCacheElement(T settlement, bool isPortUsed)
	{
		Settlement = settlement;
		IsPortUsed = isPortUsed;
	}

	public static void Sort(ref NavigationCacheElement<T> settlement1, ref NavigationCacheElement<T> settlement2, out bool isPairChanged)
	{
		isPairChanged = false;
		int num = string.Compare(settlement1.StringId, settlement2.StringId, StringComparison.Ordinal);
		if (num >= 0 && (num != 0 || !settlement1.IsPortUsed))
		{
			NavigationCacheElement<T> navigationCacheElement = settlement2;
			NavigationCacheElement<T> navigationCacheElement2 = settlement1;
			settlement1 = navigationCacheElement;
			settlement2 = navigationCacheElement2;
			isPairChanged = true;
		}
	}

	public override int GetHashCode()
	{
		return StringId.GetDeterministicHashCode() * 2 + (IsPortUsed ? 1 : 0);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is NavigationCacheElement<T> navigationCacheElement))
		{
			return false;
		}
		if (StringId == navigationCacheElement.StringId)
		{
			return IsPortUsed == navigationCacheElement.IsPortUsed;
		}
		return false;
	}

	public bool Equals(NavigationCacheElement<T> other)
	{
		if (EqualityComparer<T>.Default.Equals(Settlement, other.Settlement))
		{
			return IsPortUsed == other.IsPortUsed;
		}
		return false;
	}

	public static bool operator ==(NavigationCacheElement<T> left, NavigationCacheElement<T> right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(NavigationCacheElement<T> left, NavigationCacheElement<T> right)
	{
		return !left.Equals(right);
	}
}
