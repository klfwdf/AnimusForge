using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker
{
	// Token: 0x02000049 RID: 73
	public class MapMobilePartyTrackItemVM : MapTrackerItemVM<MobileParty>
	{
		// Token: 0x06000484 RID: 1156 RVA: 0x00011C29 File Offset: 0x0000FE29
		public MapMobilePartyTrackItemVM(MobileParty party)
			: base(party)
		{
		}

		// Token: 0x06000485 RID: 1157 RVA: 0x00011C32 File Offset: 0x0000FE32
		protected override void OnShowTooltip()
		{
			InformationManager.ShowTooltip(typeof(MobileParty), new object[] { base.TrackedObject, true, false });
		}

		// Token: 0x06000486 RID: 1158 RVA: 0x00011C64 File Offset: 0x0000FE64
		protected override bool IsVisibleOnMap()
		{
			return base.TrackedObject.AttachedTo == null && !base.TrackedObject.IsVisible;
		}

		// Token: 0x06000487 RID: 1159 RVA: 0x00011C83 File Offset: 0x0000FE83
		protected override bool GetCanToggleTrack()
		{
			return true;
		}

		// Token: 0x06000488 RID: 1160 RVA: 0x00011C86 File Offset: 0x0000FE86
		protected override string GetTrackerType()
		{
			return "MobileParty";
		}

		// Token: 0x06000489 RID: 1161 RVA: 0x00011C8D File Offset: 0x0000FE8D
		protected override CampaignUIHelper.IssueQuestFlags GetRelatedQuests()
		{
			return CampaignUIHelper.IssueQuestFlags.None;
		}
	}
}
