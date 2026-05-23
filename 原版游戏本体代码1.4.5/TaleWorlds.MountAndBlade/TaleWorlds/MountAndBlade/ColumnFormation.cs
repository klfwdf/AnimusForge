using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class ColumnFormation : IFormationArrangement
{
	public static readonly int ArrangementAspectRatio = 5;

	private readonly IFormation owner;

	private IFormationUnit _vanguard;

	private MBList2D<IFormationUnit> _units2D;

	private MBList2D<IFormationUnit> _units2DWorkspace;

	private MBList<IFormationUnit> _allUnits;

	private bool isExpandingFromRightSide = true;

	private bool IsMiddleFrontUnitPositionReserved;

	private bool _isMiddleFrontUnitPositionUsedByVanguardInFormation;

	public IFormationUnit Vanguard
	{
		get
		{
			return _vanguard;
		}
		private set
		{
			SetVanguard(value);
		}
	}

	public int ColumnCount
	{
		get
		{
			return FileCount;
		}
		set
		{
			SetColumnCount(value);
		}
	}

	protected int FileCount => _units2D.Count1;

	public int RankCount => _units2D.Count2;

	public int VanguardFileIndex
	{
		get
		{
			if (FileCount % 2 == 0)
			{
				if (isExpandingFromRightSide)
				{
					return FileCount / 2 - 1;
				}
				return FileCount / 2;
			}
			return FileCount / 2;
		}
	}

	protected float Distance => owner.Distance;

	public float DistanceMultiplier => 1.5f;

	protected float Interval => owner.Interval;

	public float IntervalMultiplier => 1.5f;

	public float Width
	{
		get
		{
			return FlankWidth;
		}
		set
		{
			FlankWidth = value;
		}
	}

	public float FlankWidth
	{
		get
		{
			return (float)(FileCount - 1) * (owner.Interval + owner.UnitDiameter) + owner.UnitDiameter;
		}
		set
		{
			int a = TaleWorlds.Library.MathF.Max(0, (int)((value - owner.UnitDiameter) / (owner.Interval + owner.UnitDiameter) + 1E-05f)) + 1;
			a = TaleWorlds.Library.MathF.Max(a, 1);
			SetColumnCount(a);
			this.OnWidthChanged?.Invoke();
		}
	}

	public List<Vec2> UnitPositionsOnVanguardFileIndex { get; private set; }

	public float Depth => RankDepth;

	public float RankDepth => (float)(RankCount - 1) * (Distance + owner.UnitDiameter) + owner.UnitDiameter;

	public float MinimumWidth => MinimumFlankWidth;

	public float MaximumWidth => (float)(UnitCount - 1) * (owner.UnitDiameter + owner.Interval) + owner.UnitDiameter;

	public float MinimumFlankWidth => (float)(TaleWorlds.Library.MathF.Max(1, TaleWorlds.Library.MathF.Ceiling(TaleWorlds.Library.MathF.Sqrt(UnitCount / ArrangementAspectRatio))) - 1) * (owner.UnitDiameter + owner.Interval) + owner.UnitDiameter;

	public bool? IsLoose => false;

	public int UnitCount => GetAllUnits().Count;

	public int PositionedUnitCount => UnitCount;

	bool IFormationArrangement.AreLocalPositionsDirty
	{
		set
		{
		}
	}

	public event Action OnWidthChanged;

	public event Action OnShapeChanged;

	public ColumnFormation(IFormation ownerFormation, IFormationUnit vanguard = null, int columnCount = 1)
	{
		owner = ownerFormation;
		_units2D = new MBList2D<IFormationUnit>(columnCount, 1);
		_units2DWorkspace = new MBList2D<IFormationUnit>(columnCount, 1);
		ReconstructUnitsFromUnits2D();
		_vanguard = vanguard;
		this.OnShapeChanged?.Invoke();
	}

	public IFormationArrangement Clone(IFormation formation)
	{
		return new ColumnFormation(formation, Vanguard, ColumnCount);
	}

	public void DeepCopyFrom(IFormationArrangement arrangement)
	{
		UnitPositionsOnVanguardFileIndex = (arrangement as ColumnFormation).GetUnitPositionsOnVanguardFileIndex();
	}

	public IFormationUnit GetPlayerUnit()
	{
		return _allUnits.FirstOrDefault((IFormationUnit unit) => unit.IsPlayerUnit);
	}

	public MBReadOnlyList<IFormationUnit> GetAllUnits()
	{
		return _allUnits;
	}

	public void GetAllUnits(in MBList<IFormationUnit> allUnitsListToBeFilledIn)
	{
		allUnitsListToBeFilledIn.Clear();
		allUnitsListToBeFilledIn.AddRange(_allUnits);
	}

	public MBList<IFormationUnit> GetUnpositionedUnits()
	{
		return null;
	}

	private bool IsUnitPositionAvailable(int fileIndex, int rankIndex)
	{
		if (IsMiddleFrontUnitPositionReserved)
		{
			(int, int) middleFrontUnitPosition = GetMiddleFrontUnitPosition();
			if (fileIndex == middleFrontUnitPosition.Item1 && rankIndex == middleFrontUnitPosition.Item2)
			{
				return false;
			}
		}
		return true;
	}

	private bool GetNextVacancy(out int fileIndex, out int rankIndex)
	{
		if (RankCount == 0)
		{
			fileIndex = -1;
			rankIndex = -1;
			return false;
		}
		rankIndex = RankCount - 1;
		for (int i = 0; i < ColumnCount; i++)
		{
			int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(i, isExpandingFromRightSide ^ (ColumnCount % 2 == 1));
			fileIndex = VanguardFileIndex + columnOffsetFromColumnIndex;
			if (_units2D[fileIndex, rankIndex] == null && IsUnitPositionAvailable(fileIndex, rankIndex))
			{
				return true;
			}
		}
		fileIndex = -1;
		rankIndex = -1;
		return false;
	}

	private IFormationUnit GetLastUnit()
	{
		if (RankCount == 0)
		{
			return null;
		}
		int index = RankCount - 1;
		for (int num = ColumnCount - 1; num >= 0; num--)
		{
			int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(num, isExpandingFromRightSide);
			int index2 = VanguardFileIndex + columnOffsetFromColumnIndex;
			IFormationUnit formationUnit = _units2D[index2, index];
			if (formationUnit != null)
			{
				return formationUnit;
			}
		}
		return null;
	}

	private void Deepen()
	{
		Deepen(this);
	}

	private void ReconstructUnitsFromUnits2D()
	{
		if (_allUnits == null)
		{
			_allUnits = new MBList<IFormationUnit>();
		}
		_allUnits.Clear();
		for (int i = 0; i < _units2D.Count1; i++)
		{
			for (int j = 0; j < _units2D.Count2; j++)
			{
				if (_units2D[i, j] != null)
				{
					_allUnits.Add(_units2D[i, j]);
				}
			}
		}
	}

	private static void Deepen(ColumnFormation formation)
	{
		formation._units2DWorkspace.ResetWithNewCount(formation.FileCount, formation.RankCount + 1);
		for (int i = 0; i < formation.FileCount; i++)
		{
			formation._units2D.CopyRowTo(i, 0, formation._units2DWorkspace, i, 0, formation.RankCount);
		}
		MBList2D<IFormationUnit> units2D = formation._units2D;
		formation._units2D = formation._units2DWorkspace;
		formation._units2DWorkspace = units2D;
		formation.ReconstructUnitsFromUnits2D();
		formation.OnShapeChanged?.Invoke();
	}

	private void Shorten()
	{
		Shorten(this);
	}

	private static void Shorten(ColumnFormation formation)
	{
		formation._units2DWorkspace.ResetWithNewCount(formation.FileCount, formation.RankCount - 1);
		for (int i = 0; i < formation.FileCount; i++)
		{
			formation._units2D.CopyRowTo(i, 0, formation._units2DWorkspace, i, 0, formation.RankCount - 1);
		}
		MBList2D<IFormationUnit> units2D = formation._units2D;
		formation._units2D = formation._units2DWorkspace;
		formation._units2DWorkspace = units2D;
		formation.ReconstructUnitsFromUnits2D();
		formation.OnShapeChanged?.Invoke();
	}

	public bool AddUnit(IFormationUnit unit)
	{
		int num = 0;
		bool flag = false;
		while (!flag && num < 100)
		{
			num++;
			if (num > 10)
			{
				TaleWorlds.Library.Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Formation\\ColumnFormation.cs", "AddUnit", 382);
			}
			if (GetNextVacancy(out var fileIndex, out var rankIndex))
			{
				unit.FormationFileIndex = fileIndex;
				unit.FormationRankIndex = rankIndex;
				_units2D[fileIndex, rankIndex] = unit;
				ReconstructUnitsFromUnits2D();
				flag = true;
			}
			else
			{
				Deepen();
			}
		}
		if (flag)
		{
			int columnOffset;
			IFormationUnit unitToFollow = GetUnitToFollow(unit, out columnOffset);
			SetUnitToFollow(unit, unitToFollow, columnOffset);
			this.OnShapeChanged?.Invoke();
		}
		return flag;
	}

	private IFormationUnit TryGetUnit(int fileIndex, int rankIndex)
	{
		if (fileIndex >= 0 && fileIndex < FileCount && rankIndex >= 0 && rankIndex < RankCount)
		{
			return _units2D[fileIndex, rankIndex];
		}
		return null;
	}

	private void AdjustFollowDataOfUnitPosition(int fileIndex, int rankIndex)
	{
		IFormationUnit formationUnit = _units2D[fileIndex, rankIndex];
		if (fileIndex == VanguardFileIndex)
		{
			if (formationUnit != null)
			{
				IFormationUnit formationUnit2 = TryGetUnit(fileIndex, rankIndex - 1);
				SetUnitToFollow(formationUnit, formationUnit2 ?? Vanguard);
			}
			for (int i = 1; i < ColumnCount; i++)
			{
				int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(i, isExpandingFromRightSide);
				IFormationUnit formationUnit3 = _units2D[fileIndex + columnOffsetFromColumnIndex, rankIndex];
				if (formationUnit3 != null)
				{
					SetUnitToFollow(formationUnit3, formationUnit ?? Vanguard, columnOffsetFromColumnIndex);
				}
			}
			IFormationUnit formationUnit4 = TryGetUnit(fileIndex, rankIndex + 1);
			if (formationUnit4 != null)
			{
				SetUnitToFollow(formationUnit4, formationUnit ?? Vanguard);
			}
		}
		else if (formationUnit != null)
		{
			IFormationUnit formationUnit5 = _units2D[VanguardFileIndex, rankIndex];
			int columnOffsetFromColumnIndex2 = GetColumnOffsetFromColumnIndex(fileIndex, isExpandingFromRightSide);
			SetUnitToFollow(formationUnit, formationUnit5 ?? Vanguard, columnOffsetFromColumnIndex2);
		}
	}

	private void ShiftUnitsForward(int fileIndex, int rankIndex)
	{
		while (true)
		{
			IFormationUnit formationUnit = TryGetUnit(fileIndex, rankIndex + 1);
			if (formationUnit == null)
			{
				break;
			}
			formationUnit.FormationRankIndex--;
			_units2D[fileIndex, rankIndex] = formationUnit;
			_units2D[fileIndex, rankIndex + 1] = null;
			ReconstructUnitsFromUnits2D();
			AdjustFollowDataOfUnitPosition(fileIndex, rankIndex);
			rankIndex++;
		}
		int num = 0;
		if (rankIndex == RankCount - 1)
		{
			for (int i = 0; i < ColumnCount; i++)
			{
				int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(i, isExpandingFromRightSide);
				if (VanguardFileIndex + columnOffsetFromColumnIndex == fileIndex)
				{
					num = i + 1;
				}
			}
		}
		IFormationUnit formationUnit2 = null;
		for (int num2 = ColumnCount - 1; num2 >= num; num2--)
		{
			int columnOffsetFromColumnIndex2 = GetColumnOffsetFromColumnIndex(num2, isExpandingFromRightSide);
			int index = VanguardFileIndex + columnOffsetFromColumnIndex2;
			formationUnit2 = _units2D[index, RankCount - 1];
			if (formationUnit2 != null)
			{
				break;
			}
		}
		if (formationUnit2 != null)
		{
			_units2D[formationUnit2.FormationFileIndex, formationUnit2.FormationRankIndex] = null;
			formationUnit2.FormationFileIndex = fileIndex;
			formationUnit2.FormationRankIndex = rankIndex;
			_units2D[fileIndex, rankIndex] = formationUnit2;
			ReconstructUnitsFromUnits2D();
			AdjustFollowDataOfUnitPosition(fileIndex, rankIndex);
		}
		if (IsLastRankEmpty())
		{
			Shorten();
		}
		this.OnShapeChanged?.Invoke();
	}

	private void ShiftUnitsBackwardForMakingRoomForVanguard(int fileIndex, int rankIndex)
	{
		if (RankCount == 1)
		{
			bool flag = false;
			int num = -1;
			for (int i = 0; i < ColumnCount; i++)
			{
				int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(i, isExpandingFromRightSide);
				if (_units2D[VanguardFileIndex + columnOffsetFromColumnIndex, 0] == null)
				{
					flag = true;
					num = VanguardFileIndex + columnOffsetFromColumnIndex;
					break;
				}
			}
			if (flag)
			{
				IFormationUnit formationUnit = _units2D[fileIndex, rankIndex];
				_units2D[fileIndex, rankIndex] = null;
				_units2D[num, 0] = formationUnit;
				ReconstructUnitsFromUnits2D();
				formationUnit.FormationFileIndex = num;
				formationUnit.FormationRankIndex = 0;
			}
			else
			{
				Deepen(this);
				IFormationUnit formationUnit2 = _units2D[fileIndex, rankIndex];
				_units2D[fileIndex, rankIndex] = null;
				_units2D[fileIndex, rankIndex + 1] = formationUnit2;
				ReconstructUnitsFromUnits2D();
				formationUnit2.FormationRankIndex++;
			}
			return;
		}
		int num2 = rankIndex;
		IFormationUnit formationUnit3 = null;
		for (rankIndex = RankCount - 1; rankIndex >= num2; rankIndex--)
		{
			IFormationUnit formationUnit4 = _units2D[fileIndex, rankIndex];
			TryGetUnit(fileIndex, rankIndex + 1);
			_units2D[fileIndex, rankIndex] = null;
			if (rankIndex + 1 < RankCount)
			{
				formationUnit4.FormationRankIndex++;
				_units2D[fileIndex, rankIndex + 1] = formationUnit4;
			}
			else
			{
				formationUnit3 = formationUnit4;
				if (formationUnit3 != null)
				{
					formationUnit3.FormationFileIndex = -1;
					formationUnit3.FormationRankIndex = -1;
				}
			}
			ReconstructUnitsFromUnits2D();
		}
		for (rankIndex = RankCount - 1; rankIndex >= num2; rankIndex--)
		{
			AdjustFollowDataOfUnitPosition(fileIndex, rankIndex);
		}
		if (formationUnit3 != null)
		{
			AddUnit(formationUnit3);
		}
		this.OnShapeChanged?.Invoke();
	}

	private bool IsLastRankEmpty()
	{
		if (RankCount == 0)
		{
			return false;
		}
		for (int i = 0; i < FileCount; i++)
		{
			if (_units2D[i, RankCount - 1] != null)
			{
				return false;
			}
		}
		return true;
	}

	public void RemoveUnit(IFormationUnit unit)
	{
		int formationFileIndex = unit.FormationFileIndex;
		int formationRankIndex = unit.FormationRankIndex;
		if (GameNetwork.IsServer)
		{
			MBDebug.Print("Removing unit at " + formationFileIndex + " " + formationRankIndex + " from column arrangement\nFileCount&RankCount: " + FileCount + " " + RankCount);
		}
		_units2D[unit.FormationFileIndex, unit.FormationRankIndex] = null;
		ReconstructUnitsFromUnits2D();
		ShiftUnitsForward(unit.FormationFileIndex, unit.FormationRankIndex);
		if (IsLastRankEmpty())
		{
			Shorten();
		}
		unit.FormationFileIndex = -1;
		unit.FormationRankIndex = -1;
		SetUnitToFollow(unit, null);
		this.OnShapeChanged?.Invoke();
		if (Vanguard == unit && !((Agent)unit).IsActive())
		{
			_vanguard = null;
			if (FileCount > 0 && RankCount > 0)
			{
				AdjustFollowDataOfUnitPosition(formationFileIndex, formationRankIndex);
			}
		}
	}

	public IFormationUnit GetUnit(int fileIndex, int rankIndex)
	{
		return _units2D[fileIndex, rankIndex];
	}

	public void OnBatchRemoveStart()
	{
	}

	public void OnBatchRemoveEnd()
	{
	}

	[Conditional("DEBUG")]
	private void AssertUnitPositions()
	{
		for (int i = 0; i < FileCount; i++)
		{
			for (int j = 0; j < RankCount; j++)
			{
				_ = _units2D[i, j];
			}
		}
	}

	[Conditional("DEBUG")]
	private void AssertUnit(IFormationUnit unit, bool isAssertingFollowed = true)
	{
		if (unit != null && isAssertingFollowed)
		{
			GetUnitToFollow(unit, out var _);
		}
	}

	private static int GetColumnOffsetFromColumnIndex(int columnIndex, bool isExpandingFromRightSide)
	{
		if (isExpandingFromRightSide)
		{
			return (columnIndex + 1) / 2 * ((columnIndex % 2 != 0) ? 1 : (-1));
		}
		return (columnIndex + 1) / 2 * ((columnIndex % 2 == 0) ? 1 : (-1));
	}

	private IFormationUnit GetUnitToFollow(IFormationUnit unit, out int columnOffset)
	{
		IFormationUnit formationUnit;
		if (unit.FormationFileIndex == VanguardFileIndex)
		{
			columnOffset = 0;
			formationUnit = ((unit.FormationRankIndex <= 0) ? null : _units2D[unit.FormationFileIndex, unit.FormationRankIndex - 1]);
		}
		else
		{
			columnOffset = unit.FormationFileIndex - VanguardFileIndex;
			formationUnit = _units2D[VanguardFileIndex, unit.FormationRankIndex];
		}
		if (formationUnit == null)
		{
			formationUnit = Vanguard;
		}
		return formationUnit;
	}

	private IEnumerable<(int, int)> GetOrderedUnitPositionIndices()
	{
		for (int rankIndex = 0; rankIndex < RankCount; rankIndex++)
		{
			for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
			{
				int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(columnIndex, isExpandingFromRightSide);
				int item = VanguardFileIndex + columnOffsetFromColumnIndex;
				yield return (item, rankIndex);
			}
		}
	}

	private Vec2 GetLocalPositionOfUnit(int fileIndex, int rankIndex)
	{
		if (UnitPositionsOnVanguardFileIndex == null)
		{
			UnitPositionsOnVanguardFileIndex = GetUnitPositionsOnVanguardFileIndex();
		}
		Vec2 orderPosition = (owner as Formation).OrderPosition;
		List<Vec2> unitPositionsOnVanguardFileIndex = UnitPositionsOnVanguardFileIndex;
		unitPositionsOnVanguardFileIndex.Insert(0, orderPosition);
		float num = Distance + owner.UnitDiameter;
		int num2 = rankIndex;
		int num3 = 1;
		Vec2 vec = unitPositionsOnVanguardFileIndex[0];
		Vec2 vec2 = vec - unitPositionsOnVanguardFileIndex[num3];
		float num4 = vec2.Normalize();
		while (num2 > 0)
		{
			if (num4 >= num)
			{
				vec += -vec2 * num;
				num4 -= num;
			}
			else
			{
				float num5 = num - num4;
				vec += -vec2 * num4;
				if (++num3 < unitPositionsOnVanguardFileIndex.Count)
				{
					vec2 = vec - unitPositionsOnVanguardFileIndex[num3];
				}
				num4 = vec2.Normalize();
				vec += -vec2 * num5;
				num4 -= num5;
			}
			num2--;
		}
		float num6 = (float)(FileCount - 1) * (Interval + owner.UnitDiameter);
		Vec2 vec3 = -vec2.TransformToParentUnitF(new Vec2((float)fileIndex * (Interval + owner.UnitDiameter) - num6 / 2f, 0f));
		vec += vec3;
		Vec2 result = (owner as Formation).Direction.TransformToLocalUnitF(vec - unitPositionsOnVanguardFileIndex[0]);
		unitPositionsOnVanguardFileIndex.RemoveAt(0);
		return result;
	}

	private Vec2 GetLocalDirectionOfUnit(int fileIndex, int rankIndex)
	{
		return Vec2.Forward;
	}

	private WorldPosition? GetWorldPositionOfUnit(int fileIndex, int rankIndex)
	{
		return null;
	}

	public Vec2? GetLocalPositionOfUnitOrDefault(int unitIndex)
	{
		(int, int) tuple = GetOrderedUnitPositionIndices().ElementAtOrValue(unitIndex, (-1, -1));
		Vec2? result;
		if (tuple.Item1 != -1 && tuple.Item2 != -1)
		{
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			result = GetLocalPositionOfUnit(item, item2);
		}
		else
		{
			result = null;
		}
		return result;
	}

	public Vec2? GetLocalDirectionOfUnitOrDefault(int unitIndex)
	{
		(int, int) tuple = (from i in GetOrderedUnitPositionIndices()
			where IsUnitPositionAvailable(i.Item1, i.Item2)
			select i).ElementAtOrValue(unitIndex, (-1, -1));
		Vec2? result;
		if (tuple.Item1 != -1 && tuple.Item2 != -1)
		{
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			result = GetLocalDirectionOfUnit(item, item2);
		}
		else
		{
			result = null;
		}
		return result;
	}

	public WorldPosition? GetWorldPositionOfUnitOrDefault(int unitIndex)
	{
		(int, int) tuple = (from i in GetOrderedUnitPositionIndices()
			where IsUnitPositionAvailable(i.Item1, i.Item2)
			select i).ElementAtOrValue(unitIndex, (-1, -1));
		if (tuple.Item1 == -1 || tuple.Item2 == -1)
		{
			return null;
		}
		var (fileIndex, rankIndex) = tuple;
		return GetWorldPositionOfUnit(fileIndex, rankIndex);
	}

	public Vec2? GetLocalPositionOfUnitOrDefault(IFormationUnit unit)
	{
		return GetLocalPositionOfUnit(unit.FormationFileIndex, unit.FormationRankIndex);
	}

	public Vec2? GetLocalPositionOfUnitOrDefaultWithAdjustment(IFormationUnit unit, float distanceBetweenAgentsAdjustment)
	{
		return GetLocalPositionOfUnit(unit.FormationFileIndex, unit.FormationRankIndex);
	}

	public WorldPosition? GetWorldPositionOfUnitOrDefault(IFormationUnit unit)
	{
		return GetWorldPositionOfUnit(unit.FormationFileIndex, unit.FormationRankIndex);
	}

	public Vec2? GetLocalDirectionOfUnitOrDefault(IFormationUnit unit)
	{
		return GetLocalDirectionOfUnit(unit.FormationFileIndex, unit.FormationRankIndex);
	}

	public List<IFormationUnit> GetUnitsToPop(int count)
	{
		List<IFormationUnit> list = new List<IFormationUnit>();
		for (int num = RankCount - 1; num >= 0; num--)
		{
			for (int num2 = ColumnCount - 1; num2 >= 0; num2--)
			{
				int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(num2, isExpandingFromRightSide);
				int index = VanguardFileIndex + columnOffsetFromColumnIndex;
				IFormationUnit formationUnit = _units2D[index, num];
				if (formationUnit != null)
				{
					list.Add(formationUnit);
					count--;
					if (count == 0)
					{
						return list;
					}
				}
			}
		}
		return list;
	}

	public List<IFormationUnit> GetUnitsToPop(int count, Vec3 targetPosition)
	{
		return GetUnitsToPop(count);
	}

	public IEnumerable<IFormationUnit> GetUnitsToPopWithCondition(int count, Func<IFormationUnit, bool> currentCondition)
	{
		for (int rankIndex = RankCount - 1; rankIndex >= 0; rankIndex--)
		{
			for (int columnIndex = ColumnCount - 1; columnIndex >= 0; columnIndex--)
			{
				int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(columnIndex, isExpandingFromRightSide);
				int index = VanguardFileIndex + columnOffsetFromColumnIndex;
				IFormationUnit formationUnit = _units2D[index, rankIndex];
				if (formationUnit != null && currentCondition(formationUnit))
				{
					yield return formationUnit;
					count--;
					if (count == 0)
					{
						yield break;
					}
				}
			}
		}
	}

	public void SwitchUnitLocations(IFormationUnit firstUnit, IFormationUnit secondUnit)
	{
		SwitchUnitLocationsAux(firstUnit, secondUnit);
		AdjustFollowDataOfUnitPosition(firstUnit.FormationFileIndex, firstUnit.FormationRankIndex);
		AdjustFollowDataOfUnitPosition(secondUnit.FormationFileIndex, secondUnit.FormationRankIndex);
	}

	private void SwitchUnitLocationsAux(IFormationUnit firstUnit, IFormationUnit secondUnit)
	{
		int formationFileIndex = firstUnit.FormationFileIndex;
		int formationRankIndex = firstUnit.FormationRankIndex;
		int formationFileIndex2 = secondUnit.FormationFileIndex;
		int formationRankIndex2 = secondUnit.FormationRankIndex;
		_units2D[formationFileIndex, formationRankIndex] = secondUnit;
		_units2D[formationFileIndex2, formationRankIndex2] = firstUnit;
		ReconstructUnitsFromUnits2D();
		firstUnit.FormationFileIndex = formationFileIndex2;
		firstUnit.FormationRankIndex = formationRankIndex2;
		secondUnit.FormationFileIndex = formationFileIndex;
		secondUnit.FormationRankIndex = formationRankIndex;
		this.OnShapeChanged?.Invoke();
	}

	public void SwitchUnitLocationsWithUnpositionedUnit(IFormationUnit firstUnit, IFormationUnit secondUnit)
	{
		TaleWorlds.Library.Debug.FailedAssert("Column formation should NOT have an unpositioned unit", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Formation\\ColumnFormation.cs", "SwitchUnitLocationsWithUnpositionedUnit", 1215);
	}

	public void SwitchUnitLocationsWithBackMostUnit(IFormationUnit unit)
	{
		if (Vanguard == null || !(Vanguard is Agent agent) || agent != unit)
		{
			IFormationUnit lastUnit = GetLastUnit();
			if (lastUnit != null && unit != null && unit != lastUnit)
			{
				SwitchUnitLocations(unit, lastUnit);
			}
		}
	}

	public float GetUnitsDistanceToFrontLine(IFormationUnit unit)
	{
		return -1f;
	}

	public Vec2? GetLocalDirectionOfRelativeFormationLocation(IFormationUnit unit)
	{
		return null;
	}

	public Vec2? GetLocalWallDirectionOfRelativeFormationLocation(IFormationUnit unit)
	{
		return null;
	}

	public IEnumerable<Vec2> GetUnavailableUnitPositions()
	{
		yield break;
	}

	public float GetOccupationWidth(int unitCount)
	{
		return FlankWidth;
	}

	public Vec2? CreateNewPosition(int unitIndex)
	{
		int num = TaleWorlds.Library.MathF.Ceiling((float)unitIndex * 1f / (float)ColumnCount) + ((unitIndex % ColumnCount == 0) ? 1 : 0);
		if (num > RankCount)
		{
			_units2D.ResetWithNewCount(ColumnCount, num);
			ReconstructUnitsFromUnits2D();
		}
		Vec2? localPositionOfUnitOrDefault = GetLocalPositionOfUnitOrDefault(unitIndex);
		Action action = this.OnShapeChanged;
		if (action != null)
		{
			action();
			return localPositionOfUnitOrDefault;
		}
		return localPositionOfUnitOrDefault;
	}

	public void InvalidateCacheOfUnitAux(Vec2 roundedLocalPosition)
	{
	}

	public void BeforeFormationFrameChange()
	{
	}

	public void OnFormationFrameChanged(bool updateCachedOrderedLocalPositions = false)
	{
	}

	private Vec2 CalculateArrangementOrientation()
	{
		IFormationUnit formationUnit = Vanguard ?? _units2D[GetMiddleFrontUnitPosition().Item1, GetMiddleFrontUnitPosition().Item2];
		if (formationUnit is Agent && owner is Formation)
		{
			return ((formationUnit as Agent).Position.AsVec2 - ((Formation)owner).CachedMedianPosition.AsVec2).Normalized();
		}
		TaleWorlds.Library.Debug.FailedAssert("Unexpected case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Formation\\ColumnFormation.cs", "CalculateArrangementOrientation", 1300);
		return GetLocalDirectionOfUnit(formationUnit.FormationFileIndex, formationUnit.FormationRankIndex);
	}

	public void OnUnitLostMount(IFormationUnit unit)
	{
		RemoveUnit(unit);
		AddUnit(unit);
	}

	public bool IsTurnBackwardsNecessary(Vec2 previousPosition, WorldPosition? newPosition, Vec2 previousDirection, bool hasNewDirection, Vec2? newDirection)
	{
		if (newPosition.HasValue && UnitCount > 0 && RankCount > 0 && (newPosition.Value.AsVec2 - previousPosition).LengthSquared >= RankDepth * RankDepth)
		{
			return TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(CalculateArrangementOrientation().RotationInRadians, (newPosition.Value.AsVec2 - (owner as Formation).CachedMedianPosition.AsVec2).Normalized().RotationInRadians)) >= System.MathF.PI * 3f / 4f;
		}
		return false;
	}

	public void TurnBackwards()
	{
		if (IsMiddleFrontUnitPositionReserved || _isMiddleFrontUnitPositionUsedByVanguardInFormation || RankCount <= 1)
		{
			return;
		}
		bool isMiddleFrontUnitPositionReserved = IsMiddleFrontUnitPositionReserved;
		IFormationUnit vanguard = _vanguard;
		if (isMiddleFrontUnitPositionReserved)
		{
			ReleaseMiddleFrontUnitPosition();
		}
		int rankCount = RankCount;
		for (int i = 0; i < rankCount / 2; i++)
		{
			for (int j = 0; j < FileCount; j++)
			{
				IFormationUnit formationUnit = _units2D[j, i];
				int num = rankCount - i - 1;
				int num2 = FileCount - j - 1;
				IFormationUnit formationUnit2 = _units2D[num2, num];
				if (formationUnit2 == null)
				{
					_units2D[num2, num] = formationUnit;
					_units2D[j, i] = null;
					if (formationUnit != null)
					{
						formationUnit.FormationFileIndex = num2;
						formationUnit.FormationRankIndex = num;
					}
				}
				else if (formationUnit != null && formationUnit != formationUnit2)
				{
					SwitchUnitLocationsAux(formationUnit, formationUnit2);
				}
			}
		}
		for (int k = 0; k < FileCount; k++)
		{
			if (_units2D[k, 0] == null && _units2D[k, 1] != null)
			{
				for (int l = 1; l < rankCount; l++)
				{
					IFormationUnit formationUnit3 = _units2D[k, l];
					formationUnit3.FormationRankIndex--;
					_units2D[k, l - 1] = formationUnit3;
					_units2D[k, l] = null;
				}
			}
		}
		isExpandingFromRightSide = !isExpandingFromRightSide;
		ReconstructUnitsFromUnits2D();
		foreach (IFormationUnit allUnit in GetAllUnits())
		{
			int columnOffset;
			IFormationUnit unitToFollow = GetUnitToFollow(allUnit, out columnOffset);
			SetUnitToFollow(allUnit, unitToFollow, columnOffset);
		}
		this.OnShapeChanged?.Invoke();
		if (isMiddleFrontUnitPositionReserved)
		{
			ReserveMiddleFrontUnitPosition(vanguard);
		}
		this.OnShapeChanged?.Invoke();
	}

	public void OnFormationDispersed()
	{
		IFormationUnit[] array = GetAllUnits().ToArray();
		foreach (IFormationUnit unit in array)
		{
			SwitchUnitIfLeftBehind(unit);
		}
	}

	public void Reset()
	{
		_units2D.ResetWithNewCount(ColumnCount, 1);
		ReconstructUnitsFromUnits2D();
		this.OnShapeChanged?.Invoke();
	}

	public virtual void RearrangeFrom(IFormationArrangement arrangement)
	{
		if (arrangement is TransposedLineFormation)
		{
			FlankWidth = arrangement.FlankWidth;
		}
		else if (arrangement is LineFormation)
		{
			FlankWidth = (float)TaleWorlds.Library.MathF.Max(0, TaleWorlds.Library.MathF.Ceiling(TaleWorlds.Library.MathF.Sqrt(arrangement.UnitCount / ArrangementAspectRatio)) - 1) * (owner.UnitDiameter + Interval) + owner.UnitDiameter;
		}
	}

	public virtual void RearrangeTo(IFormationArrangement arrangement)
	{
	}

	public virtual void RearrangeTransferUnits(IFormationArrangement arrangement)
	{
		foreach (var item in GetOrderedUnitPositionIndices().ToList())
		{
			IFormationUnit formationUnit = _units2D[item.Item1, item.Item2];
			if (formationUnit != null)
			{
				formationUnit.FormationFileIndex = -1;
				formationUnit.FormationRankIndex = -1;
				SetUnitToFollow(formationUnit, null);
				arrangement.AddUnit(formationUnit);
			}
		}
	}

	private void SetVanguard(IFormationUnit vanguard)
	{
		if (Vanguard == null && vanguard == null)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		if (UnitCount > 0)
		{
			if (Vanguard == null && vanguard != null)
			{
				flag2 = true;
			}
			else if (Vanguard != null && vanguard == null)
			{
				flag = true;
			}
		}
		(int, int) middleFrontUnitPosition = GetMiddleFrontUnitPosition();
		if (flag)
		{
			if ((Vanguard as Agent)?.Formation == owner)
			{
				RemoveUnit(Vanguard);
				AddUnit(Vanguard);
			}
			else if (RankCount > 0)
			{
				ShiftUnitsForward(middleFrontUnitPosition.Item1, middleFrontUnitPosition.Item2);
			}
		}
		else if (flag2)
		{
			if ((vanguard as Agent)?.Formation == owner)
			{
				RemoveUnit(vanguard);
				ShiftUnitsBackwardForMakingRoomForVanguard(middleFrontUnitPosition.Item1, middleFrontUnitPosition.Item2);
				if (RankCount > 0)
				{
					_units2D[middleFrontUnitPosition.Item1, middleFrontUnitPosition.Item2] = vanguard;
					ReconstructUnitsFromUnits2D();
					(vanguard.FormationFileIndex, vanguard.FormationRankIndex) = middleFrontUnitPosition;
					if (RankCount == 2)
					{
						AdjustFollowDataOfUnitPosition(middleFrontUnitPosition.Item1, middleFrontUnitPosition.Item2);
						AdjustFollowDataOfUnitPosition(middleFrontUnitPosition.Item1, middleFrontUnitPosition.Item2 + 1);
						this.OnShapeChanged?.Invoke();
					}
				}
				else
				{
					AddUnit(vanguard);
				}
			}
			else
			{
				ShiftUnitsBackwardForMakingRoomForVanguard(middleFrontUnitPosition.Item1, middleFrontUnitPosition.Item2);
			}
		}
		_vanguard = vanguard;
		if (RankCount > 0)
		{
			AdjustFollowDataOfUnitPosition(middleFrontUnitPosition.Item1, middleFrontUnitPosition.Item2);
		}
	}

	protected int GetUnitCountWithOverride()
	{
		if (owner.OverridenUnitCount.HasValue)
		{
			return owner.OverridenUnitCount.Value;
		}
		return UnitCount;
	}

	private void SetColumnCount(int columnCount)
	{
		if (ColumnCount != columnCount)
		{
			IFormationUnit[] array = GetAllUnits().ToArray();
			_units2D.ResetWithNewCount(columnCount, 1);
			ReconstructUnitsFromUnits2D();
			IFormationUnit[] array2 = array;
			foreach (IFormationUnit formationUnit in array2)
			{
				formationUnit.FormationFileIndex = -1;
				formationUnit.FormationRankIndex = -1;
				AddUnit(formationUnit);
			}
			this.OnShapeChanged?.Invoke();
		}
	}

	public void FormFromWidth(float width)
	{
		ColumnCount = TaleWorlds.Library.MathF.Ceiling(width);
	}

	public IFormationUnit GetNeighborUnitOfLeftSide(IFormationUnit unit)
	{
		int formationRankIndex = unit.FormationRankIndex;
		for (int num = unit.FormationFileIndex - 1; num >= 0; num--)
		{
			if (_units2D[num, formationRankIndex] != null)
			{
				return _units2D[num, formationRankIndex];
			}
		}
		return null;
	}

	public IFormationUnit GetNeighborUnitOfRightSide(IFormationUnit unit)
	{
		int formationRankIndex = unit.FormationRankIndex;
		for (int i = unit.FormationFileIndex + 1; i < FileCount; i++)
		{
			if (_units2D[i, formationRankIndex] != null)
			{
				return _units2D[i, formationRankIndex];
			}
		}
		return null;
	}

	public void ReserveMiddleFrontUnitPosition(IFormationUnit vanguard)
	{
		if ((vanguard as Agent)?.Formation != owner)
		{
			IsMiddleFrontUnitPositionReserved = true;
		}
		else
		{
			_isMiddleFrontUnitPositionUsedByVanguardInFormation = true;
		}
		Vanguard = vanguard;
	}

	public void ReleaseMiddleFrontUnitPosition()
	{
		IsMiddleFrontUnitPositionReserved = false;
		Vanguard = null;
		_isMiddleFrontUnitPositionUsedByVanguardInFormation = false;
	}

	private (int, int) GetMiddleFrontUnitPosition()
	{
		return (VanguardFileIndex, 0);
	}

	public Vec2 GetLocalPositionOfReservedUnitPosition()
	{
		return Vec2.Zero;
	}

	public void OnTickOccasionallyOfUnit(IFormationUnit unit, bool arrangementChangeAllowed)
	{
		if (arrangementChangeAllowed && unit.FollowedUnit != _vanguard && unit.FollowedUnit is Agent && !((Agent)unit.FollowedUnit).IsAIControlled && unit.FollowedUnit.FormationFileIndex >= 0 && unit.FollowedUnit.FormationRankIndex >= 0)
		{
			IFormationUnit followedUnit = unit.FollowedUnit;
			RemoveUnit(unit.FollowedUnit);
			AddUnit(followedUnit);
		}
	}

	public void OnTickOccasionally()
	{
	}

	private MBList<IFormationUnit> GetUnitsBehind(IFormationUnit unit)
	{
		MBList<IFormationUnit> mBList = new MBList<IFormationUnit>();
		bool flag = false;
		for (int i = 0; i < ColumnCount; i++)
		{
			int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(i, isExpandingFromRightSide);
			int num = VanguardFileIndex + columnOffsetFromColumnIndex;
			if (num == unit.FormationFileIndex)
			{
				flag = true;
			}
			if (flag && _units2D[num, unit.FormationRankIndex] != null)
			{
				mBList.Add(_units2D[num, unit.FormationRankIndex]);
			}
		}
		for (int j = 0; j < FileCount; j++)
		{
			for (int k = unit.FormationRankIndex + 1; k < RankCount; k++)
			{
				if (_units2D[j, k] != null)
				{
					mBList.Add(_units2D[j, k]);
				}
			}
		}
		return mBList;
	}

	private void SwitchUnitIfLeftBehind(IFormationUnit unit)
	{
		int columnOffset;
		IFormationUnit unitToFollow = GetUnitToFollow(unit, out columnOffset);
		if (unitToFollow == null)
		{
			float value = owner.UnitDiameter * 2f;
			IFormationUnit closestUnitTo = owner.GetClosestUnitTo(Vec2.Zero, new MBList<IFormationUnit> { unit }, value);
			if (closestUnitTo == null)
			{
				closestUnitTo = owner.GetClosestUnitTo(Vec2.Zero, GetUnitsAtRanks(0, RankCount - 1));
			}
			if (closestUnitTo != null && closestUnitTo != unit && closestUnitTo is Agent && (closestUnitTo as Agent).IsAIControlled)
			{
				SwitchUnitLocations(unit, closestUnitTo);
			}
		}
		else
		{
			float value2 = GetFollowVector(columnOffset).Length * 1.5f;
			IFormationUnit closestUnitTo2 = owner.GetClosestUnitTo(unitToFollow, new MBList<IFormationUnit> { unit }, value2);
			if (closestUnitTo2 == null)
			{
				closestUnitTo2 = owner.GetClosestUnitTo(unitToFollow, GetUnitsBehind(unit));
			}
			if (closestUnitTo2 != null && closestUnitTo2 != unit && closestUnitTo2 is Agent { IsAIControlled: not false } agent)
			{
				SwitchUnitLocations(unit, agent);
			}
		}
	}

	private void SetUnitToFollow(IFormationUnit unit, IFormationUnit unitToFollow, int columnOffset = 0)
	{
		Vec2 followVector = GetFollowVector(columnOffset);
		owner.SetUnitToFollow(unit, unitToFollow, followVector);
	}

	private Vec2 GetFollowVector(int columnOffset)
	{
		if (columnOffset == 0)
		{
			return -Vec2.Forward * (Distance + owner.UnitDiameter);
		}
		return Vec2.Side * columnOffset * (owner.UnitDiameter + Interval);
	}

	public float GetDirectionChangeTendencyOfUnit(IFormationUnit unit)
	{
		if (RankCount == 1 || unit.FormationRankIndex == -1)
		{
			return 0f;
		}
		return (float)unit.FormationRankIndex * 1f / (float)(RankCount - 1);
	}

	private MBList<IFormationUnit> GetUnitsAtRanks(int rankIndex1, int rankIndex2)
	{
		MBList<IFormationUnit> mBList = new MBList<IFormationUnit>();
		for (int i = 0; i < ColumnCount; i++)
		{
			int columnOffsetFromColumnIndex = GetColumnOffsetFromColumnIndex(i, isExpandingFromRightSide);
			int index = VanguardFileIndex + columnOffsetFromColumnIndex;
			if (_units2D[index, rankIndex1] != null)
			{
				mBList.Add(_units2D[index, rankIndex1]);
			}
		}
		for (int j = 0; j < ColumnCount; j++)
		{
			int columnOffsetFromColumnIndex2 = GetColumnOffsetFromColumnIndex(j, isExpandingFromRightSide);
			int index2 = VanguardFileIndex + columnOffsetFromColumnIndex2;
			if (_units2D[index2, rankIndex2] != null)
			{
				mBList.Add(_units2D[index2, rankIndex2]);
			}
		}
		return mBList;
	}

	public IEnumerable<T> GetUnitsAtVanguardFile<T>() where T : IFormationUnit
	{
		int fileIndex = VanguardFileIndex;
		for (int rankIndex = 0; rankIndex < RankCount; rankIndex++)
		{
			if (rankIndex == 0 && Vanguard != null)
			{
				yield return (T)Vanguard;
			}
			if (_units2D[fileIndex, rankIndex] != null)
			{
				yield return (T)_units2D[fileIndex, rankIndex];
			}
		}
	}

	public void UpdateLocalPositionErrors(bool recalculateErrors)
	{
	}

	public List<Vec2> GetUnitPositionsOnVanguardFileIndex()
	{
		IEnumerable<Agent> unitsAtVanguardFile = GetUnitsAtVanguardFile<Agent>();
		List<Vec2> list = new List<Vec2>(unitsAtVanguardFile.Count());
		foreach (Agent item in unitsAtVanguardFile)
		{
			list.Add(item.Position.AsVec2);
		}
		return list;
	}

	void IFormationArrangement.GetAllUnits(in MBList<IFormationUnit> allUnitsListToBeFilledIn)
	{
		GetAllUnits(in allUnitsListToBeFilledIn);
	}
}
