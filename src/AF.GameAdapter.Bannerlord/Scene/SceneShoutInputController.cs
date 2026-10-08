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

using SceneGroupReceipt = AnimusForge.SceneConversationSessionRuntime.SceneGroupReceipt;
using static AnimusForge.ShoutBehavior;
namespace AnimusForge;

internal sealed class SceneShoutInputController
{
    private readonly SceneShoutInputControllerPorts _ports;
    internal SceneShoutInputController(SceneShoutInputControllerPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

    private MultiSelectionInquiryData _modeInquiry;
    private Mission _modeInquiryMission;
    private Campaign _modeInquiryCampaign;
    private ShoutTargetingContext _modeInquiryTargeting;
    private long _modeInquiryRevision, _modeInquiryGeneration;
    private int _modeInquiryEpoch;
    private Action<List<InquiryElement>> _modeInquiryAffirmative, _modeInquiryNegative;

    // Exact menu ownership, not a compiler-generated closure name. Queried only on
    // presentation/selection; no tick scan, reflection, or second menu state machine.
    internal bool OwnsShoutModeInquiry(MultiSelectionInquiryData data)
    {
        return data != null && ReferenceEquals(_modeInquiry, data)
            && ReferenceEquals(_modeInquiryCampaign, Campaign.Current)
            && ReferenceEquals(Campaign.Current?.GetCampaignBehavior<ShoutBehavior>()?._j17SceneShoutInputController, this)
            && ReferenceEquals(_modeInquiryMission, Mission.Current) && Mission.Current != null && !Mission.Current.IsMissionEnding
            && Mission.Current.Mode != MissionMode.Conversation
            && Campaign.Current?.ConversationManager?.IsConversationInProgress != true
            && SaveRuntimeGuard.IsCurrentGeneration(_modeInquiryGeneration)
            && _modeInquiryEpoch == _sceneConversationEpoch
            && ReferenceEquals(_modeInquiryTargeting, _activeShoutTargetingContext)
            && ReferenceEquals(data.AffirmativeAction, _modeInquiryAffirmative)
            && ReferenceEquals(data.NegativeAction, _modeInquiryNegative);
    }

    private bool ConsumeShoutModeInquiry(MultiSelectionInquiryData data, long revision)
    {
        if (revision != _modeInquiryRevision || !OwnsShoutModeInquiry(data)) return false;
        _modeInquiry = null;
        _modeInquiryMission = null;
        _modeInquiryCampaign = null;
        _modeInquiryTargeting = null;
        _modeInquiryAffirmative = null;
        _modeInquiryNegative = null;
        return true;
    }

	internal bool _isProcessingShout = false;

	internal float _shoutProcessingStartedAt = -1f;

	internal const float ShoutProcessingFailsafeSeconds = 180f;

	internal const float ProactiveSceneOpeningProbeIntervalSeconds = 0.5f;

	internal const int ShoutHotkeyFocusDebounceMilliseconds = 1200;

	internal const int ShoutProcessingBusyMessageCooldownMilliseconds = 1500;

	internal const float ShoutChargeSecondsToMax = 4f;

	internal const float ShoutHardMaxRangeMeters = 150f;

	internal const float ShoutMinRangeMeters = 1f;

	internal const float ShoutInitialTotalAngleRadians = 1.5707964f;

	internal const float ShoutMaxTotalAngleRadians = 5.2359877f;

	internal const float ShoutPreviewArcSegmentLengthMeters = 1.4f;

	internal const float ShoutPreviewRadialSegmentLengthMeters = 1.6f;

	internal const float ShoutPreviewLineLengthScaleMultiplier = 2.4f;

	internal const float ShoutPreviewLineLengthOverlapScale = 1.2f;

	internal const float ShoutPreviewLineWidthScale = 1.6f;

	internal const float ShoutPreviewLineHeightScale = 0.38f;

	internal const int ShoutPreviewMinArcSegments = 36;

	internal const int ShoutPreviewMaxArcSegments = 520;

	internal const int ShoutPreviewMinRadialSegments = 6;

	internal const int ShoutPreviewMaxRadialSegments = 96;

	internal const int ShoutPreviewMaxMarkerCount = 760;

	internal const int ShoutPreviewMaxMarkerCreatesPerTick = 220;

	internal const int ShoutPreviewMarkerRemoveReason = 95;

	internal const float ShoutPreviewMarkerGroundOffset = 0.9f;

	internal static readonly uint ShoutPreviewMarkerTintColor = new Color(0.08f, 1f, 1f, 1f).ToUnsignedInteger();

	internal static readonly uint ShoutPreviewAgentHighlightColor = new Color(1f, 0.84f, 0.2f, 1f).ToUnsignedInteger();

	internal static readonly uint ShoutPreviewPrimaryAgentHighlightColor = new Color(1f, 0.22f, 0.72f, 1f).ToUnsignedInteger();

	internal const string ShoutPreviewMarkerItemId = "animusforge_denar_ingot_item";

	internal const string ShoutPreviewSecondaryMarkerItemId = "animusforge_denar_coin_item";

	internal const string ShoutPreviewFallbackMarkerItemId = "sling_leadammo";

	internal long _suppressShoutHotkeyUntilUtcTicks = 0L;

	internal long _lastShoutProcessingBusyMessageUtcTicks = 0L;

	internal bool _shoutHotkeyChargeActive = false;

	internal bool _shoutHotkeyChargeOpenModeMenu = false;

	internal InputKey _shoutHotkeyChargeKey = InputKey.Invalid;

	internal float _shoutHotkeyChargeStartedAt = -1f;

	internal ShoutTargetingContext _activeShoutTargetingContext = null;

	internal ShoutTargetingContext _lastRenderedShoutTargetingContext = null;

	internal readonly List<GameEntity> _shoutPreviewMarkerEntities = new List<GameEntity>();

	internal readonly Dictionary<int, ShoutPreviewAgentHighlightState> _shoutPreviewAgentHighlightStates = new Dictionary<int, ShoutPreviewAgentHighlightState>();

	internal readonly HashSet<int> _shoutPreviewDesiredAgentIndicesScratch = new HashSet<int>();

	internal readonly List<int> _shoutPreviewStaleAgentIndicesScratch = new List<int>();

	internal ItemObject _shoutPreviewMarkerItemObject = null;

	internal bool _shoutPreviewMarkerCreationFailed = false;

	internal static float GetApplicationTimeSafe()
	{
		try
		{
			return Time.ApplicationTime;
		}
		catch
		{
			return (float)Environment.TickCount / 1000f;
		}
	}

	internal void UpdateShoutHotkeyCharge(InputKey shoutKey, InputKey specialMenuKey)
	{
		// Battles still use hold/release targeting; only a targetless release bypasses the wheel.
		if (!IsSceneIllustrationBattleForExternal && UpdatePresentationHotkey(shoutKey, specialMenuKey))
		{
			return;
		}
		if (_shoutHotkeyChargeActive)
		{
			if (_isProcessingShout || _isWaitingForScenePostprocessGate)
			{
				CancelShoutHotkeyCharge("processing");
				return;
			}
			if (!ShoutUtils.IsInValidScene())
			{
				CancelShoutHotkeyCharge("invalid_scene");
				return;
			}
			if (Input.IsKeyReleased(_shoutHotkeyChargeKey))
			{
				ShoutTargetingContext targetingContext = _lastRenderedShoutTargetingContext ?? BuildCurrentShoutTargetingContext();
				bool openModeMenu = _shoutHotkeyChargeOpenModeMenu;
				CancelShoutHotkeyCharge("released");
				TryStartShoutFromHotkey(openModeMenu, targetingContext);
				return;
			}
			if (!Input.IsKeyDown(_shoutHotkeyChargeKey))
			{
				CancelShoutHotkeyCharge("key_state_lost");
				return;
			}
			DrawShoutRangePreview(BuildCurrentShoutTargetingContext());
			return;
		}
		if (Input.IsKeyPressed(specialMenuKey))
		{
			TryBeginShoutHotkeyCharge(specialMenuKey, openModeMenu: true);
		}
		else if (Input.IsKeyPressed(shoutKey))
		{
			TryBeginShoutHotkeyCharge(shoutKey, openModeMenu: false);
		}
	}

	internal void TryBeginShoutHotkeyCharge(InputKey key, bool openModeMenu)
	{
		if (ShouldSuppressShoutHotkeyAfterFocusChange())
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_focus_debounce_ignored", () => "[Hotkey] ignored during focus debounce", 1.0);
			return;
		}
		if (_isProcessingShout || _isWaitingForScenePostprocessGate)
		{
			TryShowShoutProcessingBusyMessage();
			return;
		}
		if (!ShoutUtils.IsInValidScene())
		{
			return;
		}
		_shoutHotkeyChargeActive = true;
		_shoutHotkeyChargeOpenModeMenu = openModeMenu;
		_shoutHotkeyChargeKey = key;
		_shoutHotkeyChargeStartedAt = GetApplicationTimeSafe();
		ResetPassiveStareTracking();
		_lastRenderedShoutTargetingContext = null;
		DrawShoutRangePreview(BuildCurrentShoutTargetingContext());
	}

