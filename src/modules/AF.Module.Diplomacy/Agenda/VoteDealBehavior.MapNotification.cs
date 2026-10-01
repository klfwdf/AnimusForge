using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

internal sealed class AnimusForgeAgendaMapNotification : InformationData
{
	private readonly TextObject _titleText;

	public KingdomDecision Decision { get; }

	public override TextObject TitleText => _titleText;

	public override string SoundEventPath => "event:/ui/notification/kingdom_decision";

	public AnimusForgeAgendaMapNotification(KingdomDecision decision, string title, string description)
		: base(new TextObject(string.IsNullOrWhiteSpace(description) ? "点击查看这项王国议程。" : description))
	{
		Decision = decision;
		_titleText = new TextObject(string.IsNullOrWhiteSpace(title) ? "新的王国议程" : title);
	}

	public override bool IsValid()
	{
		return VoteDealBehavior.IsAgendaMapNoticeDecisionValidForExternal(Decision);
	}
}

internal sealed class AnimusForgeAgendaMapNotificationItemVM : MapNotificationItemBaseVM
{
	private readonly KingdomDecision _decision;
	private readonly Action _openKingdom;

	public AnimusForgeAgendaMapNotificationItemVM(AnimusForgeAgendaMapNotification data)
		: base(data)
	{
		_decision = data.Decision;
		NotificationIdentifier = "vote";
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.KingdomDecisionCancelled.AddNonSerializedListener(this, OnDecisionCancelled);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnDecisionConcluded);
		_onInspect = OnInspect;
		_openKingdom = delegate
		{
			NavigationHandler.OpenKingdom(_decision);
		};
	}

	private void OnInspect()
	{
		if (!VoteDealBehavior.IsAgendaMapNoticeDecisionValidForExternal(_decision))
		{
			InformationManager.DisplayMessage(new InformationMessage("这项王国议程已经失效。"));
			ExecuteRemove();
			return;
		}

		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out TextObject disabledReason))
		{
			InformationManager.DisplayMessage(new InformationMessage(
				disabledReason?.ToString() ?? "当前无法查看王国议程。"));
			return;
		}

		if (!VoteDealBehavior.PrepareAgendaMapNoticeOpenForExternal(_decision))
		{
			InformationManager.DisplayMessage(new InformationMessage("当前无法打开这项王国议程。"));
			return;
		}

		try
		{
			_openKingdom();
			ExecuteRemove();
		}
		catch (Exception ex)
		{
			VoteDealBehavior.CancelPreparedAgendaMapNoticeOpenForExternal(_decision);
			Logger.Log("VoteDeal", "[AgendaNotice] Navigation failed: " + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage("打开王国议程失败，请稍后重试。"));
		}
	}

	private void OnDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool isPlayerInvolved)
	{
		if (decision == _decision) ExecuteRemove();
	}

	private void OnDecisionCancelled(KingdomDecision decision, bool isPlayerInvolved)
	{
		if (decision == _decision) ExecuteRemove();
	}

	private void OnClanChangedKingdom(
		Clan clan,
		Kingdom oldKingdom,
		Kingdom newKingdom,
		ChangeKingdomAction.ChangeKingdomActionDetail detail,
		bool showNotification)
	{
		if (clan == Clan.PlayerClan) ExecuteRemove();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
		CampaignEvents.KingdomDecisionCancelled.ClearListeners(this);
		CampaignEvents.KingdomDecisionConcluded.ClearListeners(this);
	}
}

public partial class VoteDealBehavior
{
	// Experimental and intentionally isolated: setting this to false restores the
	// previous AnimusForge behavior without removing any agenda or vote logic.
	private static readonly bool AgendaMapNotificationsEnabled = true;

	private readonly HashSet<KingdomDecision> _pendingAgendaMapNotices = new HashSet<KingdomDecision>();
	private readonly HashSet<KingdomDecision> _publishedAgendaMapNotices = new HashSet<KingdomDecision>();
	private MapNotificationView _agendaRegisteredMapNotificationView;
	private bool _agendaMapNoticeReconcilePending = true;
	private int _hasPendingAgendaMapNoticeWork = 1;
	private long _nextAgendaMapNoticeRetryUtcTicks;
	internal static KingdomDecision s_pendingAgendaMapNoticeDecision;

	private void OnAgendaKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
	{
		if (!AgendaMapNotificationsEnabled || decision == null || decision.IsEnforced) return;
		if (decision.Kingdom == null || decision.Kingdom != Clan.PlayerClan?.Kingdom) return;
		if (!isPlayerInvolved && decision.Kingdom.RulingClan != Clan.PlayerClan) return;
		_pendingAgendaMapNotices.Add(decision);
		_hasPendingAgendaMapNoticeWork = 1;
		_nextAgendaMapNoticeRetryUtcTicks = 0L;
	}

	private void OnAgendaMapNotificationTick(float dt)
	{
		if (!AgendaMapNotificationsEnabled || _hasPendingAgendaMapNoticeWork == 0) return;
		long nowTicks = DateTime.UtcNow.Ticks;
		if (nowTicks < _nextAgendaMapNoticeRetryUtcTicks) return;
		_nextAgendaMapNoticeRetryUtcTicks = nowTicks + TimeSpan.TicksPerSecond;
		try
		{
			TryPublishPendingAgendaMapNotifications();
		}
		catch (Exception ex)
		{
			Logger.Log("VoteDeal", "[AgendaNotice] Deferred publish failed: " + ex.Message);
		}
	}

