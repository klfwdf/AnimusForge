using System;
using System.Collections.Generic;

namespace AnimusForge.ExpeditionParade.Configuration;

internal sealed class ParadeSettings
{
	internal const int CurrentSchemaVersion = 1;

	internal int SchemaVersion { get; set; } = CurrentSchemaVersion;

	internal int MaximumTroopAgents { get; set; } = 96;

	internal int MaximumTemporaryCivilians { get; set; } = 24;

	internal int ReservedMissionAgents { get; set; } = 32;

	internal int MinimumViableTroopAgents { get; set; } = 1;

	internal float FormationGap { get; set; } = 7f;

	internal float MarchSpeed { get; set; } = 1.4f;

	internal bool IncludeHeroes { get; set; }

	internal bool AllowMountedFallbackToFoot { get; set; }

	internal bool HideRegularHud { get; set; }

	internal bool EnableFreeCamera { get; set; }

	internal bool EnableDebugRouteOverride { get; set; }

	internal bool EnableDebugDrawing { get; set; }

	internal bool EnableDetailedLogging { get; set; } = true;

	internal ParadeSettings Clone()
	{
		return (ParadeSettings)MemberwiseClone();
	}

	internal IReadOnlyList<string> Validate()
	{
		List<string> errors = new();
		if (SchemaVersion != CurrentSchemaVersion)
		{
			errors.Add("settings_schema_unsupported");
		}
		if (MaximumTroopAgents <= 0)
		{
			errors.Add("maximum_troop_agents_must_be_positive");
		}
		if (MaximumTemporaryCivilians < 0)
		{
			errors.Add("maximum_temporary_civilians_cannot_be_negative");
		}
		if (ReservedMissionAgents < 0)
		{
			errors.Add("reserved_mission_agents_cannot_be_negative");
		}
		if (MinimumViableTroopAgents <= 0 || MinimumViableTroopAgents > MaximumTroopAgents)
		{
			errors.Add("minimum_viable_troops_out_of_range");
		}
		if (FormationGap <= 0f || float.IsNaN(FormationGap) || float.IsInfinity(FormationGap))
		{
			errors.Add("formation_gap_invalid");
		}
		if (MarchSpeed <= 0f || float.IsNaN(MarchSpeed) || float.IsInfinity(MarchSpeed))
		{
			errors.Add("march_speed_invalid");
		}
		return errors;
	}
}
