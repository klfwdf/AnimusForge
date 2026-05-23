namespace TaleWorlds.Library.Graph;

public class GraphLinePointVM : ViewModel
{
	private float _horizontalValue;

	private float _verticalValue;

	[DataSourceProperty]
	public float HorizontalValue
	{
		get
		{
			return _horizontalValue;
		}
		set
		{
			if (value != _horizontalValue)
			{
				_horizontalValue = value;
				OnPropertyChangedWithValue(value, "HorizontalValue");
			}
		}
	}

	[DataSourceProperty]
	public float VerticalValue
	{
		get
		{
			return _verticalValue;
		}
		set
		{
			if (value != _verticalValue)
			{
				_verticalValue = value;
				OnPropertyChangedWithValue(value, "VerticalValue");
			}
		}
	}

	public GraphLinePointVM(float horizontalValue, float verticalValue)
	{
		HorizontalValue = horizontalValue;
		VerticalValue = verticalValue;
	}
}
