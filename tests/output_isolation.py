"""Allocate a new repository-local output directory for standalone runners."""
from __future__ import annotations

import os
import stat
import shutil
from pathlib import Path
from uuid import uuid4


def resolve_dotnet(repo: Path, requested: str | Path | None = None, major: int = 8) -> Path:
    """Resolve a per-run SDK override; never hide a broken explicit selection."""
    selected = requested or os.environ.get(f"AF_DOTNET{major}")
    if not selected and major == 8:
        selected = os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET")
    if selected:
        executable = Path(selected)
        if not executable.is_file():
            raise SystemExit(f"BLOCKED_ENV: selected dotnet executable is missing: {executable}")
        return executable.resolve()
    candidates = [repo / "local/dotnet/8.0.425/dotnet.exe"] if major == 8 else []
    on_path = shutil.which("dotnet")
    if on_path:
        candidates.append(Path(on_path))
    for executable in candidates:
        if executable.is_file():
            return executable.resolve()
    raise SystemExit(f"BLOCKED_ENV: no dotnet host found; set AF_DOTNET{major}")


def minimal_test_environment(dotnet: Path, output: Path, temp_root: Path | None = None) -> dict[str, str]:
    """Do not forward credentials into compilation/replay subprocesses."""
    allowed = ("SystemRoot", "WINDIR", "ProgramData", "OS", "ProgramFiles",
               "ProgramFiles(x86)", "CommonProgramFiles", "CommonProgramFiles(x86)",
               "PROCESSOR_ARCHITECTURE", "PROCESSOR_IDENTIFIER", "NUMBER_OF_PROCESSORS")
    env = {key: os.environ[key] for key in allowed if key in os.environ}
    # The caller owns output isolation. No global caches or user profile are used.
    home = output / "home"
    env.update({"DOTNET_ROOT": str(dotnet.parent), "PATH": str(dotnet.parent),
                "DOTNET_CLI_HOME": str(home), "HOME": str(home), "USERPROFILE": str(home),
                "APPDATA": str(output / "appdata"), "LOCALAPPDATA": str(output / "appdata"),
                "NUGET_PACKAGES": str(output / "nuget-packages"),
                "NUGET_HTTP_CACHE_PATH": str(output / "nuget-http-cache"),
                "NUGET_PLUGINS_CACHE_PATH": str(output / "nuget-plugin-cache"),
                "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1",
                "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
                "DOTNET_GENERATE_ASPNET_CERTIFICATE": "false",
                "DOTNET_CLI_UI_LANGUAGE": "en", "PYTHONIOENCODING": "utf-8"})
    # Data-path tests require a separately approved external synthetic TEMP.
    # Other tests stay local rather than inheriting the real user's TEMP.
    temporary = temp_root if temp_root is not None else output / "temp"
    temporary.mkdir(parents=True, exist_ok=True)
    env.update(TEMP=str(temporary), TMP=str(temporary))
    return env


def new_run_root(repo: Path, family: str, requested: Path | None) -> Path:
    root = repo.resolve(strict=True)
    candidate = requested if requested is not None else root / "artifacts/tests" / family / ("run-" + uuid4().hex)
    if ".." in candidate.parts:
        raise SystemExit("--run-root must not traverse parent directories")
    output = Path(os.path.abspath(candidate))
    if not output.is_relative_to(root) or not output.resolve(strict=False).is_relative_to(root):
        raise SystemExit("--run-root must be a new directory inside the repository")

    current = output
    while True:
        try:
            info = current.lstat()
        except FileNotFoundError:
            pass
        else:
            if current.is_symlink() or getattr(current, "is_junction", lambda: False)() \
                    or getattr(info, "st_file_attributes", 0) & getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0):
                raise SystemExit("--run-root must not use a reparse point")
        if current == root:
            break
        current = current.parent

    try:
        output.mkdir(parents=True, exist_ok=False)
    except FileExistsError:
        raise SystemExit("--run-root must be a new directory inside the repository") from None
    return output


