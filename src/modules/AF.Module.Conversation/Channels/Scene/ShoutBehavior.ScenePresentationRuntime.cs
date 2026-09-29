using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
	// 10 Hz. Validates the held Agent references only; never scans Mission.Agents.
	private void TickPresentationSession(float dt)
	{
		if (!_presentationActive)
		{
			return;
		}
		_presentationTickElapsed += dt;
		if (_presentationTickElapsed < PresentationTickSeconds)
		{
			return;
		}
		_presentationTickElapsed = 0f;
		Mission mission = Mission.Current;
		if (!ReferenceEquals(mission, _presentationMission) || mission == null || mission.MissionEnded)
		{
			EndPresentationSession("mission_changed");
			return;
		}
		if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
		{
			EndPresentationSession("native_conversation");
			return;
		}
		Agent player = Agent.Main;
		if (player == null || !player.IsActive() || player.Health <= 0f)
		{
			EndPresentationSession("player_down");
			return;
		}
		string combatReason = GetPresentationCombatEndReason(mission);
		if (combatReason != null)
		{
			EndPresentationSession(combatReason);
			return;
		}
		bool changed = false;
		// Typing does not pause the mission; any damage hands control straight back to the player.
		if (!_presentationCollapsed && _presentationLastPlayerHealth >= 0f && player.Health < _presentationLastPlayerHealth - 0.01f)
		{
			_presentationCollapsed = true;
			changed = true;
			InformationManager.DisplayMessage(new InformationMessage("[场景会话] 你受到了攻击，会话面板已收起（按 T 展开）。", new Color(1f, 0.55f, 0.3f)));
		}
		_presentationLastPlayerHealth = player.Health;
		GetConfiguredShoutRange(out var _, out var maxRange);
		float maxRangeSquared = maxRange * maxRange;
		Vec3 playerPosition = player.Position;
		for (int i = _presentationMembers.Count - 1; i >= 0; i--)
		{
			ScenePresentationMember member = _presentationMembers[i];
			if (!IsUsablePresentationAgent(member.Agent, mission))
			{
				_presentationMembers.RemoveAt(i);
				changed = true;
				continue;
			}
			bool inRange = ScenePresentationPolicy.IsInRange(member.State, member.Agent.Position.DistanceSquared(playerPosition), maxRangeSquared);
			if (inRange != member.InRange)
			{
				member.InRange = inRange;
				changed = true;
			}
		}
		if (!IsPresentationAudience(FindPresentationMember(_presentationAddresseeIndex)))
		{
			int next = ChoosePresentationAddressee();
			if (next < 0)
			{
				EndPresentationSession("no_participants");
				InformationManager.DisplayMessage(new InformationMessage("[场景会话] 周围已经没有可交谈的人，会话结束。", new Color(0.8f, 0.75f, 0.6f)));
				return;
			}
			_presentationAddresseeIndex = next;
			ActivateMultiSceneMovementSuppression(new int[1] { next });
			changed = true;
		}
		long fingerprint = ComputePresentationHistoryFingerprint();
		if (fingerprint != _presentationHistoryFingerprint)
		{
			_presentationHistoryFingerprint = fingerprint;
			changed = true;
		}
		if (changed)
		{
			BumpPresentation();
		}
	}

	// Real fights only (runs at the 10 Hz session tick, no agent scan). A team-hostile bystander in a
	// peaceful town is not a fight; hostile members already drop out via IsUsablePresentationAgent/range.
	private static string GetPresentationCombatEndReason(Mission mission)
	{
		if (IsMeetingPseudoCombatContext())
		{
			return "meeting";
		}
		if (IsActiveSceneConversationDuelCombat())
		{
			return "duel";
		}
		try
		{
			if (mission.GetMissionBehavior<SandBox.Missions.MissionLogics.MissionFightHandler>()?.IsThereActiveFight() == true)
			{
				return "fight";
			}
			if (mission.Mode == MissionMode.Battle || mission.Mode == MissionMode.Duel)
			{
				return "battle_mode";
			}
		}
		catch
		{
		}
		return null;
	}

	private long ComputePresentationHistoryFingerprint()
	{
		lock (_historyLock)
		{
			int count = _publicConversationHistory?.Count ?? 0;
			long last = count > 0 ? (_publicConversationHistory[count - 1]?.EventSequence ?? 0L) : 0L;
			return (last << 16) ^ count;
		}
	}

	// The turn's framed audience: every non-excluded member currently in range (locked at any distance).
	// ProcessCurrentScenePlayerShout still adds 10 m bystanders around the addressee and the player.
	private ShoutTargetingContext BuildPresentationTargetingContext()
	{
		GetConfiguredShoutRange(out var _, out var maxRange);
		ShoutTargetingContext context = new ShoutTargetingContext
		{
			RangeMeters = maxRange,
			HalfAngleRadians = ShoutMaxTotalAngleRadians * 0.5f,
			PrimaryAgentIndex = _presentationAddresseeIndex
		};
		foreach (ScenePresentationMember member in _presentationMembers)
		{
			if (!IsPresentationAudience(member))
			{
				continue;
			}
			context.CandidateAgentIndices.Add(member.AgentIndex);
			context.PreviewCandidateAgents.Add(member.Agent);
			if (TryGetPlayerPlanarDistanceMeters(member.Agent, out var distance))
			{
				context.CandidatePlayerDistancesMeters[member.AgentIndex] = distance;
			}
		}
		return context;
	}

	// Scope filter used by TryBuildSceneShoutConversationScope. Null when no session applies.
	private HashSet<int> GetPresentationExcludedAgentIndices()
	{
		if (!IsPresentationSessionLive())
		{
			return null;
		}
		HashSet<int> excluded = null;
		foreach (ScenePresentationMember member in _presentationMembers)
		{
			if (member.State == ScenePresentationParticipantState.Excluded)
			{
				(excluded ??= new HashSet<int>()).Add(member.AgentIndex);
			}
		}
		return excluded;
	}

	// Bystanders who were in this turn's audience join the session list as participating members.
	private void AbsorbPresentationAudience(IReadOnlyList<Agent> audienceAgents)
	{
		if (IsPresentationSessionLive() && AddPresentationMembers(audienceAgents))
		{
			BumpPresentation();
		}
	}

	// Hotkey while a session is open: tap = expand a collapsed panel (collapsing is mouse-only; while the
	// panel is expanded it owns input, so the key never reaches here), hold + release = merge newly framed agents.
	// Returns true when the key was handled here. Works even while an NPC reply is still running.
	private bool UpdatePresentationHotkey(InputKey shoutKey, InputKey specialMenuKey)
	{
		if (_shoutHotkeyChargeActive && _shoutHotkeyChargeMergesIntoSession)
		{
			if (!IsPresentationSessionLive())
			{
				CancelShoutHotkeyCharge("session_ended");
				return true;
			}
			float held = Math.Max(0f, GetApplicationTimeSafe() - _shoutHotkeyChargeStartedAt);
			if (Input.IsKeyReleased(_shoutHotkeyChargeKey))
			{
				ShoutTargetingContext framed = held >= PresentationTapSeconds ? (_lastRenderedShoutTargetingContext ?? BuildCurrentShoutTargetingContext()) : null;
				CancelShoutHotkeyCharge("released");
				if (framed != null)
				{
					MergeFramedIntoPresentation(framed);
				}
				else if (_presentationCollapsed)
				{
					_presentationCollapsed = false;
					BumpPresentation();
				}
				return true;
			}
			if (!Input.IsKeyDown(_shoutHotkeyChargeKey))
			{
				CancelShoutHotkeyCharge("key_state_lost");
				return true;
			}
			if (held >= PresentationTapSeconds)
			{
				DrawShoutRangePreview(BuildCurrentShoutTargetingContext());
			}
			return true;
		}
		if (!IsPresentationSessionLive() || _shoutHotkeyChargeActive)
		{
			return false;
		}
		InputKey pressed = Input.IsKeyPressed(shoutKey) ? shoutKey : Input.IsKeyPressed(specialMenuKey) ? specialMenuKey : InputKey.Invalid;
		if (pressed == InputKey.Invalid || ShouldSuppressShoutHotkeyAfterFocusChange() || !ShoutUtils.IsInValidScene())
		{
			return false;
		}
		_shoutHotkeyChargeActive = true;
		_shoutHotkeyChargeOpenModeMenu = false;
		_shoutHotkeyChargeKey = pressed;
		_shoutHotkeyChargeStartedAt = GetApplicationTimeSafe();
		_lastRenderedShoutTargetingContext = null;
		_shoutHotkeyChargeMergesIntoSession = true;
		return true;
	}

	private void MergeFramedIntoPresentation(ShoutTargetingContext framed)
	{
		List<Agent> agents = GetAgentsForShoutTargetingContext(framed);
		Agent primary = ResolvePrimaryAgentForShoutTargetingContext(framed, agents);
		if (agents.Count == 0)
		{
			_presentationCollapsed = false;
			BumpPresentation();
			return;
		}
		EnsurePresentationSession(agents, primary?.Index ?? _presentationAddresseeIndex);
	}

	// Wheel "交流": open (or merge into) the session instead of the one-shot popup. The mission is not paused.
	private bool TryOpenPresentationSessionFromWheel()
	{
		if (!IsScenePresentationSessionEnabled() || !EnsurePresentationSessionForWheelAction())
		{
			return false;
		}
		ResumeGame();
		return true;
	}

	// Give/show/etc. from the wheel: keep the session, then run the host's own trade flow unchanged.
	private bool EnsurePresentationSessionForWheelAction()
	{
		if (!IsScenePresentationSessionEnabled())
		{
			return false;
		}
		// The 10 Hz tick would end a session opened mid-fight at once; fall back to the one-shot flow instead.
		Mission mission = Mission.Current;
		if (mission == null || GetPresentationCombatEndReason(mission) != null)
		{
			return false;
		}
		List<Agent> framed = GetAgentsForShoutTargetingContext(_activeShoutTargetingContext);
		Agent primary = ResolvePrimaryAgentForShoutTargetingContext(_activeShoutTargetingContext, framed);
		return EnsurePresentationSession(framed, primary?.Index ?? -1);
	}

	private List<ScenePresentationHistoryLine> BuildPresentationHistory(int maxLines)
	{
		List<ScenePresentationHistoryLine> lines = new List<ScenePresentationHistoryLine>();
		int context = 0;
		lock (_historyLock)
		{
			for (int i = (_publicConversationHistory?.Count ?? 0) - 1; i >= 0 && lines.Count < maxLines; i--)
			{
				ConversationMessage message = _publicConversationHistory[i];
				if (message == null)
				{
					continue;
				}
				if (message.EventSequence < _presentationStartSequence && ++context > PresentationContextLines)
				{
					break;
				}
				ScenePresentationHistoryLine line = BuildPresentationHistoryLine(message);
				if (line != null)
				{
					lines.Add(line);
				}
			}
		}
		lines.Reverse();
		return lines;
	}

	private static ScenePresentationHistoryLine BuildPresentationHistoryLine(ConversationMessage message)
	{
		string role = (message.Role ?? "").Trim();
		string content = (message.Content ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			return null;
		}
		if (role.Equals("user", StringComparison.OrdinalIgnoreCase))
		{
			string target = (message.TargetName ?? "").Trim();
			return new ScenePresentationHistoryLine { Speaker = string.IsNullOrWhiteSpace(target) ? "你" : "你 → " + target, Text = content, Kind = "player" };
		}
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
		{
			return new ScenePresentationHistoryLine { Speaker = (message.SpeakerName ?? "NPC").Trim(), Text = content, Kind = "npc" };
		}
		// Only confirmed AFEF facts are shown; scene descriptions and prompt scaffolding stay internal.
		return TryNormalizeAfefFactLineForPrompt(content, out var fact)
			? new ScenePresentationHistoryLine { Speaker = "记录", Text = fact, Kind = "fact" }
			: null;
	}
}
