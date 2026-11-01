using System;
using System.Linq;
using RichExecutions.Campaign;
using RichExecutions.Core;
using RichExecutions.Customization;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    private bool _continuationChoiceOpen;
    private (ExecutionSitePreset Preset, Vec3 Origin, Vec2 Forward)? _chainedLayout;

    private bool CanContinueFieldCamp => Request.IsFieldCamp &&
        State == ExecutionSessionState.Aftermath && _aftermathSessionReleased &&
        _playerExecutionStateRestored && _aftermathConversationsRestored &&
        !_cleanupComplete && !_initializationFailed && !_continuationChoiceOpen &&
        !_endMissionNextTick && !Mission.MissionEnded && ReferenceEquals(Mission.Current, Mission) &&
        VengeanceIntegration.IsEnabled && VengeanceIntegration.IsFieldCampAvailable();

    internal void PrepareChainedLayout(ExecutionSitePreset preset, Vec3 origin, Vec2 forward)
    {
        _chainedLayout = (preset, origin, forward);
    }

    private void ShowFieldCampContinuation()
    {
        if (!CanContinueFieldCamp) return;
        var behavior = TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<RichExecutionCampaignBehavior>();
        if (behavior is null) return;
        // Enumerate only when the player asks, never on the mission tick.
        var elements = behavior.GetDeploymentPrisonerOptions(null)
            .Where(option => option.Hero != Request.Victim && !option.Hero.IsDead)
            .Select(option => new InquiryElement(option.Hero, option.Hero.Name.ToString(), null)).ToList();
        elements.Add(new InquiryElement(null,
            new TextObject("{=REX_Continue_Leave}Leave the execution site.").ToString(), null));
        _continuationChoiceOpen = true;
        try
        {
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                new TextObject("{=REX_Continue_Title}Choose the next prisoner").ToString(),
                new TextObject("{=REX_FieldCamp_Continue_Description}Choose another prisoner for this execution site, or finish the ceremony and return to camp.").ToString(),
                elements, true, 1, 1,
                new TextObject("{=REX_Continue_Confirm}Continue").ToString(),
                new TextObject("{=REX_Continue_Cancel}Leave").ToString(),
                selected =>
                {
                    _continuationChoiceOpen = false;
                    ContinueFieldCamp(selected?.FirstOrDefault()?.Identifier as Hero);
                },
                _ =>
                {
                    _continuationChoiceOpen = false;
                    ContinueFieldCamp(null);
                }), pauseGameActiveState: true);
        }
        catch
        {
            _continuationChoiceOpen = false;
            throw;
        }
    }

    private void ContinueFieldCamp(Hero? hero)
    {
        // Inquiry callbacks may arrive after TAB, teardown or a changed scene.
        if (!CanContinueFieldCamp) return;
        ExecutionRequest? nextRequest = null;
        if (hero is not null)
        {
            var behavior = TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<RichExecutionCampaignBehavior>();
            var option = behavior?.GetDeploymentPrisonerOptions(null)
                .FirstOrDefault(candidate => candidate.Hero == hero && hero != Request.Victim && !hero.IsDead);
            if (option is null) return;
            nextRequest = behavior!.BuildDeploymentRequest(hero, option.Source, CurrentMethod, Request.Charge, Request.Tone, null);
            if (nextRequest is null) return;
            var validation = _service.Validate(nextRequest, ExecutionValidationStage.Confirmation);
            if (!validation.IsValid)
            {
                InformationManager.DisplayMessage(new InformationMessage(validation.Message.ToString()));
                return;
            }
        }

        // Camp-only handoff: settle before destroying this session's death owner.
        // Ordinary town executions continue to commit exclusively at mission exit.
        if (!CommitCampaignExecutionOnMissionExit() || _outcome?.DeathCommitted != true) return;
        var preset = _customSitePreset;
        var placement = _placement;
        FinalizeSession();
        if (!_cleanupComplete) return;
        var mission = Mission;
        mission.RemoveMissionBehavior(this);
        mission.RemoveMissionBehavior(_deathLogic);
        if (nextRequest is null || preset is null || placement is null) return;

        var begun = false;
        try
        {
            var validation = _service.Begin(nextRequest);
            if (!validation.IsValid)
            {
                InformationManager.DisplayMessage(new InformationMessage(validation.Message.ToString()));
                return;
            }
            begun = true;
            ExecutionSessionCoordinator.InjectChainedSession(mission, nextRequest, _service,
                preset, placement.Origin, placement.Forward);
            begun = false;
            RexLog.Info($"Field-camp continuation {Request.SessionId} -> {nextRequest.SessionId} reused the current mission.");
        }
        catch (Exception exception)
        {
            RexLog.Error("Could not start the next field-camp ceremony.", exception);
            InformationManager.DisplayMessage(new InformationMessage(new TextObject(
                "{=REX_Error_Mission_Inject}The execution scene could not be initialized safely. Nothing was spent and the prisoner lives.").ToString()));
        }
        finally
        {
            if (begun) _service.Cancel(nextRequest, ExecutionFailureReason.ScenePlacementFailed,
                new TextObject("{=REX_Error_Mission_Inject}The execution scene could not be initialized safely. Nothing was spent and the prisoner lives."));
        }
    }
}
