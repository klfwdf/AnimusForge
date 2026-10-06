namespace TaleWorlds.Core;

public struct ShipVisualSlotInfo
{
	public string VisualSlotTag;

	public string VisualPieceId;

	public ShipVisualSlotInfo(string visualSlotId, string visualPieceId)
	{
		VisualSlotTag = visualSlotId;
		VisualPieceId = visualPieceId;
	}
}
