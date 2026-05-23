namespace TaleWorlds.TwoDimension;

public struct SpriteNinePatchParameters
{
	public static SpriteNinePatchParameters Empty;

	public bool IsValid;

	public int LeftWidth;

	public int RightWidth;

	public int TopHeight;

	public int BottomHeight;

	public SpriteNinePatchParameters(int leftWidth, int rightWidth, int topHeight, int bottomHeight)
	{
		IsValid = true;
		LeftWidth = leftWidth;
		RightWidth = rightWidth;
		TopHeight = topHeight;
		BottomHeight = bottomHeight;
	}
}
