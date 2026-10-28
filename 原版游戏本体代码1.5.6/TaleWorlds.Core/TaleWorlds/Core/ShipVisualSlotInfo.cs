namespace TaleWorlds.Core;

public struct ShipVisualSlotInfo(string visualSlotId, string visualPieceId)
{
	public string VisualSlotTag = visualSlotId;

	public string VisualPieceId = visualPieceId;
}
