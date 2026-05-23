using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TaleWorlds.Library.Graph
{
	// Token: 0x020000B7 RID: 183
	public class GraphVM : ViewModel
	{
		// Token: 0x060006C9 RID: 1737 RVA: 0x00017058 File Offset: 0x00015258
		public GraphVM(string horizontalAxisLabel, string verticalAxisLabel)
		{
			this.Lines = new MBBindingList<GraphLineVM>();
			this.HorizontalAxisLabel = horizontalAxisLabel;
			this.VerticalAxisLabel = verticalAxisLabel;
		}

		// Token: 0x060006CA RID: 1738 RVA: 0x0001707C File Offset: 0x0001527C
		public void Draw([TupleElementNames(new string[] { "line", "points" })] IEnumerable<ValueTuple<GraphLineVM, IEnumerable<GraphLinePointVM>>> linesWithPoints, in Vec2 horizontalRange, in Vec2 verticalRange, float autoRangeHorizontalCoefficient = 1f, float autoRangeVerticalCoefficient = 1f, bool useAutoHorizontalRange = false, bool useAutoVerticalRange = false)
		{
			this.Lines.Clear();
			float num = float.MaxValue;
			float num2 = float.MinValue;
			float num3 = float.MaxValue;
			float num4 = float.MinValue;
			foreach (ValueTuple<GraphLineVM, IEnumerable<GraphLinePointVM>> valueTuple in linesWithPoints)
			{
				GraphLineVM item = valueTuple.Item1;
				foreach (GraphLinePointVM graphLinePointVM in valueTuple.Item2)
				{
					if (useAutoHorizontalRange)
					{
						if (graphLinePointVM.HorizontalValue < num)
						{
							num = graphLinePointVM.HorizontalValue;
						}
						if (graphLinePointVM.HorizontalValue > num2)
						{
							num2 = graphLinePointVM.HorizontalValue;
						}
					}
					if (useAutoVerticalRange)
					{
						if (graphLinePointVM.VerticalValue < num3)
						{
							num3 = graphLinePointVM.VerticalValue;
						}
						if (graphLinePointVM.VerticalValue > num4)
						{
							num4 = graphLinePointVM.VerticalValue;
						}
					}
					item.Points.Add(graphLinePointVM);
				}
				this.Lines.Add(item);
			}
			Vec2 vec = horizontalRange;
			float x = vec.X;
			vec = horizontalRange;
			float y = vec.Y;
			vec = verticalRange;
			float x2 = vec.X;
			vec = verticalRange;
			float y2 = vec.Y;
			bool flag = num != float.MaxValue && num2 != float.MinValue;
			bool flag2 = num3 != float.MaxValue && num4 != float.MinValue;
			if (useAutoHorizontalRange && flag)
			{
				GraphVM.ExtendRangeToNearestMultipleOfCoefficient(num, num2, autoRangeHorizontalCoefficient, out x, out y);
			}
			if (useAutoVerticalRange && flag2)
			{
				GraphVM.ExtendRangeToNearestMultipleOfCoefficient(num3, num4, autoRangeVerticalCoefficient, out x2, out y2);
			}
			this.HorizontalMinValue = x;
			this.HorizontalMaxValue = y;
			this.VerticalMinValue = x2;
			this.VerticalMaxValue = y2;
		}

		// Token: 0x060006CB RID: 1739 RVA: 0x00017254 File Offset: 0x00015454
		private static void ExtendRangeToNearestMultipleOfCoefficient(float minValue, float maxValue, float coefficient, out float extendedMinValue, out float extendedMaxValue)
		{
			if (coefficient > 1E-05f)
			{
				extendedMinValue = (float)MathF.Floor(minValue / coefficient) * coefficient;
				extendedMaxValue = (float)MathF.Ceiling(maxValue / coefficient) * coefficient;
				if (extendedMinValue.ApproximatelyEqualsTo(extendedMaxValue, 1E-05f))
				{
					if (extendedMinValue - coefficient > 0f)
					{
						extendedMinValue -= coefficient;
						return;
					}
					extendedMaxValue += coefficient;
					return;
				}
			}
			else
			{
				extendedMinValue = minValue;
				extendedMaxValue = maxValue;
			}
		}

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x060006CC RID: 1740 RVA: 0x000172B9 File Offset: 0x000154B9
		// (set) Token: 0x060006CD RID: 1741 RVA: 0x000172C1 File Offset: 0x000154C1
		[DataSourceProperty]
		public MBBindingList<GraphLineVM> Lines
		{
			get
			{
				return this._lines;
			}
			set
			{
				if (value != this._lines)
				{
					this._lines = value;
					base.OnPropertyChangedWithValue<MBBindingList<GraphLineVM>>(value, "Lines");
				}
			}
		}

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x060006CE RID: 1742 RVA: 0x000172DF File Offset: 0x000154DF
		// (set) Token: 0x060006CF RID: 1743 RVA: 0x000172E7 File Offset: 0x000154E7
		[DataSourceProperty]
		public string HorizontalAxisLabel
		{
			get
			{
				return this._horizontalAxisLabel;
			}
			set
			{
				if (value != this._horizontalAxisLabel)
				{
					this._horizontalAxisLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "HorizontalAxisLabel");
				}
			}
		}

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x060006D0 RID: 1744 RVA: 0x0001730A File Offset: 0x0001550A
		// (set) Token: 0x060006D1 RID: 1745 RVA: 0x00017312 File Offset: 0x00015512
		[DataSourceProperty]
		public string VerticalAxisLabel
		{
			get
			{
				return this._verticalAxisLabel;
			}
			set
			{
				if (value != this._verticalAxisLabel)
				{
					this._verticalAxisLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "VerticalAxisLabel");
				}
			}
		}

		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x060006D2 RID: 1746 RVA: 0x00017335 File Offset: 0x00015535
		// (set) Token: 0x060006D3 RID: 1747 RVA: 0x0001733D File Offset: 0x0001553D
		[DataSourceProperty]
		public float HorizontalMinValue
		{
			get
			{
				return this._horizontalMinValue;
			}
			set
			{
				if (value != this._horizontalMinValue)
				{
					this._horizontalMinValue = value;
					base.OnPropertyChangedWithValue(value, "HorizontalMinValue");
				}
			}
		}

		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x060006D4 RID: 1748 RVA: 0x0001735B File Offset: 0x0001555B
		// (set) Token: 0x060006D5 RID: 1749 RVA: 0x00017363 File Offset: 0x00015563
		[DataSourceProperty]
		public float HorizontalMaxValue
		{
			get
			{
				return this._horizontalMaxValue;
			}
			set
			{
				if (value != this._horizontalMaxValue)
				{
					this._horizontalMaxValue = value;
					base.OnPropertyChangedWithValue(value, "HorizontalMaxValue");
				}
			}
		}

		// Token: 0x170000D4 RID: 212
		// (get) Token: 0x060006D6 RID: 1750 RVA: 0x00017381 File Offset: 0x00015581
		// (set) Token: 0x060006D7 RID: 1751 RVA: 0x00017389 File Offset: 0x00015589
		[DataSourceProperty]
		public float VerticalMinValue
		{
			get
			{
				return this._verticalMinValue;
			}
			set
			{
				if (value != this._verticalMinValue)
				{
					this._verticalMinValue = value;
					base.OnPropertyChangedWithValue(value, "VerticalMinValue");
				}
			}
		}

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x060006D8 RID: 1752 RVA: 0x000173A7 File Offset: 0x000155A7
		// (set) Token: 0x060006D9 RID: 1753 RVA: 0x000173AF File Offset: 0x000155AF
		[DataSourceProperty]
		public float VerticalMaxValue
		{
			get
			{
				return this._verticalMaxValue;
			}
			set
			{
				if (value != this._verticalMaxValue)
				{
					this._verticalMaxValue = value;
					base.OnPropertyChangedWithValue(value, "VerticalMaxValue");
				}
			}
		}

		// Token: 0x04000213 RID: 531
		private MBBindingList<GraphLineVM> _lines;

		// Token: 0x04000214 RID: 532
		private string _horizontalAxisLabel;

		// Token: 0x04000215 RID: 533
		private string _verticalAxisLabel;

		// Token: 0x04000216 RID: 534
		private float _horizontalMinValue;

		// Token: 0x04000217 RID: 535
		private float _horizontalMaxValue;

		// Token: 0x04000218 RID: 536
		private float _verticalMinValue;

		// Token: 0x04000219 RID: 537
		private float _verticalMaxValue;
	}
}
