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
internal delegate bool DeveloperSceneMechanismQueue(NpcDataPacket npc,List<NpcDataPacket> npcs,List<SceneSummonPromptTarget> summon,List<SceneGuidePromptTarget> guide,ref string content);
internal sealed class ConversationDeveloperActionTestController
{
    private readonly Func<bool> _isCurrentOwner,_canSubmit;
    private readonly SceneMovementController _sceneMovement;
    private readonly NativeConversationGameEffectsRuntime _nativeGameEffects;
    private readonly DeveloperSceneMechanismQueue _queueMechanism;
    private readonly Func<ShoutTargetingContext> _getTargeting;
    private readonly Func<ShoutTargetingContext,List<Agent>> _getCandidates;
    private readonly Action _cancel,_resume;
    private readonly Action<NpcDataPacket,string,List<NpcDataPacket>,List<SceneSummonPromptTarget>,List<SceneGuidePromptTarget>> _speech;
    internal ConversationDeveloperActionTestController(Func<bool> isCurrentOwner,Func<bool> canSubmit,SceneMovementController movement,NativeConversationGameEffectsRuntime effects,
        DeveloperSceneMechanismQueue queueMechanism,Func<ShoutTargetingContext> getTargeting,Func<ShoutTargetingContext,List<Agent>> getCandidates,Action cancel,Action resume,
        Action<NpcDataPacket,string,List<NpcDataPacket>,List<SceneSummonPromptTarget>,List<SceneGuidePromptTarget>> speech)
    { _isCurrentOwner=isCurrentOwner;_canSubmit=canSubmit;_sceneMovement=movement;_nativeGameEffects=effects;
      _queueMechanism=queueMechanism;_getTargeting=getTargeting;_getCandidates=getCandidates;_cancel=cancel;_resume=resume;_speech=speech; }
internal bool CanOpenNativeConversationTagTestForExternal()
	{
		try
		{
			return MyBehavior.IsDevDataManagementEnabledForExternal() && _canSubmit();
		}
		catch
		{
			return false;
		}
	}
internal bool OpenNativeConversationTagTestForExternal(Action onFinished = null)
	{
		try
		{
			if (!CanOpenNativeConversationTagTestForExternal() || !TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
			{
				return false;
			}
			string displayName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? npcName ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = "NPC";
			}
			Action finish = delegate
			{
				try
				{
					onFinished?.Invoke();
				}
				catch
				{
				}
			};
			Action<string> submit = delegate(string input)
			{
				try
				{
					if (TrySubmitNativeConversationTagTestForExternal(input, out var statusText))
					{
						ShowTagTestStatus(statusText, success: true);
					}
					else
					{
						ShowTagTestStatus(string.IsNullOrWhiteSpace(statusText) ? "标签测试未执行。" : statusText, success: false);
					}
				}
				finally
				{
					finish();
				}
			};
			string title = "标签输入 - " + displayName;
			string subtitle = "当前目标：" + displayName + "\n输入 NPC 可见正文和后处理标签。不会调用 API，标签会按当前 NPC 立即执行。";
			if (ShoutTextInputPopup.Show(title, subtitle, "输入正文和标签：", "", submit, finish))
			{
				return true;
			}
			InformationManager.ShowTextInquiry(new TextInquiryData(title, subtitle + "\n\n输入正文和标签：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "执行", "取消", submit, finish), pauseGameActiveState: true);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationTagTest", "[WARN] open failed: " + ex.Message);
			return false;
		}
	}
internal bool TrySubmitNativeConversationTagTestForExternal(string rawText, out string statusText)
	{
		statusText = "";
		try
		{
			if (!CanOpenNativeConversationTagTestForExternal())
			{
				statusText = "数据管理未开启，或当前没有可测试的对话目标。";
				return false;
			}
			ConversationDeveloperActionTestController instance = _isCurrentOwner() ? this : null;
			if (instance == null || !TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var npcName))
			{
				statusText = "当前没有可接入的对话对象。";
				return false;
			}
			string content = (rawText ?? "").Replace("\r", "").Trim();
			if (string.IsNullOrWhiteSpace(content))
			{
				statusText = "输入为空。";
				return false;
			}
			targetHero ??= targetCharacter?.HeroObject;
			targetCharacter ??= targetHero?.CharacterObject;
			string displayName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? npcName ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = "NPC";
			}
			int targetAgentIndex = TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			int tagCount = CountDeveloperTagTestTags(content);
			Logger.Log("NativeConversationTagTest", "submit target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? displayName) + " agentIndex=" + targetAgentIndex + " tagCount=" + tagCount + " raw=" + content.Replace("\n", "\\n"));
			NpcDataPacket nativeTagTestNpc = BuildNativeConversationNpcData(targetHero, targetCharacter);
			nativeTagTestNpc.AgentIndex = targetAgentIndex;
			List<NpcDataPacket> presentNpcs = new List<NpcDataPacket> { nativeTagTestNpc };
			Dictionary<int, Hero> resolvedHeroes = new Dictionary<int, Hero>();
			if (targetAgentIndex >= 0 && targetHero != null)
			{
				resolvedHeroes[targetAgentIndex] = targetHero;
			}
			List<SceneSummonPromptTarget> sceneSummonTargets = (targetAgentIndex >= 0) ? instance._sceneMovement.BuildSceneSummonPromptTargets(presentNpcs, resolvedHeroes) : null;
			int sceneGuideFirstPromptId = ((sceneSummonTargets != null && sceneSummonTargets.Count > 0) ? sceneSummonTargets.Max((SceneSummonPromptTarget x) => x?.PromptId ?? 0) : 0) + 1;
			Agent targetAgent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex) : null;
			List<SceneGuidePromptTarget> sceneGuideTargets = (targetAgentIndex >= 0) ? instance._sceneMovement.BuildSceneGuidePromptTargets(targetAgent, sceneGuideFirstPromptId) : null;
			if (targetHero != null)
			{
				MyBehavior.ApplyPostprocessMoodFromSceneHeroResponseExternal(targetHero, ref content);
			}
			else
			{
				try
				{
					Agent agent = (targetAgentIndex >= 0) ? Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive()) : null;
					NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
					if (npc != null)
					{
						MyBehavior.ApplyPostprocessMoodFromSceneUnnamedResponseExternal(npc.UnnamedKey, npc.Name, ref content);
					}
				}
				catch
				{
				}
			}
			bool sceneMechanismHandled = instance._queueMechanism(nativeTagTestNpc, presentNpcs, sceneSummonTargets, sceneGuideTargets, ref content);
			instance._nativeGameEffects.ApplyNativeConversationActionTags(targetHero, targetCharacter, ref content, targetAgentIndex);
			string visible = SanitizeSceneSpeechText(content);
			if (!string.IsNullOrWhiteSpace(visible))
			{
				try
				{
					ConversationHelper.UpdateDialogText(visible);
				}
				catch
				{
				}
				instance.CommitNativeConversationTagTestVisibleLine(targetHero, targetCharacter, npcName, visible, targetAgentIndex);
			}
			statusText = "标签测试已执行。目标：" + displayName + "，标签数：" + tagCount + (sceneMechanismHandled ? "，SCENE_MOVE将在退出对话框后执行。" : "") + (string.IsNullOrWhiteSpace(visible) ? "，无可见正文。" : "，已显示正文。");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationTagTest", "[ERROR] submit failed: " + ex);
			statusText = "标签测试失败：" + ex.Message;
			return false;
		}
	}
