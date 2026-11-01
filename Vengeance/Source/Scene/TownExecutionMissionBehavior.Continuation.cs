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
    private object? _continuationChoiceIdentity;
    private bool _continuationTransitionActive;
    private (ExecutionSitePreset Preset, Vec3 Origin, Vec2 Forward)? _chainedLayout;

    private bool CanContinueFieldCamp => !_continuationChoiceOpen && !_continuationTransitionActive &&
        CanContinueFieldCampScene;

    private bool CanContinueFieldCampScene => Request.IsFieldCamp &&
        State == ExecutionSessionState.Aftermath && _aftermathSessionReleased &&
        _playerExecutionStateRestored && _aftermathConversationsRestored &&
        !_cleanupComplete && !_initializationFailed &&
        !_endMissionNextTick && !Mission.MissionEnded && ReferenceEquals(Mission.Current, Mission) &&
        VengeanceIntegration.CanStartFieldCampCeremony();

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
        var leaveElement = new InquiryElement(null,
            new TextObject("{=REX_Continue_Leave}Leave the execution site.").ToString(), null);
        elements.Add(leaveElement);
        // A fresh identity is a one-shot generation, even if an old inquiry
        // delivers another callback after the player opens its replacement.
        var choiceIdentity = new object();
        _continuationChoiceIdentity = choiceIdentity;
        _continuationChoiceOpen = true;
        try
        {
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                new TextObject("{=REX_Continue_Title}Choose the next prisoner").ToString(),
                new TextObject("{=REX_FieldCamp_Continue_Description}Choose another prisoner for this execution site, or finish the ceremony and return to camp.").ToString(),
                elements, true, 1, 1,
                new TextObject("{=REX_Continue_Confirm}Continue").ToString(),
                new TextObject("{=REX_FieldCamp_Continue_Cancel}Cancel").ToString(),
                selected =>
                {
                    if (!TryCloseFieldCampContinuation(choiceIdentity)) return;
                    // Empty, malformed or foreign selections must never mean Leave.
                    if (selected is null || selected.Count != 1) return;
                    var element = selected[0];
                    if (ReferenceEquals(element, leaveElement))
                        ContinueFieldCamp(null);
                    else if (elements.Any(candidate => ReferenceEquals(candidate, element)) &&
                        element.Identifier is Hero hero)
                        ContinueFieldCamp(hero);
                },
                _ =>
                {
                    // Cancel only dismisses this inquiry; keep the ceremony and F entry.
                    TryCloseFieldCampContinuation(choiceIdentity);
                }), pauseGameActiveState: true);
        }
        catch
        {
            TryCloseFieldCampContinuation(choiceIdentity);
            throw;
        }
    }

    private bool TryCloseFieldCampContinuation(object choiceIdentity)
    {
        if (!_continuationChoiceOpen || !ReferenceEquals(_continuationChoiceIdentity, choiceIdentity)) return false;
        _continuationChoiceIdentity = null;
        _continuationChoiceOpen = false;
        return true;
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

        // Commit the old sentence once, but keep its F entry until the next
        // request has passed Begin (including extension callbacks).
        var begun = false;
        _continuationTransitionActive = true;
        try
        {
            if (!CommitCampaignExecutionOnMissionExit() || _outcome?.DeathCommitted != true ||
                !CanContinueFieldCampScene) return;
            var preset = _customSitePreset;
            var placement = _placement;
            if (nextRequest is not null)
            {
                if (preset is null || placement is null) return;
                var validation = _service.Begin(nextRequest);
                if (!validation.IsValid)
                {
                    InformationManager.DisplayMessage(new InformationMessage(validation.Message.ToString()));
                    return;
                }
                begun = true;
                // Begin subscribers may start another activity or end the scene.
                // Cancel only the new request; the existing entry stays usable.
                if (!CanContinueFieldCampScene) return;
            }

            var mission = Mission;
            FinalizeSession();
            if (!_cleanupComplete) return;
            mission.RemoveMissionBehavior(this);
            mission.RemoveMissionBehavior(_deathLogic);
            if (nextRequest is null) return;

            ExecutionSessionCoordinator.InjectChainedSession(mission, nextRequest, _service,
                preset!, placement!.Origin, placement.Forward);
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
            try
            {
                if (begun) _service.Cancel(nextRequest!, ExecutionFailureReason.ScenePlacementFailed,
                    new TextObject("{=REX_Error_FieldCamp_Unavailable}The field camp cannot host an execution right now."));
            }
            finally
            {
                _continuationTransitionActive = false;
            }
        }
    }
}
