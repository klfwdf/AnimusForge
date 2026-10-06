namespace TaleWorlds.CampaignSystem.Party;

public struct TroopTradeDifference
{
	public CharacterObject Troop { get; set; }

	public bool IsPrisoner { get; set; }

	public int FromCount { get; set; }

	public int ToCount { get; set; }

	public int DifferenceCount => FromCount - ToCount;

	public bool IsEmpty { get; private set; }

	public static TroopTradeDifference Empty => new TroopTradeDifference
	{
		IsEmpty = true
	};
}
