using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

public class CraftingPieceImageIdentifierVM : ImageIdentifierVM
{
	public CraftingPieceImageIdentifierVM(CraftingPiece craftingPiece, string pieceUsageId)
	{
		base.ImageIdentifier = new CraftingPieceImageIdentifier(craftingPiece, pieceUsageId);
	}
}
