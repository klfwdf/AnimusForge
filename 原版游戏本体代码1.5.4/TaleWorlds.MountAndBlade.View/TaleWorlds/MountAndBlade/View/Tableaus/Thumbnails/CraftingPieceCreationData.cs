using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public class CraftingPieceCreationData : ThumbnailCreationData
{
	public CraftingPiece CraftingPiece { get; private set; }

	public string Type { get; private set; }

	public CraftingPieceCreationData(CraftingPiece craftingPiece, string type, Action<Texture> setAction, Action cancelAction)
		: base(craftingPiece.StringId + "$" + type, setAction, cancelAction)
	{
		CraftingPiece = craftingPiece;
		Type = type;
	}
}
