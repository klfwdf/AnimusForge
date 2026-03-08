using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000400 RID: 1024
	public interface IMapTracksCampaignBehavior : ICampaignBehavior
	{
		// Token: 0x17000E19 RID: 3609
		// (get) Token: 0x06003FB3 RID: 16307
		MBReadOnlyList<Track> DetectedTracks { get; }

		// Token: 0x06003FB4 RID: 16308
		void AddTrack(MobileParty target, CampaignVec2 trackPosition, Vec2 trackDirection);

		// Token: 0x06003FB5 RID: 16309
		void AddMapArrow(TextObject pointerName, CampaignVec2 trackPosition, Vec2 trackDirection, float life);
	}
}
