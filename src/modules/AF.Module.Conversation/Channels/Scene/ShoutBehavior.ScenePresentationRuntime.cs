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
	private void TickPresentationSession(float dt) => Presentation.TickPresentationSession(dt);

	// Real fights only (runs at the 10 Hz session tick, no agent scan). A team-hostile bystander in a
	// peaceful town is not a fight; hostile members already drop out via IsUsablePresentationAgent/range.
private static string GetPresentationCombatEndReason(Mission mission) => ScenePresentationBannerlordAdapter.GetPresentationCombatEndReason(mission);

	private long ComputePresentationHistoryFingerprint()
		=> SceneHistoryOwner.PublicFingerprint();

	// The turn's framed audience: every non-excluded member currently in range (locked at any distance).
	// ProcessCurrentScenePlayerShout still adds 10 m bystanders around the addressee and the player.
private ShoutTargetingContext BuildPresentationTargetingContext() => _j17ScenePresentationBannerlordAdapter.BuildPresentationTargetingContext();

	// Scope filter used by TryBuildSceneShoutConversationScope. Null when no session applies.
	private HashSet<int> GetPresentationExcludedAgentIndices() => Presentation.GetPresentationExcludedAgentIndices();

	// Bystanders who were in this turn's audience join the session list as participating members.
	private void AbsorbPresentationAudience(IReadOnlyList<Agent> audienceAgents) => Presentation.AbsorbAudience(audienceAgents);

	// Hotkey while a session is open: tap = expand a collapsed panel (collapsing is mouse-only; while the
	// panel is expanded it owns input, so the key never reaches here), hold + release = merge newly framed agents.
	// Returns true when the key was handled here. Works even while an NPC reply is still running.
	private bool UpdatePresentationHotkey(InputKey shoutKey, InputKey specialMenuKey)
  => Presentation.UpdateHotkey(PresentationHotkeys, shoutKey, specialMenuKey);
 private ScenePresentationHotkeyPort _presentationHotkeys;
 private ScenePresentationHotkeyPort PresentationHotkeys => _presentationHotkeys ??= new ScenePresentationHotkeyPort
 {
  IsCharging=()=>_shoutHotkeyChargeActive,
  HeldSeconds=()=>Math.Max(0f,GetApplicationTimeSafe()-_shoutHotkeyChargeStartedAt),
  IsReleased=()=>Input.IsKeyReleased(_shoutHotkeyChargeKey), IsDown=()=>Input.IsKeyDown(_shoutHotkeyChargeKey),
  Cancel=CancelShoutHotkeyCharge, DrawPreview=()=>DrawShoutRangePreview(BuildCurrentShoutTargetingContext()),
  CaptureMerge=()=> { ShoutTargetingContext framed=_lastRenderedShoutTargetingContext ?? BuildCurrentShoutTargetingContext(); return framed==null ? null : () => MergeFramedIntoPresentation(framed); },
  BeginIfPressed=(shout,special)=> {
   InputKey pressed=Input.IsKeyPressed(shout) ? shout : Input.IsKeyPressed(special) ? special : InputKey.Invalid;
   if (pressed==InputKey.Invalid || ShouldSuppressShoutHotkeyAfterFocusChange() || !ShoutUtils.IsInValidScene()) return false;
   _shoutHotkeyChargeActive=true; _shoutHotkeyChargeOpenModeMenu=false; _shoutHotkeyChargeKey=pressed;
   _shoutHotkeyChargeStartedAt=GetApplicationTimeSafe(); _lastRenderedShoutTargetingContext=null; return true;
  }
 };

private void MergeFramedIntoPresentation(ShoutTargetingContext framed) => _j17ScenePresentationBannerlordAdapter.MergeFramedIntoPresentation(framed);

	// Wheel "交流": open (or merge into) the session instead of the one-shot popup. The mission is not paused.
private bool TryOpenPresentationSessionFromWheel() => _j17ScenePresentationBannerlordAdapter.TryOpenPresentationSessionFromWheel();

	// Give/show/etc. from the wheel: keep the session, then run the host's own trade flow unchanged.
private bool EnsurePresentationSessionForWheelAction() => _j17ScenePresentationBannerlordAdapter.EnsurePresentationSessionForWheelAction();

private List<ScenePresentationHistoryLine> BuildPresentationHistory(int maxLines) => _j17ScenePresentationBannerlordAdapter.BuildPresentationHistory(maxLines);


}
