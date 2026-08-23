using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed class ActionProviderRegistry
    {
        private readonly string _moduleRoot;
        internal const string SpeechOpeningActionId = "act_af_speech_nacisword1";

        public ActionProviderRegistry(string moduleRoot)
        {
            _moduleRoot = moduleRoot ?? throw new ArgumentNullException(nameof(moduleRoot));
            XihaiStaticReady = ProbeXihaiStaticResources(out string reason);
            XihaiStaticReason = reason;
            DanceStaticReady = ProbeDanceStaticResources(out string danceReason);
            DanceStaticReason = danceReason;
            KneelLoopStaticReady = ProbeKneelLoopStaticResources(out string kneelReason);
            KneelLoopStaticReason = kneelReason;
            SpeechOpeningStaticReady = ProbeSpeechOpeningStaticResources(out string speechReason);
            SpeechOpeningStaticReason = speechReason;
        }

        public bool XihaiStaticReady { get; }
        public string XihaiStaticReason { get; }
        public bool DanceStaticReady { get; }
        public string DanceStaticReason { get; }
        public bool KneelLoopStaticReady { get; }
        public string KneelLoopStaticReason { get; }
        public bool SpeechOpeningStaticReady { get; }
        public string SpeechOpeningStaticReason { get; }

        public MissionActionProviderSession CreateMissionSession()
        {
            return new MissionActionProviderSession(this);
        }

        private bool ProbeXihaiStaticResources(out string reason)
        {
            try
            {
                string packagePath = Path.Combine(_moduleRoot, "AssetPackages", "pack0.tpac");
                string actionTypesPath =
                    Path.Combine(_moduleRoot, "ModuleData", "action_types.xml");
                string actionSetsPath =
                    Path.Combine(_moduleRoot, "ModuleData", "action_sets.xml");
                if (!File.Exists(packagePath) || new FileInfo(packagePath).Length <= 0)
                {
                    reason = "AssetPackages/pack0.tpac is missing or empty.";
                    return false;
                }
                if (!File.Exists(actionTypesPath) || !File.Exists(actionSetsPath))
                {
                    reason = "Xihai action XML is missing.";
                    return false;
                }

                XDocument actionTypes = XDocument.Load(actionTypesPath, LoadOptions.None);
                bool actionDeclared = actionTypes
                    .Descendants("action")
                    .Any(node => string.Equals(
                        (string)node.Attribute("name"),
                        "act_af_xihai",
                        StringComparison.Ordinal));
                if (!actionDeclared)
                {
                    reason = "action_types.xml does not declare act_af_xihai.";
                    return false;
                }

                XDocument actionSets = XDocument.Load(actionSetsPath, LoadOptions.None);
                XElement set = actionSets
                    .Descendants("action_set")
                    .SingleOrDefault(node => string.Equals(
                        (string)node.Attribute("id"),
                        "as_human_warrior",
                        StringComparison.Ordinal));
                if (set == null ||
                    !string.Equals((string)set.Attribute("skeleton"), "human_skeleton", StringComparison.Ordinal) ||
                    !string.Equals((string)set.Attribute("movement_system"), "bipedal", StringComparison.Ordinal))
                {
                    reason = "action_sets.xml has no exact human warrior declaration.";
                    return false;
                }
                bool animationDeclared = set.Elements("action").Any(node =>
                    string.Equals((string)node.Attribute("type"), "act_af_xihai", StringComparison.Ordinal) &&
                    string.Equals((string)node.Attribute("animation"), "nacirase", StringComparison.Ordinal));
                if (!animationDeclared)
                {
                    reason = "action_sets.xml has no exact nacirase binding.";
                    return false;
                }

                reason = "Static TPAC/XML declarations are present; engine index is mission-scoped.";
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private bool ProbeKneelLoopStaticResources(out string reason)
        {
            try
            {
                string actionTypesPath = Path.Combine(
                    _moduleRoot, "ModuleData", "action_types.xml");
                string actionSetsPath = Path.Combine(
                    _moduleRoot, "ModuleData", "action_sets.xml");
                if (!File.Exists(actionTypesPath) || !File.Exists(actionSetsPath))
                {
                    reason = "Kneel loop action XML is missing.";
                    return false;
                }

                XDocument actionTypes = XDocument.Load(actionTypesPath, LoadOptions.None);
                int declarationCount = actionTypes
                    .Descendants("action")
                    .Count(node => string.Equals(
                        (string)node.Attribute("name"),
                        "act_af_kneel_loop",
                        StringComparison.Ordinal));
                if (declarationCount != 1)
                {
                    reason = "action_types.xml must declare act_af_kneel_loop exactly once.";
                    return false;
                }

                XElement set = XDocument.Load(actionSetsPath, LoadOptions.None)
                    .Descendants("action_set")
                    .SingleOrDefault(node => string.Equals(
                        (string)node.Attribute("id"),
                        "as_human_warrior",
                        StringComparison.Ordinal));
                bool binding = set != null && set.Elements("action").Count(node =>
                    string.Equals(
                        (string)node.Attribute("type"),
                        "act_af_kneel_loop",
                        StringComparison.Ordinal) &&
                    string.Equals(
                        (string)node.Attribute("animation"),
                        "anim_main_story_conspirator_kneel_down_1_loop",
                        StringComparison.Ordinal)) == 1;
                if (!binding)
                {
                    reason = "as_human_warrior has no unique act_af_kneel_loop loop binding.";
                    return false;
                }

                reason = "Module kneel action is bound to the Native conspirator kneel loop.";
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }
        private bool ProbeDanceStaticResources(out string reason)
        {
            try
            {
                string actionSetsPath =
                    Path.Combine(_moduleRoot, "ModuleData", "action_sets.xml");
                if (!File.Exists(actionSetsPath))
                {
                    reason = "ModuleData/action_sets.xml is missing.";
                    return false;
                }
                XDocument actionSets = XDocument.Load(actionSetsPath, LoadOptions.None);
                XElement set = actionSets
                    .Descendants("action_set")
                    .SingleOrDefault(node => string.Equals(
                        (string)node.Attribute("id"),
                        "as_human_warrior",
                        StringComparison.Ordinal));
                bool binding = set != null && set.Elements("action").Any(node =>
                    string.Equals(
                        (string)node.Attribute("type"),
                        "act_dance_norse",
                        StringComparison.Ordinal) &&
                    string.Equals(
                        (string)node.Attribute("animation"),
                        "anim_tavern_dance_norse",
                        StringComparison.Ordinal));
                if (!binding)
                {
                    reason = "action_sets.xml has no warrior tavern-dance binding.";
                    return false;
                }
                reason = "Warrior tavern-dance binding is present; visual playback is experimental.";
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private bool ProbeSpeechOpeningStaticResources(out string reason)
        {
            try
            {
                string actionTypesPath = Path.Combine(
                    _moduleRoot, "ModuleData", "action_types.xml");
                string actionSetsPath = Path.Combine(
                    _moduleRoot, "ModuleData", "action_sets.xml");
                string packagePath = Path.Combine(
                    _moduleRoot, "AssetPackages", "pack0.tpac");
                if (!File.Exists(packagePath) || new FileInfo(packagePath).Length <= 0)
                {
                    reason = "AssetPackages/pack0.tpac is missing or empty.";
                    return false;
                }
                if (!File.Exists(actionTypesPath) || !File.Exists(actionSetsPath))
                {
                    reason = "Speech opening action XML is missing.";
                    return false;
                }
                int declarationCount = XDocument.Load(actionTypesPath, LoadOptions.None)
                    .Descendants("action")
                    .Count(node => string.Equals(
                        (string)node.Attribute("name"),
                        SpeechOpeningActionId,
                        StringComparison.Ordinal));
                XElement set = XDocument.Load(actionSetsPath, LoadOptions.None)
                    .Descendants("action_set")
                    .SingleOrDefault(node => string.Equals(
                        (string)node.Attribute("id"),
                        "as_human_warrior",
                        StringComparison.Ordinal));
                int bindingCount = set?.Elements("action").Count(node =>
                    string.Equals(
                        (string)node.Attribute("type"),
                        SpeechOpeningActionId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        (string)node.Attribute("animation"),
                        "nacisword1",
                        StringComparison.Ordinal)) ?? 0;
                if (declarationCount != 1 || bindingCount != 1)
                {
                    reason = "Speech opening action must declare and bind nacisword1 exactly once.";
                    return false;
                }
                reason = "Speech opening action is bound to nacisword1; visual playback remains experimental.";
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }
    }

    internal sealed class MissionActionProviderSession
    {
        private const string ModuleKneelLoopActionId = "act_af_kneel_loop";
        private const string NativeKneelLoopActionId =
            "act_main_story_conspirator_kneel_down_1_continue";
        private readonly ActionProviderRegistry _registry;
        private readonly Dictionary<string, ActionIndexCache> _readyActions =
            new Dictionary<string, ActionIndexCache>(StringComparer.Ordinal);
        private readonly HashSet<string> _missingActions =
            new HashSet<string>(StringComparer.Ordinal);

        public MissionActionProviderSession(ActionProviderRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public bool TryResolve(
            string providerId,
            string actionId,
            out ActionIndexCache action,
            out ExecutionResultCode failure,
            out string reason)
        {
            action = ActionIndexCache.act_none;
            failure = ExecutionResultCode.ProviderUnavailable;
            reason = null;

            if (!string.Equals(providerId, "native.bannerlord", StringComparison.Ordinal) &&
                !string.Equals(providerId, "custom.xihai", StringComparison.Ordinal))
            {
                reason = "Provider implementation is not registered: " + providerId;
                return false;
            }
            if (string.Equals(providerId, "custom.xihai", StringComparison.Ordinal) &&
                !_registry.XihaiStaticReady)
            {
                reason = _registry.XihaiStaticReason;
                return false;
            }
            if (string.Equals(actionId, "act_dance_norse", StringComparison.Ordinal) &&
                !_registry.DanceStaticReady)
            {
                reason = _registry.DanceStaticReason;
                return false;
            }
            if (string.Equals(actionId, ModuleKneelLoopActionId, StringComparison.Ordinal) &&
                !_registry.KneelLoopStaticReady)
            {
                reason = _registry.KneelLoopStaticReason;
                return false;
            }
            if (string.Equals(
                    actionId,
                    ActionProviderRegistry.SpeechOpeningActionId,
                    StringComparison.Ordinal) &&
                !_registry.SpeechOpeningStaticReady)
            {
                reason = _registry.SpeechOpeningStaticReason;
                return false;
            }
            if (string.IsNullOrWhiteSpace(actionId))
            {
                failure = ExecutionResultCode.ActionIndexMissing;
                reason = "Action id is empty.";
                return false;
            }
            string cacheKey = providerId + "\u001f" + actionId;
            if (_readyActions.TryGetValue(cacheKey, out action))
            {
                failure = ExecutionResultCode.Queued;
                return true;
            }
            if (_missingActions.Contains(cacheKey))
            {
                failure = ExecutionResultCode.ActionIndexMissing;
                reason = "Action index is unavailable for this Mission.";
                return false;
            }

            try
            {
                ActionIndexCache candidate = ActionIndexCache.Create(actionId);
                if (candidate.Index < 0 &&
                    string.Equals(actionId, ModuleKneelLoopActionId, StringComparison.Ordinal))
                {
                    // Bannerlord 1.4.8 may omit a custom action type from the
                    // mission-scoped merged action index even though the module
                    // XML and TPAC are present. The Native action set already
                    // exposes the exact same loop animation, so use that
                    // engine-owned index as a runtime fallback while retaining
                    // the module binding for installations that do register it.
                    candidate = ActionIndexCache.Create(NativeKneelLoopActionId);
                    if (candidate.Index >= 0)
                    {
                        _readyActions.Add(cacheKey, candidate);
                        action = candidate;
                        failure = ExecutionResultCode.Queued;
                        reason = "Module kneel loop index missing; Native loop index fallback selected.";
                        return true;
                    }
                }
                if (candidate.Index < 0)
                {
                    _missingActions.Add(cacheKey);
                    failure = ExecutionResultCode.ActionIndexMissing;
                    reason = "ActionIndexCache.Create returned act_none for " + actionId;
                    return false;
                }
                _readyActions.Add(cacheKey, candidate);
                action = candidate;
                failure = ExecutionResultCode.Queued;
                return true;
            }
            catch (Exception ex)
            {
                _missingActions.Add(cacheKey);
                failure = ExecutionResultCode.ActionIndexMissing;
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        public void Clear()
        {
            _readyActions.Clear();
            _missingActions.Clear();
        }
    }
}
