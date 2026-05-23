using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000045 RID: 69
	public class KingdomVoteNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x060005EE RID: 1518 RVA: 0x0001F1E4 File Offset: 0x0001D3E4
		public KingdomVoteNotificationItemVM(KingdomDecisionMapNotification data)
			: base(data)
		{
			KingdomVoteNotificationItemVM <>4__this = this;
			this._decision = data.Decision;
			this._kingdomOfDecision = data.KingdomOfDecision;
			base.NotificationIdentifier = "vote";
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.KingdomDecisionCancelled.AddNonSerializedListener(this, new Action<KingdomDecision, bool>(this.OnDecisionCancelled));
			CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, new Action<KingdomDecision, DecisionOutcome, bool>(this.OnDecisionConcluded));
			this._onInspect = new Action(this.OnInspect);
			this._onInspectOpenKingdom = delegate()
			{
				<>4__this.NavigationHandler.OpenKingdom(data.Decision);
			};
		}

		// Token: 0x060005EF RID: 1519 RVA: 0x0001F2A8 File Offset: 0x0001D4A8
		private void OnInspect()
		{
			if (!this._decision.ShouldBeCancelled())
			{
				Kingdom kingdom = Clan.PlayerClan.Kingdom;
				if (kingdom != null && kingdom.UnresolvedDecisions.Any((KingdomDecision d) => d == this._decision))
				{
					TextObject textObject;
					if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
					{
						InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=lSnejlxB}You cannot participate in kingdom decisions right now.", null).ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), "", null, null, "", 0f, null, null, null), false, false);
						return;
					}
					this._onInspectOpenKingdom();
					return;
				}
			}
			InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=i9OsCshW}This kingdom decision is not relevant anymore.", null).ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), "", null, null, "", 0f, null, null, null), false, false);
			base.ExecuteRemove();
		}

		// Token: 0x060005F0 RID: 1520 RVA: 0x0001F398 File Offset: 0x0001D598
		private void OnDecisionConcluded(KingdomDecision decision, DecisionOutcome arg2, bool arg3)
		{
			if (decision == this._decision)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x060005F1 RID: 1521 RVA: 0x0001F3A9 File Offset: 0x0001D5A9
		private void OnDecisionCancelled(KingdomDecision decision, bool arg2)
		{
			if (decision == this._decision)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x0001F3BA File Offset: 0x0001D5BA
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
		{
			if (clan == Clan.PlayerClan)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x060005F3 RID: 1523 RVA: 0x0001F3CA File Offset: 0x0001D5CA
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
		}

		// Token: 0x04000280 RID: 640
		private KingdomDecision _decision;

		// Token: 0x04000281 RID: 641
		private Kingdom _kingdomOfDecision;

		// Token: 0x04000282 RID: 642
		private Action _onInspectOpenKingdom;
	}
}
