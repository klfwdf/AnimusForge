using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout;

public class GridLayout : ILayout
{
	public GridVerticalLayoutMethod VerticalLayoutMethod { get; set; }

	public GridHorizontalLayoutMethod HorizontalLayoutMethod { get; set; }

	public GridDirection Direction { get; set; }

	public IReadOnlyList<float> RowHeights { get; private set; } = new List<float>();

	public IReadOnlyList<float> ColumnWidths { get; private set; } = new List<float>();

	public GridLayout()
	{
		VerticalLayoutMethod = GridVerticalLayoutMethod.TopToBottom;
		HorizontalLayoutMethod = GridHorizontalLayoutMethod.LeftToRight;
	}

	Vector2 ILayout.MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale)
	{
		GridWidget gridWidget = (GridWidget)widget;
		Vector2 result = default(Vector2);
		int num = gridWidget.Children.Count((Widget x) => x.IsVisible);
		if (num > 0)
		{
			foreach (Widget child in gridWidget.Children)
			{
				if (child.IsVisible && (child.WidthSizePolicy == SizePolicy.CoverChildren || child.HeightSizePolicy == SizePolicy.CoverChildren))
				{
					child.Measure(default(Vector2));
				}
			}
			CalculateRowColumnCounts(gridWidget, Direction, num, out var rowCount, out var usedRowCount, out var columnCount, out var usedColumnCount);
			UpdateCellSizes(gridWidget, rowCount, usedRowCount, columnCount, usedColumnCount, measureSpec.X, measureSpec.Y);
			int num2 = 0;
			for (int num3 = 0; num3 < gridWidget.Children.Count; num3++)
			{
				Widget widget2 = gridWidget.Children[num3];
				if (widget2.IsVisible)
				{
					CalculateRowColumnIndices(num2, rowCount, columnCount, out var row, out var column);
					float element = GetElement(ColumnWidths, column);
					float element2 = GetElement(RowHeights, row);
					widget2.Measure(new Vector2(element, element2));
					num2++;
				}
			}
			result = new Vector2(ColumnWidths.Sum(), RowHeights.Sum());
		}
		return result;
	}

	void ILayout.OnLayout(Widget widget, float left, float bottom, float right, float top)
	{
		GridWidget gridWidget = (GridWidget)widget;
		int num = gridWidget.Children.Count((Widget x) => x.IsVisible);
		if (num <= 0)
		{
			return;
		}
		float totalWidth = right - left;
		float totalHeight = bottom - top;
		CalculateRowColumnCounts(gridWidget, Direction, num, out var rowCount, out var usedRowCount, out var columnCount, out var usedColumnCount);
		UpdateCellSizes(gridWidget, rowCount, usedRowCount, columnCount, usedColumnCount, totalWidth, totalHeight);
		float[] array = new float[RowHeights.Count + 1];
		float[] array2 = new float[ColumnWidths.Count + 1];
		array[0] = 0f;
		array2[0] = 0f;
		for (int num2 = 0; num2 < RowHeights.Count; num2++)
		{
			array[num2 + 1] = RowHeights[num2] + array[num2];
		}
		for (int num3 = 0; num3 < ColumnWidths.Count; num3++)
		{
			array2[num3 + 1] = ColumnWidths[num3] + array2[num3];
		}
		int num4 = 0;
		for (int num5 = 0; num5 < gridWidget.Children.Count; num5++)
		{
			Widget widget2 = gridWidget.Children[num5];
			if (!widget2.IsVisible)
			{
				continue;
			}
			CalculateRowColumnIndices(num4, rowCount, columnCount, out var row, out var column);
			int index = usedRowCount - row - 1;
			int index2 = usedColumnCount - column - 1;
			float element = GetElement(ColumnWidths, column);
			float element2 = GetElement(RowHeights, row);
			float num6 = 0f;
			float num7 = 0f;
			if (VerticalLayoutMethod == GridVerticalLayoutMethod.TopToBottom)
			{
				num6 = GetElement(array, row);
			}
			else if (VerticalLayoutMethod == GridVerticalLayoutMethod.Center)
			{
				if (Direction == GridDirection.ColumnFirst)
				{
					int index3 = MathF.Max(0, (column + 1) * usedRowCount - num) / 2 + row;
					num6 = GetElement(array, index3);
				}
			}
			else if (VerticalLayoutMethod == GridVerticalLayoutMethod.BottomToTop)
			{
				num6 = GetElement(array, index);
			}
			if (HorizontalLayoutMethod == GridHorizontalLayoutMethod.LeftToRight)
			{
				num7 = GetElement(array2, column);
			}
			else if (HorizontalLayoutMethod == GridHorizontalLayoutMethod.Center)
			{
				if (Direction == GridDirection.RowFirst)
				{
					int index4 = MathF.Max(0, (row + 1) * usedColumnCount - num) / 2 + column;
					num7 = GetElement(array2, index4);
				}
			}
			else if (HorizontalLayoutMethod == GridHorizontalLayoutMethod.RightToLeft)
			{
				num7 = GetElement(array2, index2);
			}
			widget2.Layout(num7, num6 + element2, num7 + element, num6);
			num4++;
		}
	}

	private void UpdateCellSizes(GridWidget gridWidget, int rowCount, int usedRowCount, int columnCount, int usedColumnCount, float totalWidth, float totalHeight)
	{
		float[] array = new float[usedRowCount];
		float[] array2 = new float[usedColumnCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = 0f;
		}
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j] = 0f;
		}
		int num = 0;
		for (int k = 0; k < gridWidget.Children.Count; k++)
		{
			Widget widget = gridWidget.Children[k];
			if (widget.IsVisible)
			{
				CalculateRowColumnIndices(num, rowCount, columnCount, out var row, out var column);
				float num2 = 0f;
				float num3 = 0f;
				num2 = ((gridWidget.WidthSizePolicy != SizePolicy.CoverChildren) ? (totalWidth / (float)columnCount) : ((!gridWidget.UseDynamicCellWidth || widget.WidthSizePolicy == SizePolicy.StretchToParent) ? gridWidget.DefaultScaledCellWidth : (widget.MeasuredSize.X + widget.ScaledMarginLeft + widget.ScaledMarginRight)));
				num3 = ((gridWidget.HeightSizePolicy != SizePolicy.CoverChildren) ? (totalHeight / (float)rowCount) : ((!gridWidget.UseDynamicCellHeight || widget.HeightSizePolicy == SizePolicy.StretchToParent) ? gridWidget.DefaultScaledCellHeight : (widget.MeasuredSize.Y + widget.ScaledMarginTop + widget.ScaledMarginBottom)));
				if (row >= 0 && row < array.Length)
				{
					array[row] = MathF.Max(num3, array[row]);
				}
				if (column >= 0 && column < array2.Length)
				{
					array2[column] = MathF.Max(num2, array2[column]);
				}
				num++;
			}
		}
		RowHeights = array;
		ColumnWidths = array2;
	}

	private void CalculateRowColumnIndices(int visibleIndex, int rowCount, int columnCount, out int row, out int column)
	{
		if (Direction == GridDirection.RowFirst)
		{
			row = visibleIndex / columnCount;
			column = visibleIndex % columnCount;
		}
		else
		{
			row = visibleIndex % rowCount;
			column = visibleIndex / rowCount;
		}
	}

	private void CalculateRowColumnCounts(GridWidget gridWidget, GridDirection direction, int visibleChildrenCount, out int rowCount, out int usedRowCount, out int columnCount, out int usedColumnCount)
	{
		bool flag = gridWidget.RowCount < 0;
		bool flag2 = gridWidget.ColumnCount < 0;
		rowCount = (flag ? 3 : gridWidget.RowCount);
		columnCount = (flag2 ? 3 : gridWidget.ColumnCount);
		int num = 0;
		int num2 = 0;
		if (direction == GridDirection.RowFirst)
		{
			num2 = MathF.Min(visibleChildrenCount, columnCount);
			num = ((visibleChildrenCount % columnCount > 0) ? 1 : 0) + visibleChildrenCount / columnCount;
		}
		else
		{
			num = MathF.Min(visibleChildrenCount, rowCount);
			num2 = ((visibleChildrenCount % rowCount > 0) ? 1 : 0) + visibleChildrenCount / rowCount;
		}
		bool flag3 = gridWidget.HeightSizePolicy != SizePolicy.CoverChildren;
		bool flag4 = gridWidget.WidthSizePolicy != SizePolicy.CoverChildren;
		usedRowCount = (flag3 ? rowCount : num);
		usedColumnCount = (flag4 ? columnCount : num2);
	}

	private float GetElement(IReadOnlyList<float> elements, int index)
	{
		if (index < 0 || index >= elements.Count)
		{
			return 0f;
		}
		return elements[index];
	}
}
