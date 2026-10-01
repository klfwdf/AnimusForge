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

namespace AnimusForge;


public partial class ShoutBehavior
{
	private void PublishSceneSpeechQueueItem(SceneSpeechQueueItem item)
	{
				NpcDataPacket matchedNpc = item.Npc;
				string content = item.Content;
				List<NpcDataPacket> allNpcData = item.ContextSnapshot ?? new List<NpcDataPacket>();
				List<SceneSummonPromptTarget> sceneSummonTargets = item.SceneSummonTargets;
				List<SceneGuidePromptTarget> sceneGuideTargets = item.SceneGuideTargets;
				bool commitHistory = item.CommitHistory;
				bool suppressStare = item.SuppressStare;
				bool allowPlayerDirectedActions = item.AllowPlayerDirectedActions;
				string playerDirectedActionText = item.PlayerDirectedActionText;
				string playerDirectedNpcReplyText = item.PlayerDirectedNpcReplyText;
				int requiredConversationEpoch = item.RequiredConversationEpoch;
				string afterSpeechInfoMessage = item.AfterSpeechInfoMessage;
				TaskCompletionSource<bool> completionSource = item.CompletionSource;
				float interactionTimeoutSeconds = item.InteractionTimeoutSeconds;
				int interactionParticipantCount = Math.Max(1, item.InteractionParticipantCount);
				Func<bool> canStillPublish = item.CanStillPublish;
				bool speechPublished = false;
				try
					{
						if (!CanPublishImmediateSceneReaction(canStillPublish))
						{
							completionSource?.TrySetResult(false);
							Logger.Log("ShoutBehavior", $"[ImmediateSceneReaction] discarded stale queued speech targetAgentIndex={matchedNpc?.AgentIndex ?? (-1)}");
							return;
						}
						if (ContainsOpenLordsHallActionTag(content))
						{
							if (!IsSceneConversationEpochCurrent(requiredConversationEpoch))
							{
								return;
							}
							Agent openLordsHallAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == (matchedNpc?.AgentIndex ?? (-1)));
							if (TryTriggerOpenLordsHallAction(matchedNpc, openLordsHallAgent, ref content))
							{
								ShowOpenLordsHallResponseAndScheduleEntry(matchedNpc, openLordsHallAgent, allNpcData, content, commitHistory, afterSpeechInfoMessage);
								return;
							}
						}
						bool hasDeferredIssueActionTag = allowPlayerDirectedActions && !string.IsNullOrWhiteSpace(content) && (content.IndexOf("[ACTION:ISSUE_ACCEPT_SELF]", StringComparison.OrdinalIgnoreCase) >= 0 || content.IndexOf("[ACTION:ISSUE_ACCEPT_ALT:", StringComparison.OrdinalIgnoreCase) >= 0 || content.IndexOf("[ACTION:QUEST_TURN_IN]", StringComparison.OrdinalIgnoreCase) >= 0);
						if (hasDeferredIssueActionTag)
						{
							Logger.Log("ShoutBehavior", "[DeferredIssueAction] queued_content=" + ((content ?? "").Replace("\r", "\\r").Replace("\n", "\\n")) + " npc=" + (matchedNpc?.Name ?? ""));
							Hero hero = null;
							try
							{
								hero = ResolveHeroFromAgentIndex(matchedNpc?.AgentIndex ?? (-1));
							}
							catch
							{
								hero = null;
							}
							if (hero != null)
							{
								Logger.Log("ShoutBehavior", "[DeferredIssueAction] apply hero=" + (hero.StringId ?? "") + " before=" + ((content ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
								VanillaIssueOfferBridge.ApplyIssueOfferTags(hero, ref content);
								Logger.Log("ShoutBehavior", "[DeferredIssueAction] apply_done hero=" + (hero.StringId ?? "") + " after=" + ((content ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
							}
							else
							{
								Logger.Log("ShoutBehavior", "[DeferredIssueAction] hero_resolve_failed npc=" + (matchedNpc?.Name ?? "") + " agentIndex=" + (matchedNpc?.AgentIndex ?? (-1)));
							}
						}
						if (!IsSceneConversationEpochCurrent(requiredConversationEpoch))
						{
							return;
						}
						Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == matchedNpc.AgentIndex);
						if (hasDeferredIssueActionTag && string.IsNullOrWhiteSpace(SanitizeSceneSpeechText(content)))
						{
							return;
						}
						if (!CanAgentParticipateInSceneSpeech(agent))
						{
							return;
						}
						PublicExecutionOrderRuntime.Consume(matchedNpc.AgentIndex, ref content);
						bool noblePrisonerExecutionQueued = allowPlayerDirectedActions
							&& NoblePrisonerEscortBehavior.TryProcessSceneExecutionTag(
								matchedNpc.AgentIndex,
								!string.IsNullOrWhiteSpace(playerDirectedActionText),
								ref content);
						if (allowPlayerDirectedActions)
						{
							NoblePrisonerExecutionOrderBehavior.TryProcessAcceptedTag(
								(agent?.Character as CharacterObject)?.HeroObject,
								matchedNpc.AgentIndex,
								!string.IsNullOrWhiteSpace(playerDirectedActionText),
								ref content,
								out _);
						}
						bool flag = false;
						bool flagMeetingRelease = false;
						bool flagSceneTaunt = false;
						bool flagSceneTaunt2 = false;
						bool flagNpcSurrender = false;
						SceneSpeechPlaybackInfo sceneSpeechPlaybackInfo = null;
						WorldMapPartyCommandBehavior.WorldMapOrderApplyResult worldMapResult = new WorldMapPartyCommandBehavior.WorldMapOrderApplyResult();
						try
						{
							if (agent != null && agent.Character is CharacterObject { HeroObject: not null } characterObject)
							{
								MyBehavior.ApplyPatienceFromSceneHeroResponseExternal(characterObject.HeroObject, ref content);
								if (allowPlayerDirectedActions)
								{
									TryProcessCustomPolicyAgendaActionTag(characterObject.HeroObject, ResolveScenePostprocessChainName(), playerDirectedActionText, ref content, playerDirectedNpcReplyText);
									VoteDealBehavior.ProcessAgendaTagsDispatch(characterObject.HeroObject, ref content);
									DiplomacyBehavior.ProcessDiplomacyTagsDispatch(characterObject.HeroObject, ref content);
									worldMapResult = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(characterObject.HeroObject, ref content);
									DuelBehavior.TryCacheDuelAfterLinesFromText(characterObject.HeroObject, ref content);
									DuelBehavior.TryCacheDuelStakeFromText(characterObject.HeroObject, ref content);
									VanillaIssueOfferBridge.ApplyIssueOfferTags(characterObject.HeroObject, ref content);
								if (TeamModuleServices.Gathering.TryApplyNobleGatheringTagsForExternal(characterObject.HeroObject, ref content, out var nobleFacts, out var nobleNotifications))
								{
									foreach (string generatedFact in nobleFacts ?? new List<string>())
									{
										RecordSystemFactForNearbySafe(allNpcData, generatedFact);
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
									if (MyBehavior.TryApplyPartyTransferTagsForExternal(characterObject.HeroObject, characterObject, matchedNpc.AgentIndex, ref content, out var generatedFacts, out var notifications))
									{
										if (generatedFacts != null)
										{
											foreach (string generatedFact in generatedFacts)
											{
												RecordSystemFactForNearbySafe(allNpcData, generatedFact);
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
										bool sceneRewardBeforeHasVassalage = ContainsVassalageActionTagForLog(content);
										bool sceneRewardBeforeHasKingdomAnnex = ContainsKingdomAnnexActionTagForLog(content);
										string sceneRewardChainName = ResolveScenePostprocessChainName();
										Logger.Log("ShoutBehavior", "[SceneConversation] ApplyRewardTags start chain=" + sceneRewardChainName + " target=" + (characterObject.HeroObject?.StringId ?? characterObject.StringId ?? matchedNpc?.Name ?? "unknown") + " containsVASSALAGE=" + sceneRewardBeforeHasVassalage + " containsKINGDOM_ANNEX=" + sceneRewardBeforeHasKingdomAnnex);
										RewardSystemBehavior.RpItemIntroductionContext rpItemIntroductionContext = MayContainGeneratedRpItemReward(content)
											? CreateRpItemIntroductionContextForReward(
												characterObject.HeroObject,
												matchedNpc,
												characterObject,
												matchedNpc.AgentIndex,
												characterObject.Name?.ToString() ?? matchedNpc?.Name,
												playerDirectedActionText,
												string.IsNullOrWhiteSpace(playerDirectedNpcReplyText) ? content : playerDirectedNpcReplyText)
											: null;
										RewardSystemBehavior.Instance.ApplyRewardTags(characterObject.HeroObject, Hero.MainHero, ref content, rpItemIntroductionContext);
										Logger.Log("ShoutBehavior", "[SceneConversation] ApplyRewardTags done chain=" + sceneRewardChainName + " target=" + (characterObject.HeroObject?.StringId ?? characterObject.StringId ?? matchedNpc?.Name ?? "unknown") + " beforeVASSALAGE=" + sceneRewardBeforeHasVassalage + " afterVASSALAGE=" + ContainsVassalageActionTagForLog(content) + " beforeKINGDOM_ANNEX=" + sceneRewardBeforeHasKingdomAnnex + " afterKINGDOM_ANNEX=" + ContainsKingdomAnnexActionTagForLog(content));
										List<string> list2 = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
										if (list2 != null)
										{
											foreach (string item2 in list2)
											{
												RecordSystemFactForNearbySafe(allNpcData, item2);
											}
										}
									}
					if (RomanceSystemBehavior.Instance != null)
					{
						RomanceSystemBehavior.Instance.ApplyMarriageTags(characterObject.HeroObject, Hero.MainHero, ref content, runPostprocessIfMissing: false);
					}
					SexualConceptionBehavior.TryApplyIntimacyTags(characterObject.HeroObject, ref content, ResolveScenePostprocessChainName());
									if (!ShouldSuppressSceneConversationControlForMeeting() || IsMeetingSceneConversationReleaseSensitive())
									{
										LordEncounterBehavior.TryProcessMeetingTauntAction(characterObject.HeroObject, ref content, out flag);
										LordEncounterBehavior.TryConsumeMeetingPlayerReleaseTag(characterObject.HeroObject, ref content, out flagMeetingRelease);
									}
									else
									{
										StripMeetingTauntTagsForSceneConversation(ref content);
										LordEncounterBehavior.StripMeetingPlayerReleaseTag(ref content);
									}
									if (!flag && !flagMeetingRelease && TryConsumeSceneNpcSurrenderTag(matchedNpc, ref content, out var surrenderHero, out var surrenderCharacter, out var surrenderAgentIndex))
									{
										flagNpcSurrender = LordEncounterBehavior.TryExecuteNpcSurrenderFromDirectDialog(surrenderHero, surrenderCharacter, surrenderAgentIndex, "scene_dialog_box_npc_surrender_tag");
									}
									flagSceneTaunt2 = SceneTauntBehavior.TryProcessSceneTauntAction(characterObject.HeroObject, characterObject, matchedNpc.AgentIndex, ref content, out flagSceneTaunt);
								}
							}
							else
							{
								MyBehavior.ApplyPatienceFromSceneUnnamedResponseExternal(matchedNpc.UnnamedKey, matchedNpc.Name, ref content);
								if (allowPlayerDirectedActions && agent != null && agent.Character is CharacterObject worldMapCharacter)
								{
									worldMapResult = WorldMapPartyCommandBehavior.ProcessWorldMapOrderTagsDispatch(worldMapCharacter.HeroObject, worldMapCharacter, matchedNpc.AgentIndex, ref content);
								}
								if (allowPlayerDirectedActions && agent != null && agent.Character is CharacterObject characterObject4 && MyBehavior.TryApplyPartyTransferTagsForExternal(characterObject4.HeroObject, characterObject4, matchedNpc.AgentIndex, ref content, out var generatedFacts2, out var notifications2))
								{
									if (generatedFacts2 != null)
									{
										foreach (string generatedFact2 in generatedFacts2)
										{
											RecordSystemFactForNearbySafe(allNpcData, generatedFact2);
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
								if (allowPlayerDirectedActions && agent != null && agent.Character is CharacterObject characterObject2 && RewardSystemBehavior.Instance != null)
								{
									if (RewardSystemBehavior.Instance.TryApplyNonHeroJoinPlayerPartyTagForExternal(characterObject2, matchedNpc.AgentIndex, matchedNpc.PromptGivenName, matchedNpc.PromptDisplayName, ref content, out var generatedFacts3, out var notifications3))
									{
										if (generatedFacts3 != null)
										{
											foreach (string generatedFact3 in generatedFacts3)
											{
												RecordSystemFactForNearbySafe(allNpcData, generatedFact3);
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
									string rewardGiverName = (matchedNpc.PromptDisplayName ?? matchedNpc.PromptGivenName ?? matchedNpc.Name ?? characterObject2.Name?.ToString() ?? "对方部队").Trim();
									RewardSystemBehavior.RpItemIntroductionContext rpItemIntroductionContext = MayContainGeneratedRpItemReward(content)
										? CreateRpItemIntroductionContextForReward(
											null,
											matchedNpc,
											characterObject2,
											matchedNpc.AgentIndex,
											rewardGiverName,
											playerDirectedActionText,
											string.IsNullOrWhiteSpace(playerDirectedNpcReplyText) ? content : playerDirectedNpcReplyText)
										: null;
									if (TryResolveWildernessNonHeroRewardParty(null, characterObject2, matchedNpc.AgentIndex, out var party))
									{
										RewardSystemBehavior.Instance.ApplyPartyRewardTags(party, Hero.MainHero, rewardGiverName, characterObject2, ref content, rpItemIntroductionContext);
									}
									else
									{
										RewardSystemBehavior.Instance.ApplyMerchantRewardTags(characterObject2, Hero.MainHero, ref content, rpItemIntroductionContext);
									}
									List<string> list = RewardSystemBehavior.Instance.ConsumeLastGeneratedNpcFactLines();
									if (list != null)
									{
										foreach (string item2 in list)
										{
											RecordSystemFactForNearbySafe(allNpcData, item2);
										}
									}
								}
								if (allowPlayerDirectedActions && agent != null && agent.Character is CharacterObject characterObject3)
								{
									if (!flag && !flagMeetingRelease && TryConsumeSceneNpcSurrenderTag(matchedNpc, ref content, out var surrenderHero, out var surrenderCharacter, out var surrenderAgentIndex))
									{
										flagNpcSurrender = LordEncounterBehavior.TryExecuteNpcSurrenderFromDirectDialog(surrenderHero, surrenderCharacter, surrenderAgentIndex, "scene_dialog_box_npc_surrender_tag");
									}
									flagSceneTaunt2 = SceneTauntBehavior.TryProcessSceneTauntAction(characterObject3.HeroObject, characterObject3, matchedNpc.AgentIndex, ref content, out flagSceneTaunt);
								}
							}
						}
						catch
						{
						}
						content = StripLeakedPromptContentForShout(content);
						string historyFullContent = content;
						content = StripStageDirectionsForPassiveShout(content);
						if (!allowPlayerDirectedActions)
						{
							content = StripActionTagsForSceneSpeech(content);
						}
						SceneSummonConversationSession sceneSummonConversationSession = null;
						ActiveSceneSummonRequest activeSceneSummonRequest = null;
						ActiveSceneGuideRequest activeSceneGuideRequest = null;
						bool flag7 = allowPlayerDirectedActions && !flagSceneTaunt && _sceneMovement.TryConsumeSceneFollowStopTag(matchedNpc, agent, ref content);
						bool flag8 = allowPlayerDirectedActions && !flagSceneTaunt && _sceneMovement.TryConsumeSceneFollowStartTag(matchedNpc, agent, ref content);
						bool flag5 = allowPlayerDirectedActions && !flagSceneTaunt && _sceneMovement.TryConsumeSceneEndChatActionTag(matchedNpc, agent, ref content, out sceneSummonConversationSession);
						if (flagSceneTaunt2 && string.IsNullOrWhiteSpace(content))
						{
							content = BuildFallbackSceneTauntSpeech(flagSceneTaunt);
						}
						if (flagSceneTaunt2 && !IsMeetingSceneConversationReleaseSensitive() && string.IsNullOrWhiteSpace(content))
						{
							ReleaseSceneConversationConstraints(allNpcData, matchedNpc.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true);
							if (flagSceneTaunt && matchedNpc.AgentIndex >= 0)
							{
								InterruptAgentSpeechForCombat(matchedNpc.AgentIndex, "scene_taunt_action");
							}
							return;
						}
						if (flagMeetingRelease && string.IsNullOrWhiteSpace(content))
						{
							LordEncounterBehavior.TryExecuteMeetingPlayerRelease(ResolveHeroFromAgentIndex(matchedNpc.AgentIndex), "meeting_release_player_tag_no_speech");
							return;
						}
						if (flagNpcSurrender && string.IsNullOrWhiteSpace(SanitizeSceneSpeechText(content)))
						{
							return;
						}
						if (!string.IsNullOrWhiteSpace(content))
						{
							bool flag3 = TryTriggerOpenLordsHallAction(matchedNpc, agent, ref content);
							bool flag2 = !flag3 && allowPlayerDirectedActions && !flagNpcSurrender && !flag && !flagSceneTaunt && !flagMeetingRelease && ShoutUtils.TryTriggerDuelAction(matchedNpc, playerDirectedActionText, ref content);
							bool flag6 = !flag3 && allowPlayerDirectedActions && !flagNpcSurrender && !flagSceneTaunt && !flagMeetingRelease && _sceneMovement.TryTriggerSceneSummonAction(matchedNpc, agent, sceneSummonTargets, sceneGuideTargets, ref content, out activeSceneSummonRequest);
							bool flag10 = !flag3 && allowPlayerDirectedActions && !flagNpcSurrender && !flagSceneTaunt && !flagMeetingRelease && _sceneMovement.TryTriggerSceneGuideAction(matchedNpc, agent, sceneGuideTargets, sceneSummonTargets, ref content, out activeSceneGuideRequest);
							if (!string.IsNullOrWhiteSpace(content))
							{
								if (!IsSceneConversationEpochCurrent(requiredConversationEpoch))
								{
									return;
								}
								string historyText = SanitizeSceneSpeechText(content);
								string fullHistoryText = PrepareSceneHistorySpeechText(historyFullContent);
								_sceneMovement.RefreshSceneSummonConversationForSpeaker((agent != null) ? agent.Index : matchedNpc.AgentIndex);
								bool flag4 = IsAgentHostileToMainAgent(agent);
								if (interactionTimeoutSeconds > 0f && !noblePrisonerExecutionQueued && !flagNpcSurrender && !flag4 && !flag5 && !flag6 && !flag10 && !flagMeetingRelease && !flagSceneTaunt2)
								{
									RefreshActiveInteractionTimeout(matchedNpc, interactionParticipantCount, interactionTimeoutSeconds);
								}
								bool suppressInteractionTimeoutArm = interactionParticipantCount > 1 && interactionTimeoutSeconds <= 0f;
								sceneSpeechPlaybackInfo = ShowNpcSpeechOutput(matchedNpc, agent, historyText, allowTts: true, attachTtsToSceneAgent: true, suppressInteractionTimeoutArm);
								speechPublished = !string.IsNullOrWhiteSpace(historyText);
								if (!string.IsNullOrWhiteSpace(afterSpeechInfoMessage))
								{
									InformationManager.DisplayMessage(new InformationMessage(afterSpeechInfoMessage, new Color(1f, 0.95f, 0.25f)));
								}
								if (flag7)
								{
									_sceneMovement.ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: false, sceneSpeechPlaybackInfo);
								}
								else if (flag8)
								{
									_sceneMovement.ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: true, sceneSpeechPlaybackInfo);
								}
								if (flag5 && sceneSummonConversationSession != null)
								{
									_sceneMovement.SetPendingSummonReturn(matchedNpc.AgentIndex, sceneSummonConversationSession, _sceneMovement.ShouldReturnOnlySceneSummonSpeaker(sceneSummonConversationSession, agent));
								}
								_sceneMovement.ScheduleSceneSummonReturnAfterSpeech(matchedNpc.AgentIndex, sceneSpeechPlaybackInfo);
								_sceneMovement.ScheduleSceneGuideReturnAfterSpeech(matchedNpc.AgentIndex, sceneSpeechPlaybackInfo);
								ScheduleSceneAutonomyRestoreAfterSpeech(matchedNpc.AgentIndex, sceneSpeechPlaybackInfo);
								if (flagMeetingRelease)
								{
									ScheduleMeetingReleaseAfterSpeech(matchedNpc.AgentIndex, ResolveHeroFromAgentIndex(matchedNpc.AgentIndex), sceneSpeechPlaybackInfo);
								}
								if (flag6 && activeSceneSummonRequest != null)
								{
									_sceneMovement.SchedulePreparedSceneSummonLaunch(activeSceneSummonRequest, sceneSpeechPlaybackInfo, historyText);
								}
								if (flag10 && activeSceneGuideRequest != null)
								{
									_sceneMovement.SchedulePreparedSceneGuideLaunch(activeSceneGuideRequest, sceneSpeechPlaybackInfo, historyText);
								}
								if (flag4)
								{
									RefreshHostileCombatAgentAutonomy(agent);
								}
								if (CanAgentParticipateInSceneSpeech(agent) && !suppressStare && !flag4 && !flag6 && !flag10)
								{
									HoldSceneConversationParticipants(allNpcData);
									AddAgentToStareList(agent, interruptCurrentUse: false);
								}
								if (commitHistory && !string.IsNullOrWhiteSpace(fullHistoryText))
								{
									RecordResponseForAllNearbySafe(allNpcData, matchedNpc.AgentIndex, matchedNpc.Name, fullHistoryText);
									PersistNpcSpeechToNamedHeroes(matchedNpc.AgentIndex, matchedNpc.Name, fullHistoryText, allNpcData);
								}
								if (worldMapResult?.NeedsChannelExit == true)
								{
									ScheduleWorldMapMissionExitAfterSpeech(matchedNpc.AgentIndex, sceneSpeechPlaybackInfo);
								}
								if (flagSceneTaunt2 && !IsMeetingSceneConversationReleaseSensitive())
								{
									ReleaseSceneConversationConstraints(allNpcData, matchedNpc.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true);
								}
							}
							else if (flag7)
							{
								_sceneMovement.ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: false, null);
							}
							else if (flag8)
							{
								_sceneMovement.ScheduleSceneFollowCommandAfterSpeech(matchedNpc.AgentIndex, startFollow: true, null);
							}
							if (flag2)
							{
								ReleaseSceneConversationConstraints(allNpcData, matchedNpc.AgentIndex, stopAutoGroupSession: true, clearQueuedSpeech: true, forceFullAutonomyRelease: true);
								DuelBehavior.SetNextDuelRiskWarningEnabled(_lastShoutDuelLiteralHit);
								ShoutUtils.ExecuteDuel(agent);
							}
							if (flag3)
							{
								ScheduleLordsHallMissionEntryAfterSpeech(matchedNpc.AgentIndex, sceneSpeechPlaybackInfo, "scene_tag");
								return;
							}
							else if (flag6 && activeSceneSummonRequest != null && string.IsNullOrWhiteSpace(content))
							{
								_sceneMovement.SchedulePreparedSceneSummonLaunch(activeSceneSummonRequest, null, "");
							}
							else if (flag10 && activeSceneGuideRequest != null && string.IsNullOrWhiteSpace(content))
							{
								_sceneMovement.SchedulePreparedSceneGuideLaunch(activeSceneGuideRequest, null, "");
							}
						}
						if (flag5 && sceneSummonConversationSession != null && string.IsNullOrWhiteSpace(content))
						{
							_sceneMovement.BeginSceneSummonConversationReturn(sceneSummonConversationSession);
						}
					}
					catch (Exception ex)
					{
						Logger.Log("ShoutBehavior", "[ERROR] RunSpeechQueueWorker mainThread: " + ex.Message);
						completionSource?.TrySetResult(false);
					}
				finally
				{
					completionSource?.TrySetResult(speechPublished);
				}
			
	}

	private void EnqueueSpeechLine(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool skipHistory = false, bool suppressStare = false, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, Func<bool> canStillPublish = null)
	{
		EnqueueSpeechLineWithOptions(npc, content, allNpcData, !skipHistory, suppressStare, allowPlayerDirectedActions: true, requiredConversationEpoch: 0, sceneSummonTargets, sceneGuideTargets, null, canStillPublish: canStillPublish);
	}

	private void EnqueueSpeechLineWithOptions(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, string afterSpeechInfoMessage = null, TaskCompletionSource<bool> completionSource = null, float interactionTimeoutSeconds = -1f, int interactionParticipantCount = 1, Func<bool> canStillPublish = null, string playerDirectedActionText = null, string playerDirectedNpcReplyText = null)
	{
		if (npc == null || string.IsNullOrWhiteSpace(content))
		{
			completionSource?.TrySetResult(false);
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			completionSource?.TrySetResult(false);
			return;
		}
		NpcDataPacket value = CloneNpcDataPacket(npc);
		List<NpcDataPacket> value2 = CloneNpcDataSnapshot(allNpcData);
		ApplySceneLocalDisambiguatedNames(value2);
		NpcDataPacket npcDataPacket = value2.FirstOrDefault((NpcDataPacket x) => x != null && x.AgentIndex == value.AgentIndex);
		if (npcDataPacket != null)
		{
			value.Name = npcDataPacket.Name;
			value.PromptGivenName = npcDataPacket.PromptGivenName;
			value.PromptDisplayName = npcDataPacket.PromptDisplayName;
		}
		SceneSpeechQueueItem queuedItem = new SceneSpeechQueueItem
		{
			Npc = value,
			Content = content,
			ContextSnapshot = value2,
			SceneSummonTargets = SceneMovementController.CloneSceneSummonPromptTargets(sceneSummonTargets),
			SceneGuideTargets = SceneMovementController.CloneSceneGuidePromptTargets(sceneGuideTargets),
			CommitHistory = commitHistory,
			SuppressStare = suppressStare,
			AllowPlayerDirectedActions = allowPlayerDirectedActions,
			PlayerDirectedActionText = playerDirectedActionText,
			PlayerDirectedNpcReplyText = playerDirectedNpcReplyText,
			RequiredConversationEpoch = requiredConversationEpoch,
			AfterSpeechInfoMessage = afterSpeechInfoMessage,
			CompletionSource = completionSource,
			InteractionTimeoutSeconds = interactionTimeoutSeconds,
			InteractionParticipantCount = Math.Max(1, interactionParticipantCount),
			CanStillPublish = canStillPublish
		};
		_sceneSpeechQueueOwner.Enqueue(queuedItem);
	}

	private sealed class SceneSpeechQueueItem
	{
		public NpcDataPacket Npc;

		public string Content;

		public List<NpcDataPacket> ContextSnapshot;

		public List<SceneSummonPromptTarget> SceneSummonTargets;

		public List<SceneGuidePromptTarget> SceneGuideTargets;

		public bool CommitHistory;

		public bool SuppressStare;

		public bool AllowPlayerDirectedActions = true;

		public string PlayerDirectedActionText;

		public string PlayerDirectedNpcReplyText;

		public int RequiredConversationEpoch;

		public string AfterSpeechInfoMessage;

		public TaskCompletionSource<bool> CompletionSource;

		public float InteractionTimeoutSeconds = -1f;

		public int InteractionParticipantCount = 1;

		public Func<bool> CanStillPublish;
	}

	private SceneSpeechExecutionRuntime<SceneSpeechQueueItem> _sceneSpeechExecution;
	private SceneSpeechExecutionRuntime<SceneSpeechQueueItem> _sceneSpeechQueueOwner =>
		_sceneSpeechExecution ??= new SceneSpeechExecutionRuntime<SceneSpeechQueueItem>(
			() => _isProcessingShout, action => _mainThreadActions.Enqueue(action), PublishSceneSpeechQueueItem,
			item => item.CompletionSource?.TrySetResult(false));
}
