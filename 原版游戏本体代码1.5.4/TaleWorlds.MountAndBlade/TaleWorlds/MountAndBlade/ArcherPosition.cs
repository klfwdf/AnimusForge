using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class ArcherPosition
{
	private FormationAI.BehaviorSide _closestSide;

	private int _connectedSides;

	private SiegeQuerySystem _siegeQuerySystem;

	private readonly Formation[] _lastAssignedFormations;

	public GameEntity Entity { get; }

	public TacticalPosition TacticalArcherPosition { get; }

	public int ConnectedSides
	{
		get
		{
			return _connectedSides;
		}
		private set
		{
			_connectedSides = value;
		}
	}

	public Formation GetLastAssignedFormation(int teamIndex)
	{
		if (teamIndex >= 0)
		{
			return _lastAssignedFormations[teamIndex];
		}
		return null;
	}

	public ArcherPosition(GameEntity _entity, SiegeQuerySystem siegeQuerySystem, BattleSideEnum battleSide)
	{
		Entity = _entity;
		TacticalArcherPosition = Entity.GetFirstScriptOfType<TacticalPosition>();
		_siegeQuerySystem = siegeQuerySystem;
		DetermineArcherPositionSide(battleSide);
		_lastAssignedFormations = new Formation[Mission.Current.Teams.Count];
	}

	private static int ConvertToBinaryPow(int pow)
	{
		return 1 << pow;
	}

	public bool IsArcherPositionRelatedToSide(FormationAI.BehaviorSide side)
	{
		return (ConvertToBinaryPow((int)side) & ConnectedSides) != 0;
	}

	public FormationAI.BehaviorSide GetArcherPositionClosestSide()
	{
		return _closestSide;
	}

	public void OnDeploymentFinished(SiegeQuerySystem siegeQuerySystem, BattleSideEnum battleSide)
	{
		_siegeQuerySystem = siegeQuerySystem;
		DetermineArcherPositionSide(battleSide);
	}

	private void DetermineArcherPositionSide(BattleSideEnum battleSide)
	{
		ConnectedSides = 0;
		if (TacticalArcherPosition != null)
		{
			int tacticalPositionSide = (int)TacticalArcherPosition.TacticalPositionSide;
			if (tacticalPositionSide < 3)
			{
				_closestSide = TacticalArcherPosition.TacticalPositionSide;
				ConnectedSides = ConvertToBinaryPow(tacticalPositionSide);
			}
		}
		if (ConnectedSides == 0)
		{
			if (battleSide == BattleSideEnum.Defender)
			{
				CalculateArcherPositionSideUsingDefenderLanes(_siegeQuerySystem, Entity.GlobalPosition, out _closestSide, out _connectedSides);
			}
			else
			{
				CalculateArcherPositionSideUsingAttackerRegions(_siegeQuerySystem, Entity.GlobalPosition, out _closestSide, out _connectedSides);
			}
		}
	}

	private static void CalculateArcherPositionSideUsingAttackerRegions(SiegeQuerySystem siegeQuerySystem, Vec3 position, out FormationAI.BehaviorSide _closestSide, out int ConnectedSides)
	{
		float num = position.DistanceSquared(siegeQuerySystem.LeftAttackerOrigin);
		float num2 = position.DistanceSquared(siegeQuerySystem.MiddleAttackerOrigin);
		float num3 = position.DistanceSquared(siegeQuerySystem.RightAttackerOrigin);
		FormationAI.BehaviorSide behaviorSide = FormationAI.BehaviorSide.BehaviorSideNotSet;
		ConnectedSides = ConvertToBinaryPow((int)(_closestSide = ((!(num < num2) || !(num < num3)) ? ((!(num3 < num2)) ? FormationAI.BehaviorSide.Middle : FormationAI.BehaviorSide.Right) : FormationAI.BehaviorSide.Left)));
		Vec2 vec = position.AsVec2 - siegeQuerySystem.LeftDefenderOrigin.AsVec2;
		if (vec.DotProduct(siegeQuerySystem.LeftToMidDir) >= 0f && vec.DotProduct(siegeQuerySystem.LeftToMidDir.RightVec()) >= 0f)
		{
			ConnectedSides |= ConvertToBinaryPow(0);
		}
		else
		{
			vec = position.AsVec2 - siegeQuerySystem.MidDefenderOrigin.AsVec2;
			if (vec.DotProduct(siegeQuerySystem.MidToLeftDir) >= 0f && vec.DotProduct(siegeQuerySystem.MidToLeftDir.RightVec()) >= 0f)
			{
				ConnectedSides |= ConvertToBinaryPow(0);
			}
		}
		vec = position.AsVec2 - siegeQuerySystem.MidDefenderOrigin.AsVec2;
		if (vec.DotProduct(siegeQuerySystem.LeftToMidDir) >= 0f && vec.DotProduct(siegeQuerySystem.LeftToMidDir.LeftVec()) >= 0f)
		{
			ConnectedSides |= ConvertToBinaryPow(1);
		}
		else
		{
			vec = position.AsVec2 - siegeQuerySystem.RightDefenderOrigin.AsVec2;
			if (vec.DotProduct(siegeQuerySystem.RightToMidDir) >= 0f && vec.DotProduct(siegeQuerySystem.RightToMidDir.RightVec()) >= 0f)
			{
				ConnectedSides |= ConvertToBinaryPow(1);
			}
		}
		vec = position.AsVec2 - siegeQuerySystem.RightDefenderOrigin.AsVec2;
		if (vec.DotProduct(siegeQuerySystem.MidToRightDir) >= 0f && vec.DotProduct(siegeQuerySystem.MidToRightDir.LeftVec()) >= 0f)
		{
			ConnectedSides |= ConvertToBinaryPow(2);
			return;
		}
		vec = position.AsVec2 - siegeQuerySystem.RightDefenderOrigin.AsVec2;
		if (vec.DotProduct(siegeQuerySystem.RightToMidDir) >= 0f && vec.DotProduct(siegeQuerySystem.RightToMidDir.LeftVec()) >= 0f)
		{
			ConnectedSides |= ConvertToBinaryPow(2);
		}
	}

	private static void CalculateArcherPositionSideUsingDefenderLanes(SiegeQuerySystem siegeQuerySystem, Vec3 position, out FormationAI.BehaviorSide _closestSide, out int ConnectedSides)
	{
		float num = position.DistanceSquared(siegeQuerySystem.LeftDefenderOrigin);
		float num2 = position.DistanceSquared(siegeQuerySystem.MidDefenderOrigin);
		float num3 = position.DistanceSquared(siegeQuerySystem.RightDefenderOrigin);
		FormationAI.BehaviorSide behaviorSide = FormationAI.BehaviorSide.BehaviorSideNotSet;
		behaviorSide = ((!(num < num2) || !(num < num3)) ? ((!(num3 < num2)) ? FormationAI.BehaviorSide.Middle : FormationAI.BehaviorSide.Right) : FormationAI.BehaviorSide.Left);
		FormationAI.BehaviorSide behaviorSide2 = FormationAI.BehaviorSide.BehaviorSideNotSet;
		switch (behaviorSide)
		{
		case FormationAI.BehaviorSide.Left:
			if ((position.AsVec2 - siegeQuerySystem.LeftDefenderOrigin.AsVec2).Normalized().DotProduct(siegeQuerySystem.DefenderLeftToDefenderMidDir) > 0f)
			{
				behaviorSide2 = FormationAI.BehaviorSide.Middle;
			}
			break;
		case FormationAI.BehaviorSide.Middle:
			behaviorSide2 = (((position.AsVec2 - siegeQuerySystem.MidDefenderOrigin.AsVec2).Normalized().DotProduct(siegeQuerySystem.DefenderMidToDefenderRightDir) > 0f) ? FormationAI.BehaviorSide.Right : FormationAI.BehaviorSide.Left);
			break;
		case FormationAI.BehaviorSide.Right:
			if ((position.AsVec2 - siegeQuerySystem.RightDefenderOrigin.AsVec2).Normalized().DotProduct(siegeQuerySystem.DefenderMidToDefenderRightDir) < 0f)
			{
				behaviorSide2 = FormationAI.BehaviorSide.Middle;
			}
			break;
		}
		_closestSide = behaviorSide;
		ConnectedSides = ConvertToBinaryPow((int)behaviorSide);
		if (behaviorSide2 != FormationAI.BehaviorSide.BehaviorSideNotSet)
		{
			ConnectedSides |= ConvertToBinaryPow((int)behaviorSide2);
		}
	}

	public void SetLastAssignedFormation(int teamIndex, Formation formation)
	{
		if (teamIndex >= 0)
		{
			_lastAssignedFormations[teamIndex] = formation;
		}
	}
}
