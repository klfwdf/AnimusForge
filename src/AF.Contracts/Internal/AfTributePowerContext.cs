namespace AnimusForge;

internal readonly struct AfTributePowerContext
{
	public AfTributePowerContext(
		float scorePayer,
		float scoreReceiver,
		float receiverDecisionThreshold,
		float settlementValue,
		float payerWarProgress,
		float receiverWarProgress,
		float warProgressDifference,
		float rawTributeRatio,
		float appliedTributeRatio,
		float payerFiefProsperity,
		int calculatedTribute)
	{
		ScorePayer = scorePayer;
		ScoreReceiver = scoreReceiver;
		ReceiverDecisionThreshold = receiverDecisionThreshold;
		SettlementValue = settlementValue;
		PayerWarProgress = payerWarProgress;
		ReceiverWarProgress = receiverWarProgress;
		WarProgressDifference = warProgressDifference;
		RawTributeRatio = rawTributeRatio;
		AppliedTributeRatio = appliedTributeRatio;
		PayerFiefProsperity = payerFiefProsperity;
		CalculatedTribute = calculatedTribute;
	}

	public float ScorePayer { get; }
	public float ScoreReceiver { get; }
	public float ReceiverDecisionThreshold { get; }
	public float SettlementValue { get; }
	public float PayerWarProgress { get; }
	public float ReceiverWarProgress { get; }
	public float WarProgressDifference { get; }
	public float RawTributeRatio { get; }
	public float AppliedTributeRatio { get; }
	public float PayerFiefProsperity { get; }
	public int CalculatedTribute { get; }

	public float ScoreDelta => ScoreReceiver - ScorePayer;
}
