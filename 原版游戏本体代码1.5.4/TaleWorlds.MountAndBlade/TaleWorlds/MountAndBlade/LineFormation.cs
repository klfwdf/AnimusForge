using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class LineFormation : IFormationArrangement
{
	protected const int UnitPositionAvailabilityValueOfUnprocessed = 0;

	protected const int UnitPositionAvailabilityValueOfUnavailable = 1;

	protected const int UnitPositionAvailabilityValueOfAvailable = 2;

	private static readonly Vec2i InvalidPositionIndex = new Vec2i(-1, -1);

	protected readonly IFormation owner;

	private MBList2D<IFormationUnit> _units2D;

	private MBList2D<IFormationUnit> _units2DWorkspace;

	private MBList<IFormationUnit> _allUnits;

	private bool _isBatchRemovingUnits;

	private readonly List<int> _gapFillMinRanksPerFileForBatchRemove = new List<int>();

	private bool _batchRemoveInvolvesUnavailablePositions;

	private MBList<IFormationUnit> _unpositionedUnits;

	protected MBList2D<int> UnitPositionAvailabilities;

	private MBList2D<int> _unitPositionAvailabilitiesWorkspace;

	private MBList2D<WorldPosition> _globalPositions;

	private MBList2D<WorldPosition> _globalPositionsWorkspace;

	private readonly MBWorkspace<MBQueue<(IFormationUnit, int, int)>> _displacedUnitsWorkspace;

	private readonly MBWorkspace<MBArrayList<Vec2i>> _finalOccupationsWorkspace;

	private readonly MBWorkspace<MBQueue<Vec2i>> _toBeFilledInGapsWorkspace;

	private readonly MBWorkspace<MBArrayList<Vec2i>> _finalVacanciesWorkspace;

	private readonly MBWorkspace<MBArrayList<Vec2i>> _filledInGapsWorkspace;

	private readonly MBWorkspace<MBArrayList<Vec2i>> _toBeEmptiedOutUnitPositionsWorkspace;

	private MBArrayList<bool> _filledInUnitPositionsTable;

	private MBArrayList<Vec2i> _cachedOrderedUnitPositionIndices;

	private MBArrayList<Vec2i> _cachedOrderedAndAvailableUnitPositionIndices;

	private MBArrayList<Vec2> _cachedOrderedLocalPositions;

	private Func<LineFormation, int, int, bool> _shiftUnitsBackwardsPredicateDelegate;

	private Func<LineFormation, int, int, bool> _shiftUnitsForwardsPredicateDelegate;

	protected Func<Agent, bool> _isFrontUnitDelegate;

	private bool _isCavalry;

	private bool _isStaggered = true;

	private readonly bool _isDeformingOnWidthChange;

	private bool _isMiddleFrontUnitPositionReserved;

	protected bool IsTransforming;

	protected int FileCount => _units2D.Count1;

	public int RankCount => _units2D.Count2;

	public bool AreLocalPositionsDirty { protected get; set; }

	protected float Interval => owner.Interval;

	public virtual float IntervalMultiplier => 1f;

	protected float Distance => owner.Distance;

	public virtual float DistanceMultiplier => 1f;

	protected float UnitDiameter => owner.UnitDiameter;

	public virtual float Width
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

	public virtual float Depth => RankDepth;

	public float FlankWidth
	{
		get
		{
			return (float)(FileCount - 1) * (Interval + UnitDiameter) + UnitDiameter;
		}
		set
		{
			int fileCountFromWidth = GetFileCountFromWidth(value);
			if (fileCountFromWidth > FileCount)
			{
				WidenFormation(this, fileCountFromWidth - FileCount);
			}
			else if (fileCountFromWidth < FileCount)
			{
				NarrowFormation(this, FileCount - fileCountFromWidth);
			}
			this.OnWidthChanged?.Invoke();
			this.OnShapeChanged?.Invoke();
		}
	}

	private int MinimumFileCount
	{
		get
		{
			if (IsTransforming)
			{
				return 1;
			}
			int unitCountWithOverride = GetUnitCountWithOverride();
			return TaleWorlds.Library.MathF.Max(1, (int)TaleWorlds.Library.MathF.Sqrt(unitCountWithOverride));
		}
	}

	public float RankDepth => (float)(RankCount - 1) * (Distance + UnitDiameter) + UnitDiameter;

	public float MinimumFlankWidth => (float)(MinimumFileCount - 1) * (MinimumInterval + UnitDiameter) + UnitDiameter;

	public virtual float MinimumWidth => MinimumFlankWidth;

	private float MinimumInterval => owner.MinimumInterval;

	public virtual float MaximumWidth
	{
		get
		{
			float num = UnitDiameter;
			int unitCountWithOverride = GetUnitCountWithOverride();
			if (unitCountWithOverride > 0)
			{
				num += (float)(unitCountWithOverride - 1) * (owner.MaximumInterval + UnitDiameter);
			}
			return num;
		}
	}

	public bool IsStaggered
	{
		get
		{
			return _isStaggered;
		}
		set
		{
			if (_isStaggered != value)
			{
				_isStaggered = value;
				this.OnShapeChanged?.Invoke();
			}
		}
	}

	public virtual bool? IsLoose => null;

	public bool PostponeReconstructUnitsFromUnits2D { get; set; }

	public int UnitCount => GetAllUnits().Count;

	public int PositionedUnitCount => UnitCount - _unpositionedUnits.Count;

	public event Action OnWidthChanged;

	public event Action OnShapeChanged;

	public int GetFileCountFromWidth(float width)
	{
		return TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Max(0, (int)((width - UnitDiameter) / (Interval + UnitDiameter) + 1E-05f)) + 1, MinimumFileCount);
	}

	protected int GetUnitCountWithOverride()
	{
		return owner.OverridenUnitCount ?? UnitCount;
	}

	public LineFormation(IFormation ownerFormation, bool isStaggered = true)
	{
		owner = ownerFormation;
		IsStaggered = isStaggered;
		_units2D = new MBList2D<IFormationUnit>(1, 1);
		UnitPositionAvailabilities = new MBList2D<int>(1, 1);
		_globalPositions = new MBList2D<WorldPosition>(1, 1);
		_units2DWorkspace = new MBList2D<IFormationUnit>(1, 1);
		_unitPositionAvailabilitiesWorkspace = new MBList2D<int>(1, 1);
		_globalPositionsWorkspace = new MBList2D<WorldPosition>(1, 1);
		_cachedOrderedUnitPositionIndices = new MBArrayList<Vec2i>();
		_cachedOrderedAndAvailableUnitPositionIndices = new MBArrayList<Vec2i>();
		_cachedOrderedLocalPositions = new MBArrayList<Vec2>();
		_unpositionedUnits = new MBList<IFormationUnit>();
		_displacedUnitsWorkspace = new MBWorkspace<MBQueue<(IFormationUnit, int, int)>>();
		_finalOccupationsWorkspace = new MBWorkspace<MBArrayList<Vec2i>>();
		_toBeFilledInGapsWorkspace = new MBWorkspace<MBQueue<Vec2i>>();
		_finalVacanciesWorkspace = new MBWorkspace<MBArrayList<Vec2i>>();
		_filledInGapsWorkspace = new MBWorkspace<MBArrayList<Vec2i>>();
		_toBeEmptiedOutUnitPositionsWorkspace = new MBWorkspace<MBArrayList<Vec2i>>();
		_filledInUnitPositionsTable = new MBArrayList<bool>();
		_isFrontUnitDelegate = PreferShieldedUnitsOnFront;
		ReconstructUnitsFromUnits2D();
		this.OnShapeChanged?.Invoke();
	}

	protected LineFormation(IFormation ownerFormation, bool isDeformingOnWidthChange, bool isStaggered = true)
		: this(ownerFormation, isStaggered)
	{
		_isDeformingOnWidthChange = isDeformingOnWidthChange;
	}

	public virtual IFormationArrangement Clone(IFormation formation)
	{
		return new LineFormation(formation, _isDeformingOnWidthChange, IsStaggered);
	}

	public virtual void DeepCopyFrom(IFormationArrangement arrangement)
	{
		LineFormation lineFormation = arrangement as LineFormation;
		IsStaggered = lineFormation.IsStaggered;
		IsTransforming = lineFormation.IsTransforming;
	}

	public void Reset()
	{
		_units2D = new MBList2D<IFormationUnit>(1, 1);
		UnitPositionAvailabilities = new MBList2D<int>(1, 1);
		_globalPositions = new MBList2D<WorldPosition>(1, 1);
		_units2DWorkspace = new MBList2D<IFormationUnit>(1, 1);
		_unitPositionAvailabilitiesWorkspace = new MBList2D<int>(1, 1);
		_globalPositionsWorkspace = new MBList2D<WorldPosition>(1, 1);
		_cachedOrderedUnitPositionIndices = new MBArrayList<Vec2i>();
		_cachedOrderedAndAvailableUnitPositionIndices = new MBArrayList<Vec2i>();
		_cachedOrderedLocalPositions = new MBArrayList<Vec2>();
		_unpositionedUnits.Clear();
		ReconstructUnitsFromUnits2D();
		this.OnShapeChanged?.Invoke();
	}

	protected virtual bool IsUnitPositionRestrained(int fileIndex, int rankIndex)
	{
		if (_isMiddleFrontUnitPositionReserved)
		{
			Vec2i middleFrontUnitPosition = GetMiddleFrontUnitPosition();
			if (fileIndex == middleFrontUnitPosition.Item1 && rankIndex == middleFrontUnitPosition.Item2)
			{
				return true;
			}
			return false;
		}
		return false;
	}

	protected virtual void MakeRestrainedPositionsUnavailable()
	{
		if (_isMiddleFrontUnitPositionReserved)
		{
			Vec2i middleFrontUnitPosition = GetMiddleFrontUnitPosition();
			UnitPositionAvailabilities[middleFrontUnitPosition.Item1, middleFrontUnitPosition.Item2] = 1;
		}
	}

	protected IFormationUnit GetUnitAt(int fileIndex, int rankIndex)
	{
		return _units2D[fileIndex, rankIndex];
	}

	public bool IsUnitPositionAvailable(int fileIndex, int rankIndex)
	{
		return UnitPositionAvailabilities[fileIndex, rankIndex] == 2;
	}

	private Vec2i GetNearestAvailableNeighbourPositionIndex(int fileIndex, int rankIndex)
	{
		for (int i = 1; i < FileCount + RankCount; i++)
		{
			bool flag = true;
			bool flag2 = true;
			bool flag3 = true;
			bool flag4 = true;
			int num = 0;
			for (int j = 0; j <= i; j++)
			{
				if (!(flag || flag2 || flag3 || flag4))
				{
					break;
				}
				int num2 = i - j;
				num = j;
				int num3 = fileIndex - j;
				int num4 = fileIndex + j;
				int num5 = rankIndex - num2;
				int num6 = rankIndex + num2;
				if (flag && (num3 < 0 || num5 < 0))
				{
					flag = false;
				}
				if (flag3 && (num3 < 0 || num6 >= RankCount))
				{
					flag3 = false;
				}
				if (flag2 && (num4 >= FileCount || num5 < 0))
				{
					flag2 = false;
				}
				if (flag4 && (num4 >= FileCount || num6 >= RankCount))
				{
					flag4 = false;
				}
				if (flag && UnitPositionAvailabilities[num3, num5] == 2)
				{
					return new Vec2i(num3, num5);
				}
				if (flag3 && UnitPositionAvailabilities[num3, num6] == 2)
				{
					return new Vec2i(num3, num6);
				}
				if (flag2 && UnitPositionAvailabilities[num4, num5] == 2)
				{
					return new Vec2i(num4, num5);
				}
				if (flag4 && UnitPositionAvailabilities[num4, num6] == 2)
				{
					return new Vec2i(num4, num6);
				}
			}
			flag = (flag2 = (flag3 = (flag4 = true)));
			for (int k = 0; k < i - num; k++)
			{
				if (!(flag || flag2 || flag3 || flag4))
				{
					break;
				}
				int num7 = i - k;
				int num8 = fileIndex - num7;
				int num9 = fileIndex + num7;
				int num10 = rankIndex - k;
				int num11 = rankIndex + k;
				if (flag && (num8 < 0 || num10 < 0))
				{
					flag = false;
				}
				if (flag3 && (num8 < 0 || num11 >= RankCount))
				{
					flag3 = false;
				}
				if (flag2 && (num9 >= FileCount || num10 < 0))
				{
					flag2 = false;
				}
				if (flag4 && (num9 >= FileCount || num11 >= RankCount))
				{
					flag4 = false;
				}
				if (flag && UnitPositionAvailabilities[num8, num10] == 2)
				{
					return new Vec2i(num8, num10);
				}
				if (flag3 && UnitPositionAvailabilities[num8, num11] == 2)
				{
					return new Vec2i(num8, num11);
				}
				if (flag2 && UnitPositionAvailabilities[num9, num10] == 2)
				{
					return new Vec2i(num9, num10);
				}
				if (flag4 && UnitPositionAvailabilities[num9, num11] == 2)
				{
					return new Vec2i(num9, num11);
				}
			}
		}
		return InvalidPositionIndex;
	}

	private bool GetNextVacancy(out int fileIndex, out int rankIndex)
	{
		int num = FileCount * RankCount;
		for (int i = 0; i < num; i++)
		{
			Vec2i orderedUnitPositionIndex = GetOrderedUnitPositionIndex(i);
			fileIndex = orderedUnitPositionIndex.Item1;
			rankIndex = orderedUnitPositionIndex.Item2;
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
		int num = -1;
		int num2 = -1;
		IFormationUnit result = null;
		foreach (IFormationUnit allUnit in _allUnits)
		{
			int formationFileIndex = allUnit.FormationFileIndex;
			int formationRankIndex = allUnit.FormationRankIndex;
			int num3 = formationFileIndex + formationRankIndex;
			if (formationRankIndex > num2 || num3 > num)
			{
				num = num3;
				num2 = formationRankIndex;
				result = allUnit;
			}
		}
		return result;
	}

	private static Vec2i GetOrderedUnitPositionIndexAux(int fileIndexBegin, int fileIndexEnd, int rankIndexBegin, int rankIndexEnd, int unitIndex)
	{
		int num = fileIndexEnd - fileIndexBegin + 1;
		int num2 = unitIndex / num;
		int num3 = unitIndex - num2 * num;
		num3 = ((num % 2 != 1) ? (num / 2 - 1 + ((num3 % 2 != 0) ? 1 : (-1)) * (num3 + 1) / 2) : (num / 2 + ((num3 % 2 == 0) ? 1 : (-1)) * (num3 + 1) / 2));
		return new Vec2i(num3 + fileIndexBegin, num2 + rankIndexBegin);
	}

	private Vec2i GetOrderedUnitPositionIndex(int unitIndex)
	{
		return GetOrderedUnitPositionIndexAux(0, FileCount - 1, 0, RankCount - 1, unitIndex);
	}

	private static IEnumerable<Vec2i> GetOrderedUnitPositionIndicesAux(int fileIndexBegin, int fileIndexEnd, int rankIndexBegin, int rankIndexEnd)
	{
		int fileCount = fileIndexEnd - fileIndexBegin + 1;
		int centerFileIndex;
		if (fileCount % 2 == 1)
		{
			centerFileIndex = fileCount / 2;
			for (int rankIndex = rankIndexBegin; rankIndex <= rankIndexEnd; rankIndex++)
			{
				yield return new Vec2i(fileIndexBegin + centerFileIndex, rankIndex);
				for (int fileIndexOffset = 1; fileIndexOffset <= centerFileIndex; fileIndexOffset++)
				{
					yield return new Vec2i(fileIndexBegin + centerFileIndex - fileIndexOffset, rankIndex);
					if (centerFileIndex + fileIndexOffset < fileCount)
					{
						yield return new Vec2i(fileIndexBegin + centerFileIndex + fileIndexOffset, rankIndex);
					}
				}
			}
			yield break;
		}
		centerFileIndex = fileCount / 2 - 1;
		for (int rankIndex = rankIndexBegin; rankIndex <= rankIndexEnd; rankIndex++)
		{
			yield return new Vec2i(fileIndexBegin + centerFileIndex, rankIndex);
			for (int fileIndexOffset = 1; fileIndexOffset <= centerFileIndex + 1; fileIndexOffset++)
			{
				yield return new Vec2i(fileIndexBegin + centerFileIndex + fileIndexOffset, rankIndex);
				if (centerFileIndex - fileIndexOffset >= 0)
				{
					yield return new Vec2i(fileIndexBegin + centerFileIndex - fileIndexOffset, rankIndex);
				}
			}
		}
	}

	private IEnumerable<Vec2i> GetOrderedUnitPositionIndices()
	{
		return GetOrderedUnitPositionIndicesAux(0, FileCount - 1, 0, RankCount - 1);
	}

	public Vec2? GetLocalPositionOfUnitOrDefault(int unitIndex)
	{
		Vec2i vec2i = ((unitIndex < _cachedOrderedAndAvailableUnitPositionIndices.Count) ? _cachedOrderedAndAvailableUnitPositionIndices.ElementAt(unitIndex) : InvalidPositionIndex);
		Vec2? result;
		if (vec2i != InvalidPositionIndex)
		{
			int item = vec2i.Item1;
			int item2 = vec2i.Item2;
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
		return Vec2.Forward;
	}

	public WorldPosition? GetWorldPositionOfUnitOrDefault(int unitIndex)
	{
		Vec2i vec2i = ((unitIndex < _cachedOrderedAndAvailableUnitPositionIndices.Count) ? _cachedOrderedAndAvailableUnitPositionIndices.ElementAt(unitIndex) : InvalidPositionIndex);
		WorldPosition? result;
		if (vec2i != InvalidPositionIndex)
		{
			int item = vec2i.Item1;
			int item2 = vec2i.Item2;
			result = _globalPositions[item, item2];
		}
		else
		{
			result = null;
		}
		return result;
	}

	public IEnumerable<Vec2> GetUnavailableUnitPositions()
	{
		for (int fileIndex = 0; fileIndex < FileCount; fileIndex++)
		{
			for (int rankIndex = 0; rankIndex < RankCount; rankIndex++)
			{
				if (UnitPositionAvailabilities[fileIndex, rankIndex] == 1 && !IsUnitPositionRestrained(fileIndex, rankIndex))
				{
					yield return GetLocalPositionOfUnit(fileIndex, rankIndex);
				}
			}
		}
	}

	private void InsertUnit(IFormationUnit unit, int fileIndex, int rankIndex)
	{
		unit.FormationFileIndex = fileIndex;
		unit.FormationRankIndex = rankIndex;
		_units2D[fileIndex, rankIndex] = unit;
		ReconstructUnitsFromUnits2D();
		this.OnShapeChanged?.Invoke();
	}

	public bool AddUnit(IFormationUnit unit)
	{
		bool flag = false;
		while (!flag && !AreLastRanksCompletelyUnavailable())
		{
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
				if (!IsDeepenApplicable())
				{
					break;
				}
				Deepen();
			}
		}
		if (!flag)
		{
			_unpositionedUnits.Add(unit);
			ReconstructUnitsFromUnits2D();
		}
		if (flag)
		{
			if (!(this is TransposedLineFormation) && FileCount < MinimumFileCount)
			{
				WidenFormation(this, MinimumFileCount - FileCount);
			}
			this.OnShapeChanged?.Invoke();
			if (unit is Agent { HasMount: var hasMount })
			{
				if ((owner is Formation formation && formation.CalculateHasSignificantNumberOfMounted) != _isCavalry)
				{
					BatchUnitPositionAvailabilities();
				}
				else if (_isCavalry != hasMount && owner is Formation)
				{
					((Formation)owner).QuerySystem.ForceExpireCavalryUnitRatio();
					if (((Formation)owner).CalculateHasSignificantNumberOfMounted != _isCavalry)
					{
						BatchUnitPositionAvailabilities();
					}
				}
			}
		}
		return flag;
	}

	public void RemoveUnit(IFormationUnit unit)
	{
		if (_unpositionedUnits.Remove(unit))
		{
			ReconstructUnitsFromUnits2D();
		}
		else
		{
			RemoveUnit(unit, fillInTheGap: true);
		}
	}

	public IFormationUnit GetUnit(int fileIndex, int rankIndex)
	{
		return _units2D[fileIndex, rankIndex];
	}

	public void OnBatchRemoveStart()
	{
		if (!_isBatchRemovingUnits)
		{
			_isBatchRemovingUnits = true;
			_gapFillMinRanksPerFileForBatchRemove.Clear();
			_batchRemoveInvolvesUnavailablePositions = false;
		}
	}

	public void OnBatchRemoveEnd()
	{
		if (!_isBatchRemovingUnits)
		{
			return;
		}
		if (_gapFillMinRanksPerFileForBatchRemove.Count > 0)
		{
			for (int i = 0; i < _gapFillMinRanksPerFileForBatchRemove.Count; i++)
			{
				int num = _gapFillMinRanksPerFileForBatchRemove[i];
				if (i < FileCount && num < RankCount)
				{
					FillInTheGapsOfFile(this, i, num);
				}
			}
			FillInTheGapsOfFormationAfterRemove(_batchRemoveInvolvesUnavailablePositions);
			_gapFillMinRanksPerFileForBatchRemove.Clear();
		}
		_isBatchRemovingUnits = false;
	}

	public List<IFormationUnit> GetUnitsToPop(int count)
	{
		List<IFormationUnit> list = new List<IFormationUnit>();
		if (_unpositionedUnits.Count > 0)
		{
			int num = Math.Min(count, _unpositionedUnits.Count);
			list.AddRange(_unpositionedUnits.Take(num));
			count -= num;
		}
		if (count > 0)
		{
			for (int num2 = FileCount * RankCount - 1; num2 >= 0; num2--)
			{
				Vec2i orderedUnitPositionIndex = GetOrderedUnitPositionIndex(num2);
				int item = orderedUnitPositionIndex.Item1;
				int item2 = orderedUnitPositionIndex.Item2;
				if (_units2D[item, item2] != null)
				{
					list.Add(_units2D[item, item2]);
					count--;
					if (count == 0)
					{
						break;
					}
				}
			}
		}
		return list;
	}

	private void PickUnitsWithRespectToPosition(Agent agent, float distanceSquared, ref LinkedList<Tuple<IFormationUnit, float>> collection, ref List<IFormationUnit> chosenUnits, int countToChoose, bool chooseClosest)
	{
		if (collection.Count < countToChoose)
		{
			LinkedListNode<Tuple<IFormationUnit, float>> linkedListNode = null;
			for (LinkedListNode<Tuple<IFormationUnit, float>> linkedListNode2 = collection.First; linkedListNode2 != null; linkedListNode2 = linkedListNode2.Next)
			{
				if (chooseClosest ? (linkedListNode2.Value.Item2 < distanceSquared) : (linkedListNode2.Value.Item2 > distanceSquared))
				{
					linkedListNode = linkedListNode2;
					break;
				}
			}
			if (linkedListNode != null)
			{
				collection.AddBefore(linkedListNode, new LinkedListNode<Tuple<IFormationUnit, float>>(new Tuple<IFormationUnit, float>(agent, distanceSquared)));
			}
			else
			{
				collection.AddLast(new LinkedListNode<Tuple<IFormationUnit, float>>(new Tuple<IFormationUnit, float>(agent, distanceSquared)));
			}
		}
		else if (chooseClosest ? (distanceSquared < collection.First().Item2) : (distanceSquared > collection.First().Item2))
		{
			LinkedListNode<Tuple<IFormationUnit, float>> linkedListNode3 = null;
			for (LinkedListNode<Tuple<IFormationUnit, float>> next = collection.First.Next; next != null; next = next.Next)
			{
				if (chooseClosest ? (next.Value.Item2 < distanceSquared) : (next.Value.Item2 > distanceSquared))
				{
					linkedListNode3 = next;
					break;
				}
			}
			if (linkedListNode3 != null)
			{
				collection.AddBefore(linkedListNode3, new LinkedListNode<Tuple<IFormationUnit, float>>(new Tuple<IFormationUnit, float>(agent, distanceSquared)));
			}
			else
			{
				collection.AddLast(new LinkedListNode<Tuple<IFormationUnit, float>>(new Tuple<IFormationUnit, float>(agent, distanceSquared)));
			}
			if (!chooseClosest)
			{
				chosenUnits.Add(collection.First().Item1);
			}
			collection.RemoveFirst();
		}
		else if (!chooseClosest)
		{
			chosenUnits.Add(agent);
		}
	}

	public IEnumerable<IFormationUnit> GetUnitsToPopWithCondition(int count, Func<IFormationUnit, bool> currentCondition)
	{
		foreach (IFormationUnit item3 in _unpositionedUnits.Where((IFormationUnit uu) => currentCondition(uu)))
		{
			yield return item3;
			count--;
			if (count == 0)
			{
				yield break;
			}
		}
		for (int i = FileCount * RankCount - 1; i >= 0; i--)
		{
			Vec2i orderedUnitPositionIndex = GetOrderedUnitPositionIndex(i);
			int item = orderedUnitPositionIndex.Item1;
			int item2 = orderedUnitPositionIndex.Item2;
			if (_units2D[item, item2] != null && currentCondition(_units2D[item, item2]))
			{
				yield return _units2D[item, item2];
				count--;
				if (count == 0)
				{
					break;
				}
			}
		}
	}

	private void TryToKeepDepth()
	{
		if (FileCount > MinimumFileCount)
		{
			int num = CountUnitsAtRank(RankCount - 1);
			int num2 = RankCount - 1;
			int fileCount = FileCount;
			if (FileCount > 2)
			{
				num2 *= 2;
				fileCount -= 2;
			}
			else
			{
				fileCount--;
			}
			if (num + num2 <= fileCount && MBRandom.RandomInt(RankCount * 2) == 0 && IsNarrowApplicable((FileCount <= 2) ? 1 : 2))
			{
				NarrowFormation(this, (FileCount <= 2) ? 1 : 2);
			}
		}
	}

	public List<IFormationUnit> GetUnitsToPop(int count, Vec3 targetPosition)
	{
		List<IFormationUnit> chosenUnits = new List<IFormationUnit>();
		if (_unpositionedUnits.Count > 0)
		{
			int num = Math.Min(count, _unpositionedUnits.Count);
			if (num < _unpositionedUnits.Count)
			{
				LinkedList<Tuple<IFormationUnit, float>> collection = new LinkedList<Tuple<IFormationUnit, float>>();
				bool flag = (float)num <= (float)_unpositionedUnits.Count * 0.5f;
				int num2 = (flag ? num : (_unpositionedUnits.Count - num));
				for (int i = 0; i < _unpositionedUnits.Count; i++)
				{
					if (!(_unpositionedUnits[i] is Agent { Position: var position } agent))
					{
						if (flag)
						{
							collection.AddFirst(new Tuple<IFormationUnit, float>(_unpositionedUnits[i], float.MinValue));
							if (collection.Count > num)
							{
								collection.RemoveLast();
							}
						}
						else if (collection.Count < num2)
						{
							collection.AddLast(new Tuple<IFormationUnit, float>(_unpositionedUnits[i], float.MinValue));
						}
						else
						{
							chosenUnits.Add(_unpositionedUnits[i]);
						}
					}
					else
					{
						float distanceSquared = position.DistanceSquared(targetPosition);
						PickUnitsWithRespectToPosition(agent, distanceSquared, ref collection, ref chosenUnits, num2, flag);
					}
				}
				if (flag)
				{
					chosenUnits.AddRange(collection.Select((Tuple<IFormationUnit, float> tuple) => tuple.Item1));
				}
				count -= num;
			}
			else
			{
				chosenUnits.AddRange(_unpositionedUnits.Take(num));
				count -= num;
			}
		}
		if (count > 0)
		{
			int num3 = count;
			int num4 = UnitCount - _unpositionedUnits.Count;
			bool flag2 = num4 == num3;
			bool flag3 = (float)count <= (float)num4 * 0.5f;
			LinkedList<Tuple<IFormationUnit, float>> collection2 = (flag2 ? null : new LinkedList<Tuple<IFormationUnit, float>>());
			int num5 = (flag3 ? num3 : (num4 - num3));
			for (int num6 = FileCount * RankCount - 1; num6 >= 0; num6--)
			{
				Vec2i orderedUnitPositionIndex = GetOrderedUnitPositionIndex(num6);
				int item = orderedUnitPositionIndex.Item1;
				int item2 = orderedUnitPositionIndex.Item2;
				if (_units2D[item, item2] != null)
				{
					if (flag2)
					{
						chosenUnits.Add(_units2D[item, item2]);
						count--;
						if (count == 0)
						{
							break;
						}
					}
					else if (!(_units2D[item, item2] is Agent { Position: var position2 } agent2))
					{
						if (flag3)
						{
							collection2.AddFirst(new Tuple<IFormationUnit, float>(_unpositionedUnits[num6], float.MinValue));
							if (collection2.Count > num3)
							{
								collection2.RemoveLast();
							}
						}
						else if (collection2.Count < num5)
						{
							collection2.AddLast(new Tuple<IFormationUnit, float>(_unpositionedUnits[num6], float.MinValue));
						}
						else
						{
							chosenUnits.Add(_unpositionedUnits[num6]);
						}
					}
					else
					{
						float distanceSquared2 = position2.DistanceSquared(targetPosition);
						PickUnitsWithRespectToPosition(agent2, distanceSquared2, ref collection2, ref chosenUnits, num5, flag3);
					}
				}
			}
			if (!flag2 && flag3)
			{
				chosenUnits.AddRange(collection2.Select((Tuple<IFormationUnit, float> tuple) => tuple.Item1));
			}
		}
		return chosenUnits;
	}

	private void RemoveUnit(IFormationUnit unit, bool fillInTheGap, bool isRemovingFromAnUnavailablePosition = false)
	{
		if (fillInTheGap)
		{
		}
		int formationFileIndex = unit.FormationFileIndex;
		int formationRankIndex = unit.FormationRankIndex;
		if (unit.FormationFileIndex < 0 || unit.FormationRankIndex < 0 || unit.FormationFileIndex >= FileCount || unit.FormationRankIndex >= RankCount)
		{
			TaleWorlds.Library.Debug.Print(string.Concat("Unit removed has file-rank indices: ", unit.FormationFileIndex, " ", unit.FormationRankIndex, " while line formation has file-rank counts of ", FileCount, " ", RankCount, " agent state is ", (unit as Agent)?.State, " unit detachment is ", (unit as Agent)?.Detachment));
		}
		_units2D[unit.FormationFileIndex, unit.FormationRankIndex] = null;
		ReconstructUnitsFromUnits2D();
		unit.FormationFileIndex = -1;
		unit.FormationRankIndex = -1;
		if (fillInTheGap)
		{
			if (_isBatchRemovingUnits)
			{
				int num = formationFileIndex - _gapFillMinRanksPerFileForBatchRemove.Count + 1;
				for (int i = 0; i < num; i++)
				{
					_gapFillMinRanksPerFileForBatchRemove.Add(int.MaxValue);
				}
				_gapFillMinRanksPerFileForBatchRemove[formationFileIndex] = TaleWorlds.Library.MathF.Min(formationRankIndex, _gapFillMinRanksPerFileForBatchRemove[formationFileIndex]);
				_batchRemoveInvolvesUnavailablePositions |= isRemovingFromAnUnavailablePosition;
			}
			else
			{
				FillInTheGapsOfFile(this, formationFileIndex, formationRankIndex);
				FillInTheGapsOfFormationAfterRemove(isRemovingFromAnUnavailablePosition);
			}
		}
		this.OnShapeChanged?.Invoke();
	}

	protected virtual bool TryGetUnitPositionIndexFromLocalPosition(Vec2 localPosition, out int fileIndex, out int rankIndex)
	{
		rankIndex = TaleWorlds.Library.MathF.Round((0f - localPosition.y) / (Distance + UnitDiameter));
		if (rankIndex >= RankCount)
		{
			fileIndex = -1;
			return false;
		}
		if (IsStaggered && rankIndex % 2 == 1)
		{
			localPosition.x -= (Interval + UnitDiameter) * 0.5f;
		}
		float num = (float)(FileCount - 1) * (Interval + UnitDiameter);
		fileIndex = TaleWorlds.Library.MathF.Round((localPosition.x + num / 2f) / (Interval + UnitDiameter));
		if (fileIndex >= 0)
		{
			return fileIndex < FileCount;
		}
		return false;
	}

	protected virtual Vec2 GetLocalPositionOfUnit(int fileIndex, int rankIndex)
	{
		float num = (float)(FileCount - 1) * (Interval + UnitDiameter);
		Vec2 vec = new Vec2((float)fileIndex * (Interval + UnitDiameter) - num / 2f, (float)(-rankIndex) * (Distance + UnitDiameter));
		if (IsStaggered && rankIndex % 2 == 1)
		{
			vec.x += (Interval + UnitDiameter) * 0.5f;
		}
		if ((owner as Formation).Team != null && _units2D[fileIndex, rankIndex] != null)
		{
			return vec + (_units2D[fileIndex, rankIndex] as Agent).LocalPositionError;
		}
		return vec;
	}

	protected virtual Vec2 GetLocalPositionOfUnitWithAdjustment(int fileIndex, int rankIndex, float distanceBetweenAgentsAdjustment)
	{
		float num = Interval + distanceBetweenAgentsAdjustment;
		float num2 = (float)(FileCount - 1) * (num + UnitDiameter);
		Vec2 vec = new Vec2((float)fileIndex * (num + UnitDiameter) - num2 / 2f, (float)(-rankIndex) * (Distance + UnitDiameter));
		if (IsStaggered && rankIndex % 2 == 1)
		{
			vec.x += (num + UnitDiameter) * 0.5f;
		}
		if ((owner as Formation).Team != null && _units2D[fileIndex, rankIndex] != null)
		{
			return vec + (_units2D[fileIndex, rankIndex] as Agent).LocalPositionError;
		}
		return vec;
	}

	protected virtual Vec2 GetLocalDirectionOfUnit(int fileIndex, int rankIndex)
	{
		return Vec2.Forward;
	}

	public Vec2? GetLocalPositionOfUnitOrDefault(IFormationUnit unit)
	{
		if (_unpositionedUnits.Contains(unit))
		{
			return null;
		}
		return GetLocalPositionOfUnit(unit.FormationFileIndex, unit.FormationRankIndex);
	}

	public Vec2? GetLocalPositionOfUnitOrDefaultWithAdjustment(IFormationUnit unit, float distanceBetweenAgentsAdjustment)
	{
		if (_unpositionedUnits.Contains(unit))
		{
			return null;
		}
		return GetLocalPositionOfUnitWithAdjustment(unit.FormationFileIndex, unit.FormationRankIndex, distanceBetweenAgentsAdjustment);
	}

	public virtual Vec2? GetLocalDirectionOfUnitOrDefault(IFormationUnit unit)
	{
		return Vec2.Forward;
	}

	public WorldPosition? GetWorldPositionOfUnitOrDefault(IFormationUnit unit)
	{
		if (_unpositionedUnits.Contains(unit))
		{
			return null;
		}
		return _globalPositions[unit.FormationFileIndex, unit.FormationRankIndex];
	}

	private void ReconstructUnitsFromUnits2D()
	{
		if (PostponeReconstructUnitsFromUnits2D)
		{
			return;
		}
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
		for (int k = 0; k < _unpositionedUnits.Count; k++)
		{
			_allUnits.Add(_unpositionedUnits[k]);
		}
	}

	private void FillInTheGapsOfFormationAfterRemove(bool hasUnavailablePositions)
	{
		TryReaddingUnpositionedUnits();
		FillInTheGapsOfMiddleRanks(this);
		if (Mission.Current.IsDeploymentFinished)
		{
			TryToKeepDepth();
		}
	}

	private static void WidenFormation(LineFormation formation, int fileCountFromBothFlanks)
	{
		if (fileCountFromBothFlanks % 2 == 0)
		{
			WidenFormation(formation, fileCountFromBothFlanks / 2, fileCountFromBothFlanks / 2);
		}
		else if (formation.FileCount % 2 == 0)
		{
			WidenFormation(formation, fileCountFromBothFlanks / 2 + 1, fileCountFromBothFlanks / 2);
		}
		else
		{
			WidenFormation(formation, fileCountFromBothFlanks / 2, fileCountFromBothFlanks / 2 + 1);
		}
	}

	private static void WidenFormation(LineFormation formation, int fileCountFromLeftFlank, int fileCountFromRightFlank)
	{
		formation._units2DWorkspace.ResetWithNewCount(formation.FileCount + fileCountFromLeftFlank + fileCountFromRightFlank, formation.RankCount);
		formation._unitPositionAvailabilitiesWorkspace.ResetWithNewCount(formation.FileCount + fileCountFromLeftFlank + fileCountFromRightFlank, formation.RankCount);
		formation._globalPositionsWorkspace.ResetWithNewCount(formation.FileCount + fileCountFromLeftFlank + fileCountFromRightFlank, formation.RankCount);
		for (int i = 0; i < formation.FileCount; i++)
		{
			int destinationIndex = i + fileCountFromLeftFlank;
			formation._units2D.CopyRowTo(i, 0, formation._units2DWorkspace, destinationIndex, 0, formation.RankCount);
			formation.UnitPositionAvailabilities.CopyRowTo(i, 0, formation._unitPositionAvailabilitiesWorkspace, destinationIndex, 0, formation.RankCount);
			formation._globalPositions.CopyRowTo(i, 0, formation._globalPositionsWorkspace, destinationIndex, 0, formation.RankCount);
			if (fileCountFromLeftFlank <= 0)
			{
				continue;
			}
			for (int j = 0; j < formation.RankCount; j++)
			{
				if (formation._units2D[i, j] != null)
				{
					formation._units2D[i, j].FormationFileIndex += fileCountFromLeftFlank;
				}
			}
		}
		MBList2D<IFormationUnit> units2D = formation._units2D;
		formation._units2D = formation._units2DWorkspace;
		formation._units2DWorkspace = units2D;
		formation.ReconstructUnitsFromUnits2D();
		MBList2D<int> unitPositionAvailabilities = formation.UnitPositionAvailabilities;
		formation.UnitPositionAvailabilities = formation._unitPositionAvailabilitiesWorkspace;
		formation._unitPositionAvailabilitiesWorkspace = unitPositionAvailabilities;
		MBList2D<WorldPosition> globalPositions = formation._globalPositions;
		formation._globalPositions = formation._globalPositionsWorkspace;
		formation._globalPositionsWorkspace = globalPositions;
		formation.BatchUnitPositionAvailabilities();
		if (formation._isDeformingOnWidthChange || (fileCountFromLeftFlank + fileCountFromRightFlank) % 2 == 1)
		{
			formation.OnFormationFrameChanged();
		}
		else
		{
			ShiftUnitsForwardsForWideningFormation(formation);
			formation.TryReaddingUnpositionedUnits();
			while (formation.RankCount > 1 && formation.IsRankEmpty(formation.RankCount - 1))
			{
				formation.Shorten();
			}
		}
		formation.OnShapeChanged?.Invoke();
	}

	private static void GetToBeFilledInAndToBeEmptiedOutUnitPositions(LineFormation formation, MBQueue<Vec2i> toBeFilledInUnitPositions, MBArrayList<Vec2i> toBeEmptiedOutUnitPositions)
	{
		int num = 0;
		int num2 = formation.FileCount * formation.RankCount - 1;
		while (true)
		{
			Vec2i orderedUnitPositionIndex = formation.GetOrderedUnitPositionIndex(num);
			int item = orderedUnitPositionIndex.Item1;
			int item2 = orderedUnitPositionIndex.Item2;
			Vec2i orderedUnitPositionIndex2 = formation.GetOrderedUnitPositionIndex(num2);
			int item3 = orderedUnitPositionIndex2.Item1;
			int item4 = orderedUnitPositionIndex2.Item2;
			if (item2 < item4)
			{
				if (formation._units2D[item, item2] != null || !formation.IsUnitPositionAvailable(item, item2))
				{
					num++;
					continue;
				}
				if (formation._units2D[item3, item4] == null)
				{
					num2--;
					continue;
				}
				toBeFilledInUnitPositions.Enqueue(new Vec2i(item, item2));
				toBeEmptiedOutUnitPositions.Add(new Vec2i(item3, item4));
				num++;
				num2--;
				continue;
			}
			break;
		}
	}

	private static Vec2i GetUnitPositionForFillInFromNearby(LineFormation formation, int relocationFileIndex, int relocationRankIndex, Func<LineFormation, int, int, bool> predicate, bool isRelocationUnavailable = false)
	{
		return GetUnitPositionForFillInFromNearby(formation, relocationFileIndex, relocationRankIndex, predicate, InvalidPositionIndex, isRelocationUnavailable);
	}

	private static Vec2i GetUnitPositionForFillInFromNearby(LineFormation formation, int relocationFileIndex, int relocationRankIndex, Func<LineFormation, int, int, bool> predicate, Vec2i lastFinalOccupation, bool isRelocationUnavailable = false)
	{
		int fileCount = formation.FileCount;
		int rankCount = formation.RankCount;
		bool flag = relocationFileIndex >= fileCount / 2;
		if (lastFinalOccupation != InvalidPositionIndex)
		{
			flag = lastFinalOccupation.Item1 <= relocationFileIndex;
		}
		for (int i = 1; i <= fileCount + rankCount; i++)
		{
			for (int num = TaleWorlds.Library.MathF.Min(i, rankCount - 1 - relocationRankIndex); num >= 0; num--)
			{
				int num2 = i - num;
				if (flag && relocationFileIndex - num2 >= 0 && predicate(formation, relocationFileIndex - num2, relocationRankIndex + num))
				{
					return new Vec2i(relocationFileIndex - num2, relocationRankIndex + num);
				}
				if (relocationFileIndex + num2 < fileCount && predicate(formation, relocationFileIndex + num2, relocationRankIndex + num))
				{
					return new Vec2i(relocationFileIndex + num2, relocationRankIndex + num);
				}
				if (!flag && relocationFileIndex - num2 >= 0 && predicate(formation, relocationFileIndex - num2, relocationRankIndex + num))
				{
					return new Vec2i(relocationFileIndex - num2, relocationRankIndex + num);
				}
			}
		}
		return InvalidPositionIndex;
	}

	private static void ShiftUnitsForwardsForWideningFormation(LineFormation formation)
	{
		MBQueue<Vec2i> mBQueue = formation._toBeFilledInGapsWorkspace.StartUsingWorkspace();
		MBArrayList<Vec2i> mBArrayList = formation._finalVacanciesWorkspace.StartUsingWorkspace();
		MBArrayList<Vec2i> mBArrayList2 = formation._filledInGapsWorkspace.StartUsingWorkspace();
		GetToBeFilledInAndToBeEmptiedOutUnitPositions(formation, mBQueue, mBArrayList);
		if (formation._shiftUnitsForwardsPredicateDelegate == null)
		{
			formation._shiftUnitsForwardsPredicateDelegate = ShiftUnitForwardsPredicate;
		}
		while (mBQueue.Count > 0)
		{
			Vec2i item = mBQueue.Dequeue();
			Vec2i unitPositionForFillInFromNearby = GetUnitPositionForFillInFromNearby(formation, item.Item1, item.Item2, formation._shiftUnitsForwardsPredicateDelegate);
			if (unitPositionForFillInFromNearby != InvalidPositionIndex)
			{
				int item2 = unitPositionForFillInFromNearby.Item1;
				int item3 = unitPositionForFillInFromNearby.Item2;
				IFormationUnit unit = formation._units2D[item2, item3];
				formation.RelocateUnit(unit, item.Item1, item.Item2);
				mBArrayList2.Add(item);
				Vec2i item4 = new Vec2i(item2, item3);
				if (!mBArrayList.Contains(item4))
				{
					mBQueue.Enqueue(item4);
				}
			}
		}
		formation._toBeFilledInGapsWorkspace.StopUsingWorkspace();
		formation._finalVacanciesWorkspace.StopUsingWorkspace();
		formation._filledInGapsWorkspace.StopUsingWorkspace();
		static bool ShiftUnitForwardsPredicate(LineFormation localFormation, int fileIndex, int rankIndex)
		{
			if (localFormation._units2D[fileIndex, rankIndex] != null && localFormation.IsUnitPositionAvailable(fileIndex, rankIndex))
			{
				return !localFormation._filledInGapsWorkspace.GetWorkspace().Contains(new Vec2i(fileIndex, rankIndex));
			}
			return false;
		}
	}

	private static void DeepenFormation(LineFormation formation, int rankCountFromFront, int rankCountFromRear)
	{
		formation._units2DWorkspace.ResetWithNewCount(formation.FileCount, formation.RankCount + rankCountFromFront + rankCountFromRear);
		formation._unitPositionAvailabilitiesWorkspace.ResetWithNewCount(formation.FileCount, formation.RankCount + rankCountFromFront + rankCountFromRear);
		formation._globalPositionsWorkspace.ResetWithNewCount(formation.FileCount, formation.RankCount + rankCountFromFront + rankCountFromRear);
		for (int i = 0; i < formation.FileCount; i++)
		{
			formation._units2D.CopyRowTo(i, 0, formation._units2DWorkspace, i, rankCountFromFront, formation.RankCount);
			formation.UnitPositionAvailabilities.CopyRowTo(i, 0, formation._unitPositionAvailabilitiesWorkspace, i, rankCountFromFront, formation.RankCount);
			formation._globalPositions.CopyRowTo(i, 0, formation._globalPositionsWorkspace, i, rankCountFromFront, formation.RankCount);
			if (rankCountFromFront <= 0)
			{
				continue;
			}
			for (int j = 0; j < formation.RankCount; j++)
			{
				if (formation._units2D[i, j] != null)
				{
					formation._units2D[i, j].FormationRankIndex += rankCountFromFront;
				}
			}
		}
		MBList2D<IFormationUnit> units2D = formation._units2D;
		formation._units2D = formation._units2DWorkspace;
		formation._units2DWorkspace = units2D;
		formation.ReconstructUnitsFromUnits2D();
		MBList2D<int> unitPositionAvailabilities = formation.UnitPositionAvailabilities;
		formation.UnitPositionAvailabilities = formation._unitPositionAvailabilitiesWorkspace;
		formation._unitPositionAvailabilitiesWorkspace = unitPositionAvailabilities;
		MBList2D<WorldPosition> globalPositions = formation._globalPositions;
		formation._globalPositions = formation._globalPositionsWorkspace;
		formation._globalPositionsWorkspace = globalPositions;
		formation.BatchUnitPositionAvailabilities();
		formation.OnShapeChanged?.Invoke();
	}

	protected virtual bool IsDeepenApplicable()
	{
		return true;
	}

	private void Deepen()
	{
		DeepenFormation(this, 0, 1);
	}

	private static bool DeepenForVacancy(LineFormation formation, int requestedVacancyCount, int fileOffsetFromLeftFlank, int fileOffsetFromRightFlank)
	{
		int num = 0;
		bool? flag = null;
		while (!flag.HasValue)
		{
			int num2 = formation.RankCount - 1;
			for (int i = fileOffsetFromLeftFlank; i < formation.FileCount - fileOffsetFromRightFlank; i++)
			{
				if (formation._units2D[i, num2] == null && formation.IsUnitPositionAvailable(i, num2))
				{
					num++;
				}
			}
			if (num >= requestedVacancyCount)
			{
				flag = true;
			}
			else if (!formation.AreLastRanksCompletelyUnavailable())
			{
				if (formation.IsDeepenApplicable())
				{
					formation.Deepen();
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				flag = false;
			}
		}
		return flag.Value;
	}

	protected virtual bool IsNarrowApplicable(int amount)
	{
		return true;
	}

	private static void NarrowFormation(LineFormation formation, int fileCountFromBothFlanks)
	{
		int num = fileCountFromBothFlanks / 2;
		int num2 = fileCountFromBothFlanks / 2;
		if (fileCountFromBothFlanks % 2 != 0)
		{
			if (formation.FileCount % 2 == 0)
			{
				num2++;
			}
			else
			{
				num++;
			}
		}
		if (formation.IsNarrowApplicable(num + num2))
		{
			NarrowFormation(formation, num, num2);
		}
	}

	private static bool ShiftUnitsBackwardsForNewUnavailableUnitPositions(LineFormation formation)
	{
		MBArrayList<Vec2i> mBArrayList = formation._toBeEmptiedOutUnitPositionsWorkspace.StartUsingWorkspace();
		for (int i = 0; i < formation.FileCount * formation.RankCount; i++)
		{
			Vec2i orderedUnitPositionIndex = formation.GetOrderedUnitPositionIndex(i);
			if (formation._units2D[orderedUnitPositionIndex.Item1, orderedUnitPositionIndex.Item2] != null && !formation.IsUnitPositionAvailable(orderedUnitPositionIndex.Item1, orderedUnitPositionIndex.Item2))
			{
				mBArrayList.Add(orderedUnitPositionIndex);
			}
		}
		bool flag = mBArrayList.Count > 0;
		if (flag)
		{
			MBQueue<(IFormationUnit, int, int)> mBQueue = formation._displacedUnitsWorkspace.StartUsingWorkspace();
			for (int num = mBArrayList.Count - 1; num >= 0; num--)
			{
				Vec2i vec2i = mBArrayList[num];
				IFormationUnit formationUnit = formation._units2D[vec2i.Item1, vec2i.Item2];
				if (formationUnit != null)
				{
					formation.RemoveUnit(formationUnit, fillInTheGap: false, isRemovingFromAnUnavailablePosition: true);
					mBQueue.Enqueue(ValueTuple.Create(formationUnit, vec2i.Item1, vec2i.Item2));
				}
			}
			DeepenForVacancy(formation, mBQueue.Count, 0, 0);
			MBArrayList<Vec2i> mBArrayList2 = formation._finalOccupationsWorkspace.StartUsingWorkspace();
			int num2 = 0;
			for (int j = 0; j < formation.FileCount * formation.RankCount; j++)
			{
				if (num2 >= mBQueue.Count)
				{
					break;
				}
				Vec2i orderedUnitPositionIndex2 = formation.GetOrderedUnitPositionIndex(j);
				if (formation._units2D[orderedUnitPositionIndex2.Item1, orderedUnitPositionIndex2.Item2] == null && formation.IsUnitPositionAvailable(orderedUnitPositionIndex2.Item1, orderedUnitPositionIndex2.Item2))
				{
					mBArrayList2.Add(orderedUnitPositionIndex2);
					num2++;
				}
			}
			ShiftUnitsBackwardsAux(formation, mBQueue, mBArrayList2);
			formation._displacedUnitsWorkspace.StopUsingWorkspace();
			formation._finalOccupationsWorkspace.StopUsingWorkspace();
		}
		formation._toBeEmptiedOutUnitPositionsWorkspace.StopUsingWorkspace();
		return flag;
	}

	private static void ShiftUnitsBackwardsForNarrowingFormation(LineFormation formation, int fileCountFromLeftFlank, int fileCountFromRightFlank)
	{
		formation.PostponeReconstructUnitsFromUnits2D = true;
		MBQueue<(IFormationUnit, int, int)> mBQueue = formation._displacedUnitsWorkspace.StartUsingWorkspace();
		foreach (Vec2i item in (from p in formation.GetOrderedUnitPositionIndices()
			where p.Item1 < fileCountFromLeftFlank || p.Item1 >= formation.FileCount - fileCountFromRightFlank
			select p).Reverse())
		{
			IFormationUnit formationUnit = formation._units2D[item.Item1, item.Item2];
			if (formationUnit != null)
			{
				formation.RemoveUnit(formationUnit, fillInTheGap: false);
				mBQueue.Enqueue(ValueTuple.Create(formationUnit, item.Item1, item.Item2));
			}
		}
		DeepenForVacancy(formation, mBQueue.Count, fileCountFromLeftFlank, fileCountFromRightFlank);
		IEnumerable<Vec2i> list = (from p in GetOrderedUnitPositionIndicesAux(fileCountFromLeftFlank, formation.FileCount - 1 - fileCountFromRightFlank, 0, formation.RankCount - 1)
			where formation._units2D[p.Item1, p.Item2] == null && formation.IsUnitPositionAvailable(p.Item1, p.Item2)
			select p).Take(mBQueue.Count);
		MBArrayList<Vec2i> mBArrayList = formation._finalOccupationsWorkspace.StartUsingWorkspace();
		mBArrayList.AddRange(list);
		ShiftUnitsBackwardsAux(formation, mBQueue, mBArrayList);
		formation._displacedUnitsWorkspace.StopUsingWorkspace();
		formation._finalOccupationsWorkspace.StopUsingWorkspace();
		formation.PostponeReconstructUnitsFromUnits2D = false;
		formation.ReconstructUnitsFromUnits2D();
	}

	private static void ShiftUnitsBackwardsAux(LineFormation formation, MBQueue<(IFormationUnit, int, int)> displacedUnits, MBArrayList<Vec2i> finalOccupations)
	{
		MBArrayList<bool> mBArrayList = (formation._filledInUnitPositionsTable = new MBArrayList<bool>(new bool[formation.FileCount * formation.RankCount]));
		if (formation._shiftUnitsBackwardsPredicateDelegate == null)
		{
			formation._shiftUnitsBackwardsPredicateDelegate = ShiftUnitsBackwardsPredicate;
		}
		while (!displacedUnits.IsEmpty())
		{
			(IFormationUnit, int, int) tuple = displacedUnits.Dequeue();
			IFormationUnit item = tuple.Item1;
			int item2 = tuple.Item2;
			int item3 = tuple.Item3;
			Vec2i unitPositionForFillInFromNearby = GetUnitPositionForFillInFromNearby(formation, item2, item3, formation._shiftUnitsBackwardsPredicateDelegate, (finalOccupations.Count == 1) ? finalOccupations[0] : InvalidPositionIndex, isRelocationUnavailable: true);
			if (unitPositionForFillInFromNearby != InvalidPositionIndex)
			{
				IFormationUnit formationUnit = formation._units2D[unitPositionForFillInFromNearby.Item1, unitPositionForFillInFromNearby.Item2];
				if (formationUnit != null)
				{
					formation.RemoveUnit(formationUnit, fillInTheGap: false);
					displacedUnits.Enqueue(ValueTuple.Create(formationUnit, unitPositionForFillInFromNearby.Item1, unitPositionForFillInFromNearby.Item2));
				}
				mBArrayList[unitPositionForFillInFromNearby.Item1 + unitPositionForFillInFromNearby.Item2 * formation.FileCount] = true;
				formation.InsertUnit(item, unitPositionForFillInFromNearby.Item1, unitPositionForFillInFromNearby.Item2);
				continue;
			}
			float num = float.MaxValue;
			Vec2i vec2i = InvalidPositionIndex;
			for (int i = 0; i < finalOccupations.Count; i++)
			{
				if (!mBArrayList[finalOccupations[i].Item1 + finalOccupations[i].Item2 * formation.FileCount])
				{
					float num2 = TaleWorlds.Library.MathF.Abs(finalOccupations[i].Item1 - item2) + TaleWorlds.Library.MathF.Abs(finalOccupations[i].Item2 - item3);
					if (num2 < num)
					{
						num = num2;
						vec2i = finalOccupations[i];
					}
				}
			}
			if (vec2i != InvalidPositionIndex)
			{
				mBArrayList[vec2i.Item1 + vec2i.Item2 * formation.FileCount] = true;
				formation.InsertUnit(item, vec2i.Item1, vec2i.Item2);
			}
			else
			{
				formation._unpositionedUnits.Add(item);
				formation.ReconstructUnitsFromUnits2D();
			}
		}
		static bool ShiftUnitsBackwardsPredicate(LineFormation localFormation, int fileIndex, int rankIndex)
		{
			Vec2i vec2i2 = new Vec2i(fileIndex, rankIndex);
			if (!localFormation._filledInUnitPositionsTable[vec2i2.Item1 + vec2i2.Item2 * localFormation.FileCount] && localFormation._units2D[fileIndex, rankIndex] != null)
			{
				return localFormation.IsUnitPositionAvailable(fileIndex, rankIndex);
			}
			return false;
		}
	}

	private static void NarrowFormation(LineFormation formation, int fileCountFromLeftFlank, int fileCountFromRightFlank)
	{
		ShiftUnitsBackwardsForNarrowingFormation(formation, fileCountFromLeftFlank, fileCountFromRightFlank);
		NarrowFormationAux(formation, fileCountFromLeftFlank, fileCountFromRightFlank);
	}

	private static void NarrowFormationAux(LineFormation formation, int fileCountFromLeftFlank, int fileCountFromRightFlank)
	{
		formation._units2DWorkspace.ResetWithNewCount(formation.FileCount - fileCountFromLeftFlank - fileCountFromRightFlank, formation.RankCount);
		formation._unitPositionAvailabilitiesWorkspace.ResetWithNewCount(formation.FileCount - fileCountFromLeftFlank - fileCountFromRightFlank, formation.RankCount);
		formation._globalPositionsWorkspace.ResetWithNewCount(formation.FileCount - fileCountFromLeftFlank - fileCountFromRightFlank, formation.RankCount);
		for (int i = fileCountFromLeftFlank; i < formation.FileCount - fileCountFromRightFlank; i++)
		{
			int destinationIndex = i - fileCountFromLeftFlank;
			formation._units2D.CopyRowTo(i, 0, formation._units2DWorkspace, destinationIndex, 0, formation.RankCount);
			formation.UnitPositionAvailabilities.CopyRowTo(i, 0, formation._unitPositionAvailabilitiesWorkspace, destinationIndex, 0, formation.RankCount);
			formation._globalPositions.CopyRowTo(i, 0, formation._globalPositionsWorkspace, destinationIndex, 0, formation.RankCount);
			if (fileCountFromLeftFlank <= 0)
			{
				continue;
			}
			for (int j = 0; j < formation.RankCount; j++)
			{
				if (formation._units2D[i, j] != null)
				{
					formation._units2D[i, j].FormationFileIndex -= fileCountFromLeftFlank;
				}
			}
		}
		MBList2D<IFormationUnit> units2D = formation._units2D;
		formation._units2D = formation._units2DWorkspace;
		formation._units2DWorkspace = units2D;
		formation.ReconstructUnitsFromUnits2D();
		MBList2D<int> unitPositionAvailabilities = formation.UnitPositionAvailabilities;
		formation.UnitPositionAvailabilities = formation._unitPositionAvailabilitiesWorkspace;
		formation._unitPositionAvailabilitiesWorkspace = unitPositionAvailabilities;
		MBList2D<WorldPosition> globalPositions = formation._globalPositions;
		formation._globalPositions = formation._globalPositionsWorkspace;
		formation._globalPositionsWorkspace = globalPositions;
		formation.BatchUnitPositionAvailabilities();
		formation.OnShapeChanged?.Invoke();
		if (formation._isDeformingOnWidthChange || (fileCountFromLeftFlank + fileCountFromRightFlank) % 2 == 1)
		{
			formation.OnFormationFrameChanged();
		}
	}

	private static void ShortenFormation(LineFormation formation, int front, int rear)
	{
		formation._units2DWorkspace.ResetWithNewCount(formation.FileCount, formation.RankCount - front - rear);
		formation._unitPositionAvailabilitiesWorkspace.ResetWithNewCount(formation.FileCount, formation.RankCount - front - rear);
		formation._globalPositionsWorkspace.ResetWithNewCount(formation.FileCount, formation.RankCount - front - rear);
		for (int i = 0; i < formation.FileCount; i++)
		{
			formation._units2D.CopyRowTo(i, front, formation._units2DWorkspace, i, 0, formation.RankCount - rear - front);
			formation.UnitPositionAvailabilities.CopyRowTo(i, front, formation._unitPositionAvailabilitiesWorkspace, i, 0, formation.RankCount - rear - front);
			formation._globalPositions.CopyRowTo(i, front, formation._globalPositionsWorkspace, i, 0, formation.RankCount - rear - front);
			if (front <= 0)
			{
				continue;
			}
			for (int j = front; j < formation.RankCount - rear; j++)
			{
				if (formation._units2D[i, j] != null)
				{
					formation._units2D[i, j].FormationRankIndex -= front;
				}
			}
		}
		MBList2D<IFormationUnit> units2D = formation._units2D;
		formation._units2D = formation._units2DWorkspace;
		formation._units2DWorkspace = units2D;
		formation.ReconstructUnitsFromUnits2D();
		MBList2D<int> unitPositionAvailabilities = formation.UnitPositionAvailabilities;
		formation.UnitPositionAvailabilities = formation._unitPositionAvailabilitiesWorkspace;
		formation._unitPositionAvailabilitiesWorkspace = unitPositionAvailabilities;
		MBList2D<WorldPosition> globalPositions = formation._globalPositions;
		formation._globalPositions = formation._globalPositionsWorkspace;
		formation._globalPositionsWorkspace = globalPositions;
		formation.BatchUnitPositionAvailabilities();
		formation.OnShapeChanged?.Invoke();
	}

	private void Shorten()
	{
		ShortenFormation(this, 0, 1);
	}

	private void GetFrontAndRearOfFile(int fileIndex, out bool isFileEmtpy, out int rankIndexOfFront, out int rankIndexOfRear, bool includeUnavailablePositions = false)
	{
		rankIndexOfFront = -1;
		rankIndexOfRear = RankCount;
		for (int i = 0; i < RankCount; i++)
		{
			if (_units2D[fileIndex, i] != null)
			{
				rankIndexOfFront = i;
				break;
			}
		}
		if (includeUnavailablePositions)
		{
			if (rankIndexOfFront != -1)
			{
				int num = rankIndexOfFront - 1;
				while (num >= 0 && !IsUnitPositionAvailable(fileIndex, num))
				{
					rankIndexOfFront = num;
					num--;
				}
			}
			else
			{
				bool flag = true;
				for (int j = 0; j < RankCount; j++)
				{
					if (IsUnitPositionAvailable(fileIndex, j))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					rankIndexOfFront = 0;
				}
			}
		}
		for (int num2 = RankCount - 1; num2 >= 0; num2--)
		{
			if (_units2D[fileIndex, num2] != null)
			{
				rankIndexOfRear = num2;
				break;
			}
		}
		if (includeUnavailablePositions)
		{
			if (rankIndexOfRear != RankCount)
			{
				for (int k = rankIndexOfRear + 1; k < RankCount && !IsUnitPositionAvailable(fileIndex, k); k++)
				{
					rankIndexOfRear = k;
				}
			}
			else
			{
				bool flag2 = true;
				for (int l = 0; l < RankCount; l++)
				{
					if (IsUnitPositionAvailable(fileIndex, l))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					rankIndexOfRear = RankCount - 1;
				}
			}
		}
		if (rankIndexOfFront == -1 && rankIndexOfRear == RankCount)
		{
			isFileEmtpy = true;
		}
		else
		{
			isFileEmtpy = false;
		}
	}

	private void GetFlanksOfRank(int rankIndex, out bool isRankEmpty, out int fileIndexOfLeftFlank, out int fileIndexOfRightFlank, bool includeUnavailablePositions = false)
	{
		fileIndexOfLeftFlank = -1;
		fileIndexOfRightFlank = FileCount;
		for (int i = 0; i < FileCount; i++)
		{
			if (_units2D[i, rankIndex] != null)
			{
				fileIndexOfLeftFlank = i;
				break;
			}
		}
		if (includeUnavailablePositions)
		{
			if (fileIndexOfLeftFlank != -1)
			{
				int num = fileIndexOfLeftFlank - 1;
				while (num >= 0 && !IsUnitPositionAvailable(num, rankIndex))
				{
					fileIndexOfLeftFlank = num;
					num--;
				}
			}
			else
			{
				bool flag = true;
				for (int j = 0; j < FileCount; j++)
				{
					if (IsUnitPositionAvailable(j, rankIndex))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					fileIndexOfLeftFlank = 0;
				}
			}
		}
		for (int num2 = FileCount - 1; num2 >= 0; num2--)
		{
			if (_units2D[num2, rankIndex] != null)
			{
				fileIndexOfRightFlank = num2;
				break;
			}
		}
		if (includeUnavailablePositions)
		{
			if (fileIndexOfRightFlank != FileCount)
			{
				for (int k = fileIndexOfRightFlank + 1; k < FileCount && !IsUnitPositionAvailable(k, rankIndex); k++)
				{
					fileIndexOfRightFlank = k;
				}
			}
			else
			{
				bool flag2 = true;
				for (int l = 0; l < FileCount; l++)
				{
					if (IsUnitPositionAvailable(l, rankIndex))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					fileIndexOfRightFlank = FileCount - 1;
				}
			}
		}
		if (fileIndexOfLeftFlank == -1 && fileIndexOfRightFlank == FileCount)
		{
			isRankEmpty = true;
		}
		else
		{
			isRankEmpty = false;
		}
	}

	private static void FillInTheGapsOfFile(LineFormation formation, int fileIndex, int rankIndex = 0, bool isCheckingLastRankForEmptiness = true)
	{
		FillInTheGapsOfFileAux(formation, fileIndex, rankIndex);
		while (isCheckingLastRankForEmptiness && formation.RankCount > 1 && formation.IsRankEmpty(formation.RankCount - 1))
		{
			formation.Shorten();
		}
	}

	private static void FillInTheGapsOfFileAux(LineFormation formation, int fileIndex, int rankIndex)
	{
		while (true)
		{
			int num = -1;
			while (rankIndex < formation.RankCount - 1)
			{
				if (formation._units2D[fileIndex, rankIndex] == null && formation.IsUnitPositionAvailable(fileIndex, rankIndex))
				{
					num = rankIndex;
					break;
				}
				rankIndex++;
			}
			int num2 = -1;
			while (rankIndex < formation.RankCount)
			{
				if (formation._units2D[fileIndex, rankIndex] != null)
				{
					num2 = rankIndex;
					break;
				}
				rankIndex++;
			}
			if (num != -1 && num2 != -1)
			{
				formation.RelocateUnit(formation._units2D[fileIndex, num2], fileIndex, num);
				rankIndex = num + 1;
				continue;
			}
			break;
		}
	}

	private static void FillInTheGapsOfMiddleRanks(LineFormation formation, List<IFormationUnit> relocatedUnits = null)
	{
		int num = formation.RankCount - 1;
		for (int i = 0; i < formation.FileCount; i++)
		{
			if (formation._units2D[i, num] != null || formation.IsFileFullyOccupied(i))
			{
				continue;
			}
			while (true)
			{
				formation.GetFrontAndRearOfFile(i, out var isFileEmtpy, out var _, out var rankIndexOfRear, includeUnavailablePositions: true);
				if (rankIndexOfRear == num)
				{
					break;
				}
				int num2 = rankIndexOfRear + 1;
				if (isFileEmtpy)
				{
					num2 = -1;
					for (int j = 0; j < formation.RankCount; j++)
					{
						if (formation.IsUnitPositionAvailable(i, j))
						{
							num2 = j;
							break;
						}
					}
				}
				IFormationUnit unitToFillIn = GetUnitToFillIn(formation, i, num2);
				if (unitToFillIn != null)
				{
					formation.RelocateUnit(unitToFillIn, i, num2);
					relocatedUnits?.Add(unitToFillIn);
					if (formation.IsRankEmpty(num))
					{
						formation.Shorten();
						num = formation.RankCount - 1;
					}
					continue;
				}
				for (int k = num2 + 1; k < formation.RankCount; k++)
				{
				}
				break;
			}
		}
		while (formation.RankCount > 1 && formation.IsRankEmpty(formation.RankCount - 1))
		{
			formation.Shorten();
		}
		AlignLastRank(formation);
	}

	private static void AlignRankToLeft(LineFormation formation, int fileIndex, int rankIndex)
	{
		int num = -1;
		while (fileIndex < formation.FileCount - 1)
		{
			if (formation._units2D[fileIndex, rankIndex] == null && formation.IsUnitPositionAvailable(fileIndex, rankIndex))
			{
				num = fileIndex;
				break;
			}
			fileIndex++;
		}
		int num2 = -1;
		while (fileIndex < formation.FileCount)
		{
			if (formation._units2D[fileIndex, rankIndex] != null)
			{
				num2 = fileIndex;
				break;
			}
			fileIndex++;
		}
		if (num != -1 && num2 != -1)
		{
			formation.RelocateUnit(formation._units2D[num2, rankIndex], num, rankIndex);
			AlignRankToLeft(formation, num + 1, rankIndex);
		}
	}

	private static void AlignRankToRight(LineFormation formation, int fileIndex, int rankIndex)
	{
		int num = -1;
		while (fileIndex > 0)
		{
			if (formation._units2D[fileIndex, rankIndex] == null && formation.IsUnitPositionAvailable(fileIndex, rankIndex))
			{
				num = fileIndex;
				break;
			}
			fileIndex--;
		}
		int num2 = -1;
		while (fileIndex >= 0)
		{
			if (formation._units2D[fileIndex, rankIndex] != null)
			{
				num2 = fileIndex;
				break;
			}
			fileIndex--;
		}
		if (num != -1 && num2 != -1)
		{
			formation.RelocateUnit(formation._units2D[num2, rankIndex], num, rankIndex);
			AlignRankToRight(formation, num - 1, rankIndex);
		}
	}

	private static void AlignLastRank(LineFormation formation)
	{
		int num = formation.RankCount - 1;
		formation.GetFlanksOfRank(num, out var isRankEmpty, out var fileIndexOfLeftFlank, out var fileIndexOfRightFlank, includeUnavailablePositions: true);
		if (num == 0 && isRankEmpty)
		{
			return;
		}
		AlignRankToLeft(formation, fileIndexOfLeftFlank, num);
		bool flag = false;
		bool flag2 = false;
		while (true)
		{
			formation.GetFlanksOfRank(num, out isRankEmpty, out fileIndexOfLeftFlank, out fileIndexOfRightFlank, includeUnavailablePositions: true);
			if (!flag && fileIndexOfLeftFlank < formation.FileCount - fileIndexOfRightFlank - 1 - 1)
			{
				formation.GetFlanksOfRank(num, out isRankEmpty, out var _, out var fileIndexOfRightFlank2);
				formation.RelocateUnit(formation._units2D[fileIndexOfRightFlank2, num], fileIndexOfRightFlank + 1, num);
				AlignRankToRight(formation, fileIndexOfRightFlank + 1, num);
				flag2 = true;
				continue;
			}
			if (!flag2 && fileIndexOfLeftFlank - 1 > formation.FileCount - fileIndexOfRightFlank - 1)
			{
				formation.GetFlanksOfRank(num, out isRankEmpty, out var fileIndexOfLeftFlank3, out var _);
				formation.RelocateUnit(formation._units2D[fileIndexOfLeftFlank3, num], fileIndexOfLeftFlank - 1, num);
				AlignRankToLeft(formation, fileIndexOfLeftFlank - 1, num);
				flag = true;
				continue;
			}
			break;
		}
	}

	private int CountUnitsAtRank(int rankIndex)
	{
		int num = 0;
		for (int i = 0; i < FileCount; i++)
		{
			if (_units2D[i, rankIndex] != null)
			{
				num++;
			}
		}
		return num;
	}

	private bool IsRankEmpty(int rankIndex)
	{
		for (int i = 0; i < FileCount; i++)
		{
			if (_units2D[i, rankIndex] != null)
			{
				return false;
			}
		}
		return true;
	}

	private bool IsFileFullyOccupied(int fileIndex)
	{
		bool result = true;
		for (int i = 0; i < RankCount; i++)
		{
			if (_units2D[fileIndex, i] == null && IsUnitPositionAvailable(fileIndex, i))
			{
				result = false;
				break;
			}
		}
		return result;
	}

	private bool IsRankFullyOccupied(int rankIndex)
	{
		bool result = true;
		for (int i = 0; i < FileCount; i++)
		{
			if (_units2D[i, rankIndex] == null && IsUnitPositionAvailable(i, rankIndex))
			{
				result = false;
				break;
			}
		}
		return result;
	}

	private static IFormationUnit GetUnitToFillIn(LineFormation formation, int relocationFileIndex, int relocationRankIndex)
	{
		for (int num = formation.RankCount - 1; num >= 0; num--)
		{
			if (relocationRankIndex == num)
			{
				return null;
			}
			formation.GetFlanksOfRank(num, out var isRankEmpty, out var fileIndexOfLeftFlank, out var fileIndexOfRightFlank);
			if (!isRankEmpty)
			{
				if (relocationFileIndex > fileIndexOfRightFlank)
				{
					return formation._units2D[fileIndexOfRightFlank, num];
				}
				if (relocationFileIndex < fileIndexOfLeftFlank)
				{
					return formation._units2D[fileIndexOfLeftFlank, num];
				}
				if (fileIndexOfRightFlank - relocationFileIndex > relocationFileIndex - fileIndexOfLeftFlank)
				{
					return formation._units2D[fileIndexOfLeftFlank, num];
				}
				return formation._units2D[fileIndexOfRightFlank, num];
			}
		}
		TaleWorlds.Library.Debug.FailedAssert("This line should not be reached.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Formation\\LineFormation.cs", "GetUnitToFillIn", 3202);
		return null;
	}

	protected void RelocateUnit(IFormationUnit unit, int fileIndex, int rankIndex)
	{
		_units2D[unit.FormationFileIndex, unit.FormationRankIndex] = null;
		_units2D[fileIndex, rankIndex] = unit;
		ReconstructUnitsFromUnits2D();
		unit.FormationFileIndex = fileIndex;
		unit.FormationRankIndex = rankIndex;
		this.OnShapeChanged?.Invoke();
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
		return _unpositionedUnits;
	}

	public Vec2? GetLocalDirectionOfRelativeFormationLocation(IFormationUnit unit)
	{
		if (_unpositionedUnits.Contains(unit))
		{
			return null;
		}
		Vec2 value = new Vec2(unit.FormationFileIndex, -unit.FormationRankIndex) - new Vec2((float)(FileCount - 1) * 0.5f, (float)(RankCount - 1) * -0.5f);
		value.Normalize();
		return value;
	}

	public Vec2? GetLocalWallDirectionOfRelativeFormationLocation(IFormationUnit unit)
	{
		if (_unpositionedUnits.Contains(unit))
		{
			return null;
		}
		Vec2 value = new Vec2(unit.FormationFileIndex, -unit.FormationRankIndex) - new Vec2((float)(FileCount - 1) * 0.5f, -RankCount);
		value.Normalize();
		return value;
	}

	public void GetFormationInfo(out int fileCount, out int rankCount)
	{
		fileCount = FileCount;
		rankCount = RankCount;
	}

	[Conditional("DEBUG")]
	private void AssertUnit(IFormationUnit unit, bool isAssertingUnitPositionAvailability = true)
	{
		if (isAssertingUnitPositionAvailability)
		{
			IsUnitPositionRestrained(unit.FormationFileIndex, unit.FormationRankIndex);
			if (_isMiddleFrontUnitPositionReserved && GetMiddleFrontUnitPosition().Item1 == unit.FormationFileIndex)
			{
				_ = GetMiddleFrontUnitPosition().Item2 == unit.FormationRankIndex;
			}
			else
				_ = 0;
			IsUnitPositionAvailable(unit.FormationFileIndex, unit.FormationRankIndex);
		}
	}

	[Conditional("DEBUG")]
	private void AssertUnpositionedUnit(IFormationUnit unit)
	{
	}

	public float GetUnitsDistanceToFrontLine(IFormationUnit unit)
	{
		if (_unpositionedUnits.Contains(unit))
		{
			return -1f;
		}
		return (float)unit.FormationRankIndex * (Distance + UnitDiameter) + UnitDiameter * 0.5f;
	}

	public IFormationUnit GetNeighborUnitOfLeftSide(IFormationUnit unit)
	{
		if (_unpositionedUnits.Contains(unit))
		{
			return null;
		}
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
		if (_unpositionedUnits.Contains(unit))
		{
			return null;
		}
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

	public void SwitchUnitLocationsWithUnpositionedUnit(IFormationUnit firstUnit, IFormationUnit secondUnit)
	{
		int formationFileIndex = firstUnit.FormationFileIndex;
		int formationRankIndex = firstUnit.FormationRankIndex;
		_unpositionedUnits.Remove(secondUnit);
		_units2D[formationFileIndex, formationRankIndex] = secondUnit;
		secondUnit.FormationFileIndex = formationFileIndex;
		secondUnit.FormationRankIndex = formationRankIndex;
		firstUnit.FormationFileIndex = -1;
		firstUnit.FormationRankIndex = -1;
		_unpositionedUnits.Add(firstUnit);
		ReconstructUnitsFromUnits2D();
		this.OnShapeChanged?.Invoke();
	}

	public void SwitchUnitLocations(IFormationUnit firstUnit, IFormationUnit secondUnit)
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
	}

	public void SwitchUnitLocationsWithBackMostUnit(IFormationUnit unit)
	{
		IFormationUnit lastUnit = GetLastUnit();
		if (lastUnit != null && unit != null && unit != lastUnit)
		{
			SwitchUnitLocations(unit, lastUnit);
		}
	}

	public void BeforeFormationFrameChange()
	{
	}

	public void BatchUnitPositionAvailabilities(bool isUpdatingCachedOrderedLocalPositions = true)
	{
		if (isUpdatingCachedOrderedLocalPositions)
		{
			AreLocalPositionsDirty = true;
		}
		bool areLocalPositionsDirty = AreLocalPositionsDirty;
		AreLocalPositionsDirty = false;
		if (areLocalPositionsDirty)
		{
			_cachedOrderedUnitPositionIndices.Clear();
			for (int i = 0; i < FileCount * RankCount; i++)
			{
				_cachedOrderedUnitPositionIndices.Add(GetOrderedUnitPositionIndex(i));
			}
			_cachedOrderedLocalPositions.Clear();
			for (int j = 0; j < _cachedOrderedUnitPositionIndices.Count; j++)
			{
				Vec2i vec2i = _cachedOrderedUnitPositionIndices[j];
				_cachedOrderedLocalPositions.Add(GetLocalPositionOfUnit(vec2i.Item1, vec2i.Item2));
			}
		}
		MakeRestrainedPositionsUnavailable();
		if (!owner.BatchUnitPositions(_cachedOrderedUnitPositionIndices, _cachedOrderedLocalPositions, UnitPositionAvailabilities, _globalPositions, FileCount, RankCount))
		{
			for (int k = 0; k < FileCount; k++)
			{
				for (int l = 0; l < RankCount; l++)
				{
					UnitPositionAvailabilities[k, l] = 1;
				}
			}
		}
		else
		{
			(owner as Formation).SetHasPendingUnitPositions(hasPendingUnitPositions: true);
		}
		if (areLocalPositionsDirty)
		{
			_cachedOrderedAndAvailableUnitPositionIndices.Clear();
			for (int m = 0; m < _cachedOrderedUnitPositionIndices.Count; m++)
			{
				Vec2i item = _cachedOrderedUnitPositionIndices[m];
				if (UnitPositionAvailabilities[item.Item1, item.Item2] == 2)
				{
					_cachedOrderedAndAvailableUnitPositionIndices.Add(item);
				}
			}
		}
		_isCavalry = owner is Formation formation && formation.CalculateHasSignificantNumberOfMounted;
	}

	public void OnFormationFrameChanged(bool updateCachedOrderedLocalPositions = false)
	{
		UnitPositionAvailabilities.Clear();
		BatchUnitPositionAvailabilities(updateCachedOrderedLocalPositions);
		bool flag = ShiftUnitsBackwardsForNewUnavailableUnitPositions(this);
		for (int i = 0; i < FileCount; i++)
		{
			FillInTheGapsOfFile(this, i, 0, i == FileCount - 1);
		}
		bool flag2 = TryReaddingUnpositionedUnits();
		if (flag && !flag2)
		{
			owner.OnUnitAddedOrRemoved();
		}
		FillInTheGapsOfMiddleRanks(this);
	}

	private bool TryReaddingUnpositionedUnits()
	{
		bool flag = _unpositionedUnits.Count > 0;
		int num;
		for (num = _unpositionedUnits.Count - 1; num >= 0; num--)
		{
			num = TaleWorlds.Library.MathF.Min(num, _unpositionedUnits.Count - 1);
			if (num < 0)
			{
				break;
			}
			IFormationUnit unit = _unpositionedUnits[num];
			RemoveUnit(unit);
			if (!AddUnit(unit))
			{
				break;
			}
		}
		if (flag)
		{
			owner.OnUnitAddedOrRemoved();
		}
		return flag;
	}

	private bool AreLastRanksCompletelyUnavailable(int numberOfRanksToCheck = 3)
	{
		bool result = true;
		if (RankCount < numberOfRanksToCheck)
		{
			result = false;
		}
		else
		{
			for (int i = 0; i < FileCount; i++)
			{
				for (int j = RankCount - numberOfRanksToCheck; j < RankCount; j++)
				{
					if (IsUnitPositionAvailable(i, j))
					{
						i = 2147483646;
						result = false;
						break;
					}
				}
			}
		}
		return result;
	}

	public void UpdateLocalPositionErrors(bool recalculateErrors)
	{
		if (recalculateErrors)
		{
			(owner as Formation).ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.UpdateLocalPositionError();
			});
		}
		OnFormationFrameChanged(updateCachedOrderedLocalPositions: true);
	}

	[Conditional("DEBUG")]
	private void AssertUnitPositions()
	{
		for (int i = 0; i < _units2D.Count1; i++)
		{
			for (int j = 0; j < _units2D.Count2; j++)
			{
				_ = _units2D[i, j];
			}
		}
		foreach (IFormationUnit unpositionedUnit in _unpositionedUnits)
		{
			_ = unpositionedUnit;
		}
	}

	[Conditional("DEBUG")]
	private void AssertFilePositions(int fileIndex)
	{
		GetFrontAndRearOfFile(fileIndex, out var isFileEmtpy, out var rankIndexOfFront, out var rankIndexOfRear, includeUnavailablePositions: true);
		if (!isFileEmtpy)
		{
			for (int i = rankIndexOfFront; i <= rankIndexOfRear; i++)
			{
			}
		}
	}

	[Conditional("DEBUG")]
	private void AssertRankPositions(int rankIndex)
	{
		GetFlanksOfRank(rankIndex, out var isRankEmpty, out var fileIndexOfLeftFlank, out var fileIndexOfRightFlank, includeUnavailablePositions: true);
		if (!isRankEmpty)
		{
			for (int i = fileIndexOfLeftFlank; i <= fileIndexOfRightFlank; i++)
			{
			}
		}
	}

	public void OnFormationDispersed()
	{
		IEnumerable<Vec2i> enumerable = from i in GetOrderedUnitPositionIndices()
			where IsUnitPositionAvailable(i.Item1, i.Item2)
			select i;
		MBList<IFormationUnit> mBList = GetAllUnits().ToMBList();
		foreach (Vec2i item3 in enumerable)
		{
			int item = item3.Item1;
			int item2 = item3.Item2;
			IFormationUnit formationUnit = _units2D[item, item2];
			if (formationUnit == null)
			{
				continue;
			}
			IFormationUnit closestUnitTo = owner.GetClosestUnitTo(GetLocalPositionOfUnit(item, item2), mBList);
			mBList[mBList.IndexOf(closestUnitTo)] = null;
			if (formationUnit != closestUnitTo)
			{
				if (closestUnitTo.FormationFileIndex == -1)
				{
					SwitchUnitLocationsWithUnpositionedUnit(formationUnit, closestUnitTo);
				}
				else
				{
					SwitchUnitLocations(formationUnit, closestUnitTo);
				}
			}
		}
	}

	public void OnUnitLostMount(IFormationUnit unit)
	{
	}

	public bool IsTurnBackwardsNecessary(Vec2 previousPosition, WorldPosition? newPosition, Vec2 previousDirection, bool hasNewDirection, Vec2? newDirection)
	{
		if (hasNewDirection)
		{
			return TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(newDirection.Value.RotationInRadians, previousDirection.RotationInRadians)) >= System.MathF.PI * 3f / 4f;
		}
		return false;
	}

	public virtual void TurnBackwards()
	{
		for (int i = 0; i <= FileCount / 2; i++)
		{
			int num = i;
			int num2 = FileCount - 1 - i;
			for (int j = 0; j < RankCount; j++)
			{
				int num3 = j;
				int num4 = RankCount - 1 - j;
				IFormationUnit formationUnit = _units2D[num, num3];
				IFormationUnit formationUnit2 = _units2D[num2, num4];
				if (formationUnit == formationUnit2)
				{
					continue;
				}
				if (formationUnit != null && formationUnit2 != null)
				{
					SwitchUnitLocations(formationUnit, formationUnit2);
				}
				else if (formationUnit != null)
				{
					if (IsUnitPositionAvailable(num2, num4))
					{
						RelocateUnit(formationUnit, num2, num4);
					}
				}
				else if (formationUnit2 != null && IsUnitPositionAvailable(num, num3))
				{
					RelocateUnit(formationUnit2, num, num3);
				}
			}
		}
	}

	public float GetOccupationWidth(int unitCount)
	{
		if (unitCount < 1)
		{
			return 0f;
		}
		int num = FileCount - 1;
		int num2 = 0;
		for (int i = 0; i < FileCount * RankCount; i++)
		{
			Vec2i orderedUnitPositionIndex = GetOrderedUnitPositionIndex(i);
			if (orderedUnitPositionIndex.Item1 < num)
			{
				num = orderedUnitPositionIndex.Item1;
			}
			if (orderedUnitPositionIndex.Item1 > num2)
			{
				num2 = orderedUnitPositionIndex.Item1;
			}
			if (IsUnitPositionAvailable(orderedUnitPositionIndex.Item1, orderedUnitPositionIndex.Item2))
			{
				unitCount--;
				if (unitCount == 0)
				{
					break;
				}
			}
		}
		return (float)(num2 - num) * (Interval + UnitDiameter) + UnitDiameter;
	}

	public void InvalidateCacheOfUnitAux(Vec2 roundedLocalPosition)
	{
		if (TryGetUnitPositionIndexFromLocalPosition(roundedLocalPosition, out var fileIndex, out var rankIndex))
		{
			UnitPositionAvailabilities[fileIndex, rankIndex] = 0;
		}
	}

	public Vec2? CreateNewPosition(int unitIndex)
	{
		Vec2? result = null;
		int num = 100;
		while (!result.HasValue && num > 0 && !AreLastRanksCompletelyUnavailable() && IsDeepenApplicable())
		{
			Deepen();
			result = GetLocalPositionOfUnitOrDefault(unitIndex);
			num--;
		}
		return result;
	}

	public virtual void RearrangeFrom(IFormationArrangement arrangement)
	{
		BatchUnitPositionAvailabilities();
	}

	public virtual void RearrangeTo(IFormationArrangement arrangement)
	{
		if (arrangement is ColumnFormation)
		{
			IsTransforming = true;
			ReleaseMiddleFrontUnitPosition();
		}
	}

	public virtual void RearrangeTransferUnits(IFormationArrangement arrangement)
	{
		if (arrangement is LineFormation lineFormation && !(arrangement is TransposedLineFormation))
		{
			lineFormation._units2D = _units2D;
			lineFormation._allUnits = _allUnits;
			lineFormation.UnitPositionAvailabilities = UnitPositionAvailabilities;
			lineFormation._globalPositions = _globalPositions;
			lineFormation._unpositionedUnits = _unpositionedUnits;
			lineFormation.AreLocalPositionsDirty = true;
			lineFormation.OnFormationFrameChanged();
			return;
		}
		for (int i = 0; i < FileCount * RankCount; i++)
		{
			Vec2i orderedUnitPositionIndex = GetOrderedUnitPositionIndex(i);
			int item = orderedUnitPositionIndex.Item1;
			int item2 = orderedUnitPositionIndex.Item2;
			IFormationUnit formationUnit = _units2D[item, item2];
			if (formationUnit != null)
			{
				formationUnit.FormationFileIndex = -1;
				formationUnit.FormationRankIndex = -1;
				arrangement.AddUnit(formationUnit);
			}
		}
		foreach (IFormationUnit unpositionedUnit in _unpositionedUnits)
		{
			arrangement.AddUnit(unpositionedUnit);
		}
	}

	public static float CalculateWidth(float interval, float unitDiameter, int unitCountOnLine)
	{
		return (float)TaleWorlds.Library.MathF.Max(0, unitCountOnLine - 1) * (interval + unitDiameter) + unitDiameter;
	}

	public void FormFromFlankWidth(int unitCountOnLine, bool skipSingleFileChangesForPerformance = false)
	{
		if (!skipSingleFileChangesForPerformance || TaleWorlds.Library.MathF.Abs(FileCount - unitCountOnLine) > 1)
		{
			FlankWidth = CalculateWidth(Interval, UnitDiameter, unitCountOnLine);
		}
	}

	public void ReserveMiddleFrontUnitPosition(IFormationUnit vanguard)
	{
		_isMiddleFrontUnitPositionReserved = true;
		OnFormationFrameChanged();
	}

	public void ReleaseMiddleFrontUnitPosition()
	{
		_isMiddleFrontUnitPositionReserved = false;
		OnFormationFrameChanged();
	}

	private Vec2i GetMiddleFrontUnitPosition()
	{
		return GetOrderedUnitPositionIndex(0);
	}

	public Vec2 GetLocalPositionOfReservedUnitPosition()
	{
		Vec2i middleFrontUnitPosition = GetMiddleFrontUnitPosition();
		return GetLocalPositionOfUnit(middleFrontUnitPosition.Item1, middleFrontUnitPosition.Item2);
	}

	private void SwitchFrontUnitTypesToFrontRows()
	{
		if (Interval <= 0f)
		{
			return;
		}
		for (int i = 1; i < RankCount; i++)
		{
			for (int j = 0; j < FileCount; j++)
			{
				Agent agent = _units2D[j, i] as Agent;
				Agent agent2 = _units2D[j, i - 1] as Agent;
				if (agent != null && agent2 != null && _isFrontUnitDelegate(agent) && !_isFrontUnitDelegate(agent2))
				{
					SwitchUnitLocations(agent, agent2);
				}
			}
		}
		for (int num = RankCount - 1; num > 0; num--)
		{
			for (int k = 0; k < FileCount; k++)
			{
				if (!(_units2D[k, num] is Agent agent3) || !_isFrontUnitDelegate(agent3))
				{
					continue;
				}
				bool flag = false;
				int index = num - 1;
				for (int l = 0; l < FileCount; l++)
				{
					if (_units2D[l, index] is Agent agent4 && !_isFrontUnitDelegate(agent4))
					{
						SwitchUnitLocations(agent3, agent4);
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return;
				}
			}
		}
	}

	public void OnTickOccasionally()
	{
		SwitchFrontUnitTypesToFrontRows();
		UpdateFrontUnitTypeDelegate();
	}

	protected virtual void UpdateFrontUnitTypeDelegate()
	{
		if ((owner as Formation).QuerySystem.IsUnderCavalryChargeFromFront)
		{
			_isFrontUnitDelegate = PreferBracerUnitsOnFront;
		}
		else
		{
			_isFrontUnitDelegate = PreferShieldedUnitsOnFront;
		}
	}

	public virtual float GetDirectionChangeTendencyOfUnit(IFormationUnit unit)
	{
		if (RankCount == 1 || unit.FormationRankIndex == -1)
		{
			return 0f;
		}
		return (float)unit.FormationRankIndex * 1f / (float)(RankCount - 1);
	}

	public int GetCachedOrderedAndAvailableUnitPositionIndicesCount()
	{
		return _cachedOrderedAndAvailableUnitPositionIndices.Count;
	}

	public Vec2i GetCachedOrderedAndAvailableUnitPositionIndexAt(int i)
	{
		return _cachedOrderedAndAvailableUnitPositionIndices[i];
	}

	public WorldPosition GetGlobalPositionAtIndex(int indexX, int indexY)
	{
		return _globalPositions[indexX, indexY];
	}

	protected bool PreferShieldedUnitsOnFront(Agent agent)
	{
		return agent.HasShieldCached;
	}

	protected bool PreferBracerUnitsOnFront(Agent agent)
	{
		return agent.CanPerformBraceCached;
	}

	void IFormationArrangement.GetAllUnits(in MBList<IFormationUnit> allUnitsListToBeFilledIn)
	{
		GetAllUnits(in allUnitsListToBeFilledIn);
	}
}
