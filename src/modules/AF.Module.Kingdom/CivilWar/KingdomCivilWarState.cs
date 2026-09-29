using System;
using System.Collections.Generic;

namespace AnimusForge;

internal enum KingdomCivilWarStage
{
	None = 0,
	Tension = 1,
	FactionsFormed = 2,
	Standoff = 3,
	OpenWar = 4,
	Reconciled = 5
}

internal enum KingdomCivilWarSide
{
	Crown = 0,
	Opposition = 1,
	Middle = 2
}

internal sealed class KingdomCivilWarClanState
{
	public string ClanId = "";
	public KingdomCivilWarSide Side;
	public bool LeansOpposition;
	public bool BloodShy;
	public int Grievance;
}

internal enum KingdomCivilWarDemandKind
{
	None = 0,
	ContinueWar = 1,
	MakePeace = 2,
	KeepDecision = 3,
	RevokeDecision = 4,
	Redress = 5,
	Autonomy = 6,
	Usurpation = 7
}

internal sealed class KingdomCivilWarFactionState
{
	public string Id = "";
	public string Name = "";
	public string Demand = "";
	public KingdomCivilWarDemandKind Kind;
	public string LeaderClanId = "";
	public string EventKey = "";
	public int CreatedWeek;
	public bool Satisfied;
	public bool Separatist;
	public int Grievance;
	public List<string> ClanIds = new List<string>();
}

internal sealed class KingdomCivilWarKingdomState
{
	public string KingdomId = "";
	public KingdomCivilWarStage Stage = KingdomCivilWarStage.FactionsFormed;
	public int StageWeek;
	public int OppositionGrievance;
	public string OppositionLeaderClanId = "";
	public string CrownLeaderClanId = "";
	public string ActiveFactionId = "";
	public string PlayerSide = "";
	public bool OpenWarConsequencesApplied;
	public bool LoyalistsReady;
	public string Summary = "";
	public List<KingdomCivilWarClanState> Clans = new List<KingdomCivilWarClanState>();
	public List<KingdomCivilWarFactionState> Factions = new List<KingdomCivilWarFactionState>();
	public List<string> ConsumedEventKeys = new List<string>();
	public List<string> Reasons = new List<string>();
}

internal sealed class KingdomCivilWarStorage
{
	public int Version = 1;
	public Dictionary<string, KingdomCivilWarKingdomState> Kingdoms = new Dictionary<string, KingdomCivilWarKingdomState>(StringComparer.OrdinalIgnoreCase);
}
