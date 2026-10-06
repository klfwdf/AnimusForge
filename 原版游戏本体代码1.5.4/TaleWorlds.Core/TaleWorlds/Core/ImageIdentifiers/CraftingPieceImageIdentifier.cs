namespace TaleWorlds.Core.ImageIdentifiers;

public class CraftingPieceImageIdentifier : ImageIdentifier
{
	public CraftingPieceImageIdentifier(CraftingPiece craftingPiece, string pieceUsageId)
	{
		base.Id = ((craftingPiece != null) ? (craftingPiece.StringId + "$" + pieceUsageId) : "");
		base.AdditionalArgs = "";
		base.TextureProviderName = "CraftingPieceImageTextureProvider";
	}
}
