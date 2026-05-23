namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.NameMarker;

public class MarkerRect
{
	public float Left { get; private set; }

	public float Right { get; private set; }

	public float Top { get; private set; }

	public float Bottom { get; private set; }

	public float CenterX => Left + (Right - Left) / 2f;

	public float CenterY => Top + (Bottom - Top) / 2f;

	public float Width => Right - Left;

	public float Height => Bottom - Top;

	public MarkerRect()
	{
		Reset();
	}

	public void Reset()
	{
		Left = 0f;
		Right = 0f;
		Top = 0f;
		Bottom = 0f;
	}

	public void UpdatePoints(float left, float right, float top, float bottom)
	{
		Left = left;
		Right = right;
		Top = top;
		Bottom = bottom;
	}

	public bool IsOverlapping(MarkerRect other)
	{
		if (!(other.Left > Right) && !(other.Right < Left) && !(other.Top > Bottom))
		{
			return !(other.Bottom < Top);
		}
		return false;
	}
}