# Physical source moves only. Historical git object/review keys stay unchanged.
CURRENT_SOURCE_RELOCATIONS = {
    'AIConfigHandler.cs': 'src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs',
    'AIConfigModel.cs': 'src/modules/AF.Module.Prompt/Configuration/AIConfigModel.cs',
    'ActionPostprocessConfigModel.cs': 'src/modules/AF.Module.Prompt/Configuration/ActionPostprocessConfigModel.cs',
    'AgentVictoryRetreatNullTeamSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/AgentVictoryRetreatNullTeamSafePatch.cs',
    'AiErrorAnalysisInquiry.cs': 'src/AF.GameAdapter.Bannerlord/UI/Errors/AiErrorAnalysisInquiry.cs',
    'AnimusForgeApiOnboardingPopup.cs': 'src/AF.GameAdapter.Bannerlord/UI/Onboarding/AnimusForgeApiOnboardingPopup.cs',
    'AnimusForgeApiOnboardingVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Onboarding/AnimusForgeApiOnboardingVM.cs',
    'AnimusForgeConversationHistoryAutoScrollPanel.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeConversationHistoryAutoScrollPanel.cs',
    'AnimusForgeConversationHistoryLogItemVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeConversationHistoryLogItemVM.cs',
    'AnimusForgeConversationHistoryLogPopup.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeConversationHistoryLogPopup.cs',
    'AnimusForgeConversationHistoryLogVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeConversationHistoryLogVM.cs',
    'AnimusForgeCourierUiSprites.cs': 'src/AF.GameAdapter.Bannerlord/UI/Courier/AnimusForgeCourierUiSprites.cs',
    'AnimusForgeFillBarClipWidget.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/AnimusForgeFillBarClipWidget.cs',
    'AnimusForgeMobilePartyAiSafetyPatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/AnimusForgeMobilePartyAiSafetyPatch.cs',
    'AnimusForgeModulePaths.cs': 'src/AF.Persistence/AnimusForgeModulePaths.cs',
    'AnimusForgeNativeConversationEditableTextWidget.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationEditableTextWidget.cs',
    'AnimusForgeNativeConversationOverlay.Presentation.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.Presentation.cs',
    'AnimusForgeNativeConversationOverlay.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.cs',
    'AnimusForgeNativeConversationOverlayVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlayVM.cs',
    'AnimusForgePlayerNotorietyUiSprites.cs': 'src/AF.GameAdapter.Bannerlord/UI/Social/AnimusForgePlayerNotorietyUiSprites.cs',
    'AnimusForgePlayerRpForgeUiSprites.cs': 'src/AF.GameAdapter.Bannerlord/UI/Economy/AnimusForgePlayerRpForgeUiSprites.cs',
    'AnimusForgeQuickInfo.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/AnimusForgeQuickInfo.cs',
    'AnimusForgeRuntimeBrushSpriteGuard.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/AnimusForgeRuntimeBrushSpriteGuard.cs',
    'AnimusForgeTerminalSettings.cs': 'src/modules/AF.Module.UI/Settings/AnimusForgeTerminalSettings.cs',
    'AnimusForgeTextInputSanitizer.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/AnimusForgeTextInputSanitizer.cs',
    'AnimusForgeUiColors.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/AnimusForgeUiColors.cs',
    'AnimusForgeUniqueCosmeticItemBehavior.cs': 'src/modules/AF.Module.Settlement/Host/AnimusForgeUniqueCosmeticItemBehavior.cs',
    'AnimusForgeWeeklyReportMapNotification.cs': 'src/AF.GameAdapter.Bannerlord/UI/Weekly/AnimusForgeWeeklyReportMapNotification.cs',
    'AutoHeightSyncWidget.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/AutoHeightSyncWidget.cs',
    'BannerlordApiCompat.cs': 'src/AF.GameAdapter.Bannerlord/Compatibility/BannerlordApiCompat.cs',
    'BannerlordExceptionSentinel.cs': 'src/AF.GameAdapter.Bannerlord/Diagnostics/BannerlordExceptionSentinel.cs',
    'BattleObserverInspectionPrisonerSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/BattleObserverInspectionPrisonerSafePatch.cs',
    'CampaignSaveChunkHelper.cs': 'src/AF.Persistence/CampaignSaveChunkHelper.cs',
    'CampaignTickDiagnosticsPatch.cs': 'src/AF.GameAdapter.Bannerlord/Diagnostics/CampaignTickDiagnosticsPatch.cs',
    'ChatDuelHandler.cs': 'src/modules/AF.Module.Duel/Host/ChatDuelHandler.cs',
    'CompanionProactiveChatBehavior.cs': 'src/modules/AF.Module.Conversation/Proactive/CompanionProactiveChatBehavior.cs',
    'CompatibilityAudit.cs': 'src/AF.GameAdapter.Bannerlord/Diagnostics/CompatibilityAudit.cs',
    'ContinueConversationSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/ContinueConversationSafePatch.cs',
    'ConversationCameraSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/ConversationCameraSafePatch.cs',
    'ConversationExceptionGuard.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/ConversationExceptionGuard.cs',
    'ConversationHelper.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/ConversationHelper.cs',
    'ConversationManagerSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/ConversationManagerSafePatch.cs',
    'ConversationMessage.cs': 'src/modules/AF.Module.Conversation/Internal/History/ConversationMessage.cs',
    'ConversationVMCapturePatch.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/ConversationVMCapturePatch.cs',
    'CourierDeliveryBehavior.cs': 'src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.cs',
    'CourierFoodConsumptionModel.cs': 'src/AF.GameAdapter.Bannerlord/Courier/CourierFoodConsumptionModel.cs',
    'CourierLetterInputPopup.cs': 'src/AF.GameAdapter.Bannerlord/UI/Courier/CourierLetterInputPopup.cs',
    'CourierLetterInputPopupVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Courier/CourierLetterInputPopupVM.cs',
    'CourierLetterThemeVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Courier/CourierLetterThemeVM.cs',
    'CourierLetterThemes.cs': 'src/AF.GameAdapter.Bannerlord/UI/Courier/CourierLetterThemes.cs',
    'CourierMobilePartyAIModel.cs': 'src/AF.GameAdapter.Bannerlord/Courier/CourierMobilePartyAIModel.cs',
    'CourierPartyTransitionModel.cs': 'src/AF.GameAdapter.Bannerlord/Courier/CourierPartyTransitionModel.cs',
    'CourierPartyTransitionRegistration.cs': 'src/AF.GameAdapter.Bannerlord/Courier/CourierPartyTransitionRegistration.cs',
    'CourierVisibleLetterSanitizer.cs': 'src/modules/AF.Module.Conversation/Channels/Courier/CourierVisibleLetterSanitizer.cs',
    'CraftingOrderLoadSafetyPatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/CraftingOrderLoadSafetyPatch.cs',
    'CriticalUiLipSyncTeardownPatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/CriticalUiLipSyncTeardownPatch.cs',
    'DebtPromiseQuest.cs': 'src/modules/AF.Module.Economy/Host/DebtPromiseQuest.cs',
    'DevHistoryEditPopup.cs': 'src/AF.GameAdapter.Bannerlord/UI/Editors/DevHistoryEditPopup.cs',
    'DevHistoryEditPopupVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Editors/DevHistoryEditPopupVM.cs',
    'DevLargeSelectionPopup.cs': 'src/AF.GameAdapter.Bannerlord/UI/Editors/DevLargeSelectionPopup.cs',
    'DevLargeSelectionPopupVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Editors/DevLargeSelectionPopupVM.cs',
    'DevMultilineEditableTextWidget.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/DevMultilineEditableTextWidget.cs',
    'DevTextEditorHelper.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/DevTextEditorHelper.cs',
    'DevWeeklyReportPopup.cs': 'src/AF.GameAdapter.Bannerlord/UI/Weekly/DevWeeklyReportPopup.cs',
    'DevWeeklyReportPopupVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Weekly/DevWeeklyReportPopupVM.cs',
    'DuelBehavior.Outcomes.cs': 'src/modules/AF.Module.Duel/Host/DuelBehavior.Outcomes.cs',
    'DuelBehavior.cs': 'src/modules/AF.Module.Duel/Host/DuelBehavior.cs',
    'DynamicPatcher.cs': 'src/AF.GameAdapter.Bannerlord/Diagnostics/DynamicPatcher.cs',
    'EncyclopediaEntityLinkFormatter.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/EncyclopediaEntityLinkFormatter.cs',
    'EncyclopediaEntityLinkNavigationCoordinator.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/EncyclopediaEntityLinkNavigationCoordinator.cs',
    'EndMissionInternalSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/EndMissionInternalSafePatch.cs',
    'ExternalBrowserLauncher.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/ExternalBrowserLauncher.cs',
    'FeatureDiagnosticLogFile.cs': 'src/AF.GameAdapter.Bannerlord/Diagnostics/FeatureDiagnosticLogFile.cs',
    'FloatingTextItemVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/FloatingTextItemVM.cs',
    'FloatingTextManager.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/FloatingTextManager.cs',
    'FloatingTextMissionView.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/FloatingTextMissionView.cs',
    'FloatingTextVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/FloatingTextVM.cs',
    'FourberieDuelCompatibility.cs': 'src/modules/AF.Module.Duel/Host/FourberieDuelCompatibility.cs',
    'FreezeWatchdog.cs': 'src/AF.GameAdapter.Bannerlord/Diagnostics/FreezeWatchdog.cs',
    'GiveAssetTagCodec.cs': 'src/modules/AF.Module.Economy/Host/GiveAssetTagCodec.cs',
    'GuardrailConfigModel.cs': 'src/modules/AF.Module.Prompt/Configuration/GuardrailConfigModel.cs',
    'GuardrailRuleHit.cs': 'src/modules/AF.Module.Prompt/Retrieval/GuardrailRuleHit.cs',
    'GuardrailRulePromptConfig.cs': 'src/modules/AF.Module.Prompt/Configuration/GuardrailRulePromptConfig.cs',
    'HeroClosestSettlementSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/HeroClosestSettlementSafePatch.cs',
    'HotkeyInputGuard.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/HotkeyInputGuard.cs',
    'InteractionComponentSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/InteractionComponentSafePatch.cs',
    'KingdomDecisionCleanupSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/KingdomDecisionCleanupSafePatch.cs',
    'KnowledgeLibraryBehavior.cs': 'src/modules/AF.Module.Knowledge/Host/KnowledgeLibraryBehavior.cs',
    'KnowledgeRetrievalConfig.cs': 'src/modules/AF.Module.Knowledge/Configuration/KnowledgeRetrievalConfig.cs',
    'LipSyncFacialAnimSuppressPatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/LipSyncFacialAnimSuppressPatch.cs',
    'LlmRetryPrompt.cs': 'src/AF.GameAdapter.Bannerlord/UI/Errors/LlmRetryPrompt.cs',
    'Logger.cs': 'src/AF.GameAdapter.Bannerlord/Diagnostics/Logger.cs',
    'MainAgentControllerSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/MainAgentControllerSafePatch.cs',
    'MapSceneTerrainTypeSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/MapSceneTerrainTypeSafePatch.cs',
    'MapSeaContextGuard.cs': 'src/AF.GameAdapter.Bannerlord/Compatibility/MapSeaContextGuard.cs',
    'MarriageSceneNotificationSafety.cs': 'src/modules/AF.Module.Social/Host/MarriageSceneNotificationSafety.cs',
    'McmDropdownRuntimeRefresh.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/McmDropdownRuntimeRefresh.cs',
    'MenuTroopSelectionTeardownSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/MenuTroopSelectionTeardownSafePatch.cs',
    'MissionScreenSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/MissionScreenSafePatch.cs',
    'MissionUiInterruptionPatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/MissionUiInterruptionPatch.cs',
    'MissionViewExceptionGuard.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/MissionViewExceptionGuard.cs',
    'ModOnboardingBehavior.cs': 'src/modules/AF.Module.Onboarding/Host/ModOnboardingBehavior.cs',
    'NameMarkerSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/NameMarkerSafePatch.cs',
    'NativeConversationAnswerAreaController.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/NativeConversationAnswerAreaController.cs',
    'NoblePrisonerEscortBehavior.cs': 'src/modules/AF.Module.Encounter/Escort/NoblePrisonerEscortBehavior.cs',
    'NoblePrisonerEscortLog.cs': 'src/modules/AF.Module.Encounter/Escort/NoblePrisonerEscortLog.cs',
    'NoblePrisonerEscortMissionBehavior.cs': 'src/modules/AF.Module.Encounter/Escort/NoblePrisonerEscortMissionBehavior.cs',
    'NonBlockingErrorReport.cs': 'src/AF.GameAdapter.Bannerlord/UI/Errors/NonBlockingErrorReport.cs',
    'NpcDataPacket.cs': 'src/modules/AF.Module.Conversation/Channels/Scene/NpcDataPacket.cs',
    'NpcInitiatedOpeningRouter.cs': 'src/modules/AF.Module.Conversation/Proactive/NpcInitiatedOpeningRouter.cs',
    'OnnxCrossEncoderReranker.cs': 'src/modules/AF.Module.Knowledge/Semantic/OnnxCrossEncoderReranker.cs',
    'OnnxEmbeddingEngine.cs': 'src/modules/AF.Module.Knowledge/Semantic/OnnxEmbeddingEngine.cs',
    'PassageUsePointSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/PassageUsePointSafePatch.cs',
    'Patch_GlobalUI_Click.cs': 'src/AF.GameAdapter.Bannerlord/UI/Common/Patch_GlobalUI_Click.cs',
    'PerfProbe.cs': 'src/AF.GameAdapter.Bannerlord/Diagnostics/PerfProbe.cs',
    'PlayerEncounterCompat.cs': 'src/AF.GameAdapter.Bannerlord/Compatibility/PlayerEncounterCompat.cs',
    'PlayerEncounterPropertySafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/PlayerEncounterPropertySafePatch.cs',
    'PlayerNotorietyBehavior.ConversationOutcomes.cs': 'src/modules/AF.Module.Social/Host/PlayerNotorietyBehavior.ConversationOutcomes.cs',
    'PlayerNotorietyBehavior.cs': 'src/modules/AF.Module.Social/Host/PlayerNotorietyBehavior.cs',
    'PlayerNotorietyCharacterDeveloperPatch.cs': 'src/AF.GameAdapter.Bannerlord/UI/Social/PlayerNotorietyCharacterDeveloperPatch.cs',
    'PlayerNotorietyPopup.cs': 'src/AF.GameAdapter.Bannerlord/UI/Social/PlayerNotorietyPopup.cs',
    'PlayerNotorietyPopupVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Social/PlayerNotorietyPopupVM.cs',
    'PlayerRpCraftItemComponentService.cs': 'src/modules/AF.Module.Economy/Host/PlayerRpCraftItemComponentService.cs',
    'PlayerRpCraftModels.cs': 'src/modules/AF.Module.Economy/Host/PlayerRpCraftModels.cs',
    'PlayerRpCraftTemplateSelectorLog.cs': 'src/modules/AF.Module.Economy/Host/PlayerRpCraftTemplateSelectorLog.cs',
    'PlayerRpForgePopup.cs': 'src/AF.GameAdapter.Bannerlord/UI/Economy/PlayerRpForgePopup.cs',
    'PlayerRpForgePopupVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Economy/PlayerRpForgePopupVM.cs',
    'PostprocessRuleEntry.cs': 'src/modules/AF.Module.Prompt/Configuration/PostprocessRuleEntry.cs',
    'PreprocessFormatException.cs': 'src/modules/AF.Module.Prompt/Composition/PreprocessFormatException.cs',
    'PreprocessPromptsConfigModel.cs': 'src/modules/AF.Module.Prompt/Configuration/PreprocessPromptsConfigModel.cs',
    'ProactiveNpcRequestBehavior.cs': 'src/modules/AF.Module.Conversation/Proactive/ProactiveNpcRequestBehavior.cs',
    'ProactiveNpcRequestPromptsConfigModel.cs': 'src/modules/AF.Module.Prompt/Configuration/ProactiveNpcRequestPromptsConfigModel.cs',
    'ProcessPartnerSentenceSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/ProcessPartnerSentenceSafePatch.cs',
    'ProcessSentenceSafePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/ProcessSentenceSafePatch.cs',
    'PromptComposer.cs': 'src/modules/AF.Module.Prompt/Composition/PromptComposer.cs',
    'PromptListRetrievalService.cs': 'src/modules/AF.Module.Prompt/Retrieval/PromptListRetrievalService.cs',
    'RagWarmupCoordinator.cs': 'src/modules/AF.Module.Knowledge/Semantic/RagWarmupCoordinator.cs',
    'RewardSystemBehavior.PlayerRpCrafting.cs': 'src/modules/AF.Module.Economy/Host/RewardSystemBehavior.PlayerRpCrafting.cs',
    'RewardSystemBehavior.RpItemIntroduction.cs': 'src/modules/AF.Module.Economy/Host/RewardSystemBehavior.RpItemIntroduction.cs',
    'RomanceSystemBehavior.cs': 'src/modules/AF.Module.Social/Host/RomanceSystemBehavior.cs',
    'RpItemIntroductionPromptsConfigModel.cs': 'src/modules/AF.Module.Prompt/Configuration/RpItemIntroductionPromptsConfigModel.cs',
    'RtsCameraCompat.cs': 'src/AF.GameAdapter.Bannerlord/Compatibility/RtsCameraCompat.cs',
    'SexualConceptionBehavior.cs': 'src/modules/AF.Module.Social/Host/SexualConceptionBehavior.cs',
    'ShoutBehavior.CampaignLifetime.cs': 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.CampaignLifetime.cs',
    'ShoutBehavior.ModuleNativeSubmission.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.ModuleNativeSubmission.cs',
    'ShoutBehavior.NativeActionCommit.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeActionCommit.cs',
    'ShoutBehavior.NativeActionDispatch.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeActionDispatch.cs',
    'ShoutBehavior.NativeAdmission.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeAdmission.cs',
    'ShoutBehavior.NativeCompletion.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeCompletion.cs',
    'ShoutBehavior.NativeMainReply.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeMainReply.cs',
    'ShoutBehavior.NativePendingHistory.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativePendingHistory.cs',
    'ShoutBehavior.NativePreparation.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativePreparation.cs',
    'ShoutBehavior.NativePromptBuild.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativePromptBuild.cs',
    'ShoutBehavior.NativeTurn.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurn.cs',
    'ShoutBehavior.NativeTurnCommit.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnCommit.cs',
    'ShoutBehavior.NativeTurnPresentation.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPresentation.cs',
    'ShoutBehavior.NativeTurnPrompt.cs': 'src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPrompt.cs',
    'ShoutBehavior.PersonaPreparation.cs': 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.PersonaPreparation.cs',
    'ShoutBehavior.cs': 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.cs',
    'ShoutNetwork.cs': 'src/modules/AF.Module.Llm/ShoutNetwork.cs',
    'ShoutTextInputFocusChangePatch.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/ShoutTextInputFocusChangePatch.cs',
    'ShoutTextInputPopup.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/ShoutTextInputPopup.cs',
    'ShoutTextInputPopupVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/ShoutTextInputPopupVM.cs',
    'ShoutTitleLinkTextWidget.cs': 'src/AF.GameAdapter.Bannerlord/UI/Conversation/ShoutTitleLinkTextWidget.cs',
    'ShoutUtils.cs': 'src/modules/AF.Module.Conversation/Channels/Scene/ShoutUtils.cs',
    'TerminalWeeklyReportBrowserPopupVM.cs': 'src/AF.GameAdapter.Bannerlord/UI/Weekly/TerminalWeeklyReportBrowserPopupVM.cs',
    'TownAmbientAiClient.cs': 'src/modules/AF.Module.Llm/Ambient/TownAmbientAiClient.cs',
    'TraceHelper.cs': 'src/AF.GameAdapter.Bannerlord/Diagnostics/TraceHelper.cs',
    'TransferQuantitySpec.cs': 'src/modules/AF.Module.Economy/Host/TransferQuantitySpec.cs',
    'TtsEngine.cs': 'src/modules/AF.Module.Llm/Tts/TtsEngine.cs',
    'VanillaIssueOfferBridge.cs': 'src/modules/AF.Module.Issue/Host/VanillaIssueOfferBridge.cs',
    'VanillaIssuePromptBehavior.cs': 'src/modules/AF.Module.Issue/Host/VanillaIssuePromptBehavior.cs',
    'VoiceMapper.cs': 'src/modules/AF.Module.Llm/Tts/VoiceMapper.cs',
    'WorkshopDailyTickSafetyPatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/Safety/WorkshopDailyTickSafetyPatch.cs',
    'WorldEntityRetrievalService.cs': 'src/modules/AF.Module.Knowledge/Entities/WorldEntityRetrievalService.cs',
    'WorldMapGovernorExpeditionNativeLifecyclePatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/WorldMap/WorldMapGovernorExpeditionNativeLifecyclePatch.cs',
    'WorldMapOrderedArmySurvivalPatch.cs': 'src/AF.GameAdapter.Bannerlord/Patches/WorldMap/WorldMapOrderedArmySurvivalPatch.cs',
    'YjThinkingCompat.cs': 'src/modules/AF.Module.Llm/Protocol/YjThinkingCompat.cs',
}


def current_source_path(repo: Path, historical_path: str) -> Path:
    """Resolve current checkout input without rewriting a historical baseline key."""
    return repo / CURRENT_SOURCE_RELOCATIONS.get(historical_path, historical_path)
