using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

using static AnimusForge.SceneMovementController;
using static AnimusForge.ShoutBehavior;

namespace AnimusForge;

internal sealed partial class SceneConversationSessionRuntime
{
	internal async Task ProcessCapturedScenePlayerShoutAsync(string shoutText, string extraFact, int? forcedPrimaryAgentIndex,
		ScenePlayerShoutRequest request, Action<Action> runWithObservationScope, SceneGroupReceipt receipt = null)
	{
		await WaitForScenePostprocessGateAsync("before_player_shout_pipeline");
		Task groupTask = await _dispatcher.RunAsync("scene_player_input", "player", request.TargetingContext.PrimaryAgentIndex, () =>
		{
			if (!IsScenePlayerShoutRequestCurrent(request))
			{
				receipt?.Fail("scene.stale_context");
				return (Task)null;
			}
			Task startedGroup = null;
			Action process = () => startedGroup = ProcessCurrentScenePlayerShout(shoutText, extraFact, forcedPrimaryAgentIndex, request, receipt);
			if (runWithObservationScope == null)
			{
				process();
			}
			else
			{
				runWithObservationScope(process);
			}
			return startedGroup;
		}, (Task)null);
		await _dispatcher.RunAsync("scene_group_note", "player", request.TargetingContext.PrimaryAgentIndex, () => { _ports.NotePresentationRoundGroup(groupTask); return true; }, false);
		if (groupTask != null)
		{
			await groupTask.ConfigureAwait(false);
		}
		else receipt?.Fail("scene.group_not_started");
	}

