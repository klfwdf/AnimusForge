using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimusForge.Refactor.Modules;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using static AnimusForge.ShoutBehavior;
namespace AnimusForge;
// System speech preserves its separate immediate queue protocol, but shares the
// existing effects/history/audio ports and the one mission operation registry.
internal sealed class SceneSystemNpcShoutRuntime
{
    private readonly SceneSpeechEffectPorts _ports;
    private readonly ConversationGameThreadDispatcher _dispatcher;
    private readonly Func<int> _conversationEpoch;
    private readonly Func<int> _sceneSessionId;
    private readonly Func<bool> _isOwnerCurrent;
    internal SceneSystemNpcShoutRuntime(SceneSpeechEffectPorts ports, ConversationGameThreadDispatcher dispatcher,
        Func<int> conversationEpoch, Func<int> sceneSessionId, Func<bool> isOwnerCurrent)
    { _ports=ports??throw new ArgumentNullException(nameof(ports)); _dispatcher=dispatcher??throw new ArgumentNullException(nameof(dispatcher));
      _conversationEpoch=conversationEpoch??throw new ArgumentNullException(nameof(conversationEpoch));
      _sceneSessionId=sceneSessionId??throw new ArgumentNullException(nameof(sceneSessionId));
      _isOwnerCurrent=isOwnerCurrent??throw new ArgumentNullException(nameof(isOwnerCurrent)); }
	internal async Task<bool> Enqueue(Agent speakerAgent, string content)
	{
        Task<bool> queued = await _dispatcher.RunAsync("scene_system_capture", "scene", -1,
            () => CaptureAndQueue(speakerAgent, content), (Task<bool>)null).ConfigureAwait(false);
        return queued != null && await queued.ConfigureAwait(false);
    }

