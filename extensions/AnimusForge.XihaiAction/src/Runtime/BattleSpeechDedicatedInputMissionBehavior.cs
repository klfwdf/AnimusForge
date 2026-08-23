using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Entry helpers for the battle-speech choices exposed by AF's Y-key menu.
    ///
    /// The UI is reused when available, but submission goes straight to
    /// BattleSpeechRuntimeHost and never enters AF's ordinary T-key scene-shout
    /// route. Actor authority is frozen by the menu choice: "演讲" is always the
    /// player, while "他人演讲" is always the already-selected primary NPC.
    /// </summary>
    internal sealed partial class BattleSpeechMissionBehavior
    {
        private void StartDedicatedNpcSpeechGeneration(
            ActiveBattleSpeechSessionV1 session,
            BattleSpeechCapturedInputV1 input,
            string topic)
        {
            if (session == null || input == null || session.Speaker == null)
            {
                return;
            }
            Agent speaker = session.Speaker;
            Agent[] framed = input.FramedTargets ?? Array.Empty<Agent>();
            Guid sessionId = session.SessionId;
            int epoch = session.ConversationEpoch;
            Mission mission = Mission;
            double submittedAt = mission?.CurrentTime ?? 0d;
            CancellationToken lifetimeToken = _v2LifetimeCancellation.Token;
            // AF's reflection bridge builds the NPC packet, scene context and
            // prompt before its first await. Run that whole preparation off the
            // Mission thread so Y-menu selection cannot freeze the battle UI.
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await GenerateAndQueueDedicatedNpcSpeechAsync(
                            mission,
                            sessionId,
                            speaker,
                            framed,
                            topic,
                            epoch,
                            submittedAt).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        SceneActionsLog.Error(
                            "BATTLE_SPEECH_INPUT",
                            "Dedicated NPC speech background task failed closed.",
                            ex);
                    }
                },
                lifetimeToken);
        }

        private async Task GenerateAndQueueDedicatedNpcSpeechAsync(
            Mission mission,
            Guid sessionId,
            Agent speaker,
            IReadOnlyList<Agent> framed,
            string topic,
            int conversationEpoch,
            double submittedAt)
        {
            DedicatedNpcSpeechResultV1 result =
                await AfCompatV130.GenerateDedicatedNpcSpeechAsync(
                    speaker,
                    framed,
                    topic).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_INPUT",
                    "Dedicated NPC speech generation failed closed. Session=" +
                    sessionId.ToString("N") + " Reason=" +
                    (result.Error ?? "unknown"));
                return;
            }
            if (!BattleSpeechRuntimeHost.SubmitGeneratedNpcReply(
                    mission,
                    sessionId,
                    speaker,
                    result.Content,
                    result.AfBehavior,
                    result.AfNpcPacket,
                    conversationEpoch,
                    submittedAt))
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_INPUT",
                    "Dedicated NPC speech result dropped because the session ended. Session=" +
                    sessionId.ToString("N"));
            }
        }

        internal bool TryOpenSpeechInputFromShoutMenu(
            bool npcSpeech,
            Agent player,
            Agent primaryTarget,
            IReadOnlyList<Agent> framedTargets,
            int conversationEpoch)
        {
            if (_closed || !ReferenceEquals(Mission, Mission.Current) ||
                player == null || !player.IsActive())
            {
                return false;
            }
            if (_active != null)
            {
                NotifyDedicatedSpeechMessage("当前已有一场演讲正在进行。", Colors.Yellow);
                return false;
            }
            if (!TryResolvePhase(out BattleSpeechPhaseV1 phase))
            {
                NotifyDedicatedSpeechMessage(
                    SceneActionsText.BattleSpeechNotReady().ToString(),
                    Colors.Yellow);
                return false;
            }
            if (npcSpeech && !IsEligibleSpeaker(primaryTarget))
            {
                NotifyDedicatedSpeechMessage("当前目标不能作为演讲者。", Colors.Yellow);
                return false;
            }

            // "他人演讲" is an actor-selection command, not a topic editor.
            // The selected primary target is frozen by the Y menu; AF generates a
            // fresh troop-facing body from the current battlefield context. This
            // avoids opening a second text popup and avoids making the player
            // supply a topic that could leak into the NPC's reply.
            if (npcSpeech)
            {
                bool queued = BattleSpeechRuntimeHost.SubmitDedicatedNpcSpeech(
                    Mission,
                    player,
                    primaryTarget,
                    framedTargets ?? Array.Empty<Agent>(),
                    conversationEpoch,
                    Mission.CurrentTime);
                AfCompatV130.CompleteDedicatedSpeechMenuInput();
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_INPUT",
                    "Y-menu NPC speech submitted without topic input. Primary=" +
                    primaryTarget.Index + " FramedTargets=" +
                    (framedTargets?.Count ?? 0) + " Queued=" + queued);
                if (!queued)
                {
                    NotifyDedicatedSpeechMessage(
                        "他人演讲请求未进入战场通道，请确认当前仍在战斗中。",
                        Colors.Yellow);
                }
                return queued;
            }

            Agent frozenSpeaker = npcSpeech ? primaryTarget : player;
            Agent[] frozenTargets = (framedTargets ?? Array.Empty<Agent>()).ToArray();
            if (npcSpeech &&
                !frozenTargets.Any(agent => agent != null && agent.Index == primaryTarget.Index))
            {
                frozenTargets = frozenTargets.Concat(new[] { primaryTarget }).ToArray();
            }
            Action cancel = () => AfCompatV130.CompleteDedicatedSpeechMenuInput();
            Action<string> submit = text =>
            {
                string content = (text ?? string.Empty).Replace("\r", "").Trim();
                if (content.Length == 0)
                {
                    return;
                }
                // Always wrap the body with the menu-owned actor prefix. This
                // prevents text entered in one menu branch from retargeting the
                // request through an embedded "你演讲："/"他演讲：" phrase.
                string routedText = (npcSpeech ? "你演讲：" : "我演讲：") + content;
                bool queued = false;
                try
                {
                    queued = BattleSpeechRuntimeHost.SubmitDedicatedSpeech(
                        Mission,
                        routedText,
                        player,
                        primaryTarget,
                        frozenTargets,
                        conversationEpoch,
                        Mission.CurrentTime);
                }
                finally
                {
                    AfCompatV130.CompleteDedicatedSpeechMenuInput();
                }
                if (!queued)
                {
                    NotifyDedicatedSpeechMessage(
                        "演讲请求未进入战场通道，请确认当前仍在战斗中。",
                        Colors.Yellow);
                }
            };

            string title = npcSpeech ? "他人演讲" : "演讲";
            string subtitle = npcSpeech
                ? "当前目标将作为演讲者，AF 会根据你输入的主题重新生成一段对己方士兵的演讲正文。"
                : "你将作为演讲者，直接输入要对己方士兵说的正文。";
            bool opened = AfCompatV130.TryOpenDedicatedSpeechInput(
                title,
                subtitle,
                npcSpeech ? "输入演讲主题：" : "输入对士兵说的演讲正文：",
                submit,
                cancel);
            if (opened)
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_INPUT",
                    "Y-menu battle speech input opened. Kind=" +
                    (npcSpeech ? "npc" : "player") + " Phase=" + phase +
                    " FramedTargets=" + frozenTargets.Length +
                    " Primary=" + (primaryTarget?.Index.ToString() ?? "none"));
                return true;
            }

            // Older AF builds may not expose the custom popup. Keep the channel
            // independent by using the native inquiry only as a UI fallback;
            // its callbacks still submit directly to the speech host.
            try
            {
                InformationManager.ShowTextInquiry(
                    new TextInquiryData(
                        title,
                        subtitle + "\n\n请输入内容：",
                        true,
                        true,
                        "发表",
                        "取消",
                        submit,
                        cancel),
                    pauseGameActiveState: true);
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_INPUT",
                    "Y-menu battle speech input opened with native inquiry fallback. Kind=" +
                    (npcSpeech ? "npc" : "player"));
                return true;
            }
            catch (Exception ex)
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_INPUT",
                    "Y-menu battle speech input could not open: " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static void NotifyDedicatedSpeechMessage(string text, Color color)
        {
            try
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(text ?? string.Empty, color));
            }
            catch
            {
            }
        }
    }
}
