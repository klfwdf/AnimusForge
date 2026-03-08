using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests
{
	// Token: 0x02000023 RID: 35
	public class QuestMarkerVM : ViewModel
	{
		// Token: 0x1700006C RID: 108
		// (get) Token: 0x06000226 RID: 550 RVA: 0x00013072 File Offset: 0x00011272
		// (set) Token: 0x06000227 RID: 551 RVA: 0x0001307A File Offset: 0x0001127A
		public TextObject QuestTitle { get; private set; }

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x06000228 RID: 552 RVA: 0x00013083 File Offset: 0x00011283
		// (set) Token: 0x06000229 RID: 553 RVA: 0x0001308B File Offset: 0x0001128B
		public TextObject QuestHintText { get; private set; }

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x0600022A RID: 554 RVA: 0x00013094 File Offset: 0x00011294
		// (set) Token: 0x0600022B RID: 555 RVA: 0x0001309C File Offset: 0x0001129C
		public CampaignUIHelper.IssueQuestFlags IssueQuestFlag { get; private set; }

		// Token: 0x0600022C RID: 556 RVA: 0x000130A5 File Offset: 0x000112A5
		public QuestMarkerVM(CampaignUIHelper.IssueQuestFlags issueQuestFlag, TextObject questTitle = null, TextObject questHintText = null)
		{
			this.RefreshWith(issueQuestFlag, questTitle, questHintText);
		}

		// Token: 0x0600022D RID: 557 RVA: 0x000130B8 File Offset: 0x000112B8
		public void RefreshWith(CampaignUIHelper.IssueQuestFlags issueQuestFlag, TextObject questTitle = null, TextObject questHintText = null)
		{
			this.IssueQuestFlag = issueQuestFlag;
			this.QuestMarkerType = (int)issueQuestFlag;
			this.QuestTitle = questTitle ?? TextObject.GetEmpty();
			this.QuestHintText = questHintText;
			if (this.QuestHintText != null)
			{
				this.QuestHint = new HintViewModel(this.QuestHintText, null);
			}
			this.IsTrackMarker = issueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedIssue || issueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest;
			this.RefreshValues();
		}

		// Token: 0x0600022E RID: 558 RVA: 0x00013122 File Offset: 0x00011322
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (!TextObject.IsNullOrEmpty(this.QuestHintText))
			{
				this.QuestHint = new HintViewModel(this.QuestHintText, null);
				return;
			}
			this.QuestHint = new HintViewModel();
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x0600022F RID: 559 RVA: 0x00013155 File Offset: 0x00011355
		// (set) Token: 0x06000230 RID: 560 RVA: 0x0001315D File Offset: 0x0001135D
		[DataSourceProperty]
		public bool IsTrackMarker
		{
			get
			{
				return this._isTrackMarker;
			}
			set
			{
				if (value != this._isTrackMarker)
				{
					this._isTrackMarker = value;
					base.OnPropertyChangedWithValue(value, "IsTrackMarker");
				}
			}
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x06000231 RID: 561 RVA: 0x0001317B File Offset: 0x0001137B
		// (set) Token: 0x06000232 RID: 562 RVA: 0x00013183 File Offset: 0x00011383
		[DataSourceProperty]
		public int QuestMarkerType
		{
			get
			{
				return this._questMarkerType;
			}
			set
			{
				if (value != this._questMarkerType)
				{
					this._questMarkerType = value;
					base.OnPropertyChangedWithValue(value, "QuestMarkerType");
				}
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x06000233 RID: 563 RVA: 0x000131A1 File Offset: 0x000113A1
		// (set) Token: 0x06000234 RID: 564 RVA: 0x000131A9 File Offset: 0x000113A9
		[DataSourceProperty]
		public HintViewModel QuestHint
		{
			get
			{
				return this._questHint;
			}
			set
			{
				if (value != this._questHint)
				{
					this._questHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "QuestHint");
				}
			}
		}

		// Token: 0x040000FF RID: 255
		private bool _isTrackMarker;

		// Token: 0x04000100 RID: 256
		private int _questMarkerType;

		// Token: 0x04000101 RID: 257
		private HintViewModel _questHint;
	}
}
