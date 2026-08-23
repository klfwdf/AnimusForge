using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge.SceneActions.Core
{
    public sealed class SceneActionCatalog
    {
        private static readonly Regex KeyPattern =
            new Regex("^[a-z0-9][a-z0-9_-]{0,63}$", RegexOptions.CultureInvariant);
        private static readonly Regex ProviderPattern = new Regex(
            "^[a-z0-9]+(?:[._-][a-z0-9]+)*$",
            RegexOptions.CultureInvariant);

        private readonly Dictionary<string, ActionDefinition> _actions;
        private readonly Dictionary<string, IntentDefinition> _intents;
        private readonly Dictionary<string, AliasDefinition> _forceAliases;
        private readonly Dictionary<string, AliasDefinition> _exactAliases;

        public SceneActionCatalog(
            IEnumerable<ActionDefinition> actions,
            IEnumerable<IntentDefinition> intents,
            IEnumerable<AliasDefinition> aliases)
        {
            _actions = BuildUnique(actions, action => action.Key, "action");
            _intents = BuildUnique(intents, intent => intent.Key, "intent");
            _forceAliases = new Dictionary<string, AliasDefinition>(StringComparer.Ordinal);
            _exactAliases = new Dictionary<string, AliasDefinition>(StringComparer.Ordinal);

            ValidateDefinitions();
            HashSet<string> normalizedAliases = new HashSet<string>(StringComparer.Ordinal);
            foreach (AliasDefinition alias in aliases ?? Enumerable.Empty<AliasDefinition>())
            {
                if (alias == null ||
                    string.IsNullOrEmpty(alias.Text) ||
                    !string.Equals(alias.Text, alias.Text.Trim(), StringComparison.Ordinal) ||
                    (!alias.AllowForceExact && !alias.AllowExactCommand))
                {
                    throw new InvalidOperationException("Alias definition is incomplete.");
                }
                string normalized = CommandParser.Normalize(alias.Text);
                if (string.IsNullOrEmpty(normalized))
                {
                    throw new InvalidOperationException("Alias text cannot be empty.");
                }
                if (normalized.Length > 32 ||
                    normalized.IndexOf('*') >= 0 ||
                    normalized.StartsWith("act_", StringComparison.OrdinalIgnoreCase) ||
                    ContainsControlCharacter(normalized))
                {
                    throw new InvalidOperationException("Unsafe alias text: " + alias.Text);
                }
                if (!normalizedAliases.Add(normalized))
                {
                    throw new InvalidOperationException(
                        "Duplicate normalized alias: " + normalized);
                }
                if (!_intents.ContainsKey(alias.IntentKey))
                {
                    throw new InvalidOperationException(
                        "Alias references unknown intent: " + alias.IntentKey);
                }
                if (alias.AllowForceExact)
                {
                    AddAlias(_forceAliases, normalized, alias, "force_exact");
                }
                if (alias.AllowExactCommand)
                {
                    AddAlias(_exactAliases, normalized, alias, "exact_command");
                }
            }
        }

        public IReadOnlyDictionary<string, ActionDefinition> Actions => _actions;
        public IReadOnlyDictionary<string, IntentDefinition> Intents => _intents;
        public IReadOnlyDictionary<string, AliasDefinition> ForceAliases => _forceAliases;
        public IReadOnlyDictionary<string, AliasDefinition> ExactAliases => _exactAliases;

        public bool TryGetForceAlias(string normalized, out AliasDefinition alias)
        {
            return _forceAliases.TryGetValue(normalized, out alias);
        }

        public bool TryGetExactAlias(string normalized, out AliasDefinition alias)
        {
            return _exactAliases.TryGetValue(normalized, out alias);
        }

        public bool TryGetIntent(string key, out IntentDefinition intent)
        {
            return _intents.TryGetValue(key ?? string.Empty, out intent);
        }

        public bool IsClassifierSelectable(string intentKey)
        {
            return _intents.TryGetValue(intentKey ?? string.Empty, out IntentDefinition intent) &&
                   intent.ClassifierSelectable;
        }

        public bool TrySelectAction(
            string actionKey,
            RuntimeIdentity runtime,
            SceneActionSettings settings,
            out SelectedAction selected,
            out ExecutionResultCode failure)
        {
            selected = null;
            failure = ExecutionResultCode.ActionIndexMissing;
            if (!_actions.TryGetValue(actionKey ?? string.Empty, out ActionDefinition action))
            {
                return false;
            }

            List<ActionVariant> matches = action.RuntimeVariants
                .Where(variant => MatchesRuntime(variant, runtime))
                .ToList();
            if (matches.Count != 1)
            {
                return false;
            }

            ActionVariant match = matches[0];
            bool enabled = match.EnabledByDefault;
            bool explicitlyEnabled = false;
            if (settings?.ActionOverrides != null &&
                settings.ActionOverrides.TryGetValue(actionKey, out ActionOverride actionOverride) &&
                actionOverride?.Enabled.HasValue == true)
            {
                enabled = actionOverride.Enabled.Value;
                explicitlyEnabled = actionOverride.Enabled.Value;
            }
            if (!enabled)
            {
                failure = ExecutionResultCode.ReleaseStageBlocked;
                return false;
            }
            if (match.ReleaseStage != ReleaseStage.Validated && !explicitlyEnabled)
            {
                failure = ExecutionResultCode.ReleaseStageBlocked;
                return false;
            }

            selected = new SelectedAction(action, match);
            failure = ExecutionResultCode.Queued;
            return true;
        }

        private void ValidateDefinitions()
        {
            foreach (ActionDefinition action in _actions.Values)
            {
                RequireKey(action.Key, "action");
                if (string.IsNullOrWhiteSpace(action.ProviderId) ||
                    !ProviderPattern.IsMatch(action.ProviderId))
                {
                    throw new InvalidOperationException(
                        "Invalid provider key: " + action.ProviderId);
                }
                if (action.RuntimeVariants == null || action.RuntimeVariants.Count == 0)
                {
                    throw new InvalidOperationException("Action has no variants: " + action.Key);
                }
                if (float.IsNaN(action.CooldownSeconds) ||
                    action.CooldownSeconds < 0f ||
                    action.CooldownSeconds > 60f)
                {
                    throw new InvalidOperationException(
                        "Action cooldown is outside the safe range: " + action.Key);
                }
                if ((action.Mode == ActionMode.Stateful &&
                     string.IsNullOrWhiteSpace(action.StateTag)) ||
                    (action.Mode != ActionMode.Stateful &&
                     !string.IsNullOrWhiteSpace(action.StateTag)))
                {
                    throw new InvalidOperationException(
                        "Action state tag does not match its mode: " + action.Key);
                }

                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                HashSet<string> selectors = new HashSet<string>(StringComparer.Ordinal);
                foreach (ActionVariant variant in action.RuntimeVariants)
                {
                    if (!ids.Add(variant.Id ?? string.Empty))
                    {
                        throw new InvalidOperationException(
                            "Duplicate variant id for action " + action.Key);
                    }
                    if (string.IsNullOrWhiteSpace(variant.Id) ||
                        string.IsNullOrWhiteSpace(variant.GameVersionEquals) ||
                        string.IsNullOrWhiteSpace(variant.RuntimeBuildId) ||
                        variant.RuntimeAdapterContract < 1)
                    {
                        throw new InvalidOperationException(
                            "Runtime selector is incomplete for action " + action.Key);
                    }
                    if ((variant.Channel != 0 && variant.Channel != 1) ||
                        (variant.EnforceAll && variant.Channel != 0) ||
                        float.IsNaN(variant.BlendInSeconds) ||
                        variant.BlendInSeconds < 0f || variant.BlendInSeconds > 2f ||
                        float.IsNaN(variant.ActionSpeed) ||
                        variant.ActionSpeed < 0.1f || variant.ActionSpeed > 3f)
                    {
                        throw new InvalidOperationException(
                            "Playback parameters are unsafe for action " + action.Key);
                    }
                    string selector = string.Join("\u001f", new[]
                    {
                        variant.GameVersionEquals ?? string.Empty,
                        variant.RuntimeBuildId ?? string.Empty,
                        variant.RuntimeAdapterContract.ToString()
                    });
                    if (!selectors.Add(selector))
                    {
                        throw new InvalidOperationException(
                            "Duplicate runtime selector for action " + action.Key);
                    }
                    if (variant.EnabledByDefault && variant.ReleaseStage != ReleaseStage.Validated)
                    {
                        throw new InvalidOperationException(
                            "Unvalidated variant defaults enabled: " + variant.Id);
                    }
                    if (variant.ReleaseStage == ReleaseStage.Validated &&
                        string.IsNullOrWhiteSpace(variant.ValidationReportId))
                    {
                        throw new InvalidOperationException(
                            "Validated variant lacks validation report: " + variant.Id);
                    }
                    if (action.Mode == ActionMode.Stateful &&
                        (string.IsNullOrWhiteSpace(variant.EnterActionId) ||
                         string.IsNullOrWhiteSpace(variant.HoldActionId) ||
                         string.IsNullOrWhiteSpace(variant.ExitActionId)))
                    {
                        throw new InvalidOperationException(
                            "Stateful action has an incomplete phase chain: " + action.Key);
                    }
                    if (action.Mode == ActionMode.Stateful &&
                        new[]
                        {
                            variant.EnterActionId,
                            variant.HoldActionId,
                            variant.ExitActionId
                        }.Any(value => !value.StartsWith("act_", StringComparison.Ordinal)))
                    {
                        throw new InvalidOperationException(
                            "Stateful phase is not an engine action id: " + action.Key);
                    }
                    if (action.Mode == ActionMode.Stateful &&
                        ((variant.ActionIds != null && variant.ActionIds.Count != 0) ||
                         variant.EnterSafetyTimeoutSeconds <= 0f ||
                         variant.ExitSafetyTimeoutSeconds <= 0f))
                    {
                        throw new InvalidOperationException(
                            "Stateful action has invalid union fields: " + action.Key);
                    }
                    if (action.Mode != ActionMode.Stateful &&
                        (variant.ActionIds == null || variant.ActionIds.Count == 0))
                    {
                        throw new InvalidOperationException(
                            "Playable action has no action ids: " + action.Key);
                    }
                    if (action.Mode != ActionMode.Stateful &&
                        variant.ActionIds.Any(value =>
                            string.IsNullOrWhiteSpace(value) ||
                            !value.StartsWith("act_", StringComparison.Ordinal)))
                    {
                        throw new InvalidOperationException(
                            "Playable action contains a non-engine id: " + action.Key);
                    }
                    if (action.Mode != ActionMode.Stateful &&
                        (!string.IsNullOrEmpty(variant.EnterActionId) ||
                         !string.IsNullOrEmpty(variant.HoldActionId) ||
                         !string.IsNullOrEmpty(variant.ExitActionId)))
                    {
                        throw new InvalidOperationException(
                            "Non-stateful action contains state phases: " + action.Key);
                    }
                    if (action.Mode == ActionMode.OneShot && variant.ActionIds.Count != 1)
                    {
                        throw new InvalidOperationException(
                            "OneShot must contain exactly one action id: " + action.Key);
                    }
                    if (action.Mode == ActionMode.Looping && variant.ActionIds.Count != 1)
                    {
                        throw new InvalidOperationException(
                            "Looping must contain exactly one action id: " + action.Key);
                    }
                    if (action.Mode == ActionMode.RandomGroup &&
                        (variant.ActionIds.Count < 2 ||
                         variant.ActionIds.Distinct(StringComparer.Ordinal).Count() !=
                         variant.ActionIds.Count))
                    {
                        throw new InvalidOperationException(
                            "RandomGroup must contain at least two unique action ids: " +
                            action.Key);
                    }
                }
            }

            foreach (IntentDefinition intent in _intents.Values)
            {
                RequireKey(intent.Key, "intent");
                if (intent.Kind == IntentKind.PlayAction)
                {
                    if (string.IsNullOrWhiteSpace(intent.ActionKey) ||
                        !_actions.ContainsKey(intent.ActionKey))
                    {
                        throw new InvalidOperationException(
                            "Intent references unknown action: " + intent.Key);
                    }
                }
                else if (intent.Kind == IntentKind.ExitOwnedState)
                {
                    if (!string.IsNullOrWhiteSpace(intent.ActionKey) ||
                        intent.AcceptedStateTags == null ||
                        intent.AcceptedStateTags.Count == 0 ||
                        intent.AcceptedStateTags.Any(tag => !_actions.Values.Any(action =>
                            action.Mode == ActionMode.Stateful &&
                            string.Equals(action.StateTag, tag, StringComparison.Ordinal))))
                    {
                        throw new InvalidOperationException(
                            "ExitOwnedState has an invalid state reference: " + intent.Key);
                    }
                }
                else if (intent.Kind == IntentKind.ReleaseOwnedAction ||
                         intent.Kind == IntentKind.DrawWeapon ||
                         intent.Kind == IntentKind.SheatheWeapon)
                {
                    if (!string.IsNullOrWhiteSpace(intent.ActionKey) ||
                        (intent.AcceptedStateTags != null &&
                         intent.AcceptedStateTags.Count != 0))
                    {
                        throw new InvalidOperationException(
                            "Runtime control intent contains an action/state reference: " +
                            intent.Key);
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        "Intent has an unknown kind: " + intent.Key);
                }
            }
        }

        private static bool MatchesRuntime(ActionVariant variant, RuntimeIdentity runtime)
        {
            return variant != null && runtime != null &&
                   string.Equals(
                       variant.GameVersionEquals,
                       runtime.GameVersion,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       variant.RuntimeBuildId,
                       runtime.RuntimeBuildId,
                       StringComparison.Ordinal) &&
                   variant.RuntimeAdapterContract == runtime.RuntimeAdapterContract;
        }

        private static Dictionary<string, T> BuildUnique<T>(
            IEnumerable<T> values,
            Func<T, string> keySelector,
            string kind)
        {
            Dictionary<string, T> result =
                new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (T value in values ?? Enumerable.Empty<T>())
            {
                string key = keySelector(value);
                if (string.IsNullOrWhiteSpace(key) || result.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        "Invalid or duplicate " + kind + " key: " + key);
                }
                result.Add(key, value);
            }
            return result;
        }

        private static void AddAlias(
            IDictionary<string, AliasDefinition> registry,
            string normalized,
            AliasDefinition alias,
            string resolver)
        {
            if (registry.ContainsKey(normalized))
            {
                throw new InvalidOperationException(
                    "Duplicate normalized alias for " + resolver + ": " + normalized);
            }
            registry.Add(normalized, alias);
        }

        private static void RequireKey(string key, string kind)
        {
            if (string.IsNullOrWhiteSpace(key) ||
                !KeyPattern.IsMatch(key) ||
                key.StartsWith("act_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid " + kind + " key: " + key);
            }
        }

        private static bool ContainsControlCharacter(string text)
        {
            foreach (char character in text)
            {
                if (char.IsControl(character))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
