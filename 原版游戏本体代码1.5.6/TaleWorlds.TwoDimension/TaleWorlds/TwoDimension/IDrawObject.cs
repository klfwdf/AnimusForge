namespace TaleWorlds.TwoDimension;

public interface IDrawObject
{
	bool IsValid { get; }

	Rectangle2D Rectangle { get; set; }
}