internal static void ShowTagTestStatus(string statusText, bool success)
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(statusText ?? "", success ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.45f, 0.25f)));
		}
		catch
		{
		}
	}
internal static int CountDeveloperTagTestTags(string text)
	{
		try
		{
			string text2 = text ?? "";
			return GiveAssetTagCodec.Extract(text2).Count + Regex.Matches(GiveAssetTagCodec.StripTags(text2), "\\[(?:ACTION:[^\\]]*|A:(?:H_J_P_P_(?:C&L|[CL])|C_J_P_K|C_J_K:[^\\]]+|P_J_K_[MV]|P_L_K)|AD:[^\\]]*|ADP:[^\\]]*|ASS:[^\\]]*|GUI:[^\\]]*|ATT:[^\\]]*|ATP:[^\\]]*|FOL|STP|END|RELAY:[^\\]]*|AFEF[^\\]]*|AF_SCENE_SESSION:[^\\]]*|CONTENT)\\]", RegexOptions.IgnoreCase).Count;
		}
		catch
		{
			return 0;
		}
	}
internal void CommitNativeConversationTagTestVisibleLine(Hero targetHero, CharacterObject targetCharacter, string npcName, string visible, int targetAgentIndex)
	{
		visible = (visible ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(visible))
		{
			return;
		}
		try
		{
			targetHero ??= targetCharacter?.HeroObject;
			targetCharacter ??= targetHero?.CharacterObject;
			NpcDataPacket npc = BuildNativeConversationNpcData(targetHero, targetCharacter);
			npc.AgentIndex = targetAgentIndex;
			string displayName = (GetSceneNpcHistoryNameForPrompt(npc) ?? npcName ?? targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
			if (string.IsNullOrWhiteSpace(displayName))
			{
				displayName = "NPC";
			}
			if (targetHero != null)
			{
				int sceneSessionId = TryGetCurrentSceneHistorySessionIdForHistoryPersistence();
				if (sceneSessionId >= 0)
				{
					MyBehavior.AppendExternalSceneDialogueHistory(targetHero, null, visible, null, sceneSessionId);
				}
				else
				{
					MyBehavior.AppendExternalDialogueHistory(targetHero, null, visible, null);
				}
			}
			else
			{
				AppendWildernessNonHeroMemory(npc, targetHero, targetCharacter, targetAgentIndex, null, visible, null, TryGetCurrentSceneHistorySessionIdForHistoryPersistence());
			}
			RecordNativeConversationNpcLineForExternal(targetHero, targetCharacter, displayName, visible, targetAgentIndex, npc);
			MarkNativeConversationCurrentDialogRecorded(targetHero, targetCharacter, npcName, visible, targetAgentIndex, npc);
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversationTagTest", "[WARN] commit visible line failed: " + ex.Message);
		}
	}
internal void OnShoutTagTestConfirmed(string input, int? forcedPrimaryAgentIndex)
	{
		string content = (input ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			_cancel();
			return;
		}
		try
		{
			if (!MyBehavior.IsDevDataManagementEnabledForExternal())
			{
				InformationManager.DisplayMessage(new InformationMessage("[标签测试] 请先在 MCM 开启数据管理。", new Color(1f, 0.45f, 0.25f)));
				return;
			}
			ShoutTargetingContext targetingContext = _getTargeting();
			List<Agent> nearbyAgents = _getCandidates(targetingContext) ?? new List<Agent>();
			if (forcedPrimaryAgentIndex.HasValue && !nearbyAgents.Any((Agent a) => a != null && a.Index == forcedPrimaryAgentIndex.Value))
			{
				Agent forcedAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == forcedPrimaryAgentIndex.Value && a.IsActive());
				if (forcedAgent != null)
				{
					nearbyAgents.Insert(0, forcedAgent);
				}
			}
			nearbyAgents = nearbyAgents.Where((Agent a) => a != null && a.IsActive()).ToList();
			if (nearbyAgents.Count == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage("[标签测试] 没有找到当前场景目标。", new Color(1f, 0.45f, 0.25f)));
				return;
			}
			List<NpcDataPacket> allNpcData = nearbyAgents.Select((Agent a) => ShoutUtils.ExtractNpcData(a)).Where((NpcDataPacket d) => d != null).ToList();
			ApplySceneLocalDisambiguatedNames(allNpcData);
			Agent primaryTarget = null;
			if (forcedPrimaryAgentIndex.HasValue)
			{
				primaryTarget = nearbyAgents.FirstOrDefault((Agent a) => a != null && a.Index == forcedPrimaryAgentIndex.Value);
			}
			primaryTarget ??= ResolvePrimaryAgentForShoutTargetingContext(targetingContext, nearbyAgents) ?? nearbyAgents.FirstOrDefault();
			NpcDataPacket primaryNpc = (primaryTarget != null) ? allNpcData.FirstOrDefault((NpcDataPacket d) => d.AgentIndex == primaryTarget.Index) : allNpcData.FirstOrDefault();
			if (primaryNpc == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("[标签测试] 没有找到可执行标签的 NPC。", new Color(1f, 0.45f, 0.25f)));
				return;
			}
			Dictionary<int, Hero> resolvedHeroes = new Dictionary<int, Hero>();
			foreach (Agent agent in nearbyAgents)
			{
				if (agent?.Character is CharacterObject { HeroObject: not null } character)
				{
					resolvedHeroes[agent.Index] = character.HeroObject;
				}
			}
			List<SceneSummonPromptTarget> sceneSummonTargets = _sceneMovement.BuildSceneSummonPromptTargets(allNpcData, resolvedHeroes);
			int sceneGuideFirstPromptId = ((sceneSummonTargets != null && sceneSummonTargets.Count > 0) ? sceneSummonTargets.Max((SceneSummonPromptTarget x) => x?.PromptId ?? 0) : 0) + 1;
			List<SceneGuidePromptTarget> sceneGuideTargets = _sceneMovement.BuildSceneGuidePromptTargets(primaryTarget, sceneGuideFirstPromptId);
			Logger.Log("SceneTagTest", "submit target=" + (primaryNpc?.Name ?? "") + " agentIndex=" + primaryNpc.AgentIndex + " tagCount=" + CountDeveloperTagTestTags(content) + " raw=" + content.Replace("\n", "\\n"));
			_speech(primaryNpc,content,allNpcData,sceneSummonTargets,sceneGuideTargets);
			InformationManager.DisplayMessage(new InformationMessage("[标签测试] 已提交给 " + (primaryNpc.Name ?? "NPC") + "，标签数：" + CountDeveloperTagTestTags(content), new Color(0.4f, 1f, 0.4f)));
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTagTest", "[ERROR] submit failed: " + ex);
			InformationManager.DisplayMessage(new InformationMessage("[标签测试] 执行失败：" + ex.Message, new Color(1f, 0.35f, 0.25f)));
		}
		finally
		{
			_resume();
		}
	}
}
