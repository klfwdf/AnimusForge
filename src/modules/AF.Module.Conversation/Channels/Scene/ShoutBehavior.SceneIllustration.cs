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
    internal static Action SceneIllustrationGalleryHook;
    internal static void OpenSceneIllustrationGalleryForUi()
    {
        if (IsSceneIllustrationAvailableForExternal) SceneIllustrationGalleryHook?.Invoke();
    }
    public static int SceneIllustrationVersionForExternal { get; private set; }
    public static bool IsSceneIllustrationAvailableForExternal => SceneIllustrationAvailableHook?.Invoke() == true;
    public static bool IsSceneIllustrationBusyForExternal => SceneIllustrationBusyHook?.Invoke() == true;
    public static string SceneIllustrationStatusForExternal => SceneIllustrationStatusHook?.Invoke() ?? "";
    public static void NotifySceneIllustrationChangedForExternal() => SceneIllustrationVersionForExternal++;
    public static void RequestSceneIllustrationForExternal(Func<bool> panelStillOpen)
    {
        if (SceneIllustrationRequestHook != null) SceneIllustrationRequestHook(panelStillOpen);
        else InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Shout_IllustrationUnavailable", "[AI illustration] Scene capture and image generation is not loaded.")));
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
        if (!ShoutTextInputPopup.Show(packet?.Name ?? ConversationUiLocalization.Text("AF_CONV_UI_Shout_BattleScene", IsSceneIllustrationBattleForExternal ? "Battle scene" : "Scene"),
            imageOnly ? ConversationUiLocalization.Text("AF_CONV_UI_Shout_IllustrationPending", "The shout reply is not finished; this panel currently only provides scene image generation.") : packet == null ? ConversationUiLocalization.Text("AF_CONV_UI_Shout_IllustrationNoTarget", "There is no valid shout target; you can generate an image, but sending a shout still requires selecting a valid target.") : ConversationUiLocalization.Text("AF_CONV_UI_Shout_IllustrationBattleHint", "You can manually generate an illustration of the current scene during combat."),
            ConversationUiLocalization.Text("AF_CONV_UI_Shout_IllustrationDraftHint", "Image generation does not use unsent input drafts."), "", input =>
            {
                if (packet == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Shout_NoSendTarget", "[Scene shout] There is no target available for sending a shout.")));
                    close(); return;
                }
                // Keep onboarding, target eligibility and movement suppression on the real send path.
                if (!TryPrepareShoutTarget(out var prepared) || prepared.AgentIndex != packet.AgentIndex)
                {
                    InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Shout_TargetInvalid", "[Scene shout] The target or communication eligibility is no longer valid; select targets again.")));
                    close(); return;
                }
                OnShoutConfirmedWithContext(input, null, packet.AgentIndex);
            }, close, BuildShoutTargetEncyclopediaAction(packet), enableIllustration: true))
        {
            close();
            InformationManager.DisplayMessage(new InformationMessage(ConversationUiLocalization.Text("AF_CONV_UI_Shout_InputOpenFailed", "[Scene shout] Failed to open the input panel.")));
        }
    }
}