	private void ResetAgendaMapNotificationRuntime()
	{
		_pendingAgendaMapNotices.Clear();
		_publishedAgendaMapNotices.Clear();
		_agendaRegisteredMapNotificationView = null;
		_agendaMapNoticeReconcilePending = true;
		_hasPendingAgendaMapNoticeWork = AgendaMapNotificationsEnabled ? 1 : 0;
		_nextAgendaMapNoticeRetryUtcTicks = 0L;
		s_pendingAgendaMapNoticeDecision = null;
	}

	private void ForgetAgendaMapNotification(KingdomDecision decision)
	{
		if (decision == null) return;
		_pendingAgendaMapNotices.Remove(decision);
		_publishedAgendaMapNotices.Remove(decision);
		ForgetRequiredAgendaVotePrompt(decision);
		if (s_pendingAgendaMapNoticeDecision == decision) s_pendingAgendaMapNoticeDecision = null;
	}

	private void ReconcileAndPublishAgendaMapNotifications()
	{
		if (!AgendaMapNotificationsEnabled) return;
		_agendaMapNoticeReconcilePending = true;
		_hasPendingAgendaMapNoticeWork = 1;
		TryPublishPendingAgendaMapNotifications();
	}

	private void TryPublishPendingAgendaMapNotifications()
	{
		if (!AgendaMapNotificationsEnabled || !CanPublishAgendaMapNotification()) return;
		if (!TryEnsureAgendaMapNotificationRegistered()) return;

		if (_agendaMapNoticeReconcilePending)
		{
			_agendaMapNoticeReconcilePending = false;
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			if (playerKingdom?.UnresolvedDecisions != null)
			{
				foreach (KingdomDecision decision in playerKingdom.UnresolvedDecisions)
				{
					if (IsAgendaMapNoticeDecisionValidForExternal(decision) && !_publishedAgendaMapNotices.Contains(decision))
					{
						_pendingAgendaMapNotices.Add(decision);
					}
				}
			}
		}

		foreach (KingdomDecision decision in _pendingAgendaMapNotices.ToList())
		{
			if (!IsAgendaMapNoticeDecisionValidForExternal(decision))
			{
				_pendingAgendaMapNotices.Remove(decision);
				_publishedAgendaMapNotices.Remove(decision);
				continue;
			}
			if (_publishedAgendaMapNotices.Contains(decision))
			{
				_pendingAgendaMapNotices.Remove(decision);
				continue;
			}

			MBInformationManager.AddNotice(new AnimusForgeAgendaMapNotification(
				decision,
				"王国议程：" + GetSafeDecisionTitle(decision),
				BuildAgendaMapNoticeDescription(decision)));
			_publishedAgendaMapNotices.Add(decision);
			_pendingAgendaMapNotices.Remove(decision);
			Logger.Log("VoteDeal", "[AgendaNotice] Published: " + GetSafeDecisionTitle(decision));
		}

		_hasPendingAgendaMapNoticeWork = _pendingAgendaMapNotices.Count > 0 || _agendaMapNoticeReconcilePending ? 1 : 0;
	}

	private bool TryEnsureAgendaMapNotificationRegistered()
	{
		try
		{
			MapNotificationView view = MapScreen.Instance?.MapNotificationView;
			if (view == null) return false;
			if (!ReferenceEquals(_agendaRegisteredMapNotificationView, view))
			{
				view.RegisterMapNotificationType(typeof(AnimusForgeAgendaMapNotification), typeof(AnimusForgeAgendaMapNotificationItemVM));
				_agendaRegisteredMapNotificationView = view;
				// MapNotificationView is recreated when the player returns to the campaign map.
				// Published decisions belong to this campaign runtime, not to one view instance;
				// clearing them here republishes every still-pending agenda (especially allied
				// calls to war) each time the map UI is rebuilt.
				_agendaMapNoticeReconcilePending = true;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("VoteDeal", "[AgendaNotice] Registration failed: " + ex.Message);
			return false;
		}
	}

	private static bool CanPublishAgendaMapNotification()
	{
		try
		{
			return Game.Current?.GameStateManager?.ActiveState is MapState
				&& MapScreen.Instance?.MapNotificationView != null;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsAgendaMapNoticeDecisionValidForExternal(KingdomDecision decision)
	{
		if (!AgendaMapNotificationsEnabled || decision == null || decision.IsEnforced) return false;
		try
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			if (playerKingdom == null || decision.Kingdom != playerKingdom) return false;
			if (playerKingdom.UnresolvedDecisions == null || !playerKingdom.UnresolvedDecisions.Contains(decision)) return false;
			if (!decision.IsPlayerParticipant && playerKingdom.RulingClan != Clan.PlayerClan) return false;
			if (TryGetRedundantSingleClanFiefAgenda(playerKingdom, decision, out _, out _)) return false;
			return !decision.ShouldBeCancelled();
		}
		catch
		{
			return false;
		}
	}

	internal static bool PrepareAgendaMapNoticeOpenForExternal(KingdomDecision decision)
	{
		if (!IsAgendaMapNoticeDecisionValidForExternal(decision)) return false;
		s_pendingAgendaMapNoticeDecision = decision;
		return true;
	}

	internal static void CancelPreparedAgendaMapNoticeOpenForExternal(KingdomDecision decision)
	{
		if (s_pendingAgendaMapNoticeDecision == decision) s_pendingAgendaMapNoticeDecision = null;
	}

	private static string BuildAgendaMapNoticeDescription(KingdomDecision decision)
	{
		try
		{
			if (decision.TriggerTime.IsFuture)
			{
				float days = Math.Max(0f, decision.TriggerTime.RemainingDaysFromNow);
				return $"你可以参与这项议程，约 {days:F1} 天后进入投票。点击查看议程详情。";
			}
			return "这项议程已经进入投票阶段。点击前往投票界面。";
		}
		catch
		{
			return "你可以参与这项王国议程。点击查看详情。";
		}
	}
}
