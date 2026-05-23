using System;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000090 RID: 144
	public struct AIBehaviorData : IEquatable<AIBehaviorData>
	{
		// Token: 0x06001255 RID: 4693 RVA: 0x00053CA0 File Offset: 0x00051EA0
		public AIBehaviorData(IMapPoint party, AiBehavior aiBehavior, MobileParty.NavigationType navigationType, bool willGatherArmy, bool isFromPort, bool isTargetingPort)
		{
			this.Party = party;
			this.AiBehavior = aiBehavior;
			this.NavigationType = navigationType;
			this.WillGatherArmy = willGatherArmy;
			this.IsFromPort = isFromPort;
			this.IsTargetingPort = isTargetingPort;
			this.Position = CampaignVec2.Zero;
		}

		// Token: 0x06001256 RID: 4694 RVA: 0x00053CDA File Offset: 0x00051EDA
		public AIBehaviorData(CampaignVec2 position, AiBehavior aiBehavior, MobileParty.NavigationType navigationType, bool willGatherArmy, bool isFromPort, bool isTargetingPort)
		{
			this.Position = position;
			this.Party = null;
			this.AiBehavior = aiBehavior;
			this.NavigationType = navigationType;
			this.WillGatherArmy = willGatherArmy;
			this.IsFromPort = isFromPort;
			this.IsTargetingPort = isTargetingPort;
		}

		// Token: 0x06001257 RID: 4695 RVA: 0x00053D10 File Offset: 0x00051F10
		public override bool Equals(object obj)
		{
			return obj is AIBehaviorData && (AIBehaviorData)obj == this;
		}

		// Token: 0x06001258 RID: 4696 RVA: 0x00053D2D File Offset: 0x00051F2D
		public bool Equals(AIBehaviorData other)
		{
			return other == this;
		}

		// Token: 0x06001259 RID: 4697 RVA: 0x00053D3C File Offset: 0x00051F3C
		public override int GetHashCode()
		{
			int aiBehavior = (int)this.AiBehavior;
			int num = aiBehavior.GetHashCode();
			num = ((this.Party != null) ? ((num * 397) ^ this.Party.GetHashCode()) : num);
			num = (num * 397) ^ this.WillGatherArmy.GetHashCode();
			num = (num * 397) ^ this.IsTargetingPort.GetHashCode();
			num = (num * 397) ^ this.IsFromPort.GetHashCode();
			num = (num * 397) ^ this.NavigationType.GetHashCode();
			return (num * 397) ^ this.Position.GetHashCode();
		}

		// Token: 0x0600125A RID: 4698 RVA: 0x00053DE8 File Offset: 0x00051FE8
		public static bool operator ==(AIBehaviorData a, AIBehaviorData b)
		{
			return a.Party == b.Party && a.AiBehavior == b.AiBehavior && a.NavigationType == b.NavigationType && a.WillGatherArmy == b.WillGatherArmy && a.IsFromPort == b.IsFromPort && a.IsTargetingPort == b.IsTargetingPort && a.Position == b.Position;
		}

		// Token: 0x0600125B RID: 4699 RVA: 0x00053E5C File Offset: 0x0005205C
		public static bool operator !=(AIBehaviorData a, AIBehaviorData b)
		{
			return !(a == b);
		}

		// Token: 0x04000609 RID: 1545
		public static readonly AIBehaviorData Invalid = new AIBehaviorData(null, AiBehavior.None, MobileParty.NavigationType.None, false, false, false);

		// Token: 0x0400060A RID: 1546
		public IMapPoint Party;

		// Token: 0x0400060B RID: 1547
		public CampaignVec2 Position;

		// Token: 0x0400060C RID: 1548
		public AiBehavior AiBehavior;

		// Token: 0x0400060D RID: 1549
		public bool WillGatherArmy;

		// Token: 0x0400060E RID: 1550
		public bool IsFromPort;

		// Token: 0x0400060F RID: 1551
		public bool IsTargetingPort;

		// Token: 0x04000610 RID: 1552
		public MobileParty.NavigationType NavigationType;
	}
}
