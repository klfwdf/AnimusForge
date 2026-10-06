using System.Collections.Generic;

namespace TaleWorlds.Library.Graph;

public class GraphVM : ViewModel
{
	private MBBindingList<GraphLineVM> _lines;

	private string _horizontalAxisLabel;

	private string _verticalAxisLabel;

	private float _horizontalMinValue;

	private float _horizontalMaxValue;

	private float _verticalMinValue;

	private float _verticalMaxValue;

	[DataSourceProperty]
	public MBBindingList<GraphLineVM> Lines
	{
		get
		{
			return _lines;
		}
		set
		{
			if (value != _lines)
			{
				_lines = value;
				OnPropertyChangedWithValue(value, "Lines");
			}
		}
	}

	[DataSourceProperty]
	public string HorizontalAxisLabel
	{
		get
		{
			return _horizontalAxisLabel;
		}
		set
		{
			if (value != _horizontalAxisLabel)
			{
				_horizontalAxisLabel = value;
				OnPropertyChangedWithValue(value, "HorizontalAxisLabel");
			}
		}
	}

	[DataSourceProperty]
	public string VerticalAxisLabel
	{
		get
		{
			return _verticalAxisLabel;
		}
		set
		{
			if (value != _verticalAxisLabel)
			{
				_verticalAxisLabel = value;
				OnPropertyChangedWithValue(value, "VerticalAxisLabel");
			}
		}
	}

	[DataSourceProperty]
	public float HorizontalMinValue
	{
		get
		{
			return _horizontalMinValue;
		}
		set
		{
			if (value != _horizontalMinValue)
			{
				_horizontalMinValue = value;
				OnPropertyChangedWithValue(value, "HorizontalMinValue");
			}
		}
	}

	[DataSourceProperty]
	public float HorizontalMaxValue
	{
		get
		{
			return _horizontalMaxValue;
		}
		set
		{
			if (value != _horizontalMaxValue)
			{
				_horizontalMaxValue = value;
				OnPropertyChangedWithValue(value, "HorizontalMaxValue");
			}
		}
	}

	[DataSourceProperty]
	public float VerticalMinValue
	{
		get
		{
			return _verticalMinValue;
		}
		set
		{
			if (value != _verticalMinValue)
			{
				_verticalMinValue = value;
				OnPropertyChangedWithValue(value, "VerticalMinValue");
			}
		}
	}

	[DataSourceProperty]
	public float VerticalMaxValue
	{
		get
		{
			return _verticalMaxValue;
		}
		set
		{
			if (value != _verticalMaxValue)
			{
				_verticalMaxValue = value;
				OnPropertyChangedWithValue(value, "VerticalMaxValue");
			}
		}
	}

	public GraphVM(string horizontalAxisLabel, string verticalAxisLabel)
	{
		Lines = new MBBindingList<GraphLineVM>();
		HorizontalAxisLabel = horizontalAxisLabel;
		VerticalAxisLabel = verticalAxisLabel;
	}

	public void Draw(IEnumerable<(GraphLineVM line, IEnumerable<GraphLinePointVM> points)> linesWithPoints, in Vec2 horizontalRange, in Vec2 verticalRange, float autoRangeHorizontalCoefficient = 1f, float autoRangeVerticalCoefficient = 1f, bool useAutoHorizontalRange = false, bool useAutoVerticalRange = false)
	{
		Lines.Clear();
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		foreach (var linesWithPoint in linesWithPoints)
		{
			var (graphLineVM, _) = linesWithPoint;
			foreach (GraphLinePointVM item in linesWithPoint.points)
			{
				if (useAutoHorizontalRange)
				{
					if (item.HorizontalValue < num)
					{
						num = item.HorizontalValue;
					}
					if (item.HorizontalValue > num2)
					{
						num2 = item.HorizontalValue;
					}
				}
				if (useAutoVerticalRange)
				{
					if (item.VerticalValue < num3)
					{
						num3 = item.VerticalValue;
					}
					if (item.VerticalValue > num4)
					{
						num4 = item.VerticalValue;
					}
				}
				graphLineVM.Points.Add(item);
			}
			Lines.Add(graphLineVM);
		}
		float extendedMinValue = horizontalRange.X;
		float extendedMaxValue = horizontalRange.Y;
		float extendedMinValue2 = verticalRange.X;
		float extendedMaxValue2 = verticalRange.Y;
		bool flag = num != float.MaxValue && num2 != float.MinValue;
		bool flag2 = num3 != float.MaxValue && num4 != float.MinValue;
		if (useAutoHorizontalRange && flag)
		{
			ExtendRangeToNearestMultipleOfCoefficient(num, num2, autoRangeHorizontalCoefficient, out extendedMinValue, out extendedMaxValue);
		}
		if (useAutoVerticalRange && flag2)
		{
			ExtendRangeToNearestMultipleOfCoefficient(num3, num4, autoRangeVerticalCoefficient, out extendedMinValue2, out extendedMaxValue2);
		}
		HorizontalMinValue = extendedMinValue;
		HorizontalMaxValue = extendedMaxValue;
		VerticalMinValue = extendedMinValue2;
		VerticalMaxValue = extendedMaxValue2;
	}

	private static void ExtendRangeToNearestMultipleOfCoefficient(float minValue, float maxValue, float coefficient, out float extendedMinValue, out float extendedMaxValue)
	{
		if (coefficient > 1E-05f)
		{
			extendedMinValue = (float)MathF.Floor(minValue / coefficient) * coefficient;
			extendedMaxValue = (float)MathF.Ceiling(maxValue / coefficient) * coefficient;
			if (extendedMinValue.ApproximatelyEqualsTo(extendedMaxValue))
			{
				if (extendedMinValue - coefficient > 0f)
				{
					extendedMinValue -= coefficient;
				}
				else
				{
					extendedMaxValue += coefficient;
				}
			}
		}
		else
		{
			extendedMinValue = minValue;
			extendedMaxValue = maxValue;
		}
	}
}
