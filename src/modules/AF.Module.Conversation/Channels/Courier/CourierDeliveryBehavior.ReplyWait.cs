using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
	private void ShowCourierReplyWaitPopupAndPause(CourierSession session, Hero recipient)
	{
		if (session == null || session.ReplyGenerated)
		{
			return;
		}
		BeginCourierReplyWaitPause(session, recipient);
		if (session.ReplyWaitPopupShown)
		{
			return;
		}
		session.ReplyWaitPopupShown = true;
		try
		{
			if (IsInboundToPlayer(session))
			{
				string senderName = recipient?.Name?.ToString() ?? session.SenderName ?? "NPC";
				InformationManager.ShowInquiry(new InquiryData("等待信使来信生成", "信使已经抵达你的队伍，正在等待" + senderName + "写完信件正文。\n\n游戏时间已暂停，信件生成完成后会自动送达。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true, prioritize: true);
			}
			else
			{
				string name = recipient?.Name?.ToString() ?? session.RecipientName ?? "NPC";
				InformationManager.ShowInquiry(new InquiryData("等待信使回信生成", "信使已经抵达 " + name + " 的位置，正在等待对方读信并写下回信。\n\n游戏时间已暂停，回信生成完成后会自动继续并执行后处理标签。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true, prioritize: true);
			}
		}
		catch (Exception ex)
		{
			Log("show reply wait inquiry failed session=" + session.Id + " error=" + ex.Message);
			InformationManager.DisplayMessage(new InformationMessage(IsInboundToPlayer(session) ? "信使已抵达，正在等待来信生成。游戏时间已暂停。" : "信使已抵达，正在等待回信生成。游戏时间已暂停。", Colors.Yellow));
		}
	}

	private void BeginCourierReplyWaitPause(CourierSession session, Hero recipient)
	{
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign == null)
			{
				return;
			}
			if (!_courierReplyWaitTimeLocked)
			{
				_courierReplyWaitPreviousMode = campaign.TimeControlMode;
				_courierReplyWaitPreviousLock = campaign.TimeControlModeLock;
				campaign.TimeControlMode = CampaignTimeControlMode.Stop;
				campaign.SetTimeControlModeLock(true);
				_courierReplyWaitTimeLocked = true;
				Log("reply wait time locked session=" + (session?.Id ?? "") + " recipient=" + SafeHeroId(recipient));
			}
			else
			{
				campaign.SetTimeSpeed(0);
			}
		}
		catch (Exception ex)
		{
			Log("reply wait pause failed session=" + (session?.Id ?? "") + " error=" + ex.Message);
		}
	}

	private void EndCourierReplyWaitPause(CourierSession completedSession, string reason)
	{
		if (completedSession != null)
		{
			completedSession.ReplyWaitPopupShown = false;
		}
		if (HasActiveCourierReplyWait())
		{
			return;
		}
		try
		{
			InformationManager.HideInquiry();
		}
		catch
		{
		}
		if (!_courierReplyWaitTimeLocked)
		{
			return;
		}
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign != null)
			{
				campaign.SetTimeControlModeLock(_courierReplyWaitPreviousLock);
				if (!_courierReplyWaitPreviousLock)
				{
					campaign.TimeControlMode = _courierReplyWaitPreviousMode;
				}
			}
			Log("reply wait time released reason=" + (reason ?? ""));
		}
		catch (Exception ex)
		{
			Log("reply wait release failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
		_courierReplyWaitTimeLocked = false;
	}

	private bool HasActiveCourierReplyWait()
	{
		try
		{
			lock (_sessionLock)
			{
				return _sessions.Values.Any(x => x != null && !IsTerminalStage(x) && !x.ReplyGenerated && x.ReplyWaitPopupShown);
			}
		}
		catch
		{
			return false;
		}
	}
}
