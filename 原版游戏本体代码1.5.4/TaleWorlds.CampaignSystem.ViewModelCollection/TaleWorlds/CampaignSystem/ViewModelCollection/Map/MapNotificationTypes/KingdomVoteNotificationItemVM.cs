using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class KingdomVoteNotificationItemVM : MapNotificationItemBaseVM
{
	private KingdomDecision _decision;

	private Kingdom _kingdomOfDecision;

	private Action _onInspectOpenKingdom;

	public KingdomVoteNotificationItemVM(KingdomDecisionMapNotification data)
		: base(data)
	{
		KingdomVoteNotificationItemVM kingdomVoteNotificationItemVM = this;
		_decision = data.Decision;
		_kingdomOfDecision = data.KingdomOfDecision;
		base.NotificationIdentifier = "vote";
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.KingdomDecisionCancelled.AddNonSerializedListener(this, OnDecisionCancelled);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnDecisionConcluded);
		_onInspect = OnInspect;
		_onInspectOpenKingdom = delegate
		{
			kingdomVoteNotificationItemVM.NavigationHandler.OpenKingdom(data.Decision);
		};
	}

	private void OnInspect()
	{
		if (!_decision.ShouldBeCancelled())
		{
			Kingdom kingdom = Clan.PlayerClan.Kingdom;
			if (kingdom != null && kingdom.UnresolvedDecisions.Any((KingdomDecision d) => d == _decision))
			{
				if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var _))
				{
					InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=lSnejlxB}You cannot participate in kingdom decisions right now.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
				}
				else
				{
					_onInspectOpenKingdom();
				}
				return;
			}
		}
		InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=i9OsCshW}This kingdom decision is not relevant anymore.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
		ExecuteRemove();
	}

	private void OnDecisionConcluded(KingdomDecision decision, DecisionOutcome arg2, bool arg3)
	{
		if (decision == _decision)
		{
			ExecuteRemove();
		}
	}

	private void OnDecisionCancelled(KingdomDecision decision, bool arg2)
	{
		if (decision == _decision)
		{
			ExecuteRemove();
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (clan == Clan.PlayerClan)
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
	}
}
