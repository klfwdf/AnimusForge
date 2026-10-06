using System;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct FormOrder
{
	public enum FormOrderEnum
	{
		Deep,
		Wide,
		Wider,
		Custom
	}

	private float _customFlankWidth;

	public readonly FormOrderEnum OrderEnum;

	public static readonly FormOrder FormOrderDeep = new FormOrder(FormOrderEnum.Deep);

	public static readonly FormOrder FormOrderWide = new FormOrder(FormOrderEnum.Wide);

	public static readonly FormOrder FormOrderWider = new FormOrder(FormOrderEnum.Wider);

	public float CustomFlankWidth
	{
		get
		{
			return _customFlankWidth;
		}
		set
		{
			_customFlankWidth = value;
		}
	}

	public OrderType OrderType => OrderEnum switch
	{
		FormOrderEnum.Wide => OrderType.FormWide, 
		FormOrderEnum.Wider => OrderType.FormWider, 
		FormOrderEnum.Custom => OrderType.FormCustom, 
		_ => OrderType.FormDeep, 
	};

	private FormOrder(FormOrderEnum orderEnum, float customFlankWidth = -1f)
	{
		OrderEnum = orderEnum;
		_customFlankWidth = customFlankWidth;
	}

	public static FormOrder FormOrderCustom(float customWidth)
	{
		return new FormOrder(FormOrderEnum.Custom, customWidth);
	}

	public void OnApply(Formation formation)
	{
		OnApplyToArrangement(formation, formation.Arrangement);
	}

	public static int GetUnitCountOf(Formation formation)
	{
		if (!formation.OverridenUnitCount.HasValue)
		{
			return formation.CountOfUnitsWithoutDetachedOnes;
		}
		return formation.OverridenUnitCount.Value;
	}

	public bool OnApplyToCustomArrangement(Formation formation, IFormationArrangement arrangement)
	{
		return false;
	}

	private void OnApplyToArrangement(Formation formation, IFormationArrangement arrangement)
	{
		if (OnApplyToCustomArrangement(formation, arrangement))
		{
			return;
		}
		if (arrangement is ColumnFormation)
		{
			ColumnFormation columnFormation = arrangement as ColumnFormation;
			if (GetUnitCountOf(formation) > 0)
			{
				columnFormation.FormFromWidth(GetRankVerticalFormFileCount(formation));
			}
			if (OrderEnum == FormOrderEnum.Custom && TaleWorlds.Library.MathF.Abs(CustomFlankWidth - arrangement.FlankWidth) > 0.01f)
			{
				ArrangementOrder.TransposeLineFormation(formation);
				formation.OnTick += formation.TickForColumnArrangementInitialPositioning;
			}
		}
		else if (arrangement is RectilinearSchiltronFormation)
		{
			(arrangement as RectilinearSchiltronFormation).Form();
		}
		else if (arrangement is CircularSchiltronFormation)
		{
			(arrangement as CircularSchiltronFormation).Form();
		}
		else if (arrangement is CircularFormation)
		{
			CircularFormation circularFormation = arrangement as CircularFormation;
			int unitCountOf = GetUnitCountOf(formation);
			int? maxFileCount = GetMaxFileCount(unitCountOf);
			float num = 0f;
			if (maxFileCount.HasValue)
			{
				int rankCount = TaleWorlds.Library.MathF.Max(1, TaleWorlds.Library.MathF.Ceiling((float)unitCountOf * 1f / (float)maxFileCount.Value));
				num = circularFormation.GetCircumferenceFromRankCount(rankCount);
			}
			else
			{
				num = System.MathF.PI * CustomFlankWidth;
			}
			circularFormation.FormFromCircumference(num);
		}
		else if (arrangement is SquareFormation)
		{
			SquareFormation squareFormation = arrangement as SquareFormation;
			int unitCountOf2 = GetUnitCountOf(formation);
			int? maxFileCount2 = GetMaxFileCount(unitCountOf2);
			if (maxFileCount2.HasValue)
			{
				int rankCount2 = TaleWorlds.Library.MathF.Max(1, TaleWorlds.Library.MathF.Ceiling((float)unitCountOf2 * 1f / (float)maxFileCount2.Value));
				squareFormation.FormFromRankCount(rankCount2);
			}
			else
			{
				squareFormation.FormFromBorderSideWidth(CustomFlankWidth);
			}
		}
		else if (arrangement is SkeinFormation)
		{
			SkeinFormation skeinFormation = arrangement as SkeinFormation;
			int unitCountOf3 = GetUnitCountOf(formation);
			int? maxFileCount3 = GetMaxFileCount(unitCountOf3);
			if (maxFileCount3.HasValue)
			{
				skeinFormation.FormFromFlankWidth(maxFileCount3.Value);
			}
			else
			{
				skeinFormation.FlankWidth = CustomFlankWidth;
			}
		}
		else if (arrangement is WedgeFormation)
		{
			WedgeFormation wedgeFormation = arrangement as WedgeFormation;
			int unitCountOf4 = GetUnitCountOf(formation);
			int? maxFileCount4 = GetMaxFileCount(unitCountOf4);
			if (maxFileCount4.HasValue)
			{
				wedgeFormation.FormFromFlankWidth(maxFileCount4.Value);
			}
			else
			{
				wedgeFormation.FlankWidth = CustomFlankWidth;
			}
		}
		else if (arrangement is TransposedLineFormation)
		{
			TransposedLineFormation transposedLineFormation = arrangement as TransposedLineFormation;
			int unitCountOf5 = GetUnitCountOf(formation);
			if (unitCountOf5 > 0)
			{
				int? num2 = GetMaxFileCount(unitCountOf5);
				if (!num2.HasValue)
				{
					num2 = transposedLineFormation.GetFileCountFromWidth(CustomFlankWidth);
				}
				TaleWorlds.Library.MathF.Ceiling((float)unitCountOf5 * 1f / (float)num2.Value);
				transposedLineFormation.FormFromFlankWidth(GetRankVerticalFormFileCount(formation));
			}
		}
		else if (arrangement is LineFormation)
		{
			LineFormation lineFormation = arrangement as LineFormation;
			int unitCountOf6 = GetUnitCountOf(formation);
			int? maxFileCount5 = GetMaxFileCount(unitCountOf6);
			if (maxFileCount5.HasValue)
			{
				lineFormation.FormFromFlankWidth(maxFileCount5.Value, unitCountOf6 > 40);
			}
			else
			{
				lineFormation.FlankWidth = CustomFlankWidth;
			}
		}
		else
		{
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Orders\\FormOrder.cs", "OnApplyToArrangement", 230);
		}
	}

	private int? GetMaxFileCount(int unitCount)
	{
		return GetMaxFileCountStatic(OrderEnum, unitCount);
	}

	public static int? GetMaxFileCountStatic(FormOrderEnum order, int unitCount)
	{
		return GetMaxFileCountAux(order, unitCount);
	}

	private int GetRankVerticalFormFileCount(IFormation formation)
	{
		int arrangementAspectRatio = ColumnFormation.ArrangementAspectRatio;
		int countOfUnitsWithoutLooseDetachedOnes = (formation as Formation).CountOfUnitsWithoutLooseDetachedOnes;
		switch (OrderEnum)
		{
		case FormOrderEnum.Wide:
			return TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Sqrt((float)countOfUnitsWithoutLooseDetachedOnes / ((float)arrangementAspectRatio * 1f))), 1);
		case FormOrderEnum.Wider:
			return TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Sqrt((float)countOfUnitsWithoutLooseDetachedOnes / ((float)arrangementAspectRatio * 0.5f))), 1);
		case FormOrderEnum.Deep:
			return TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Sqrt((float)countOfUnitsWithoutLooseDetachedOnes / ((float)arrangementAspectRatio * 2f))), 1);
		case FormOrderEnum.Custom:
			return TaleWorlds.Library.MathF.Floor((_customFlankWidth + formation.Interval) / (formation.UnitDiameter + formation.Interval));
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Orders\\FormOrder.cs", "GetRankVerticalFormFileCount", 274);
			return 1;
		}
	}

	private static int? GetMaxFileCountAux(FormOrderEnum order, int unitCount)
	{
		if (order == FormOrderEnum.Custom)
		{
			return null;
		}
		int value = 0;
		switch (order)
		{
		case FormOrderEnum.Wide:
			value = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Sqrt((float)unitCount / 16f)), 1) * 16;
			break;
		case FormOrderEnum.Wider:
			value = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Sqrt((float)unitCount / 64f)), 1) * 64;
			break;
		case FormOrderEnum.Deep:
			value = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Sqrt((float)unitCount / 4f)), 1) * 4;
			break;
		}
		return value;
	}

	public override bool Equals(object obj)
	{
		if (obj is FormOrder formOrder)
		{
			return formOrder == this;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)OrderEnum;
	}

	public static bool operator !=(FormOrder f1, FormOrder f2)
	{
		return f1.OrderEnum != f2.OrderEnum;
	}

	public static bool operator ==(FormOrder f1, FormOrder f2)
	{
		return f1.OrderEnum == f2.OrderEnum;
	}
}