	internal Task ProcessCurrentScenePlayerShout(string shoutText, string extraFact, int? forcedPrimaryAgentIndex,
		ScenePlayerShoutRequest request, SceneGroupReceipt receipt = null)
	{
        _ports.PreparePlayerRound(shoutText);
		ShoutTargetingContext targetingContext = request.TargetingContext;
		List<Agent> framedAgents = _ports.GetAgentsForShoutTargetingContext(targetingContext);
		if (framedAgents.Count == 0)
		{
			receipt?.Fail("scene.no_framed_target");
			InformationManager.DisplayMessage(new InformationMessage("你正在自言自语...", new Color(0.6f, 0.6f, 0.6f)));
			_ports.ResumeGame();
			return Task.CompletedTask;
		}
		Agent primaryTarget = null;
		if (forcedPrimaryAgentIndex.HasValue)
		{
			primaryTarget = framedAgents.FirstOrDefault((Agent a) => a != null && a.Index == forcedPrimaryAgentIndex.Value);
			if (primaryTarget == null)
			{
				receipt?.Fail("scene.primary_unavailable");
				InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 异色主对象已经离场，请重新框选。", new Color(1f, 0.5f, 0.3f)));
				_ports.ResumeGame();
				return Task.CompletedTask;
			}
		}
		if (primaryTarget == null)
		{
			receipt?.Fail("scene.primary_unavailable");
			primaryTarget = ResolvePrimaryAgentForShoutTargetingContext(targetingContext, framedAgents);
		}
		if (primaryTarget == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 没有找到异色主对象，请重新框选。", new Color(1f, 0.5f, 0.3f)));
			_ports.ResumeGame();
			return Task.CompletedTask;
		}
		int conversationEpoch = BeginNewPlayerDrivenSceneConversationEpoch();
		receipt?.Bind(conversationEpoch);
		bool audienceBuilt = TryBuildSceneShoutConversationScope(framedAgents, primaryTarget, conversationEpoch, out var conversationScope, out var audienceAgents, _ports.GetPresentationExcludedAgentIndices());
		if (audienceBuilt)
		{
			_ports.AbsorbPresentationAudience(audienceAgents);
		}
		if (!audienceBuilt)
		{
			receipt?.Fail("scene.audience_stale");
			InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 在场人物快照已失效，请重新框选。", new Color(1f, 0.5f, 0.3f)));
			_ports.ResumeGame();
			return Task.CompletedTask;
		}
		_ports.ActivateMultiSceneMovementSuppression(new int[1] { primaryTarget.Index });
		InformationManager.DisplayMessage(new InformationMessage((primaryTarget.Name?.ToString() ?? "异色主对象") + " 正在思考...", new Color(0.7f, 0.7f, 0.7f)));
		string sceneDesc = ShoutUtils.GetCurrentSceneDescription();
		List<NpcDataPacket> allNpcData = audienceAgents.Select((Agent a) => ShoutUtils.ExtractNpcData(a)).Where((NpcDataPacket d) => d != null).ToList();
		ApplySceneLocalDisambiguatedNames(allNpcData);
		NpcDataPacket primaryDataPacket = allNpcData.FirstOrDefault((NpcDataPacket d) => d.AgentIndex == primaryTarget.Index);
		if (primaryDataPacket == null)
		{
			receipt?.Fail("scene.primary_unavailable");
			_ports.RemoveSceneMovementSuppressionAgents(new int[1] { primaryTarget.Index });
			InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 无法读取异色主对象，请重新框选。", new Color(1f, 0.5f, 0.3f)));
			_ports.ResumeGame();
			return Task.CompletedTask;
		}
		List<NpcDataPacket> framedNpcData = allNpcData.Where((NpcDataPacket npc) => conversationScope.TryGetEntry(npc.AgentIndex, out var entry) && entry.IsFramed).ToList();
		string sceneTauntExtraFact = SceneTauntBehavior.BuildFrightenedCivilianShoutExtraFactExternal(primaryTarget);
		if (!string.IsNullOrWhiteSpace(sceneTauntExtraFact))
		{
			extraFact = string.IsNullOrWhiteSpace(extraFact) ? sceneTauntExtraFact : (extraFact + "\n" + sceneTauntExtraFact);
		}
		string combatShoutExtraFact = BuildCombatActiveShoutExtraFact(primaryTarget);
		if (!string.IsNullOrWhiteSpace(combatShoutExtraFact))
		{
			extraFact = string.IsNullOrWhiteSpace(extraFact) ? combatShoutExtraFact : (extraFact + "\n" + combatShoutExtraFact);
		}
		if (ProactiveNpcRequestBehavior.TryConsumePendingSceneOpeningForAgents(framedAgents, out var proactiveSceneExtraFact))
		{
			extraFact = string.IsNullOrWhiteSpace(extraFact) ? proactiveSceneExtraFact : (extraFact + "\n" + proactiveSceneExtraFact);
		}
		int personalizedPlayerCraftFactAgentIndex =
			ContainsPlayerCraftedAfefInspectionSuffix(extraFact)
				? primaryTarget.Index
				: -1;
		string sharedExtraFact = personalizedPlayerCraftFactAgentIndex >= 0
			? StripPlayerCraftedAfefInspectionSuffix(extraFact)
			: extraFact;
		List<Agent> directlyEngagedAgents = new List<Agent> { primaryTarget };
		_ports.ResetStaringForActiveInteraction(directlyEngagedAgents, primaryTarget);
		_ports.ExtendStaringHoldForPlayerDrivenSceneRound(1);
        _ports.ClearPendingHeroFacts();
		if (!string.IsNullOrWhiteSpace(sharedExtraFact))
		{
			_ports.RecordExtraFactToSceneHistory(sharedExtraFact, allNpcData);
			if (personalizedPlayerCraftFactAgentIndex >= 0)
			{
				_ports.PromotePersonalizedExtraFactInScenePrivateHistory(
					extraFact,
					personalizedPlayerCraftFactAgentIndex);
			}
			if (ShouldDeferExtraFactPersistenceUntilAfterSceneReply(sharedExtraFact))
			{
				_ports.SetPendingHeroHistoryExtraFactAfterSceneReply(
					extraFact,
					allNpcData,
					personalizedPlayerCraftFactAgentIndex);
			}
			else
			{
				try
				{
					bool factAccepted = _ports.PersistExtraFactToNamedHeroes(
						extraFact,
						allNpcData,
						personalizedPlayerCraftFactAgentIndex,
						requireMemoryReceipt: receipt != null);
					if (!factAccepted) receipt?.Fail("scene.fact_unconfirmed");
				}
				catch
				{
					receipt?.Fail("scene.fact_unconfirmed");
				}
			}
		}
		_ports.RecordPlayerSpeechToMessageFeed(shoutText);
        _ports.ShowPlayerSpeech(shoutText);
		List<NpcDataPacket> capturedNpcData = allNpcData;
		Dictionary<int, Agent> capturedAudienceAgentsByIndex = new Dictionary<int, Agent>(audienceAgents.Count);
		Dictionary<int, Hero> capturedResolvedHeroes = new Dictionary<int, Hero>();
		for (int i = 0; i < audienceAgents.Count; i++)
		{
			Agent audienceAgent = audienceAgents[i];
			if (audienceAgent != null && audienceAgent.Index >= 0)
			{
				capturedAudienceAgentsByIndex[audienceAgent.Index] = audienceAgent;
			}
			if (audienceAgent?.Character is CharacterObject audienceCharacter && audienceCharacter.HeroObject != null)
			{
				capturedResolvedHeroes[audienceAgent.Index] = audienceCharacter.HeroObject;
			}
		}
		if (!_ports.RecordPlayerMessage(shoutText, capturedNpcData, primaryDataPacket?.AgentIndex ?? (-1), primaryDataPacket?.Name ?? "", capturedAudienceAgentsByIndex, requireMemoryReceipt: receipt != null))
			receipt?.Fail("scene.history_unconfirmed");
		_ports.TrackPlayerInteraction(primaryDataPacket, 1, -1f, false, targetingContext);

		_ports.ResumeGame();

		var requestLifetime = _sceneRequestLifetime;
		async Task RunGroupAsync()
		{
			try
			{
				using IDisposable requestWorker = requestLifetime.Enter();
				using IDisposable cancellationScope = LlmNonStreamingTransport.PushOwnerCancellation(requestLifetime.Token);
				Dictionary<int, PrecomputedShoutRagContext> precomputedContexts = new Dictionary<int, PrecomputedShoutRagContext>();
				await HandleGroupResponse(shoutText, capturedNpcData, sceneDesc, primaryDataPacket, sharedExtraFact, precomputedContexts, capturedResolvedHeroes, conversationEpoch, conversationScope, framedNpcData, receipt);
			}
			catch (Exception ex)
			{
				receipt?.Fail("scene.group_exception");
				Logger.Log("ShoutBehavior", "[ERROR] ProcessShoutConfirmedInternal background failed: " + ex.Message);
				if (ex is PreprocessFormatException
					&& (receipt == null || IsModuleSceneGroupCurrent(receipt, receipt.RuntimeGeneration, receipt.SceneSessionId, receipt.ConversationEpoch)))
				{
					_ports.PostMainThread(delegate
					{
						try
						{
							if (receipt != null && !IsModuleSceneGroupCurrent(receipt, receipt.RuntimeGeneration, receipt.SceneSessionId, receipt.ConversationEpoch)) return;
							LlmRetryPrompt.ShowFailurePopup("AnimusForge 前处理失败", ex.Message);
						}
						catch
						{
						}
					});
				}
			}
		}
		return RunSceneGroupOnMainThreadAsync(RunGroupAsync);
	}


