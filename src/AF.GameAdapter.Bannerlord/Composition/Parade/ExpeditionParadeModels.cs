using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

internal enum ExpeditionParadeType
{
	Expedition,
	Victory
}

internal enum ExpeditionParadeFormationKind
{
	Infantry,
	Ranged,
	Cavalry,
	HorseArcher
}

internal sealed class ExpeditionParadeWarRecord
{
	public string FirstFactionId { get; set; } = "";
	public string SecondFactionId { get; set; } = "";
	public double DeclarationDay { get; set; }
	public int FirstOpeningFiefs { get; set; }
	public int SecondOpeningFiefs { get; set; }
	public bool HasExactOpeningSnapshot { get; set; }
}

internal sealed class ExpeditionParadeVictoryRecord
{
	public string Id { get; set; } = "";
	public string WinningFactionId { get; set; } = "";
	public string LosingFactionId { get; set; } = "";
	public double PeaceDay { get; set; }
	public double ExpiresDay { get; set; }
	public string Basis { get; set; } = "";
}

internal sealed class ExpeditionParadeFormationSpec
{
	public string SourceId { get; set; } = "";
	public string SourceName { get; set; } = "";
	public Banner Banner { get; set; }
	public ExpeditionParadeFormationKind Kind { get; set; }
	public bool Mounted { get; set; }
	public List<CharacterObject> Troops { get; set; } = new List<CharacterObject>();
	public int Columns => Mounted ? 2 : 3;
	public int Rows => Math.Max(1, (Troops.Count + Columns - 1) / Columns);
}

internal sealed class ExpeditionParadeRequest
{
	public ExpeditionParadeType Type { get; set; }
	public string SettlementId { get; set; } = "";
	public string GovernorHeroId { get; set; } = "";
	public bool FromSettlementMenu { get; set; }
	public double QueuedDay { get; set; }
	public List<ExpeditionParadeFormationSpec> Formations { get; set; } = new List<ExpeditionParadeFormationSpec>();
	public int DisplayTroopCount { get; set; }
}

internal sealed class ExpeditionParadeAvailability
{
	public Settlement Settlement { get; set; }
	public bool ExpeditionAvailable { get; set; }
	public bool VictoryAvailable { get; set; }
	public string ExpeditionReason { get; set; } = "";
	public string VictoryReason { get; set; } = "";
	public bool Any => ExpeditionAvailable || VictoryAvailable;
}

