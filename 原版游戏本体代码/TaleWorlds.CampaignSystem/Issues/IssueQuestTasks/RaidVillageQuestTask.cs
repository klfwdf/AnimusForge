using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Issues.IssueQuestTasks
{
	// Token: 0x02000381 RID: 897
	public class RaidVillageQuestTask : QuestTaskBase
	{
		// Token: 0x0600340A RID: 13322 RVA: 0x000D59E2 File Offset: 0x000D3BE2
		public RaidVillageQuestTask(Village village, Action onSucceededAction, Action onFailedAction, Action onCanceledAction, DialogFlow dialogFlow = null)
			: base(dialogFlow, onSucceededAction, onFailedAction, onCanceledAction)
		{
			this._targetVillage = village;
		}

		// Token: 0x0600340B RID: 13323 RVA: 0x000D59F7 File Offset: 0x000D3BF7
		public void OnVillageLooted(Village village)
		{
			if (this._targetVillage == village)
			{
				base.Finish((this._targetVillage.Owner.MapEvent.AttackerSide.LeaderParty == MobileParty.MainParty.Party) ? QuestTaskBase.FinishStates.Success : QuestTaskBase.FinishStates.Fail);
			}
		}

		// Token: 0x0600340C RID: 13324 RVA: 0x000D5A32 File Offset: 0x000D3C32
		public void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
		{
			if (!FactionManager.IsAtWarAgainstFaction(newKingdom, this._targetVillage.Settlement.MapFaction))
			{
				base.Finish(QuestTaskBase.FinishStates.Cancel);
			}
		}

		// Token: 0x0600340D RID: 13325 RVA: 0x000D5A53 File Offset: 0x000D3C53
		public override void SetReferences()
		{
			CampaignEvents.VillageLooted.AddNonSerializedListener(this, new Action<Village>(this.OnVillageLooted));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
		}

		// Token: 0x04000ED9 RID: 3801
		[SaveableField(50)]
		private readonly Village _targetVillage;
	}
}
