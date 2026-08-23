using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior
    {
        private List<string> BuildEffectiveClassifierAllowList()
        {
            List<string> allowed = new List<string>();
            foreach (IntentDefinition intent in SceneActionsRuntimeHost.Catalog.Intents.Values
                         .OrderBy(value => value.Key, System.StringComparer.Ordinal))
            {
                if (!intent.ClassifierSelectable)
                {
                    continue;
                }
                if (intent.Kind == IntentKind.PlayAction)
                {
                    if (SceneActionsRuntimeHost.Catalog.TrySelectAction(
                            intent.ActionKey,
                            SceneActionsRuntimeHost.Runtime,
                            SceneActionsRuntimeHost.Settings,
                            out SelectedAction selected,
                            out _) &&
                        IsSelectedActionReady(selected))
                    {
                        allowed.Add(intent.Key);
                    }
                    continue;
                }

                bool stateReady = SceneActionsRuntimeHost.Catalog.Actions.Values.Any(action =>
                    action.Mode == ActionMode.Stateful &&
                    intent.AcceptedStateTags.Contains(action.StateTag, System.StringComparer.Ordinal) &&
                    SceneActionsRuntimeHost.Catalog.TrySelectAction(
                        action.Key,
                        SceneActionsRuntimeHost.Runtime,
                        SceneActionsRuntimeHost.Settings,
                        out SelectedAction selected,
                        out _) &&
                    IsSelectedActionReady(selected));
                if (stateReady)
                {
                    allowed.Add(intent.Key);
                }
            }
            return allowed;
        }

        private bool IsSelectedActionReady(SelectedAction selected)
        {
            IEnumerable<string> actionIds = selected.Definition.Mode == ActionMode.Stateful
                ? new[]
                {
                    selected.Variant.EnterActionId,
                    selected.Variant.HoldActionId,
                    selected.Variant.ExitActionId
                }
                : selected.Variant.ActionIds;
            foreach (string actionId in actionIds)
            {
                if (!_providerSession.TryResolve(
                    selected.Definition.ProviderId,
                    actionId,
                    out _,
                    out _,
                    out _))
                {
                    return false;
                }
            }
            return true;
        }
        private bool TryValidateProgramReady(
            ActionProgramV4 program,
            out ExecutionResultCode failure,
            out string reason)
        {
            failure = ExecutionResultCode.InvalidCommand;
            reason = "Action program is missing.";
            if (program == null || program.ActionCount > SceneActionsRuntimeHost.Settings.MaxProgramActions)
            {
                return false;
            }

            foreach (string key in program.Steps
                         .SelectMany(step => step.IntentKeys)
                         .Distinct(StringComparer.Ordinal))
            {
                if (!SceneActionsRuntimeHost.Catalog.TryGetIntent(
                    key,
                    out IntentDefinition intent))
                {
                    reason = "Program intent is absent from the frozen V4 catalog: " + key;
                    return false;
                }
                if (intent.Kind == IntentKind.ReleaseOwnedAction ||
                    intent.Kind == IntentKind.DrawWeapon ||
                    intent.Kind == IntentKind.SheatheWeapon)
                {
                    continue;
                }
                if (intent.Kind == IntentKind.ExitOwnedState)
                {
                    bool exitReady = SceneActionsRuntimeHost.Catalog.Actions.Values.Any(action =>
                        action.Mode == ActionMode.Stateful &&
                        intent.AcceptedStateTags.Contains(
                            action.StateTag,
                            StringComparer.Ordinal) &&
                        SceneActionsRuntimeHost.Catalog.TrySelectAction(
                            action.Key,
                            SceneActionsRuntimeHost.Runtime,
                            SceneActionsRuntimeHost.Settings,
                            out SelectedAction stateAction,
                            out _) &&
                        IsSelectedActionReady(stateAction));
                    if (!exitReady)
                    {
                        failure = ExecutionResultCode.ProviderUnavailable;
                        reason = "Exit intent has no runtime-ready owned-state chain: " + key;
                        return false;
                    }
                    continue;
                }

                if (!SceneActionsRuntimeHost.Catalog.TrySelectAction(
                    intent.ActionKey,
                    SceneActionsRuntimeHost.Runtime,
                    SceneActionsRuntimeHost.Settings,
                    out SelectedAction selected,
                    out failure))
                {
                    reason = "Program action is not enabled for the exact runtime: " + key;
                    return false;
                }
                if (!IsSelectedActionReady(selected))
                {
                    failure = ExecutionResultCode.ProviderUnavailable;
                    reason = "Program action provider is not ready: " + key;
                    return false;
                }
            }
            failure = ExecutionResultCode.Queued;
            reason = null;
            return true;
        }
    }
}
