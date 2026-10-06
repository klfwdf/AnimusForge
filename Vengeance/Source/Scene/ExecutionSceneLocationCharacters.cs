using System;
using System.Collections.Generic;
using RichExecutions.Diagnostics;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

// Native passage/flee logic resolves a LocationCharacter by the exact Origin.
// Temporary ceremony actors need that identity before any town AI can run.
internal sealed class ExecutionSceneLocationCharacters
{
    private readonly List<(Location Location, LocationComplex Complex, LocationCharacter Character)> _owned = new();

    internal void Register(Agent agent, Location? location, LocationComplex? complex, bool civilianEquipment)
    {
        if (agent.Character is not CharacterObject character || character.IsHero) return;
        if (location == null || complex == null || agent.Origin == null)
            throw new InvalidOperationException("Temporary execution actors require a live town location and origin.");
        if (location.GetLocationCharacter(agent.Origin) != null) return;

        var entry = new LocationCharacter(
            new AgentData(agent.Origin).Monster(agent.Monster),
            SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors,
            null, fixedLocation: true, LocationCharacter.CharacterRelations.Neutral,
            null, civilianEquipment, overrideBodyProperties: false);
        // Record ownership before AddCharacter so failed initialization can clean up.
        _owned.Add((location, complex, entry));
        location.AddCharacter(entry);
    }

    internal bool Clear()
    {
        var complete = true;
        for (var i = _owned.Count - 1; i >= 0; i--)
        {
            var entry = _owned[i];
            try
            {
                // A frightened spectator may already have used a door. Remove
                // from the captured complex as well as the initial location.
                entry.Complex.RemoveCharacterIfExists(entry.Character);
                entry.Location.RemoveLocationCharacter(entry.Character);
                _owned.RemoveAt(i);
            }
            catch (Exception exception)
            {
                complete = false;
                RexLog.Error("Could not remove a temporary execution location character; cleanup will retry.", exception);
            }
        }
        return complete;
    }
}
