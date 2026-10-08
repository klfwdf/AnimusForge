using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class ScenePresentationTradeOption
{
	public int Index { get; internal set; }
    public long FlowRevision { get; internal set; }
	public string Name { get; internal set; } = "";
	public string Category { get; internal set; } = "";
	public int Available { get; internal set; }
	public int UnitValue { get; internal set; }
	// Opaque host option; presentation code only passes it back for thumbnails.
	public object HostOption { get; internal set; }
 internal bool IsSettlement { get; set; }
 internal string ValidationName { get; set; }
}

// Give/show inside the persistent scene session. Replaces the old popup chain (resource list, one
// amount popup per item, one-shot text popup) with one panel, but reuses the host's own option list,
// eligibility rules and commit (OnShoutTradeChatConfirmed): the gift is staged and delivered together
// with the player's next line, exactly like the one-shot "给予其物品并交流" flow.
public partial class ShoutBehavior
{


	// Used by BeginShoutTradeFlow and the scene give panel, so both apply the same target rules.
private string GetShoutTradeTargetIneligibility(ShoutChatMode mode) => _j17SceneTradeBannerlordAdapter.GetShoutTradeTargetIneligibility(mode);

internal static string GetNoShoutTradeOptionsMessage(ShoutChatMode mode) => SceneTradeBannerlordAdapter.GetNoShoutTradeOptionsMessage(mode);

internal static bool TryParsePresentationTradeMode(string mode, out ShoutChatMode result) => SceneTradeController.TryParsePresentationTradeMode(mode, out result);

	// Wheel give/show: keep (or open) the session and ask the panel to show its give view.
private bool TryOpenPresentationTradeFromWheel(string mode) => _j17SceneTradeController.TryOpenPresentationTradeFromWheel(mode);

private void ReleasePresentationTrade() => _j17SceneTradeController.ReleasePresentationTrade();

public static string ConsumeScenePresentationTradeRequestForExternal() => SceneTradeController.ConsumeScenePresentationTradeRequestForExternal();

	public static bool HasScenePresentationStagedTradeForExternal => SceneTradeController.HasScenePresentationStagedTradeForExternal;

public static string GetScenePresentationStagedTradeSummaryForExternal() => SceneTradeController.GetScenePresentationStagedTradeSummaryForExternal();

	// Loads the host option list for the session addressee. Never shows a popup.
public static List<ScenePresentationTradeOption> LoadScenePresentationTradeOptionsForExternal(string mode, out string status) => SceneTradeBannerlordAdapter.LoadScenePresentationTradeOptionsForExternal(mode, out status);

	// Stages the chosen items; nothing moves until the next line is sent.
    public static bool StageScenePresentationTradeForExternal(long flowRevision, IReadOnlyList<int> indices, IReadOnlyList<int> amounts, out string status)
        => SceneTradeController.StageScenePresentationTradeForExternal(flowRevision, indices, amounts, out status);
    public static void CancelScenePresentationTradeForExternal(long flowRevision)
        => SceneTradeController.CancelScenePresentationTradeForExternal(flowRevision);
public static bool StageScenePresentationTradeForExternal(IReadOnlyList<int> indices, IReadOnlyList<int> amounts, out string status) => SceneTradeController.StageScenePresentationTradeForExternal(indices, amounts, out status);
private void StagePresentationTransferItem(int index,int amount) => _j17SceneTradeController.StagePresentationTransferItem(index, amount);

public static void CancelScenePresentationTradeForExternal() => SceneTradeController.CancelScenePresentationTradeForExternal();

	// Called from the session submit: same commit as the one-shot flow's chat step.
private bool SubmitPresentationTrade(string content, out string status) => _j17SceneTradeController.SubmitPresentationTrade(content, out status);
}
