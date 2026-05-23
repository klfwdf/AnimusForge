using System;
using TaleWorlds.Core.ImageIdentifiers;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers
{
	// Token: 0x0200001F RID: 31
	public class CraftingPieceImageIdentifierVM : ImageIdentifierVM
	{
		// Token: 0x0600019B RID: 411 RVA: 0x000059D2 File Offset: 0x00003BD2
		public CraftingPieceImageIdentifierVM(CraftingPiece craftingPiece, string pieceUsageId)
		{
			base.ImageIdentifier = new CraftingPieceImageIdentifier(craftingPiece, pieceUsageId);
		}
	}
}
