namespace TaleWorlds.TwoDimension;

internal class TextTokenOutput
{
	public float X { get; private set; }

	public float Y { get; private set; }

	public float Width { get; private set; }

	public float Height { get; private set; }

	public float Scale { get; private set; }

	public SimpleRectangle Rectangle { get; private set; }

	public TextToken Token { get; private set; }

	public string Style { get; private set; }

	public TextTokenOutput(TextToken token, float width, float height, string style, float scaleValue)
	{
		Token = token;
		Width = width;
		Height = height;
		Rectangle = new SimpleRectangle(0f, 0f, Width, Height);
		Style = style;
		Scale = scaleValue;
	}

	public void SetPosition(float x, float y)
	{
		X = x;
		Y = y;
		Rectangle = new SimpleRectangle(x, y, Width, Height);
	}
}
