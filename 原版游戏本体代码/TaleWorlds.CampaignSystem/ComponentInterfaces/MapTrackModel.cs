using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B7 RID: 439
	public abstract class MapTrackModel : MBGameModel<MapTrackModel>
	{
		// Token: 0x1700074C RID: 1868
		// (get) Token: 0x06001D5E RID: 7518
		public abstract float MaxTrackLife { get; }

		// Token: 0x06001D5F RID: 7519
		public abstract float GetSkipTrackChance(MobileParty mobileParty);

		// Token: 0x06001D60 RID: 7520
		public abstract float GetMaxTrackSpottingDistanceForMainParty();

		// Token: 0x06001D61 RID: 7521
		public abstract bool CanPartyLeaveTrack(MobileParty mobileParty);

		// Token: 0x06001D62 RID: 7522
		public abstract float GetTrackDetectionDifficultyForMainParty(Track track, float trackSpottingDistance);

		// Token: 0x06001D63 RID: 7523
		public abstract float GetSkillFromTrackDetected(Track track);

		// Token: 0x06001D64 RID: 7524
		public abstract int GetTrackLife(MobileParty mobileParty);

		// Token: 0x06001D65 RID: 7525
		public abstract TextObject TrackTitle(Track track);

		// Token: 0x06001D66 RID: 7526
		public abstract IEnumerable<ValueTuple<TextObject, string>> GetTrackDescription(Track track);

		// Token: 0x06001D67 RID: 7527
		public abstract uint GetTrackColor(Track track);

		// Token: 0x06001D68 RID: 7528
		public abstract float GetTrackScale(Track track);
	}
}
