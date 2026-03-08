using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map.DistanceCache
{
	// Token: 0x02000224 RID: 548
	public readonly struct NavigationCacheElement<T> : IEquatable<NavigationCacheElement<T>> where T : ISettlementDataHolder
	{
		// Token: 0x1700080B RID: 2059
		// (get) Token: 0x060020C0 RID: 8384 RVA: 0x0009096C File Offset: 0x0008EB6C
		public CampaignVec2 PortPosition
		{
			get
			{
				T settlement = this.Settlement;
				return settlement.PortPosition;
			}
		}

		// Token: 0x1700080C RID: 2060
		// (get) Token: 0x060020C1 RID: 8385 RVA: 0x00090990 File Offset: 0x0008EB90
		public CampaignVec2 GatePosition
		{
			get
			{
				T settlement = this.Settlement;
				return settlement.GatePosition;
			}
		}

		// Token: 0x1700080D RID: 2061
		// (get) Token: 0x060020C2 RID: 8386 RVA: 0x000909B4 File Offset: 0x0008EBB4
		public string StringId
		{
			get
			{
				T settlement = this.Settlement;
				return settlement.StringId;
			}
		}

		// Token: 0x060020C3 RID: 8387 RVA: 0x000909D5 File Offset: 0x0008EBD5
		public NavigationCacheElement(T settlement, bool isPortUsed)
		{
			this.Settlement = settlement;
			this.IsPortUsed = isPortUsed;
		}

		// Token: 0x060020C4 RID: 8388 RVA: 0x000909E8 File Offset: 0x0008EBE8
		public static void Sort(ref NavigationCacheElement<T> settlement1, ref NavigationCacheElement<T> settlement2, out bool isPairChanged)
		{
			isPairChanged = false;
			int num = string.Compare(settlement1.StringId, settlement2.StringId, StringComparison.Ordinal);
			if (num < 0 || (num == 0 && settlement1.IsPortUsed))
			{
				return;
			}
			NavigationCacheElement<T> navigationCacheElement = settlement2;
			NavigationCacheElement<T> navigationCacheElement2 = settlement1;
			settlement1 = navigationCacheElement;
			settlement2 = navigationCacheElement2;
			isPairChanged = true;
		}

		// Token: 0x060020C5 RID: 8389 RVA: 0x00090A3A File Offset: 0x0008EC3A
		public override int GetHashCode()
		{
			return this.StringId.GetDeterministicHashCode() * 2 + (this.IsPortUsed ? 1 : 0);
		}

		// Token: 0x060020C6 RID: 8390 RVA: 0x00090A58 File Offset: 0x0008EC58
		public override bool Equals(object obj)
		{
			if (obj is NavigationCacheElement<T>)
			{
				NavigationCacheElement<T> navigationCacheElement = (NavigationCacheElement<T>)obj;
				return this.StringId == navigationCacheElement.StringId && this.IsPortUsed == navigationCacheElement.IsPortUsed;
			}
			return false;
		}

		// Token: 0x060020C7 RID: 8391 RVA: 0x00090A9E File Offset: 0x0008EC9E
		public bool Equals(NavigationCacheElement<T> other)
		{
			return EqualityComparer<T>.Default.Equals(this.Settlement, other.Settlement) && this.IsPortUsed == other.IsPortUsed;
		}

		// Token: 0x060020C8 RID: 8392 RVA: 0x00090AC8 File Offset: 0x0008ECC8
		public static bool operator ==(NavigationCacheElement<T> left, NavigationCacheElement<T> right)
		{
			return left.Equals(right);
		}

		// Token: 0x060020C9 RID: 8393 RVA: 0x00090AD2 File Offset: 0x0008ECD2
		public static bool operator !=(NavigationCacheElement<T> left, NavigationCacheElement<T> right)
		{
			return !left.Equals(right);
		}

		// Token: 0x04000992 RID: 2450
		public readonly T Settlement;

		// Token: 0x04000993 RID: 2451
		public readonly bool IsPortUsed;
	}
}
