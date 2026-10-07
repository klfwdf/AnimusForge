using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    // Narrow presentation seam. The Illustrator owner installs these; dialogue has no HTTP/image state.
    public static Func<bool> SceneIllustrationAvailableHook;
    public static Func<bool> SceneIllustrationBusyHook;
    public static Func<string> SceneIllustrationStatusHook;
    public static Action<Func<bool>> SceneIllustrationRequestHook;
    public static int SceneIllustrationVersionForExternal { get; private set; }
    public static bool IsSceneIllustrationAvailableForExternal => SceneIllustrationAvailableHook?.Invoke() == true;
    public static bool IsSceneIllustrationBusyForExternal => SceneIllustrationBusyHook?.Invoke() == true;
    public static string SceneIllustrationStatusForExternal => SceneIllustrationStatusHook?.Invoke() ?? "";
    public static void NotifySceneIllustrationChangedForExternal() => SceneIllustrationVersionForExternal++;
    public static void RequestSceneIllustrationForExternal(Func<bool> panelStillOpen)
    {
        if (SceneIllustrationRequestHook != null) SceneIllustrationRequestHook(panelStillOpen);
        else InformationManager.DisplayMessage(new InformationMessage("[AI画卷] 截图生图功能尚未加载。"));
    }

    public static bool IsSceneIllustrationBattleForExternal
    {
        get
        {
            Mission mission = Mission.Current;
            if (mission == null) return false;
            // The custom map meeting uses a battle mission to host a peaceful conversation.
            if (IsMeetingPseudoCombatContext()) return false;
            switch (mission.Mode)
            {
                case MissionMode.Battle: case MissionMode.Duel: case MissionMode.Stealth:
                case MissionMode.Deployment: case MissionMode.Tournament: return true;
            }
            return mission.IsFieldBattle || mission.IsSiegeBattle || !mission.IsFriendlyMission ||
                GetPresentationCombatEndReason(mission) != null;
        }
    }

    public static string CaptureSceneIllustrationDialogueForExternal()
    {
        ShoutBehavior owner = CurrentInstance;
        if (owner == null || Mission.Current == null) return "";
        return owner.SceneHistoryOwner.CaptureIllustrationDialogue();
    }

    private void OpenBattleShoutInput(bool imageOnly = false)
    {
        Mission mission = Mission.Current;
        float speed = mission?.Scene?.TimeSpeed ?? 1f;
        var agents = imageOnly ? null : GetAgentsForShoutTargetingContext(_activeShoutTargetingContext);
        Agent target = agents == null ? null : ResolvePrimaryAgentForShoutTargetingContext(_activeShoutTargetingContext, agents);
        NpcDataPacket packet = target == null ? null : ShoutUtils.ExtractNpcData(target);
        // Opening an image-only panel must not retire an unrelated in-flight shout round.
        Action close = () =>
        {
            if (!imageOnly) OnShoutCancelled();
            if (ReferenceEquals(Mission.Current, mission) && mission?.Scene != null) mission.Scene.TimeSpeed = speed;
        };
        if (!imageOnly) PauseGame();
        if (!ShoutTextInputPopup.Show(packet?.Name ?? (IsSceneIllustrationBattleForExternal ? "战斗现场" : "场景现场"),
            imageOnly ? "喊话回复尚未完成；当前面板仅提供截图生图。" : packet == null ? "当前没有有效喊话目标，可以点击生图；发送喊话仍需框选有效目标。" : "战斗中可手动生成当前现场插画。",
            "生图不使用未发送的输入草稿。", "", input =>
            {
                if (packet == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 当前没有可发送喊话的目标。"));
                    close(); return;
                }
                // Keep onboarding, target eligibility and movement suppression on the real send path.
                if (!TryPrepareShoutTarget(out var prepared) || prepared.AgentIndex != packet.AgentIndex)
                {
                    InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 目标或交流资格已失效，请重新框选。"));
                    close(); return;
                }
                OnShoutConfirmedWithContext(input, null, packet.AgentIndex);
            }, close, BuildShoutTargetEncyclopediaAction(packet), enableIllustration: true))
        {
            close();
            InformationManager.DisplayMessage(new InformationMessage("[场景喊话] 输入面板打开失败。"));
        }
    }
}
