namespace NavalDLC.ViewModelCollection.Port.PortScreenHandlers;

public readonly struct PortChangeInfo
{
	public readonly float GoldCost;

	public readonly string Description;

	public PortChangeInfo(float goldCost, string description)
	{
		GoldCost = goldCost;
		Description = description;
	}
}