	internal ScenePlayerShoutRequest CaptureScenePlayerShoutRequest(IEnumerable<Agent> framedTargets, int primaryAgentIndex)
	{
		List<Agent> frozen = (framedTargets ?? Enumerable.Empty<Agent>()).Where(agent => agent != null).Distinct().ToList();
		ShoutTargetingContext targetingContext = new ShoutTargetingContext
		{
			PrimaryAgentIndex = primaryAgentIndex,
			CandidateAgentIndices = frozen.Select(agent => agent.Index).ToList(),
			PreviewCandidateAgents = frozen
		};
		return _scenePlayerShoutRequestOwner.Capture(
			this,
			Mission.Current,
			Agent.Main,
			SaveRuntimeGuard.CaptureGeneration(),
			_ports.SceneSessionId(),
			Volatile.Read(ref _sceneConversationEpoch),
			targetingContext);
	}

	internal bool IsScenePlayerShoutRequestCurrent(ScenePlayerShoutRequest request)
	{
		return _scenePlayerShoutRequestOwner.IsCurrent(
			request,
			this,
			Mission.Current,
			Agent.Main,
			request != null && SaveRuntimeGuard.IsCurrentGeneration(request.RuntimeGeneration),
			_ports.SceneSessionId(),
			Volatile.Read(ref _sceneConversationEpoch));
	}

}
