using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker
{
	// Token: 0x02000048 RID: 72
	public class MapMarkerTrackerItemVM : MapTrackerItemVM<MapMarker>
	{
		// Token: 0x0600047D RID: 1149 RVA: 0x00011B7E File Offset: 0x0000FD7E
		public MapMarkerTrackerItemVM(MapMarker marker)
			: base(marker)
		{
		}

		// Token: 0x0600047E RID: 1150 RVA: 0x00011B87 File Offset: 0x0000FD87
		protected override void OnShowTooltip()
		{
			InformationManager.ShowTooltip(typeof(MapMarker), new object[] { base.TrackedObject, true, false });
		}

		// Token: 0x0600047F RID: 1151 RVA: 0x00011BB9 File Offset: 0x0000FDB9
		protected override bool IsVisibleOnMap()
		{
			return base.TrackedObject.IsVisibleOnMap;
		}

		// Token: 0x06000480 RID: 1152 RVA: 0x00011BC6 File Offset: 0x0000FDC6
		protected override bool GetCanToggleTrack()
		{
			return true;
		}

		// Token: 0x06000481 RID: 1153 RVA: 0x00011BC9 File Offset: 0x0000FDC9
		protected override string GetTrackerType()
		{
			return "Default";
		}

		// Token: 0x06000482 RID: 1154 RVA: 0x00011BD0 File Offset: 0x0000FDD0
		protected override CampaignUIHelper.IssueQuestFlags GetRelatedQuests()
		{
			CampaignUIHelper.IssueQuestFlags result = CampaignUIHelper.IssueQuestFlags.None;
			QuestBase questBase = Campaign.Current.QuestManager.Quests.FirstOrDefault((QuestBase q) => q.StringId == base.TrackedObject.QuestId);
			if (questBase != null)
			{
				result = (questBase.IsSpecialQuest ? CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest : CampaignUIHelper.IssueQuestFlags.ActiveIssue);
			}
			return result;
		}
	}
}
