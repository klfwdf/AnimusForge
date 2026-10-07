using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class ScenePresentationParticipantInfo
{
	public int AgentIndex { get; internal set; }
	public string Name { get; internal set; } = "";
	public string Role { get; internal set; } = "";
	public ScenePresentationParticipantState State { get; internal set; }
	public bool IsAddressee { get; internal set; }
	public bool IsInRange { get; internal set; }
	public CharacterObject Character { get; internal set; }
}

public sealed class ScenePresentationHistoryLine
{
	public string Speaker { get; internal set; } = "";
	public string Text { get; internal set; } = "";
	public string Kind { get; internal set; } = "";
}

// Stable legacy hooks consumed by DialogueUI. The controller owns all session state.
public partial class ShoutBehavior
{
 public static Func<bool> ScenePresentationSessionHook;
 public static Func<bool> ScenePresentationBlocksHotkeysHook;
 private ScenePresentationController _scenePresentation;
 private ScenePresentationController Presentation => _scenePresentation ??= new ScenePresentationController(
  CanAgentParticipateInSceneSpeech, () => Interlocked.Read(ref _currentConversationEventSequence),
  GetPresentationCombatEndReason, () => { GetConfiguredShoutRange(out var _, out var range); return range; },
  ComputePresentationHistoryFingerprint, index => ActivateMultiSceneMovementSuppression(new[] { index }),
  DeactivateMultiSceneMovementSuppression, ReleasePresentationTrade,
  DuelSettings.ShouldAutoExcludeUnframedShoutParticipants);
 private bool _shoutHotkeyChargeMergesIntoSession { get => Presentation.MergesHotkeyCharge; set => Presentation.MergesHotkeyCharge=value; }
 private const float PresentationTapSeconds = 0.25f;
 private const int PresentationContextLines = 4;
 private static bool IsScenePresentationSessionEnabled()
 {
  try { return ScenePresentationSessionHook?.Invoke() == true; }
  catch { return false; }
 }
 private bool IsPresentationSessionLive() => Presentation.IsPresentationSessionLive();
 private static void BumpPresentation() => ScenePresentationController.BumpPresentation();
 private ScenePresentationController.Member FindPresentationMember(int index) => Presentation.FindPresentationMember(index);
 private bool IsPresentationAudience(ScenePresentationController.Member member) => Presentation.IsPresentationAudience(member);
 private int ChoosePresentationAddressee() => Presentation.ChoosePresentationAddressee();
 private bool AddPresentationMembers(IEnumerable<Agent> agents) => Presentation.AddPresentationMembers(agents);
 private bool EnsurePresentationSession(IReadOnlyList<Agent> agents, int primary) => Presentation.EnsurePresentationSession(agents, primary);
 private void EndPresentationSession(string reason) => Presentation.EndPresentationSession(reason);
}
