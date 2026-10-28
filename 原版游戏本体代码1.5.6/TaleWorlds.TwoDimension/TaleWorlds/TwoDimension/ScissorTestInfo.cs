namespace TaleWorlds.TwoDimension;

public struct ScissorTestInfo
{
	private float _x;

	private float _x2;

	private float _y;

	private float _y2;

	public float X => _x;

	public float X2 => _x2;

	public float Y => _y;

	public float Y2 => _y2;

	public ScissorTestInfo(float x, float y, float x2, float y2)
	{
		_x = x;
		_y = y;
		_x2 = x2;
		_y2 = y2;
	}

	public void ReduceToIntersection(ScissorTestInfo other)
	{
		_x = Mathf.Max(_x, other._x);
		_y = Mathf.Max(_y, other._y);
		_x2 = Mathf.Min(_x2, other._x2);
		_y2 = Mathf.Min(_y2, other._y2);
	}

	public SimpleRectangle GetSimpleRectangle()
	{
		return new SimpleRectangle(_x, _y, _x2 - _x, _y2 - _y);
	}

	public bool IsCollide(in Rectangle2D other)
	{
		SimpleRectangle boundingBox = other.GetBoundingBox();
		return GetSimpleRectangle().IsCollide(boundingBox);
	}
}
