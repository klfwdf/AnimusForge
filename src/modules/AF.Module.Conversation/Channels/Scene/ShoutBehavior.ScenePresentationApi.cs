using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// Presentation-facing API for the persistent scene session. Reads are cheap; mutations run on the
// main thread and only change session membership/addressee — never the request pipeline itself.
public partial class ShoutBehavior
{
	public static int ScenePresentationVersionForExternal => ScenePresentationController._presentationVersion;

	public static bool IsScenePresentationActiveForExternal
		=> ScenePresentationController._presentationActive && ScenePresentationController._presentationMissionRef.TryGetTarget(out Mission mission) && ReferenceEquals(mission, Mission.Current);

	public static bool IsScenePresentationCollapsedForExternal => ScenePresentationController._presentationCollapsed;

	// True until the previous line's round is over: replies generated, postprocess settled, replies spoken.
	public static bool IsScenePresentationBusyForExternal => ScenePresentationBannerlordAdapter.IsScenePresentationBusyForExternal;

private bool IsPresentationSubmitBlocked() => _j17ScenePresentationBannerlordAdapter.IsPresentationSubmitBlocked();

public static void SetScenePresentationCollapsedForExternal(bool collapsed) => ScenePresentationBannerlordAdapter.SetScenePresentationCollapsedForExternal(collapsed);

public static void EndScenePresentationForExternal(string reason) => ScenePresentationBannerlordAdapter.EndScenePresentationForExternal(reason);

public static bool SubmitScenePresentationTextForExternal(string text, out string status) => ScenePresentationBannerlordAdapter.SubmitScenePresentationTextForExternal(text, out status);

public static List<ScenePresentationParticipantInfo> GetScenePresentationParticipantsForExternal() => ScenePresentationBannerlordAdapter.GetScenePresentationParticipantsForExternal();

public static List<ScenePresentationHistoryLine> GetScenePresentationHistoryForExternal(int maxLines = 40) => ScenePresentationBannerlordAdapter.GetScenePresentationHistoryForExternal(maxLines);

	// Card click: talk to this person next. An excluded member is brought back as participating.
public static bool SetScenePresentationAddresseeForExternal(int agentIndex) => ScenePresentationBannerlordAdapter.SetScenePresentationAddresseeForExternal(agentIndex);

	// Seal click: 参与 → 屏蔽 → 锁定. The addressee skips 屏蔽.
public static void CycleScenePresentationParticipantForExternal(int agentIndex) => ScenePresentationBannerlordAdapter.CycleScenePresentationParticipantForExternal(agentIndex);

	// Batch bar: include everyone, or exclude everyone except the addressee and locked members.
public static void SetAllScenePresentationParticipantsForExternal(bool include) => ScenePresentationBannerlordAdapter.SetAllScenePresentationParticipantsForExternal(include);

public static bool OpenScenePresentationEncyclopediaForExternal() => ScenePresentationBannerlordAdapter.OpenScenePresentationEncyclopediaForExternal();

	// The wheel's center portrait: the framed primary target of the pending shout.
public static Agent GetScenePresentationWheelTargetForExternal() => ScenePresentationBannerlordAdapter.GetScenePresentationWheelTargetForExternal();
}
