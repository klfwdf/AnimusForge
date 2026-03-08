using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker
{
	// Token: 0x02000047 RID: 71
	public class MapArmyTrackItemVM : MapTrackerItemVM<Army>
	{
		// Token: 0x06000477 RID: 1143 RVA: 0x00011B1B File Offset: 0x0000FD1B
		public MapArmyTrackItemVM(Army trackableObject)
			: base(trackableObject)
		{
		}

		// Token: 0x06000478 RID: 1144 RVA: 0x00011B24 File Offset: 0x0000FD24
		protected override void OnShowTooltip()
		{
			InformationManager.ShowTooltip(typeof(Army), new object[] { base.TrackedObject, true, false });
		}

		// Token: 0x06000479 RID: 1145 RVA: 0x00011B56 File Offset: 0x0000FD56
		protected override bool IsVisibleOnMap()
		{
			MobileParty leaderParty = base.TrackedObject.LeaderParty;
			return leaderParty != null && !leaderParty.IsVisible;
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x00011B71 File Offset: 0x0000FD71
		protected override bool GetCanToggleTrack()
		{
			return true;
		}

		// Token: 0x0600047B RID: 1147 RVA: 0x00011B74 File Offset: 0x0000FD74
		protected override string GetTrackerType()
		{
			return "Army";
		}

		// Token: 0x0600047C RID: 1148 RVA: 0x00011B7B File Offset: 0x0000FD7B
		protected override CampaignUIHelper.IssueQuestFlags GetRelatedQuests()
		{
			return CampaignUIHelper.IssueQuestFlags.None;
		}
	}
}
