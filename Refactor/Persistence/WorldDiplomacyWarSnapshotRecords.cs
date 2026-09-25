namespace AnimusForge;

public sealed class WorldDiplomacyBorderRelation
{
	public bool SharesBorder;
	public string FirstSettlementId = "";
	public string FirstSettlementName = "";
	public string SecondSettlementId = "";
	public string SecondSettlementName = "";
	public float Distance = float.MaxValue;
}

public sealed class WarSituationSnapshot
{
	public int Day;
	public bool IsAtWar;
	public int WarDays;
	public float AuthorStrength;
	public float TargetStrength;
	public float AuthorProgress;
	public float TargetProgress;
	public int AuthorInflictedCasualties;
	public int AuthorSufferedCasualties;
	public int AuthorSuccessfulSieges;
	public int TargetSuccessfulSieges;
	public int AuthorOtherWars;
	public int TargetOtherWars;
	public float AuthorPeacePressure;
	public float TargetPeacePressure;
	public float AuthorCessionScore;
	public float TargetCessionScore;
	public int AuthorSuggestedTribute;
	public int TargetSuggestedTribute;
}
