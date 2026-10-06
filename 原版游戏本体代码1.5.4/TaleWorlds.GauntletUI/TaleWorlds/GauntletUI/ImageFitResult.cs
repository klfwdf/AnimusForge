namespace TaleWorlds.GauntletUI;

public readonly struct ImageFitResult
{
	public readonly float OffsetX;

	public readonly float OffsetY;

	public readonly float Width;

	public readonly float Height;

	public ImageFitResult(float offsetX, float offsetY, float width, float height)
	{
		OffsetX = offsetX;
		OffsetY = offsetY;
		Width = width;
		Height = height;
	}
}
