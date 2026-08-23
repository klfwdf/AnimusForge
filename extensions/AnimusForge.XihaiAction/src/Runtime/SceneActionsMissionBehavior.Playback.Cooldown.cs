using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnimusForge.SceneActions.Core;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior
    {
        private bool IsCooldownActive(Agent agent, string actionKey, double now)
        {
            string key = agent.Index + "\u001f" + actionKey;
            if (!_cooldowns.TryGetValue(key, out CooldownRecord record))
            {
                return false;
            }
            if (!ReferenceEquals(record.Agent, agent) || now >= record.UntilMissionTime)
            {
                _cooldowns.Remove(key);
                return false;
            }
            return true;
        }
        private void SetCooldown(Agent agent, SelectedAction selected, double now)
        {
            float seconds = selected.Definition.CooldownSeconds;
            if (SceneActionsRuntimeHost.Settings.ActionOverrides.TryGetValue(
                selected.Definition.Key,
                out ActionOverride actionOverride) &&
                actionOverride?.CooldownSeconds.HasValue == true)
            {
                seconds = actionOverride.CooldownSeconds.Value;
            }
            _cooldowns[agent.Index + "\u001f" + selected.Definition.Key] = new CooldownRecord
            {
                Agent = agent,
                UntilMissionTime = now + Math.Max(0f, seconds)
            };
        }
        private void CleanupCooldowns(double now)
        {
            foreach (KeyValuePair<string, CooldownRecord> entry in _cooldowns.ToArray())
            {
                if (now >= entry.Value.UntilMissionTime || !entry.Value.Agent.IsActive())
                {
                    _cooldowns.Remove(entry.Key);
                }
            }
        }
    }
}