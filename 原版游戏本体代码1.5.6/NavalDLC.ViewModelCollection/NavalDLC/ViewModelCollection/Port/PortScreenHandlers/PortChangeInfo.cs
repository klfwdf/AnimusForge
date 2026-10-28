namespace NavalDLC.ViewModelCollection.Port.PortScreenHandlers;

public readonly struct PortChangeInfo(float goldCost, string description)
{
	public readonly float GoldCost = goldCost;

	public readonly string Description = description;
}
