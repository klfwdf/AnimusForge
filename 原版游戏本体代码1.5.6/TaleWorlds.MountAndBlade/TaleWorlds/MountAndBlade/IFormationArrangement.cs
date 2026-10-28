using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface IFormationArrangement
{
	float Width { get; set; }

	float Depth { get; }

	float FlankWidth { get; set; }

	float RankDepth { get; }

	float MinimumWidth { get; }

	float MaximumWidth { get; }

	float MinimumFlankWidth { get; }

	bool? IsLoose { get; }

	float IntervalMultiplier { get; }

	float DistanceMultiplier { get; }

	int UnitCount { get; }

	int RankCount { get; }

	int PositionedUnitCount { get; }

	bool AreLocalPositionsDirty { set; }

	event Action OnWidthChanged;

	event Action OnShapeChanged;

	IFormationUnit GetPlayerUnit();

	MBReadOnlyList<IFormationUnit> GetAllUnits();

	void GetAllUnits(in MBList<IFormationUnit> allUnitsListToBeFilledIn);

	MBList<IFormationUnit> GetUnpositionedUnits();

	bool AddUnit(IFormationUnit unit);

	void RemoveUnit(IFormationUnit unit);

	IFormationUnit GetUnit(int fileIndex, int rankIndex);

	void OnBatchRemoveStart();

	void OnBatchRemoveEnd();

	Vec2? GetLocalPositionOfUnitOrDefault(int unitIndex);

	Vec2? GetLocalPositionOfUnitOrDefault(IFormationUnit unit);

	Vec2? GetLocalPositionOfUnitOrDefaultWithAdjustment(IFormationUnit unit, float distanceBetweenAgentsAdjustment);

	Vec2? GetLocalDirectionOfUnitOrDefault(int unitIndex);

	Vec2? GetLocalDirectionOfUnitOrDefault(IFormationUnit unit);

	WorldPosition? GetWorldPositionOfUnitOrDefault(int unitIndex);

	WorldPosition? GetWorldPositionOfUnitOrDefault(IFormationUnit unit);

	List<IFormationUnit> GetUnitsToPop(int count);

	List<IFormationUnit> GetUnitsToPop(int count, Vec3 targetPosition);

	IEnumerable<IFormationUnit> GetUnitsToPopWithCondition(int count, Func<IFormationUnit, bool> conditionFunction);

	void SwitchUnitLocations(IFormationUnit firstUnit, IFormationUnit secondUnit);

	void SwitchUnitLocationsWithUnpositionedUnit(IFormationUnit firstUnit, IFormationUnit secondUnit);

	void SwitchUnitLocationsWithBackMostUnit(IFormationUnit unit);

	IFormationUnit GetNeighborUnitOfLeftSide(IFormationUnit unit);

	IFormationUnit GetNeighborUnitOfRightSide(IFormationUnit unit);

	Vec2? GetLocalWallDirectionOfRelativeFormationLocation(IFormationUnit unit);

	IEnumerable<Vec2> GetUnavailableUnitPositions();

	float GetOccupationWidth(int unitCount);

	Vec2? CreateNewPosition(int unitIndex);

	void BeforeFormationFrameChange();

	void OnFormationFrameChanged(bool updateCachedOrderedLocalPositions = false);

	bool IsTurnBackwardsNecessary(Vec2 previousPosition, WorldPosition? newPosition, Vec2 previousDirection, bool hasNewDirection, Vec2? newDirection);

	void TurnBackwards();

	void OnFormationDispersed();

	void Reset();

	IFormationArrangement Clone(IFormation formation);

	void DeepCopyFrom(IFormationArrangement arrangement);

	void RearrangeTo(IFormationArrangement arrangement);

	void RearrangeFrom(IFormationArrangement arrangement);

	void RearrangeTransferUnits(IFormationArrangement arrangement);

	void ReserveMiddleFrontUnitPosition(IFormationUnit vanguard);

	void ReleaseMiddleFrontUnitPosition();

	Vec2 GetLocalPositionOfReservedUnitPosition();

	void OnUnitLostMount(IFormationUnit unit);

	float GetDirectionChangeTendencyOfUnit(IFormationUnit unit);

	void UpdateLocalPositionErrors(bool recalculateErrors = true);

	void OnTickOccasionally();
}