	internal void CancelShoutHotkeyCharge(string reason)
	{
		if (_shoutHotkeyChargeActive)
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_charge_cancel:" + (reason ?? ""), () => "[Hotkey] charge cancelled reason=" + (reason ?? ""), 1.0);
		}
		_shoutHotkeyChargeActive = false;
		_shoutHotkeyChargeOpenModeMenu = false;
		_shoutHotkeyChargeMergesIntoSession = false;
		_shoutHotkeyChargeKey = InputKey.Invalid;
		_shoutHotkeyChargeStartedAt = -1f;
		ClearShoutRangePreviewEntities();
		ClearShoutPreviewAgentHighlights();
		_lastRenderedShoutTargetingContext = null;
	}

	internal ShoutTargetingContext BuildCurrentShoutTargetingContext()
	{
		float elapsedSeconds = 0f;
		if (_shoutHotkeyChargeStartedAt >= 0f)
		{
			elapsedSeconds = Math.Max(0f, GetApplicationTimeSafe() - _shoutHotkeyChargeStartedAt);
		}
		return BuildShoutTargetingContext(elapsedSeconds);
	}

	internal static void GetConfiguredShoutRange(out float initialRange, out float maxRange)
	{
		initialRange = 4f;
		maxRange = ShoutHardMaxRangeMeters;
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				initialRange = settings.ShoutInitialRangeMeters;
				maxRange = settings.ShoutMaxRangeMeters;
			}
		}
		catch
		{
			initialRange = 4f;
			maxRange = ShoutHardMaxRangeMeters;
		}
		if (float.IsNaN(initialRange) || float.IsInfinity(initialRange))
		{
			initialRange = 4f;
		}
		if (float.IsNaN(maxRange) || float.IsInfinity(maxRange))
		{
			maxRange = ShoutHardMaxRangeMeters;
		}
		initialRange = Math.Max(ShoutMinRangeMeters, Math.Min(ShoutHardMaxRangeMeters, initialRange));
		maxRange = Math.Max(initialRange, Math.Min(ShoutHardMaxRangeMeters, maxRange));
	}

	internal ShoutTargetingContext BuildShoutTargetingContext(float elapsedSeconds)
	{
		GetConfiguredShoutRange(out var initialRange, out var maxRange);
		float progress = Math.Max(0f, Math.Min(1f, elapsedSeconds / ShoutChargeSecondsToMax));
		progress *= progress;
		float range = initialRange + (maxRange - initialRange) * progress;
		float totalAngle = ShoutInitialTotalAngleRadians + (ShoutMaxTotalAngleRadians - ShoutInitialTotalAngleRadians) * progress;
		float halfAngle = totalAngle * 0.5f;
		List<Agent> agents = ShoutUtils.GetNearbyNPCAgents(range, halfAngle) ?? new List<Agent>();
		Agent primary = ShoutUtils.GetMostCenteredAgent(agents) ?? agents.FirstOrDefault();
		Dictionary<int, float> candidateDistances = new Dictionary<int, float>();
		foreach (Agent agent in agents)
		{
			if (agent == null)
			{
				continue;
			}
			if (TryGetPlayerPlanarDistanceMeters(agent, out var distanceMeters))
			{
				candidateDistances[agent.Index] = distanceMeters;
			}
		}
		return new ShoutTargetingContext
		{
			RangeMeters = range,
			HalfAngleRadians = halfAngle,
			PrimaryAgentIndex = primary?.Index ?? (-1),
			CandidateAgentIndices = agents.Where((Agent agent) => agent != null).Select((Agent agent) => agent.Index).Distinct().ToList(),
			CandidatePlayerDistancesMeters = candidateDistances,
			PreviewCandidateAgents = agents
		};
	}

	internal static bool TryGetPlayerPlanarDistanceMeters(Agent targetAgent, out float distanceMeters)
	{
		distanceMeters = 0f;
		if (targetAgent == null || !targetAgent.IsActive() || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		try
		{
			float distanceSquared = targetAgent.Position.AsVec2.DistanceSquared(Agent.Main.Position.AsVec2);
			if (float.IsNaN(distanceSquared) || float.IsInfinity(distanceSquared) || distanceSquared < 0f)
			{
				return false;
			}
			distanceMeters = (float)Math.Sqrt(distanceSquared);
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal void DrawShoutRangePreview(ShoutTargetingContext targetingContext)
	{
		if (targetingContext == null || Mission.Current?.Scene == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			_lastRenderedShoutTargetingContext = null;
			ClearShoutPreviewAgentHighlights();
			return;
		}
		try
		{
			Vec2 forward = Agent.Main.LookDirection.AsVec2;
			if (forward.LengthSquared <= 1E-05f)
			{
				_lastRenderedShoutTargetingContext = null;
				ClearShoutPreviewAgentHighlights();
				return;
			}
			forward.Normalize();
			float forwardAngle = (float)Math.Atan2(forward.y, forward.x);
			float startAngle = forwardAngle - targetingContext.HalfAngleRadians;
			float endAngle = forwardAngle + targetingContext.HalfAngleRadians;
			Vec3 center = GetShoutPreviewGroundPoint(Agent.Main.Position);
			List<ShoutPreviewLineSegment> previewSegments = BuildShoutPreviewLineSegments(center, targetingContext.RangeMeters, startAngle, endAngle);
			UpdateShoutPreviewLineEntities(previewSegments);
			UpdateShoutPreviewAgentHighlights(targetingContext);
			_lastRenderedShoutTargetingContext = targetingContext;
		}
		catch (Exception ex)
		{
			_lastRenderedShoutTargetingContext = null;
			ClearShoutPreviewAgentHighlights();
			Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_failed", () => "[Hotkey] range preview failed: " + ex.Message, 2.0);
		}
	}

	internal void UpdateShoutPreviewAgentHighlights(ShoutTargetingContext targetingContext)
	{
		_shoutPreviewDesiredAgentIndicesScratch.Clear();
		_shoutPreviewStaleAgentIndicesScratch.Clear();
		List<Agent> previewCandidateAgents = targetingContext?.PreviewCandidateAgents;
		int primaryAgentIndex = targetingContext?.PrimaryAgentIndex ?? (-1);
		if (previewCandidateAgents != null)
		{
			for (int i = 0; i < previewCandidateAgents.Count; i++)
			{
				Agent agent = previewCandidateAgents[i];
				if (agent == null || !agent.IsActive() || agent.Index < 0 || !_shoutPreviewDesiredAgentIndicesScratch.Add(agent.Index))
				{
					continue;
				}
				ShoutPreviewAgentHighlightRole role = ((agent.Index == primaryAgentIndex) ? ShoutPreviewAgentHighlightRole.Primary : ShoutPreviewAgentHighlightRole.Candidate);
				uint color = ((role == ShoutPreviewAgentHighlightRole.Primary) ? ShoutPreviewPrimaryAgentHighlightColor : ShoutPreviewAgentHighlightColor);
				if (_shoutPreviewAgentHighlightStates.TryGetValue(agent.Index, out var state))
				{
					if (ReferenceEquals(state.Agent, agent) && state.Role == role)
					{
						continue;
					}
					if (!ReferenceEquals(state.Agent, agent))
					{
						TrySetShoutPreviewAgentHighlight(state.Agent, null);
					}
					if (TrySetShoutPreviewAgentHighlight(agent, color))
					{
						_shoutPreviewAgentHighlightStates[agent.Index] = new ShoutPreviewAgentHighlightState
						{
							Agent = agent,
							Role = role
						};
					}
					continue;
				}
				if (TrySetShoutPreviewAgentHighlight(agent, color))
				{
					_shoutPreviewAgentHighlightStates.Add(agent.Index, new ShoutPreviewAgentHighlightState
					{
						Agent = agent,
						Role = role
					});
				}
			}
		}
		foreach (KeyValuePair<int, ShoutPreviewAgentHighlightState> highlightState in _shoutPreviewAgentHighlightStates)
		{
			if (!_shoutPreviewDesiredAgentIndicesScratch.Contains(highlightState.Key))
			{
				_shoutPreviewStaleAgentIndicesScratch.Add(highlightState.Key);
			}
		}
		for (int j = 0; j < _shoutPreviewStaleAgentIndicesScratch.Count; j++)
		{
			int agentIndex = _shoutPreviewStaleAgentIndicesScratch[j];
			if (_shoutPreviewAgentHighlightStates.TryGetValue(agentIndex, out var staleState))
			{
				TrySetShoutPreviewAgentHighlight(staleState.Agent, null);
				_shoutPreviewAgentHighlightStates.Remove(agentIndex);
			}
		}
	}

	internal void ClearShoutPreviewAgentHighlights()
	{
		if (_shoutPreviewAgentHighlightStates.Count == 0)
		{
			_shoutPreviewDesiredAgentIndicesScratch.Clear();
			_shoutPreviewStaleAgentIndicesScratch.Clear();
			return;
		}
		foreach (ShoutPreviewAgentHighlightState state in _shoutPreviewAgentHighlightStates.Values)
		{
			TrySetShoutPreviewAgentHighlight(state.Agent, null);
		}
		_shoutPreviewAgentHighlightStates.Clear();
		_shoutPreviewDesiredAgentIndicesScratch.Clear();
		_shoutPreviewStaleAgentIndicesScratch.Clear();
	}

	internal static bool TrySetShoutPreviewAgentHighlight(Agent agent, uint? color)
	{
		if (agent == null)
		{
			return false;
		}
		try
		{
			if (color.HasValue && !agent.IsActive())
			{
				return false;
			}
			if (agent.AgentVisuals == null)
			{
				return false;
			}
			agent.AgentVisuals.SetContourColor(color);
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static List<ShoutPreviewLineSegment> BuildShoutPreviewLineSegments(Vec3 center, float range, float startAngle, float endAngle)
	{
		float totalAngle = Math.Max(0.1f, Math.Abs(endAngle - startAngle));
		int arcSegments = ClampShoutPreviewSegmentCount((int)Math.Ceiling(range * totalAngle / ShoutPreviewArcSegmentLengthMeters), ShoutPreviewMinArcSegments, ShoutPreviewMaxArcSegments);
		int radialSegments = ClampShoutPreviewSegmentCount((int)Math.Ceiling(range / ShoutPreviewRadialSegmentLengthMeters), ShoutPreviewMinRadialSegments, ShoutPreviewMaxRadialSegments);
		List<ShoutPreviewLineSegment> segments = new List<ShoutPreviewLineSegment>(Math.Min(ShoutPreviewMaxMarkerCount, arcSegments + radialSegments * 2));
		AddShoutPreviewArcSegments(segments, center, range, startAngle, endAngle, arcSegments);
		AddShoutPreviewBoundarySegments(segments, center, range, startAngle, radialSegments);
		AddShoutPreviewBoundarySegments(segments, center, range, endAngle, radialSegments);
		return segments;
	}

	internal static int ClampShoutPreviewSegmentCount(int value, int minValue, int maxValue)
	{
		return Math.Max(minValue, Math.Min(maxValue, value));
	}

	internal static void AddShoutPreviewArcSegments(List<ShoutPreviewLineSegment> segments, Vec3 center, float range, float startAngle, float endAngle, int arcSegments)
	{
		if (segments == null || arcSegments <= 0 || range <= 0f)
		{
			return;
		}
		for (int i = 0; i < arcSegments; i++)
		{
			if (segments.Count >= ShoutPreviewMaxMarkerCount)
			{
				return;
			}
			float t = (float)i / arcSegments;
			float t2 = (float)(i + 1) / arcSegments;
			segments.Add(new ShoutPreviewLineSegment
			{
				Start = GetShoutPreviewArcPoint(center, range, startAngle + (endAngle - startAngle) * t),
				End = GetShoutPreviewArcPoint(center, range, startAngle + (endAngle - startAngle) * t2)
			});
		}
	}

	internal static void AddShoutPreviewBoundarySegments(List<ShoutPreviewLineSegment> segments, Vec3 center, float range, float angle, int radialSegments)
	{
		if (segments == null || radialSegments <= 0 || range <= 0f)
		{
			return;
		}
		for (int i = 0; i < radialSegments; i++)
		{
			if (segments.Count >= ShoutPreviewMaxMarkerCount)
			{
				return;
			}
			float startDistance = range * i / radialSegments;
			float endDistance = range * (i + 1) / radialSegments;
			if (endDistance <= 0.05f)
			{
				continue;
			}
			segments.Add(new ShoutPreviewLineSegment
			{
				Start = GetShoutPreviewArcPoint(center, startDistance, angle),
				End = GetShoutPreviewArcPoint(center, endDistance, angle)
			});
		}
	}

	internal static Vec3 GetShoutPreviewArcPoint(Vec3 center, float range, float angle)
	{
		Vec3 point = new Vec3(center.x + (float)Math.Cos(angle) * range, center.y + (float)Math.Sin(angle) * range, center.z, -1f);
		return GetShoutPreviewGroundPoint(point);
	}

	internal static Vec3 GetShoutPreviewGroundPoint(Vec3 point)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission?.Scene != null)
			{
				point.z = mission.Scene.GetGroundHeightAtPosition(point, BodyFlags.CommonCollisionExcludeFlags) + ShoutPreviewMarkerGroundOffset;
			}
			else
			{
				point.z += ShoutPreviewMarkerGroundOffset;
			}
		}
		catch
		{
			point.z += ShoutPreviewMarkerGroundOffset;
		}
		return point;
	}

	internal void UpdateShoutPreviewLineEntities(List<ShoutPreviewLineSegment> segments)
	{
		if (segments == null || segments.Count == 0)
		{
			HideExtraShoutPreviewMarkerEntities(0);
			return;
		}
		int desiredCount = Math.Min(segments.Count, ShoutPreviewMaxMarkerCount);
		EnsureShoutPreviewMarkerCount(desiredCount, segments[0].Start);
		int visibleCount = Math.Min(desiredCount, _shoutPreviewMarkerEntities.Count);
		for (int i = 0; i < visibleCount; i++)
		{
			GameEntity entity = _shoutPreviewMarkerEntities[i];
			if (entity == null)
			{
				continue;
			}
			try
			{
				if (TryBuildShoutPreviewLineFrame(segments[i], out var frame))
				{
					entity.SetFrame(ref frame);
					entity.SetVisibilityExcludeParents(true);
				}
				else
				{
					entity.SetVisibilityExcludeParents(false);
				}
			}
			catch
			{
			}
		}
		HideExtraShoutPreviewMarkerEntities(visibleCount);
	}

	internal void EnsureShoutPreviewMarkerCount(int desiredCount, Vec3 spawnPosition)
	{
		desiredCount = Math.Min(Math.Max(0, desiredCount), ShoutPreviewMaxMarkerCount);
		int createdThisTick = 0;
		while (!_shoutPreviewMarkerCreationFailed && _shoutPreviewMarkerEntities.Count < desiredCount && createdThisTick < ShoutPreviewMaxMarkerCreatesPerTick)
		{
			GameEntity entity = TryCreateShoutPreviewMarkerEntity(spawnPosition);
			if (entity == null)
			{
				_shoutPreviewMarkerCreationFailed = true;
				Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_marker_create_failed", () => "[Hotkey] range preview marker creation failed", 2.0);
				break;
			}
			_shoutPreviewMarkerEntities.Add(entity);
			createdThisTick++;
		}
	}

	internal static bool TryBuildShoutPreviewLineFrame(ShoutPreviewLineSegment segment, out MatrixFrame frame)
	{
		frame = MatrixFrame.Identity;
		Vec3 delta = new Vec3(segment.End.x - segment.Start.x, segment.End.y - segment.Start.y, segment.End.z - segment.Start.z, -1f);
		float length = (float)Math.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
		if (float.IsNaN(length) || float.IsInfinity(length) || length < 0.05f)
		{
			return false;
		}
		Vec3 line = new Vec3(delta.x / length, delta.y / length, delta.z / length, -1f);
		Vec3 up = new Vec3(0f, 0f, 1f, -1f);
		Vec3 widthAxis = CrossVec3(up, line);
		if (!TryNormalizeVec3(ref widthAxis))
		{
			widthAxis = new Vec3(-line.y, line.x, 0f, -1f);
			if (!TryNormalizeVec3(ref widthAxis))
			{
				widthAxis = new Vec3(0f, 1f, 0f, -1f);
			}
		}
		Vec3 adjustedUp = CrossVec3(line, widthAxis);
		if (!TryNormalizeVec3(ref adjustedUp))
		{
			adjustedUp = up;
		}
		frame.origin = new Vec3((segment.Start.x + segment.End.x) * 0.5f, (segment.Start.y + segment.End.y) * 0.5f, (segment.Start.z + segment.End.z) * 0.5f, -1f);
		frame.rotation.s = ScaleVec3(line, Math.Max(0.25f, length * ShoutPreviewLineLengthScaleMultiplier + ShoutPreviewLineLengthOverlapScale));
		frame.rotation.f = ScaleVec3(widthAxis, ShoutPreviewLineWidthScale);
		frame.rotation.u = ScaleVec3(adjustedUp, ShoutPreviewLineHeightScale);
		return true;
	}

	internal static Vec3 CrossVec3(Vec3 a, Vec3 b)
	{
		return new Vec3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x, -1f);
	}

	internal static Vec3 ScaleVec3(Vec3 value, float scale)
	{
		return new Vec3(value.x * scale, value.y * scale, value.z * scale, -1f);
	}

	internal static bool TryNormalizeVec3(ref Vec3 value)
	{
		float lengthSquared = value.x * value.x + value.y * value.y + value.z * value.z;
		if (float.IsNaN(lengthSquared) || float.IsInfinity(lengthSquared) || lengthSquared <= 1E-06f)
		{
			return false;
		}
		float invLength = 1f / (float)Math.Sqrt(lengthSquared);
		value = new Vec3(value.x * invLength, value.y * invLength, value.z * invLength, -1f);
		return true;
	}

	internal void HideExtraShoutPreviewMarkerEntities(int visibleCount)
	{
		for (int i = visibleCount; i < _shoutPreviewMarkerEntities.Count; i++)
		{
			try
			{
				_shoutPreviewMarkerEntities[i]?.SetVisibilityExcludeParents(false);
			}
			catch
			{
			}
		}
	}

	internal GameEntity TryCreateShoutPreviewMarkerEntity(Vec3 spawnPosition)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission?.Scene == null)
			{
				return null;
			}
			ItemObject markerItem = TryGetShoutPreviewMarkerItemObject();
			if (markerItem == null)
			{
				return null;
			}
			MatrixFrame frame = MatrixFrame.Identity;
			frame.origin = spawnPosition;
			frame.rotation.ApplyScaleLocal(ShoutPreviewLineWidthScale);
			MissionWeapon missionWeapon = new MissionWeapon(markerItem, null, null, 1);
			GameEntity entity = mission.SpawnWeaponWithNewEntity(ref missionWeapon, Mission.WeaponSpawnFlags.WithStaticPhysics | Mission.WeaponSpawnFlags.CannotBePickedUp, frame);
			if (entity == null)
			{
				return null;
			}
			entity.Name = "animusforge_shout_range_marker";
			entity.EntityFlags |= EntityFlags.DontSaveToScene | EntityFlags.PhysicsDisabled;
			entity.SetMobility(GameEntity.Mobility.Stationary);
			try
			{
				entity.SetPhysicsState(false, setChildren: true);
			}
			catch
			{
			}
			TryTintShoutPreviewMarker(entity);
			entity.SetFrame(ref frame);
			entity.SetVisibilityExcludeParents(true);
			return entity;
		}
		catch (Exception ex)
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_marker_create_exception", () => "[Hotkey] range preview marker creation exception: " + ex.Message, 2.0);
			return null;
		}
	}

	internal ItemObject TryGetShoutPreviewMarkerItemObject()
	{
		if (_shoutPreviewMarkerItemObject != null)
		{
			return _shoutPreviewMarkerItemObject;
		}
		try
		{
			var objectManager = Game.Current?.ObjectManager;
			_shoutPreviewMarkerItemObject = objectManager?.GetObject<ItemObject>(ShoutPreviewMarkerItemId) ?? objectManager?.GetObject<ItemObject>(ShoutPreviewSecondaryMarkerItemId) ?? objectManager?.GetObject<ItemObject>(ShoutPreviewFallbackMarkerItemId);
			if (_shoutPreviewMarkerItemObject != null)
			{
				Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_marker_item:" + _shoutPreviewMarkerItemObject.StringId, () => "[Hotkey] range preview marker item=" + _shoutPreviewMarkerItemObject.StringId, 1.0);
			}
			return _shoutPreviewMarkerItemObject;
		}
		catch (Exception ex)
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_charge_preview_marker_item_lookup_failed", () => "[Hotkey] range preview marker item lookup failed: " + ex.Message, 2.0);
			return null;
		}
	}

	internal static void TryTintShoutPreviewMarker(GameEntity entity)
	{
		if (entity == null)
		{
			return;
		}
		try
		{
			entity.SetContourColor(ShoutPreviewMarkerTintColor);
			entity.SetVectorArgument(0.08f, 1f, 1f, 1f);
			entity.SetColor(ShoutPreviewMarkerTintColor, ShoutPreviewMarkerTintColor, "");
			for (int i = 0; i < 8; i++)
			{
				MetaMesh metaMesh = entity.GetMetaMesh(i);
				if (metaMesh == null)
				{
					continue;
				}
				metaMesh.SetFactor1(ShoutPreviewMarkerTintColor);
				metaMesh.SetFactor2(ShoutPreviewMarkerTintColor);
				metaMesh.SetGlossMultiplier(6f);
			}
		}
		catch
		{
		}
	}

	internal void ClearShoutRangePreviewEntities()
	{
		if (_shoutPreviewMarkerEntities.Count == 0)
		{
			return;
		}
		foreach (GameEntity entity in _shoutPreviewMarkerEntities)
		{
			try
			{
				entity?.Remove(ShoutPreviewMarkerRemoveReason);
			}
			catch
			{
			}
		}
		_shoutPreviewMarkerEntities.Clear();
		_shoutPreviewMarkerCreationFailed = false;
	}

	internal List<Agent> GetAgentsForShoutTargetingContext(ShoutTargetingContext targetingContext)
	{
		if (targetingContext == null)
		{
			return ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>();
		}
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (targetingContext.CandidateAgentIndices == null || targetingContext.CandidateAgentIndices.Count == 0 || agents == null)
		{
			return new List<Agent>();
		}
		HashSet<int> wanted = new HashSet<int>(targetingContext.CandidateAgentIndices);
		Dictionary<int, Agent> snapshotAgents = new Dictionary<int, Agent>();
		if (targetingContext.PreviewCandidateAgents != null)
		{
			for (int i = 0; i < targetingContext.PreviewCandidateAgents.Count; i++)
			{
				Agent snapshotAgent = targetingContext.PreviewCandidateAgents[i];
				if (snapshotAgent != null && wanted.Contains(snapshotAgent.Index) && !snapshotAgents.ContainsKey(snapshotAgent.Index))
				{
					snapshotAgents.Add(snapshotAgent.Index, snapshotAgent);
				}
			}
		}
		Dictionary<int, Agent> liveAgents = new Dictionary<int, Agent>();
		foreach (Agent agent in agents)
		{
			if (agent != null && wanted.Contains(agent.Index) && agent != Agent.Main && agent.IsActive() && agent.IsHuman && !RichExecutions.Core.VengeanceIntegration.IsExecutedVictim(agent) && (!snapshotAgents.TryGetValue(agent.Index, out var snapshotAgent2) || ReferenceEquals(snapshotAgent2, agent)))
			{
				liveAgents[agent.Index] = agent;
			}
		}
		List<Agent> result = new List<Agent>();
		foreach (int agentIndex in targetingContext.CandidateAgentIndices)
		{
			if (liveAgents.TryGetValue(agentIndex, out var agent))
			{
				result.Add(agent);
			}
		}
		return result;
	}

	internal static Agent ResolvePrimaryAgentForShoutTargetingContext(ShoutTargetingContext targetingContext, List<Agent> agents)
	{
		if (targetingContext == null || targetingContext.PrimaryAgentIndex < 0 || agents == null)
		{
			return null;
		}
		return agents.FirstOrDefault((Agent a) => a != null && a.Index == targetingContext.PrimaryAgentIndex);
	}

	internal static bool TryBuildSceneShoutConversationScope(List<Agent> framedAgents, Agent primaryAgent, int conversationEpoch, out SceneShoutConversationScope scope, out List<Agent> audienceAgents, HashSet<int> excludedAgentIndices = null, bool excludeUnframedAgents = false)
	{
		if (excludedAgentIndices != null && primaryAgent != null)
		{
			excludedAgentIndices.Remove(primaryAgent.Index);
			framedAgents = framedAgents?.Where(agent => agent != null && !excludedAgentIndices.Contains(agent.Index)).ToList();
		}
		scope = null;
		audienceAgents = new List<Agent>();
		Mission mission = Mission.Current;
		Agent playerAgent = Agent.Main;
		var missionAgents = mission?.Agents;
		if (mission == null || missionAgents == null || playerAgent == null || primaryAgent == null || !playerAgent.IsActive() || !primaryAgent.IsActive())
		{
			return false;
		}
		Vec3 primaryAnchorPosition = primaryAgent.Position;
		Vec3 playerAnchorPosition = playerAgent.Position;
		List<Agent> primaryAnchorAgents = new List<Agent>();
		List<Agent> playerAnchorAgents = new List<Agent>();
		// In restricted mode, retain anchor/LOS metadata for invited agents without scanning bystanders.
		IEnumerable<Agent> anchorCandidates = excludeUnframedAgents
			? ((IEnumerable<Agent>)framedAgents ?? Enumerable.Empty<Agent>())
			: missionAgents;
		foreach (Agent agent in anchorCandidates)
		{
			if (agent == null || agent == playerAgent || !agent.IsActive() || !agent.IsHuman
				|| RichExecutions.Core.VengeanceIntegration.IsExecutedVictim(agent)
				|| (excludedAgentIndices != null && excludedAgentIndices.Contains(agent.Index)))
			{
				continue;
			}
			try
			{
				if (agent.State != AgentState.Active || agent.Health <= 0f)
				{
					continue;
				}
				float primaryDistanceSquared = agent.Position.DistanceSquared(primaryAnchorPosition);
				if (!float.IsNaN(primaryDistanceSquared) && !float.IsInfinity(primaryDistanceSquared) && primaryDistanceSquared <= SceneShoutConversationScope.AnchorRadiusSquared && ShoutUtils.HasShoutLineOfSightBetweenAgents(primaryAgent, agent, failClosed: true))
				{
					primaryAnchorAgents.Add(agent);
				}
				float playerDistanceSquared = agent.Position.DistanceSquared(playerAnchorPosition);
				if (!float.IsNaN(playerDistanceSquared) && !float.IsInfinity(playerDistanceSquared) && playerDistanceSquared <= SceneShoutConversationScope.AnchorRadiusSquared && ShoutUtils.HasShoutLineOfSightBetweenAgents(playerAgent, agent, failClosed: true))
				{
					playerAnchorAgents.Add(agent);
				}
			}
			catch
			{
			}
		}
		if (!SceneShoutConversationScope.TryCreate(mission, conversationEpoch, primaryAgent, playerAgent, primaryAnchorPosition, playerAnchorPosition, framedAgents ?? new List<Agent>(), primaryAnchorAgents, playerAnchorAgents, out scope) || scope == null)
		{
			return false;
		}
		audienceAgents = new List<Agent>(scope.Count);
		for (int i = 0; i < scope.Entries.Count; i++)
		{
			Agent agent2 = scope.Entries[i].AgentReference;
			if (agent2 != null)
			{
				audienceAgents.Add(agent2);
			}
		}
		Logger.Log("ShoutBehavior", "[SceneAudience] captured epoch=" + conversationEpoch + " primary=" + scope.PrimaryAgentIndex + " framed=" + scope.FramedCount + " primary10m=" + scope.PrimaryAnchorCount + " player10m=" + scope.PlayerAnchorCount + " total=" + scope.Count);
		return audienceAgents.Count > 0;
	}

	internal void TryStartShoutFromHotkey(bool openModeMenu, ShoutTargetingContext targetingContext = null)
	{
		if (ShouldSuppressShoutHotkeyAfterFocusChange())
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_focus_debounce_ignored", () => "[Hotkey] ignored during focus debounce", 1.0);
			return;
		}
		if (_isProcessingShout || _isWaitingForScenePostprocessGate)
		{
			if (IsSceneIllustrationBattleForExternal && IsSceneIllustrationAvailableForExternal && !ShoutTextInputPopup.IsOpen)
			{
				OpenBattleShoutInput(imageOnly: true);
				return;
			}
			TryShowShoutProcessingBusyMessage();
			return;
		}
		if (!ShoutUtils.IsInValidScene())
		{
			return;
		}
		// Presentation session styles merge T and Y: both open the action wheel.
		openModeMenu = openModeMenu || IsSceneIllustrationBattleForExternal || IsScenePresentationSessionEnabled();
		_activeShoutTargetingContext = targetingContext;
		LogShoutTargetingContextSnapshot(openModeMenu, targetingContext);
		BeginShoutProcessing(openModeMenu ? "hotkey_special_menu" : "hotkey_shout_input");
        try
        {
            if (IsSceneIllustrationAvailableForExternal
                && targetingContext != null && targetingContext.CandidateAgentIndices.Count == 0)
            {
                OpenBattleShoutInput();
                return;
            }
            if (openModeMenu)
			{
				TriggerShout();
			}
			else
			{
				TriggerShoutDirectInput();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] TriggerShout failed: " + ex.Message);
			ResumeGame();
			InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 打开喊话界面失败，已重置状态。", new Color(1f, 0.3f, 0.3f)));
		}
	}

	internal static void LogShoutTargetingContextSnapshot(bool openModeMenu, ShoutTargetingContext targetingContext)
	{
		if (targetingContext == null)
		{
			Logger.LogVerbose("ShoutBehavior", "hotkey_targeting_snapshot_null", () => "[Hotkey] targeting snapshot is null mode=" + (openModeMenu ? "special" : "direct"), 1.0);
			return;
		}
		int count = targetingContext.CandidateAgentIndices?.Count ?? 0;
		float totalAngleDegrees = targetingContext.HalfAngleRadians * 2f * 57.29578f;
		Logger.LogVerbose("ShoutBehavior", "hotkey_targeting_snapshot", () => "[Hotkey] targeting snapshot mode=" + (openModeMenu ? "special" : "direct") + " range=" + targetingContext.RangeMeters.ToString("0.##") + " angle=" + totalAngleDegrees.ToString("0.#") + " candidates=" + count + " primary=" + targetingContext.PrimaryAgentIndex, 0.5);
	}

	internal void BeginShoutProcessing(string reason)
	{
		_scenePlayerShoutRequestOwner.InvalidateCurrent();
		Interlocked.Increment(ref _sceneShoutProcessingSequence);
		_isProcessingShout = true;
		_shoutProcessingStartedAt = GetApplicationTimeSafe();
		Logger.Log("ShoutBehavior", "[Processing] begin reason=" + (reason ?? ""));
	}

	internal void EndShoutProcessing(string reason)
	{
		_scenePlayerShoutRequestOwner.InvalidateModuleTickets();
		Interlocked.Increment(ref _sceneShoutProcessingSequence);
		if (_isProcessingShout || _shoutProcessingStartedAt >= 0f)
		{
			Logger.Log("ShoutBehavior", "[Processing] end reason=" + (reason ?? "") + " elapsed=" + GetShoutProcessingElapsedSeconds().ToString("0.###"));
		}
		CancelShoutHotkeyCharge("processing_end");
		_activeShoutTargetingContext = null;
		_isProcessingShout = false;
		_shoutProcessingStartedAt = -1f;
	}

	internal float GetShoutProcessingElapsedSeconds()
	{
		if (_shoutProcessingStartedAt < 0f)
		{
			return 0f;
		}
		return Math.Max(0f, GetApplicationTimeSafe() - _shoutProcessingStartedAt);
	}

	internal void ArmShoutHotkeyFocusDebounce(string reason)
	{
		try
		{
			long ticks = DateTime.UtcNow.AddMilliseconds(ShoutHotkeyFocusDebounceMilliseconds).Ticks;
			Interlocked.Exchange(ref _suppressShoutHotkeyUntilUtcTicks, ticks);
			Logger.LogVerbose("ShoutBehavior", "hotkey_focus_debounce_armed:" + (reason ?? ""), () => "[Hotkey] focus debounce armed reason=" + (reason ?? "") + " ms=" + ShoutHotkeyFocusDebounceMilliseconds, 1.0);
		}
		catch
		{
		}
	}

	internal bool ShouldSuppressShoutHotkeyAfterFocusChange()
	{
		try
		{
			long ticks = Interlocked.Read(ref _suppressShoutHotkeyUntilUtcTicks);
			return ticks > 0L && DateTime.UtcNow.Ticks < ticks;
		}
		catch
		{
			return false;
		}
	}

	internal void TryShowShoutProcessingBusyMessage()
	{
		try
		{
			long ticks = DateTime.UtcNow.Ticks;
			long num = Interlocked.Read(ref _lastShoutProcessingBusyMessageUtcTicks);
			if (num > 0L && ticks - num < TimeSpan.FromMilliseconds(ShoutProcessingBusyMessageCooldownMilliseconds).Ticks)
			{
				return;
			}
			Interlocked.Exchange(ref _lastShoutProcessingBusyMessageUtcTicks, ticks);
		}
		catch
		{
		}
		InformationManager.DisplayMessage(new InformationMessage("正在处理中...", new Color(1f, 1f, 0f)));
	}

	internal bool IsMissionPausedByShoutUi()
	{
		try
		{
			var scene = Mission.Current?.Scene;
			return scene != null && scene.TimeSpeed <= 0.001f;
		}
		catch
		{
			return false;
		}
	}

	internal void ClearStaleShoutProcessingIfNeeded()
	{
		if (!_isProcessingShout || _shoutProcessingStartedAt < 0f || ShoutTextInputPopup.IsOpen || IsMissionPausedByShoutUi())
		{
			return;
		}
		float elapsed = GetShoutProcessingElapsedSeconds();
		if (elapsed < ShoutProcessingFailsafeSeconds)
		{
			return;
		}
		Logger.Log("ShoutBehavior", "[WARN] stale shout processing flag cleared elapsed=" + elapsed.ToString("0.###"));
		EndShoutProcessing("failsafe_timeout");
	}

	internal static void NotifyGameWindowFocusChanged(bool focusGained)
	{
		try
		{
			CurrentInstance?._j17SceneShoutInputController.ArmShoutHotkeyFocusDebounce(focusGained ? "focus_gained_event" : "focus_lost_event");
		}
		catch
		{
		}
	}

	internal static bool IsSceneShoutInputActiveForExternal()
	{
		try
		{
			ShoutBehavior currentInstance = CurrentInstance;
			return currentInstance != null && (currentInstance._j17SceneShoutInputController._isProcessingShout
				|| currentInstance._j17SceneShoutInputController._isWaitingForScenePostprocessGate
				|| currentInstance._j17SceneShoutInputController._shoutHotkeyChargeActive
				|| ShoutTextInputPopup.IsOpen);
		}
		catch
		{
			return false;
		}
	}

	internal static bool CanAgentParticipateInSceneSpeech(Agent agent)
	{
		try
		{
			return agent != null
				&& agent.IsHuman
				&& agent.IsActive()
				&& agent.State == AgentState.Active
				&& agent.Health > 0f;
		}
		catch
		{
			return false;
		}
	}

	internal static bool CanAgentParticipateInSceneSpeechExternal(int agentIndex)
	{
		var agents = Mission.Current?.Agents;
		if (agentIndex < 0 || agents == null)
		{
			return false;
		}
		try
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
			return CanAgentParticipateInSceneSpeech(agent);
		}
		catch
		{
			return false;
		}
	}

	internal static void OpenNativeConversationInput(bool showMessage = true)
	{
		try
		{
			_nativeConversationInputOpen = true;
			_nativeConversationInputTargetKey = ResolveCurrentNativeConversationTargetKey();
			if (showMessage)
			{
				InformationManager.DisplayMessage(new InformationMessage("\u5df2\u6253\u5f00 AnimusForge \u81ea\u7531\u5bf9\u8bdd\u8f93\u5165\u6846\u3002", new Color(0.4f, 1f, 0.4f)));
			}
		}
		catch
		{
			_nativeConversationInputOpen = true;
			_nativeConversationInputTargetKey = "";
		}
	}

	internal static void CloseNativeConversationInput(bool clearSessionHistory = false)
	{
		CompleteNativeConversationTtsPlaybackWait(null, "native_conversation_closed", force: true);
		_nativeConversationInputOpen = false;
		_nativeConversationInputTargetKey = "";
		CloseNativeSessionHistoryForInput(clearSessionHistory);
        if (clearSessionHistory) SceneConversationHistoryOwner.ResetNativeIllustrationHistoryBoundary();
	}

	internal static bool _nativeConversationInputOpen;

	internal static string _nativeConversationInputTargetKey = "";

	internal static bool IsNativeConversationInputOpenForExternal()
	{
		try
		{
			if (!_nativeConversationInputOpen || CurrentInstance == null || Campaign.Current?.ConversationManager?.IsConversationInProgress != true || PlayerEncounterCompat.IsInPostBattleResultFlow())
			{
				return false;
			}
			string currentKey = ResolveCurrentNativeConversationTargetKey();
			return string.IsNullOrWhiteSpace(_nativeConversationInputTargetKey) || string.IsNullOrWhiteSpace(currentKey) || string.Equals(_nativeConversationInputTargetKey, currentKey, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	internal static bool CanSubmitNativeConversationForExternal()
	{
		try
		{
			return CurrentInstance != null && Campaign.Current?.ConversationManager?.IsConversationInProgress == true && !PlayerEncounterCompat.IsInPostBattleResultFlow() && TryResolveNativeConversationTarget(out var _, out var _, out var _);
		}
		catch
		{
			return false;
		}
	}

	internal static Hero GetNativeConversationTargetHeroForExternal()
	{
		try
		{
			if (!TryResolveNativeConversationTarget(out var targetHero, out var targetCharacter, out var _))
			{
				return null;
			}
			return targetHero ?? targetCharacter?.HeroObject;
		}
		catch
		{
			return null;
		}
	}

	internal static void OpenNativeConversationInputForExternal()
	{
		OpenNativeConversationInput();
	}

	internal static void OpenNativeConversationInputSilentlyForExternal()
	{
		OpenNativeConversationInput(showMessage: false);
	}

	internal static void CloseNativeConversationInputForExternal()
	{
		CloseNativeConversationInput();
	}

	internal static int TryResolveNativeConversationAgentIndex(Hero targetHero, CharacterObject targetCharacter)
	{
		try
		{
			Agent conversationAgent = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
			if (IsValidNativeConversationTargetAgent(conversationAgent, targetHero, targetCharacter))
			{
				return conversationAgent.Index;
			}
			if (IsUsableNativeConversationFallbackAgent(conversationAgent, targetHero, targetCharacter))
			{
				Logger.Log("NativeConversation", "[AgentResolve] using fallback OneToOneConversationAgent index=" + conversationAgent.Index + " target=" + (targetHero?.StringId ?? targetCharacter?.StringId ?? "nonhero"));
				return conversationAgent.Index;
			}
			CharacterObject character = targetCharacter ?? targetHero?.CharacterObject;
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if ((character == null && targetHero == null) || agents == null)
			{
				return -1;
			}
			Agent agent = agents.FirstOrDefault((Agent a) => IsValidNativeConversationTargetAgent(a, targetHero, targetCharacter));
			if (agent != null)
			{
				return agent.Index;
			}
			if (targetHero == null)
			{
				Agent fallbackAgent = agents.FirstOrDefault((Agent a) => IsUsableNativeConversationFallbackAgent(a, targetHero, targetCharacter));
				if (fallbackAgent != null)
				{
					Logger.Log("NativeConversation", "[AgentResolve] using fallback mission agent index=" + fallbackAgent.Index + " targetCharacter=" + (targetCharacter?.StringId ?? "nonhero"));
					return fallbackAgent.Index;
				}
			}
			return -1;
		}
		catch
		{
			return -1;
		}
	}

	internal static bool IsUsableNativeConversationFallbackAgent(Agent agent, Hero targetHero, CharacterObject targetCharacter)
	{
		if (agent == null || agent.IsMainAgent || agent.Character == null || targetHero != null)
		{
			return false;
		}
		try
		{
			if (!agent.IsActive())
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		CharacterObject agentCharacter = agent.Character as CharacterObject;
		if (agentCharacter == null || agentCharacter.HeroObject != null)
		{
			return false;
		}
		if (targetCharacter != null && ReferenceEquals(agentCharacter, targetCharacter))
		{
			return true;
		}
		try
		{
			Agent conversationAgent = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
			if (conversationAgent != null && conversationAgent.Index == agent.Index)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			PartyBase agentParty = agent.Origin?.BattleCombatant as PartyBase;
			if (encounteredParty != null && agentParty != null && ReferenceEquals(encounteredParty, agentParty))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	internal static bool IsValidNativeConversationTargetAgent(Agent agent, Hero targetHero, CharacterObject targetCharacter)
	{
		if (agent == null || agent.IsMainAgent || agent.Character == null)
		{
			return false;
		}
		try
		{
			if (!agent.IsActive())
			{
				return false;
			}
		}
		catch
		{
		}
		CharacterObject agentCharacter = agent.Character as CharacterObject;
		if (agentCharacter == null)
		{
			return false;
		}
		Hero agentHero = agentCharacter.HeroObject;
		if (targetHero != null)
		{
			if (ReferenceEquals(agentHero, targetHero))
			{
				return true;
			}
			string targetHeroId = (targetHero.StringId ?? "").Trim();
			string agentHeroId = (agentHero?.StringId ?? "").Trim();
			return !string.IsNullOrWhiteSpace(targetHeroId) && string.Equals(agentHeroId, targetHeroId, StringComparison.OrdinalIgnoreCase);
		}
		CharacterObject character = targetCharacter;
		if (character == null)
		{
			return false;
		}
		if (ReferenceEquals(agentCharacter, character))
		{
			return true;
		}
		string targetCharacterId = (character.StringId ?? "").Trim();
		string agentCharacterId = (agentCharacter.StringId ?? "").Trim();
		return !string.IsNullOrWhiteSpace(targetCharacterId) && string.Equals(agentCharacterId, targetCharacterId, StringComparison.OrdinalIgnoreCase);
	}

	internal void TriggerShout()
	{
        long menuRevision = ++_modeInquiryRevision;
        _modeInquiry = null;
		if (!TryPrepareShoutTarget(out var primaryDataPacket))
		{
			return;
		}
		string text = primaryDataPacket?.Name ?? "附近的人";
		bool battleSpeechMenu = BattleSpeechRuntimeHost.CanOpenSpeechMenu(Mission.Current);
		List<Agent> battleSpeechFramedTargets = null;
		Agent battleSpeechPrimaryTarget = null;
		if (battleSpeechMenu)
		{
			battleSpeechFramedTargets = GetAgentsForShoutTargetingContext(_activeShoutTargetingContext) ?? new List<Agent>();
			battleSpeechPrimaryTarget = ResolvePrimaryAgentForShoutTargetingContext(
				_activeShoutTargetingContext,
				battleSpeechFramedTargets);
		}
		bool npcSpeechTargetAllowed = battleSpeechMenu &&
			IsPlayerSideBattleSpeechTarget(
				Mission.Current,
				Agent.Main,
				battleSpeechPrimaryTarget);
		List<InquiryElement> inquiryElements = new List<InquiryElement>
		{
			new InquiryElement("normal", "交流", null, isEnabled: true, ""),
			new InquiryElement("give", "给予其物品并交流", null, isEnabled: true, ""),
			new InquiryElement("show", "向其展示物品并交流", null, isEnabled: true, ""),
			new InquiryElement("give_troops", "给予部队并交流", null, isEnabled: true, ""),
			new InquiryElement("give_prisoners", "给予俘虏并交流", null, isEnabled: true, ""),
			new InquiryElement("give_settlements", "转移固定资产并交流", null, isEnabled: true, "")
		};
		if (MyBehavior.IsDevDataManagementEnabledForExternal())
		{
			inquiryElements.Add(new InquiryElement("tag_test", "标签测试", null, isEnabled: true, "输入 NPC 可见正文和后处理标签，不调用 API，按当前目标立即执行。"));
		}
		if (battleSpeechMenu)
		{
			inquiryElements.Add(new InquiryElement(
				"battle_speech_player",
				"演讲",
				null,
				isEnabled: Agent.Main != null && Agent.Main.IsActive(),
				"你亲自面向己方士兵发表演讲。"));
			if (npcSpeechTargetAllowed)
			{
				inquiryElements.Add(new InquiryElement(
					"battle_speech_npc",
					"他人演讲",
					null,
					isEnabled: true,
					"让当前框选的己方主目标发表演讲。"));
			}
		}
        MultiSelectionInquiryData data = null;
		data = new MultiSelectionInquiryData(text, "当前目标：" + text + "\n此菜单用于边交流边给予或展示物品，也可转移部队、俘虏或固定资产。\n请选择交流方式：", inquiryElements, isExitShown: true, 1, 1, "确定", "取消", delegate(List<InquiryElement> selected)
		{
            if (!ConsumeShoutModeInquiry(data, menuRevision)) return;
			if (selected == null || selected.Count == 0)
			{
				ResumeGame();
			}
			else
			{
				string text2 = (selected[0]?.Identifier ?? "").ToString();
				if (text2 == "battle_speech_player" || text2 == "battle_speech_npc")
				{
					OpenBattleSpeechFromShoutMenu(
						text2 == "battle_speech_npc",
						battleSpeechPrimaryTarget,
						battleSpeechFramedTargets,
						_sceneConversationEpoch);
				}
				else if (text2 == "normal" && TryOpenPresentationSessionFromWheel())
				{
					// The persistent session panel owns input; the one-shot popup is not opened.
				}
				// With a session style the give/show choices open the session's own give panel;
				// the old popup chain (resource list → amounts → one-shot input) is only the fallback.
				else if (text2 == "give")
				{
					if (!TryOpenPresentationTradeFromWheel("give")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.Give);
				}
				else if (text2 == "show")
				{
					if (!TryOpenPresentationTradeFromWheel("show")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.Show);
				}
				else if (text2 == "give_troops")
				{
					if (!TryOpenPresentationTradeFromWheel("give_troops")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.GiveTroops);
				}
				else if (text2 == "give_prisoners")
				{
					if (!TryOpenPresentationTradeFromWheel("give_prisoners")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.GivePrisoners);
				}
				else if (text2 == "give_settlements")
				{
					if (!TryOpenPresentationTradeFromWheel("give_settlements")) BeginShoutTradeFlow(primaryDataPacket, ShoutChatMode.GiveSettlements);
				}
				else if (text2 == "tag_test")
				{
					OpenShoutTagTestInput(primaryDataPacket);
				}
				else
				{
					OpenShoutTextInput(primaryDataPacket, null, null);
				}
			}
		}, delegate
		{
            if (!ConsumeShoutModeInquiry(data, menuRevision)) return;
			OnShoutCancelled();
		}, "", isSeachAvailable: true);
        _modeInquiry = data;
        _modeInquiryMission = Mission.Current;
        _modeInquiryCampaign = Campaign.Current;
        _modeInquiryTargeting = _activeShoutTargetingContext;
        _modeInquiryGeneration = SaveRuntimeGuard.CaptureGeneration();
        _modeInquiryEpoch = _sceneConversationEpoch;
        _modeInquiryAffirmative = data.AffirmativeAction;
        _modeInquiryNegative = data.NegativeAction;
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	internal void OpenBattleSpeechFromShoutMenu(
		bool npcSpeech,
		Agent primaryTarget,
		IReadOnlyList<Agent> framedTargets,
		int conversationEpoch)
	{
		// TryPrepareShoutTarget paused AF and acquired ordinary scene-shout
		// movement control. The dedicated speech channel owns the following UI,
		// so release only that control before handing the frozen agent snapshot
		// to BattleSpeechRuntimeHost.
		DeactivateMultiSceneMovementSuppression();
		EndShoutProcessing("battle_speech_menu_takeover");
		Agent player = Agent.Main;
		AfCompatV130.BeginDedicatedSpeechMenuInput();
		bool opened = BattleSpeechRuntimeHost.TryOpenSpeechInputFromShoutMenu(
			Mission.Current,
			npcSpeech,
			player,
			primaryTarget,
			framedTargets ?? Array.Empty<Agent>(),
			conversationEpoch);
		if (!opened)
		{
			InformationManager.DisplayMessage(new InformationMessage(
				"演讲输入框无法打开，已返回普通场景状态。",
				new Color(1f, 0.5f, 0.3f)));
			AfCompatV130.CompleteDedicatedSpeechMenuInput();
		}
	}

	internal static bool IsPlayerSideBattleSpeechTarget(
		Mission mission,
		Agent player,
		Agent primaryTarget)
	{
		if (mission?.PlayerTeam == null ||
			player == null ||
			primaryTarget == null ||
			!primaryTarget.IsActive() ||
			!primaryTarget.IsHuman ||
			ReferenceEquals(primaryTarget, player) ||
			!ReferenceEquals(primaryTarget.Mission, mission) ||
			primaryTarget.Team == null ||
			!primaryTarget.Team.IsValid ||
			primaryTarget.Team.Side != mission.PlayerTeam.Side)
		{
			return false;
		}
		return true;
	}

	internal void TriggerShoutDirectInput()
	{
		if (!TryPrepareShoutTarget(out var primaryDataPacket))
		{
			return;
		}
		OpenShoutTextInput(primaryDataPacket, null, null);
	}

	internal bool TryPrepareShoutTarget(out NpcDataPacket primaryDataPacket)
	{
		primaryDataPacket = null;
		if (!ModOnboardingBehavior.EnsureSetupReady())
		{
			EndShoutProcessing("setup_not_ready");
			return false;
		}
		// The presentation wheel and session run unpaused; the one-shot flows keep pausing.
		if (!IsScenePresentationSessionEnabled())
		{
			PauseGame();
		}
		ShoutTargetingContext targetingContext = _activeShoutTargetingContext;
		List<Agent> nearbyNPCAgents = GetAgentsForShoutTargetingContext(targetingContext);
		if (nearbyNPCAgents == null || nearbyNPCAgents.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("你正在自言自语...", new Color(0.6f, 0.6f, 0.6f)));
			ResumeGame();
			return false;
		}
		List<NpcDataPacket> source = (from a in nearbyNPCAgents
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		Agent primaryTarget = ResolvePrimaryAgentForShoutTargetingContext(targetingContext, nearbyNPCAgents);
		if (primaryTarget == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 异色主对象已经离场，请重新框选。", new Color(1f, 0.5f, 0.3f)));
			ResumeGame();
			return false;
		}
		primaryDataPacket = source.FirstOrDefault((NpcDataPacket d) => d.AgentIndex == primaryTarget.Index);
		if (primaryDataPacket == null)
		{
			InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 没有找到可交流的目标。", new Color(1f, 0.5f, 0.3f)));
			ResumeGame();
			return false;
		}
		ActivateMultiSceneMovementSuppression(new int[1] { primaryDataPacket.AgentIndex });
		return true;
	}

	internal void OpenShoutTextInput(NpcDataPacket primaryDataPacket, string preface, string extraFact)
	{
		string text = primaryDataPacket?.Name ?? "附近的人";
		string titleText = text;
		string text2 = string.IsNullOrWhiteSpace(preface) ? "" : preface;
		PauseGame();
		if (!ShoutTextInputPopup.Show(titleText, text2, "请输入你想说的话：", "", delegate(string input)
		{
			OnShoutConfirmedWithContext(input, extraFact, primaryDataPacket?.AgentIndex);
        }, OnShoutCancelled, BuildShoutTargetEncyclopediaAction(primaryDataPacket), enableIllustration: true))
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(titleText, (string.IsNullOrWhiteSpace(text2) ? "" : (text2 + "\n")) + "请输入你想说的话：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "发送", "取消", delegate(string input)
			{
				OnShoutConfirmedWithContext(input, extraFact, primaryDataPacket?.AgentIndex);
			}, OnShoutCancelled), pauseGameActiveState: true);
		}
	}

	internal void OpenShoutTagTestInput(NpcDataPacket primaryDataPacket)
	{
		if (!MyBehavior.IsDevDataManagementEnabledForExternal())
		{
			InformationManager.DisplayMessage(new InformationMessage("[标签测试] 请先在 MCM 开启数据管理。", new Color(1f, 0.45f, 0.25f)));
			OnShoutCancelled();
			return;
		}
		string text = primaryDataPacket?.Name ?? "附近的人";
		string titleText = "标签输入 - " + text;
		string subtitle = "当前目标：" + text + "\n输入 NPC 可见正文和后处理标签。不会调用 API，标签会按当前目标立即执行。";
		PauseGame();
		if (!ShoutTextInputPopup.Show(titleText, subtitle, "输入正文和标签：", "", delegate(string input)
		{
			OnShoutTagTestConfirmed(input, primaryDataPacket?.AgentIndex);
		}, OnShoutCancelled, BuildShoutTargetEncyclopediaAction(primaryDataPacket)))
		{
			InformationManager.ShowTextInquiry(new TextInquiryData(titleText, subtitle + "\n\n输入正文和标签：", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "执行", "取消", delegate(string input)
			{
				OnShoutTagTestConfirmed(input, primaryDataPacket?.AgentIndex);
			}, OnShoutCancelled), pauseGameActiveState: true);
		}
	}

	internal Action BuildShoutTargetEncyclopediaAction(NpcDataPacket targetNpc)
	{
		Hero hero = null;
		try
		{
			if (targetNpc != null && targetNpc.IsHero)
			{
				hero = ResolveHeroFromAgentIndex(targetNpc.AgentIndex);
			}
		}
		catch
		{
			hero = null;
		}
		if (hero == null)
		{
			return null;
		}
		return delegate
		{
			OpenHeroEncyclopediaFromShoutInput(hero);
		};
	}

	internal static void OpenHeroEncyclopediaFromShoutInput(Hero hero)
	{
		if (hero == null)
		{
			return;
		}
		try
		{
			string link = hero.EncyclopediaLink;
			if (!string.IsNullOrWhiteSpace(link))
			{
				Campaign.Current?.EncyclopediaManager?.GoToLink(link);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[WARN] Failed to open shout target encyclopedia: " + ex.Message);
		}
	}

	internal long _sceneShoutProcessingSequence;

	internal async void OnShoutConfirmed(string shoutText)
	{
		await ProcessShoutConfirmedInternal(shoutText, null, null);
	}

	internal async void OnShoutConfirmedWithContext(string shoutText, string extraFact, int? forcedPrimaryAgentIndex)
	{
		await ProcessShoutConfirmedInternal(shoutText, extraFact, forcedPrimaryAgentIndex);
	}

	internal object CaptureScenePlayerShoutRequestForReplay(Agent[] framedTargets, Agent primaryTarget)
	{
		if (!IsBannerlordMainThreadForNativeActions() || framedTargets == null || primaryTarget == null
			|| !framedTargets.Any(agent => ReferenceEquals(agent, primaryTarget)))
		{
			return null;
		}
		return CaptureScenePlayerShoutRequest(framedTargets, primaryTarget.Index);
	}

	internal bool IsCapturedScenePlayerShoutRequestCurrent(object capturedRequest)
	{
		ScenePlayerShoutRequest request = capturedRequest as ScenePlayerShoutRequest;
		return IsBannerlordMainThreadForNativeActions()
			&& IsScenePlayerShoutRequestCurrent(request)
			&& _scenePlayerShoutRequestOwner.IsUnclaimed(request);
	}

	internal bool TryReplayCapturedScenePlayerShout(string shoutText, string extraFact, int? forcedPrimaryAgentIndex,
		object capturedRequest, Action<Action> runWithObservationScope)
	{
		ScenePlayerShoutRequest request = capturedRequest as ScenePlayerShoutRequest;
		if (!IsBannerlordMainThreadForNativeActions() || !IsScenePlayerShoutRequestCurrent(request)
			|| string.IsNullOrWhiteSpace(shoutText) || runWithObservationScope == null
			|| !_scenePlayerShoutRequestOwner.TryClaim(request))
		{
			return false;
		}
		_ = ProcessCapturedScenePlayerShoutAsync(shoutText, extraFact, forcedPrimaryAgentIndex, request, runWithObservationScope);
		return true;
	}

	internal async Task ProcessShoutConfirmedInternal(string shoutText, string extraFact, int? forcedPrimaryAgentIndex)
	{
		if (!IsBannerlordMainThreadForNativeActions())
		{
			Logger.Log("ShoutBehavior", "[SceneInput] rejected off-main-thread submission");
			return;
		}
		if (string.IsNullOrWhiteSpace(shoutText))
		{
			ResumeGame();
			return;
		}
		ShoutTargetingContext targetingContext = _activeShoutTargetingContext;
		List<Agent> framedTargets = GetAgentsForShoutTargetingContext(targetingContext);
		int primaryAgentIndex = forcedPrimaryAgentIndex ?? targetingContext?.PrimaryAgentIndex ?? -1;
		ScenePlayerShoutRequest request = CaptureScenePlayerShoutRequest(framedTargets, primaryAgentIndex);
		if (request == null)
		{
			return;
		}
		_scenePlayerShoutRequestOwner.MarkStarted(request);
		await ProcessCapturedScenePlayerShoutAsync(shoutText, extraFact, forcedPrimaryAgentIndex, request, null);
	}

	internal void PauseGame()
	{
		Mission mission = Mission.Current;
		if (mission?.Scene != null)
		{
			mission.Scene.TimeSpeed = 0.0001f;
		}
		PauseTtsForShoutUi();
	}

	internal void ResumeGame()
	{
		Mission mission = Mission.Current;
		if (mission?.Scene != null)
		{
			mission.Scene.TimeSpeed = 1f;
		}
		ResumeTtsAfterShoutUi();
		EndShoutProcessing("resume_game");
	}

	internal void OnShoutCancelled()
	{
		DeactivateMultiSceneMovementSuppression();
		ResumeGame();
	}

    private void ResetPassiveStareTracking() => _ports.ResetPassiveStareTracking_L7764();
    private void OnShoutTagTestConfirmed(string input, int? forcedPrimaryAgentIndex) => _ports.OnShoutTagTestConfirmed_L13792(input, forcedPrimaryAgentIndex);
    private void BeginShoutTradeFlow(NpcDataPacket targetNpc, ShoutChatMode mode) => _ports.BeginShoutTradeFlow_L14081(targetNpc, mode);
    private Hero ResolveHeroFromAgentIndex(int agentIndex) => _ports.ResolveHeroFromAgentIndex_L17635(agentIndex);
    private void ActivateMultiSceneMovementSuppression(IEnumerable<int> participantAgentIndices) => _ports.ActivateMultiSceneMovementSuppression_L18651(participantAgentIndices);
    private void DeactivateMultiSceneMovementSuppression() => _ports.DeactivateMultiSceneMovementSuppression_L18702();
    private void PauseTtsForShoutUi() => _ports.PauseTtsForShoutUi_L87();
    private void ResumeTtsAfterShoutUi() => _ports.ResumeTtsAfterShoutUi_L88();
    private bool UpdatePresentationHotkey(InputKey shoutKey, InputKey specialMenuKey) => _ports.UpdatePresentationHotkey_L76(shoutKey, specialMenuKey);
    private bool TryOpenPresentationSessionFromWheel() => _ports.TryOpenPresentationSessionFromWheel_L108();
    private ScenePresentationController Presentation { get => _ports.GetPresentation(); }
    private bool _shoutHotkeyChargeMergesIntoSession { get => _ports.Get_shoutHotkeyChargeMergesIntoSession(); set => _ports.Set_shoutHotkeyChargeMergesIntoSession(value); }
    private bool TryOpenPresentationTradeFromWheel(string mode) => _ports.TryOpenPresentationTradeFromWheel_L82(mode);
    private int _sceneConversationEpoch { get => _ports.Get_sceneConversationEpoch(); set => _ports.Set_sceneConversationEpoch(value); }
    private bool _isWaitingForScenePostprocessGate { get => _ports.Get_isWaitingForScenePostprocessGate(); set => _ports.Set_isWaitingForScenePostprocessGate(value); }
    private ScenePlayerShoutRequestOwner _scenePlayerShoutRequestOwner { get => _ports.Get_scenePlayerShoutRequestOwner(); }
    private Task ProcessCapturedScenePlayerShoutAsync(string shoutText, string extraFact, int? forcedPrimaryAgentIndex,
		ScenePlayerShoutRequest request, Action<Action> runWithObservationScope, SceneGroupReceipt receipt = null) => _ports.ProcessCapturedScenePlayerShoutAsync_L191(shoutText, extraFact, forcedPrimaryAgentIndex, request, runWithObservationScope, receipt);
    private ScenePlayerShoutRequest CaptureScenePlayerShoutRequest(IEnumerable<Agent> framedTargets, int primaryAgentIndex) => _ports.CaptureScenePlayerShoutRequest_L195(framedTargets, primaryAgentIndex);
    private void OpenBattleShoutInput(bool imageOnly = false) => _ports.OpenBattleShoutInput(imageOnly);
    private bool IsScenePlayerShoutRequestCurrent(ScenePlayerShoutRequest request) => _ports.IsScenePlayerShoutRequestCurrent_L196(request);
}

internal sealed class SceneShoutInputControllerPorts
{
    internal Action<bool> OpenBattleShoutInput;
    internal delegate void ResetPassiveStareTracking_L7764Callback();
    internal ResetPassiveStareTracking_L7764Callback ResetPassiveStareTracking_L7764;
    internal delegate void OnShoutTagTestConfirmed_L13792Callback(string input, int? forcedPrimaryAgentIndex);
    internal OnShoutTagTestConfirmed_L13792Callback OnShoutTagTestConfirmed_L13792;
    internal delegate void BeginShoutTradeFlow_L14081Callback(NpcDataPacket targetNpc, ShoutChatMode mode);
    internal BeginShoutTradeFlow_L14081Callback BeginShoutTradeFlow_L14081;
    internal delegate Hero ResolveHeroFromAgentIndex_L17635Callback(int agentIndex);
    internal ResolveHeroFromAgentIndex_L17635Callback ResolveHeroFromAgentIndex_L17635;
    internal delegate void ActivateMultiSceneMovementSuppression_L18651Callback(IEnumerable<int> participantAgentIndices);
    internal ActivateMultiSceneMovementSuppression_L18651Callback ActivateMultiSceneMovementSuppression_L18651;
    internal delegate void DeactivateMultiSceneMovementSuppression_L18702Callback();
    internal DeactivateMultiSceneMovementSuppression_L18702Callback DeactivateMultiSceneMovementSuppression_L18702;
    internal delegate void PauseTtsForShoutUi_L87Callback();
    internal PauseTtsForShoutUi_L87Callback PauseTtsForShoutUi_L87;
    internal delegate void ResumeTtsAfterShoutUi_L88Callback();
    internal ResumeTtsAfterShoutUi_L88Callback ResumeTtsAfterShoutUi_L88;
    internal delegate bool UpdatePresentationHotkey_L76Callback(InputKey shoutKey, InputKey specialMenuKey);
    internal UpdatePresentationHotkey_L76Callback UpdatePresentationHotkey_L76;
    internal delegate bool TryOpenPresentationSessionFromWheel_L108Callback();
    internal TryOpenPresentationSessionFromWheel_L108Callback TryOpenPresentationSessionFromWheel_L108;
    internal Func<ScenePresentationController> GetPresentation;
    internal Func<bool> Get_shoutHotkeyChargeMergesIntoSession;
    internal Action<bool> Set_shoutHotkeyChargeMergesIntoSession;
    internal delegate bool TryOpenPresentationTradeFromWheel_L82Callback(string mode);
    internal TryOpenPresentationTradeFromWheel_L82Callback TryOpenPresentationTradeFromWheel_L82;
    internal Func<int> Get_sceneConversationEpoch;
    internal Action<int> Set_sceneConversationEpoch;
    internal Func<bool> Get_isWaitingForScenePostprocessGate;
    internal Action<bool> Set_isWaitingForScenePostprocessGate;
    internal Func<ScenePlayerShoutRequestOwner> Get_scenePlayerShoutRequestOwner;
    internal delegate Task ProcessCapturedScenePlayerShoutAsync_L191Callback(string shoutText, string extraFact, int? forcedPrimaryAgentIndex, ScenePlayerShoutRequest request, Action<Action> runWithObservationScope, SceneGroupReceipt receipt);
    internal ProcessCapturedScenePlayerShoutAsync_L191Callback ProcessCapturedScenePlayerShoutAsync_L191;
    internal delegate ScenePlayerShoutRequest CaptureScenePlayerShoutRequest_L195Callback(IEnumerable<Agent> framedTargets, int primaryAgentIndex);
    internal CaptureScenePlayerShoutRequest_L195Callback CaptureScenePlayerShoutRequest_L195;
    internal delegate bool IsScenePlayerShoutRequestCurrent_L196Callback(ScenePlayerShoutRequest request);
    internal IsScenePlayerShoutRequestCurrent_L196Callback IsScenePlayerShoutRequestCurrent_L196;
}
