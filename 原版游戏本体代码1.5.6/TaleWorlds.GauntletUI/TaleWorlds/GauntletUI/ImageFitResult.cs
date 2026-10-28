namespace TaleWorlds.GauntletUI;

public readonly struct ImageFitResult(float offsetX, float offsetY, float width, float height)
{
	public readonly float OffsetX = offsetX;

	public readonly float OffsetY = offsetY;

	public readonly float Width = width;

	public readonly float Height = height;
}