    private Task<bool> CaptureAndQueue(Agent speakerAgent, string content)
    {
		try
		{
			if (speakerAgent == null || !speakerAgent.IsActive() || string.IsNullOrWhiteSpace(content) || Mission.Current == null)
			{
				return Task.FromResult(false);
			}
			List<Agent> list = ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>();
			if (!list.Any((Agent a) => a != null && a.Index == speakerAgent.Index))
			{
				list.Add(speakerAgent);
			}
			List<NpcDataPacket> allNpcData = (from a in list
				where a != null
				select ShoutUtils.ExtractNpcData(a) into d
				where d != null
				select d).ToList();
			ApplySceneLocalDisambiguatedNames(allNpcData);
			NpcDataPacket speakerData = ShoutUtils.ExtractNpcData(speakerAgent);
			if (speakerData == null)
			{
				return Task.FromResult(false);
			}
			NpcDataPacket npcDataPacket = allNpcData.FirstOrDefault((NpcDataPacket npc) => npc != null && npc.AgentIndex == speakerData.AgentIndex);
			if (npcDataPacket != null)
			{
				speakerData.PromptGivenName = npcDataPacket.PromptGivenName;
				speakerData.PromptDisplayName = npcDataPacket.PromptDisplayName;
			}
			string safeContent = content.Trim();
            Mission sourceMission = Mission.Current;
            long generation = SaveRuntimeGuard.CaptureGeneration();
            int epoch = _conversationEpoch();
            int sessionId = _sceneSessionId();
            return _dispatcher.RunAsync("scene_system_effects", "scene", speakerData.AgentIndex, () =>
            {
                if (!_isOwnerCurrent() || !SaveRuntimeGuard.IsCurrentGeneration(generation)
                    || !ReferenceEquals(Mission.Current, sourceMission)
                    || epoch != _conversationEpoch() || sessionId != _sceneSessionId())
                    return false;
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == speakerData.AgentIndex);
                if (!speakerAgent.IsActive() || !ReferenceEquals(agent, speakerAgent)) return false;
				try
				{
					string aiResponse = safeContent;
					bool meetingTauntEscalated = false;
					bool meetingReleaseTriggered = false;
					bool sceneTauntActionHandled = false;
					bool sceneTauntEscalated = false;
					bool npcSurrenderTriggered = false;
					WorldMapPartyCommandBehavior.WorldMapOrderApplyResult worldMapResult = new WorldMapPartyCommandBehavior.WorldMapOrderApplyResult();
					try
					{
							if (agent != null && agent.Character is CharacterObject { HeroObject: not null } characterObject)
							{
								MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(characterObject.HeroObject, ref aiResponse);
								VoteDealBehavior.ProcessAgendaTagsDispatch(characterObject.HeroObject, ref aiResponse);
									DiplomacyConversationBridge.ProcessDiplomacyTagsDispatch(characterObject.HeroObject, ref aiResponse);
								worldMapResult = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(characterObject.HeroObject, ref aiResponse);
								DuelBehavior.TryCacheDuelAfterLinesFromText(characterObject.HeroObject, ref aiResponse);
								DuelBehavior.TryCacheDuelStakeFromText(characterObject.HeroObject, ref aiResponse);
								VanillaIssueOfferBridge.ApplyIssueOfferTags(characterObject.HeroObject, ref aiResponse);
								if (TeamModuleServices.Gathering.TryApplyNobleGatheringTagsForExternal(characterObject.HeroObject, ref aiResponse, out var nobleFacts, out var nobleNotifications))
								{
									foreach (string generatedFact in nobleFacts ?? new List<string>())
									{
										_ports.RecordSystemFactForNearbySafe(allNpcData, generatedFact);
										MyBehavior.AppendExternalDialogueHistory(characterObject.HeroObject, null, null, generatedFact);
									}
									foreach (string notification in nobleNotifications ?? new List<string>())
									{
										if (!string.IsNullOrWhiteSpace(notification))
										{
											InformationManager.DisplayMessage(new InformationMessage(notification, new Color(0.4f, 1f, 0.4f)));
										}
									}
								}
								if (MyBehavior.TryApplyPartyTransferTagsForExternal(characterObject.HeroObject, characterObject, speakerData.AgentIndex, ref aiResponse, out var generatedFacts, out var notifications))
								{
									if (generatedFacts != null)
									{
										foreach (string generatedFact in generatedFacts)
										{
											_ports.RecordSystemFactForNearbySafe(allNpcData, generatedFact);
											MyBehavior.AppendExternalDialogueHistory(characterObject.HeroObject, null, null, generatedFact);
										}
									}
									if (notifications != null)
									{
										foreach (string notification in notifications)
										{
											if (!string.IsNullOrWhiteSpace(notification))
											{
												InformationManager.DisplayMessage(new InformationMessage(notification, new Color(0.4f, 1f, 0.4f)));
											}
										}
									}
								}
								if (RewardSystemBehavior.Instance != null)
								{
									bool sceneRewardBeforeHasVassalage = ContainsVassalageActionTagForLog(aiResponse);
									bool sceneRewardBeforeHasKingdomAnnex = ContainsKingdomAnnexActionTagForLog(aiResponse);
									string sceneRewardChainName = ResolveScenePostprocessChainName();
									Logger.Log("ShoutBehavior", "[SceneSystemShout] ApplyRewardTags start chain=" + sceneRewardChainName + " target=" + (characterObject.HeroObject?.StringId ?? characterObject.StringId ?? speakerData?.Name ?? "unknown") + " containsVASSALAGE=" + sceneRewardBeforeHasVassalage + " containsKINGDOM_ANNEX=" + sceneRewardBeforeHasKingdomAnnex);
									RewardSystemBehavior.RpItemIntroductionContext rpItemIntroductionContext = MayContainGeneratedRpItemReward(aiResponse)
										? CreateRpItemIntroductionContextForReward(
											characterObject.HeroObject,
											speakerData,
											characterObject,
											speakerData.AgentIndex,
											characterObject.Name?.ToString() ?? speakerData?.Name,
											null,
											aiResponse)
										: null;
									RewardSystemBehavior.Instance.ApplyRewardTags(characterObject.HeroObject, Hero.MainHero, ref aiResponse, rpItemIntroductionContext);
									Logger.Log("ShoutBehavior", "[SceneSystemShout] ApplyRewardTags done chain=" + sceneRewardChainName + " target=" + (characterObject.HeroObject?.StringId ?? characterObject.StringId ?? speakerData?.Name ?? "unknown") + " beforeVASSALAGE=" + sceneRewardBeforeHasVassalage + " afterVASSALAGE=" + ContainsVassalageActionTagForLog(aiResponse) + " beforeKINGDOM_ANNEX=" + sceneRewardBeforeHasKingdomAnnex + " afterKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTagForLog(aiResponse));
									List<string> list2 = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
									if (list2 != null)
									{
										foreach (string item in list2)
										{
											_ports.RecordSystemFactForNearbySafe(allNpcData, item);
										}
									}
								}
				if (RomanceSystemBehavior.Instance != null)
				{
					RomanceSystemBehavior.Instance.ApplyMarriageTags(characterObject.HeroObject, Hero.MainHero, ref aiResponse, runPostprocessIfMissing: false);
				}
				SexualConceptionBehavior.TryApplyIntimacyTags(characterObject.HeroObject, ref aiResponse, ResolveScenePostprocessChainName());
								if (!ShouldSuppressSceneConversationControlForMeeting() || IsMeetingSceneConversationReleaseSensitive())
								{
									LordEncounterBehavior.TryProcessMeetingTauntAction(characterObject.HeroObject, ref aiResponse, out meetingTauntEscalated);
									LordEncounterBehavior.TryConsumeMeetingPlayerReleaseTag(characterObject.HeroObject, ref aiResponse, out meetingReleaseTriggered);
								}
								else
								{
									StripMeetingTauntTagsForSceneConversation(ref aiResponse);
									LordEncounterBehavior.StripMeetingPlayerReleaseTag(ref aiResponse);
								}
								if (!meetingTauntEscalated && !meetingReleaseTriggered && _ports.TryConsumeSceneNpcSurrenderTag(speakerData, ref aiResponse, out var surrenderHero, out var surrenderCharacter, out var surrenderAgentIndex))
								{
									npcSurrenderTriggered = LordEncounterBehavior.TryExecuteNpcSurrenderFromDirectDialog(surrenderHero, surrenderCharacter, surrenderAgentIndex, "scene_dialog_box_npc_surrender_tag");
								}
								sceneTauntActionHandled = SceneTauntBehavior.TryProcessSceneTauntAction(characterObject.HeroObject, characterObject, speakerData.AgentIndex, ref aiResponse, out sceneTauntEscalated);
							}
						else
						{
							MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(speakerData.UnnamedKey, speakerData.Name, ref aiResponse);
							if (agent != null && agent.Character is CharacterObject worldMapCharacter)
							{
								worldMapResult = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(worldMapCharacter.HeroObject, worldMapCharacter, speakerData.AgentIndex, ref aiResponse);
							}
							if (agent != null && agent.Character is CharacterObject characterObject4 && MyBehavior.TryApplyPartyTransferTagsForExternal(characterObject4.HeroObject, characterObject4, speakerData.AgentIndex, ref aiResponse, out var generatedFacts2, out var notifications2))
							{
								if (generatedFacts2 != null)
								{
									foreach (string generatedFact2 in generatedFacts2)
									{
										_ports.RecordSystemFactForNearbySafe(allNpcData, generatedFact2);
										if (characterObject4.HeroObject != null)
										{
											MyBehavior.AppendExternalDialogueHistory(characterObject4.HeroObject, null, null, generatedFact2);
										}
									}
								}
								if (notifications2 != null)
								{
									foreach (string notification2 in notifications2)
									{
										if (!string.IsNullOrWhiteSpace(notification2))
										{
											InformationManager.DisplayMessage(new InformationMessage(notification2, new Color(0.4f, 1f, 0.4f)));
										}
									}
								}
							}
							if (agent != null && agent.Character is CharacterObject characterObject2 && RewardSystemBehavior.Instance != null)
							{
								if (RewardSystemBehavior.Instance.TryApplyNonHeroJoinPlayerPartyTagForExternal(characterObject2, speakerData.AgentIndex, speakerData.PromptGivenName, speakerData.PromptDisplayName, ref aiResponse, out var generatedFacts3, out var notifications3))
								{
									if (generatedFacts3 != null)
									{
										foreach (string generatedFact3 in generatedFacts3)
										{
											_ports.RecordSystemFactForNearbySafe(allNpcData, generatedFact3);
										}
									}
									if (notifications3 != null)
									{
										foreach (string notification3 in notifications3)
										{
											if (!string.IsNullOrWhiteSpace(notification3))
											{
												InformationManager.DisplayMessage(new InformationMessage(notification3, notification3.IndexOf("失败", StringComparison.OrdinalIgnoreCase) >= 0 ? new Color(1f, 0.45f, 0.25f) : new Color(0.4f, 1f, 0.4f)));
											}
										}
									}
								}
								string rewardGiverName = (speakerData.PromptDisplayName ?? speakerData.PromptGivenName ?? speakerData.Name ?? characterObject2.Name?.ToString() ?? "对方部队").Trim();
								RewardSystemBehavior.RpItemIntroductionContext rpItemIntroductionContext = MayContainGeneratedRpItemReward(aiResponse)
									? CreateRpItemIntroductionContextForReward(
										null,
										speakerData,
										characterObject2,
										speakerData.AgentIndex,
										rewardGiverName,
										null,
										aiResponse)
									: null;
								if (TryResolveWildernessNonHeroRewardParty(null, characterObject2, speakerData.AgentIndex, out var party))
								{
									RewardSystemBehavior.Instance.ApplyPartyRewardTags(party, Hero.MainHero, rewardGiverName, characterObject2, ref aiResponse, rpItemIntroductionContext);
								}
								else
								{
									RewardSystemBehavior.Instance.ApplyMerchantRewardTags(characterObject2, Hero.MainHero, ref aiResponse, rpItemIntroductionContext);
								}
								List<string> list = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
								if (list != null)
								{
									foreach (string item2 in list)
									{
										_ports.RecordSystemFactForNearbySafe(allNpcData, item2);
									}
								}
							}
							if (agent != null && agent.Character is CharacterObject characterObject3)
							{
								if (!meetingTauntEscalated && !meetingReleaseTriggered && _ports.TryConsumeSceneNpcSurrenderTag(speakerData, ref aiResponse, out var surrenderHero, out var surrenderCharacter, out var surrenderAgentIndex))
								{
									npcSurrenderTriggered = LordEncounterBehavior.TryExecuteNpcSurrenderFromDirectDialog(surrenderHero, surrenderCharacter, surrenderAgentIndex, "scene_dialog_box_npc_surrender_tag");
								}
								sceneTauntActionHandled = SceneTauntBehavior.TryProcessSceneTauntAction(characterObject3.HeroObject, characterObject3, speakerData.AgentIndex, ref aiResponse, out sceneTauntEscalated);
							}
						}
					}
					catch
					{
					}
					if (sceneTauntActionHandled && string.IsNullOrWhiteSpace(aiResponse))
					{
						aiResponse = BuildFallbackSceneTauntSpeech(sceneTauntEscalated);
					}
					if (meetingReleaseTriggered && string.IsNullOrWhiteSpace(aiResponse))
					{
						LordEncounterBehavior.TryExecuteMeetingPlayerRelease(_ports.ResolveHeroFromAgentIndex(speakerData.AgentIndex), "meeting_release_player_immediate");
						return false;
					}
					if (npcSurrenderTriggered && string.IsNullOrWhiteSpace(SanitizeSceneSpeechText(aiResponse)))
					{
						return false;
					}
					bool flag = false;
					try
					{
						flag = !npcSurrenderTriggered && !meetingTauntEscalated && !sceneTauntEscalated && !meetingReleaseTriggered && ShoutUtils.TryTriggerDuelAction(speakerData, "", ref aiResponse);
					}
					catch
					{
					}
					if (!string.IsNullOrWhiteSpace(aiResponse))
					{
						SceneSpeechPlaybackInfo sceneSpeechPlaybackInfo = _ports.ShowNpcSpeechOutput(speakerData, agent, aiResponse);
						if (meetingReleaseTriggered)
						{
							_ports.ScheduleMeetingReleaseAfterSpeech(speakerData.AgentIndex, _ports.ResolveHeroFromAgentIndex(speakerData.AgentIndex), sceneSpeechPlaybackInfo);
						}
						if (agent != null && agent.IsActive())
						{
							_ports.AddAgentToStareList(agent, interruptCurrentUse: false);
						}
						_ports.RecordResponseForAllNearbySafe(allNpcData, speakerData.AgentIndex, speakerData.Name, aiResponse);
						_ports.PersistNpcSpeechToNamedHeroes(speakerData.AgentIndex, speakerData.Name, aiResponse, allNpcData);
						if (worldMapResult?.NeedsChannelExit == true)
						{
							_ports.ScheduleWorldMapMissionExitAfterSpeech(speakerData.AgentIndex, sceneSpeechPlaybackInfo);
						}
					}
					if (flag)
					{
						_ports.ReleaseSceneConversationConstraints(allNpcData, speakerData.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true, forceFullAutonomyRelease: true);
						DuelBehavior.SetNextDuelRiskWarningEnabled(_ports.GetDuelLiteralHit());
						ShoutUtils.ExecuteDuel(agent);
					}
				}
				catch
				{
				}
            return true;
			}, false, forceQueue: true);
		}
		catch
		{
		}
        return Task.FromResult(false);
	}
}
