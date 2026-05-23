using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001DB RID: 475
	public abstract class AgeModel : MBGameModel<AgeModel>
	{
		// Token: 0x17000790 RID: 1936
		// (get) Token: 0x06001E54 RID: 7764
		public abstract int BecomeInfantAge { get; }

		// Token: 0x17000791 RID: 1937
		// (get) Token: 0x06001E55 RID: 7765
		public abstract int BecomeChildAge { get; }

		// Token: 0x17000792 RID: 1938
		// (get) Token: 0x06001E56 RID: 7766
		public abstract int BecomeTeenagerAge { get; }

		// Token: 0x17000793 RID: 1939
		// (get) Token: 0x06001E57 RID: 7767
		public abstract int HeroComesOfAge { get; }

		// Token: 0x17000794 RID: 1940
		// (get) Token: 0x06001E58 RID: 7768
		public abstract int BecomeOldAge { get; }

		// Token: 0x17000795 RID: 1941
		// (get) Token: 0x06001E59 RID: 7769
		public abstract int MiddleAdultHoodAge { get; }

		// Token: 0x17000796 RID: 1942
		// (get) Token: 0x06001E5A RID: 7770
		public abstract int MaxAge { get; }

		// Token: 0x06001E5B RID: 7771
		public abstract void GetAgeLimitForLocation(CharacterObject character, out int minimumAge, out int maximumAge, string additionalTags = "");
	}
}
