namespace TaleWorlds.Core;

public struct ShipSlotAndPieceName
{
	public string SlotName;

	public string PieceName;

	public ShipSlotAndPieceName(string slotName, string pieceName)
	{
		SlotName = slotName;
		PieceName = pieceName;
	}
}
