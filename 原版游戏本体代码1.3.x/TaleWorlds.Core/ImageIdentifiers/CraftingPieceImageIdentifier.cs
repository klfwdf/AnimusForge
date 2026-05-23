using System;

namespace TaleWorlds.Core.ImageIdentifiers
{
	// Token: 0x020000E5 RID: 229
	public class CraftingPieceImageIdentifier : ImageIdentifier
	{
		// Token: 0x06000B7F RID: 2943 RVA: 0x00025015 File Offset: 0x00023215
		public CraftingPieceImageIdentifier(CraftingPiece craftingPiece, string pieceUsageId)
		{
			base.Id = ((craftingPiece != null) ? (craftingPiece.StringId + "$" + pieceUsageId) : "");
			base.AdditionalArgs = "";
			base.TextureProviderName = "CraftingPieceImageTextureProvider";
		}
	}
}
