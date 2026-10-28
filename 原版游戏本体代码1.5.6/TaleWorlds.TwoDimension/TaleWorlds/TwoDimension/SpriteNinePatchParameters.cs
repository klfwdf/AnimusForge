namespace TaleWorlds.TwoDimension;

public struct SpriteNinePatchParameters(int leftWidth, int rightWidth, int topHeight, int bottomHeight)
{
	public static SpriteNinePatchParameters Empty;

	public bool IsValid = true;

	public int LeftWidth = leftWidth;

	public int RightWidth = rightWidth;

	public int TopHeight = topHeight;

	public int BottomHeight = bottomHeight;
}
