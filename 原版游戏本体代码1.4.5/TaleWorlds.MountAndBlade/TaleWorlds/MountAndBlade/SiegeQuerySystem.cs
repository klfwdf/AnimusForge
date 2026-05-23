using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SiegeQuerySystem
{
	private enum RegionEnum
	{
		Left,
		LeftClose,
		Middle,
		MiddleClose,
		Right,
		RightClose,
		Inside
	}

	private const float LaneProximityDistance = 15f;

	private readonly Team _attackerTeam;

	public Vec2 DefenderLeftToDefenderMidDir;

	public Vec2 DefenderMidToDefenderRightDir;

	private readonly QueryData<int> _leftRegionMemberCount;

	private readonly QueryData<int> _leftCloseAttackerCount;

	private readonly QueryData<int> _middleRegionMemberCount;

	private readonly QueryData<int> _middleCloseAttackerCount;

	private readonly QueryData<int> _rightRegionMemberCount;

	private readonly QueryData<int> _rightCloseAttackerCount;

	private readonly QueryData<int> _insideAttackerCount;

	private readonly QueryData<int> _leftDefenderCount;

	private readonly QueryData<int> _middleDefenderCount;

	private readonly QueryData<int> _rightDefenderCount;

	public int LeftRegionMemberCount => _leftRegionMemberCount.Value;

	public int LeftCloseAttackerCount => _leftCloseAttackerCount.Value;

	public int MiddleRegionMemberCount => _middleRegionMemberCount.Value;

	public int MiddleCloseAttackerCount => _middleCloseAttackerCount.Value;

	public int RightRegionMemberCount => _rightRegionMemberCount.Value;

	public int RightCloseAttackerCount => _rightCloseAttackerCount.Value;

	public int InsideAttackerCount => _insideAttackerCount.Value;

	public int LeftDefenderCount => _leftDefenderCount.Value;

	public int MiddleDefenderCount => _middleDefenderCount.Value;

	public int RightDefenderCount => _rightDefenderCount.Value;

	public Vec3 LeftDefenderOrigin { get; }

	public Vec3 MidDefenderOrigin { get; }

	public Vec3 RightDefenderOrigin { get; }

	public Vec3 LeftAttackerOrigin { get; }

	public Vec3 MiddleAttackerOrigin { get; }

	public Vec3 RightAttackerOrigin { get; }

	public Vec2 LeftToMidDir { get; }

	public Vec2 MidToLeftDir { get; }

	public Vec2 MidToRightDir { get; }

	public Vec2 RightToMidDir { get; }

	public SiegeQuerySystem(Team team, IEnumerable<SiegeLane> lanes)
	{
		Mission mission = Mission.Current;
		_attackerTeam = mission.AttackerTeam;
		Team defenderTeam = mission.DefenderTeam;
		SiegeLane siegeLane = lanes.FirstOrDefault((SiegeLane l) => l.LaneSide == FormationAI.BehaviorSide.Left);
		SiegeLane siegeLane2 = lanes.FirstOrDefault((SiegeLane l) => l.LaneSide == FormationAI.BehaviorSide.Middle);
		lanes.FirstOrDefault((SiegeLane l) => l.LaneSide == FormationAI.BehaviorSide.Right);
		Mission current = Mission.Current;
		WeakGameEntity weakGameEntity = current.Scene.FindWeakEntityWithTag("left_defender_origin");
		if (weakGameEntity.IsValid)
		{
			LeftDefenderOrigin = weakGameEntity.GlobalPosition;
		}
		else
		{
			LeftDefenderOrigin = (siegeLane.DefenderOrigin.AsVec2.IsNonZero() ? siegeLane.DefenderOrigin.GetGroundVec3() : Vec3.Zero);
		}
		WeakGameEntity weakGameEntity2 = current.Scene.FindWeakEntityWithTag("left_attacker_origin");
		if (weakGameEntity2.IsValid)
		{
			LeftAttackerOrigin = weakGameEntity2.GlobalPosition;
		}
		else
		{
			LeftAttackerOrigin = (siegeLane.AttackerOrigin.AsVec2.IsNonZero() ? siegeLane.AttackerOrigin.GetGroundVec3() : Vec3.Zero);
		}
		WeakGameEntity weakGameEntity3 = current.Scene.FindWeakEntityWithTag("middle_defender_origin");
		if (weakGameEntity3.IsValid)
		{
			MidDefenderOrigin = weakGameEntity3.GlobalPosition;
		}
		else
		{
			MidDefenderOrigin = (siegeLane2.DefenderOrigin.AsVec2.IsNonZero() ? siegeLane2.DefenderOrigin.GetGroundVec3() : Vec3.Zero);
		}
		WeakGameEntity weakGameEntity4 = current.Scene.FindWeakEntityWithTag("middle_attacker_origin");
		if (weakGameEntity4.IsValid)
		{
			MiddleAttackerOrigin = weakGameEntity4.GlobalPosition;
		}
		else
		{
			MiddleAttackerOrigin = (siegeLane2.AttackerOrigin.AsVec2.IsNonZero() ? siegeLane2.AttackerOrigin.GetGroundVec3() : Vec3.Zero);
		}
		WeakGameEntity weakGameEntity5 = current.Scene.FindWeakEntityWithTag("right_defender_origin");
		if (weakGameEntity5.IsValid)
		{
			RightDefenderOrigin = weakGameEntity5.GlobalPosition;
		}
		else
		{
			RightDefenderOrigin = (siegeLane2.DefenderOrigin.AsVec2.IsNonZero() ? siegeLane2.DefenderOrigin.GetGroundVec3() : Vec3.Zero);
		}
		WeakGameEntity weakGameEntity6 = current.Scene.FindWeakEntityWithTag("right_attacker_origin");
		if (weakGameEntity6.IsValid)
		{
			RightAttackerOrigin = weakGameEntity6.GlobalPosition;
		}
		else
		{
			RightAttackerOrigin = (siegeLane2.AttackerOrigin.AsVec2.IsNonZero() ? siegeLane2.AttackerOrigin.GetGroundVec3() : Vec3.Zero);
		}
		LeftToMidDir = (MiddleAttackerOrigin.AsVec2 - LeftDefenderOrigin.AsVec2).Normalized();
		MidToLeftDir = (LeftAttackerOrigin.AsVec2 - MidDefenderOrigin.AsVec2).Normalized();
		MidToRightDir = (RightAttackerOrigin.AsVec2 - MidDefenderOrigin.AsVec2).Normalized();
		RightToMidDir = (MiddleAttackerOrigin.AsVec2 - RightDefenderOrigin.AsVec2).Normalized();
		_leftRegionMemberCount = new QueryData<int>(() => LocateAttackers(RegionEnum.Left), 5f);
		_leftCloseAttackerCount = new QueryData<int>(() => LocateAttackers(RegionEnum.LeftClose), 5f);
		_middleRegionMemberCount = new QueryData<int>(() => LocateAttackers(RegionEnum.Middle), 5f);
		_middleCloseAttackerCount = new QueryData<int>(() => LocateAttackers(RegionEnum.MiddleClose), 5f);
		_rightRegionMemberCount = new QueryData<int>(() => LocateAttackers(RegionEnum.Right), 5f);
		_rightCloseAttackerCount = new QueryData<int>(() => LocateAttackers(RegionEnum.RightClose), 5f);
		_insideAttackerCount = new QueryData<int>(() => LocateAttackers(RegionEnum.Inside), 5f);
		_leftDefenderCount = new QueryData<int>(() => mission.GetNearbyAllyAgentsCount(LeftDefenderOrigin.AsVec2, 10f, defenderTeam), 5f);
		_middleDefenderCount = new QueryData<int>(() => mission.GetNearbyAllyAgentsCount(MidDefenderOrigin.AsVec2, 10f, defenderTeam), 5f);
		_rightDefenderCount = new QueryData<int>(() => mission.GetNearbyAllyAgentsCount(RightDefenderOrigin.AsVec2, 10f, defenderTeam), 5f);
		DefenderLeftToDefenderMidDir = (MidDefenderOrigin.AsVec2 - LeftDefenderOrigin.AsVec2).Normalized();
		DefenderMidToDefenderRightDir = (RightDefenderOrigin.AsVec2 - MidDefenderOrigin.AsVec2).Normalized();
		InitializeTelemetryScopeNames();
	}

	private int LocateAttackers(RegionEnum region)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		foreach (Agent activeAgent in _attackerTeam.ActiveAgents)
		{
			Vec2 vec = activeAgent.Position.AsVec2 - LeftDefenderOrigin.AsVec2;
			Vec2 vec2 = activeAgent.Position.AsVec2 - MidDefenderOrigin.AsVec2;
			Vec2 vec3 = activeAgent.Position.AsVec2 - RightDefenderOrigin.AsVec2;
			if (vec.Normalize() < 15f && Math.Abs(activeAgent.Position.z - LeftDefenderOrigin.z) <= 3f)
			{
				num2++;
				num++;
				continue;
			}
			if (vec.DotProduct(LeftToMidDir) >= 0f && vec.DotProduct(LeftToMidDir.RightVec()) >= 0f)
			{
				num++;
			}
			else if (vec2.DotProduct(MidToLeftDir) >= 0f && vec2.DotProduct(MidToLeftDir.RightVec()) >= 0f)
			{
				num++;
			}
			if (vec3.Normalize() < 15f && Math.Abs(activeAgent.Position.z - RightDefenderOrigin.z) <= 3f)
			{
				num6++;
				num5++;
				continue;
			}
			if (vec3.DotProduct(RightToMidDir) >= 0f && vec3.DotProduct(RightToMidDir.LeftVec()) >= 0f)
			{
				num5++;
			}
			else if (vec2.DotProduct(MidToRightDir) >= 0f && vec2.DotProduct(MidToRightDir.LeftVec()) >= 0f)
			{
				num5++;
			}
			if (vec2.Normalize() < 15f && Math.Abs(activeAgent.Position.z - MidDefenderOrigin.z) <= 3f)
			{
				num4++;
				num3++;
				continue;
			}
			if ((vec2.DotProduct(MidToLeftDir) < 0f || vec2.DotProduct(MidToLeftDir.RightVec()) < 0f || vec.DotProduct(LeftToMidDir) < 0f || vec.DotProduct(LeftToMidDir.RightVec()) < 0f) && (vec2.DotProduct(MidToRightDir) < 0f || vec2.DotProduct(MidToRightDir.LeftVec()) < 0f || vec3.DotProduct(RightToMidDir) < 0f || vec3.DotProduct(RightToMidDir.LeftVec()) < 0f))
			{
				num3++;
			}
			if (activeAgent.GetCurrentNavigationFaceId() % 10 == 1)
			{
				num7++;
			}
		}
		float currentTime = Mission.Current.CurrentTime;
		_leftRegionMemberCount.SetValue(num, currentTime);
		_leftCloseAttackerCount.SetValue(num2, currentTime);
		_middleRegionMemberCount.SetValue(num3, currentTime);
		_middleCloseAttackerCount.SetValue(num4, currentTime);
		_rightRegionMemberCount.SetValue(num5, currentTime);
		_rightCloseAttackerCount.SetValue(num6, currentTime);
		_insideAttackerCount.SetValue(num7, currentTime);
		return region switch
		{
			RegionEnum.Left => num, 
			RegionEnum.LeftClose => num2, 
			RegionEnum.Middle => num3, 
			RegionEnum.MiddleClose => num4, 
			RegionEnum.Right => num5, 
			RegionEnum.RightClose => num6, 
			RegionEnum.Inside => num7, 
			_ => 0, 
		};
	}

	public void Expire()
	{
		_leftRegionMemberCount.Expire();
		_leftCloseAttackerCount.Expire();
		_middleRegionMemberCount.Expire();
		_middleCloseAttackerCount.Expire();
		_rightRegionMemberCount.Expire();
		_rightCloseAttackerCount.Expire();
		_insideAttackerCount.Expire();
		_leftDefenderCount.Expire();
		_middleDefenderCount.Expire();
		_rightDefenderCount.Expire();
	}

	private void InitializeTelemetryScopeNames()
	{
	}

	public int DeterminePositionAssociatedSide(Vec3 position)
	{
		float num = position.AsVec2.DistanceSquared(LeftDefenderOrigin.AsVec2);
		float num2 = position.AsVec2.DistanceSquared(MidDefenderOrigin.AsVec2);
		float num3 = position.AsVec2.DistanceSquared(RightDefenderOrigin.AsVec2);
		FormationAI.BehaviorSide behaviorSide = ((!(num < num2) || !(num < num3)) ? ((!(num3 < num2)) ? FormationAI.BehaviorSide.Middle : FormationAI.BehaviorSide.Right) : FormationAI.BehaviorSide.Left);
		FormationAI.BehaviorSide behaviorSide2 = FormationAI.BehaviorSide.BehaviorSideNotSet;
		switch (behaviorSide)
		{
		case FormationAI.BehaviorSide.Left:
			if ((position.AsVec2 - LeftDefenderOrigin.AsVec2).Normalized().DotProduct(DefenderLeftToDefenderMidDir) > 0f)
			{
				behaviorSide2 = FormationAI.BehaviorSide.Middle;
			}
			break;
		case FormationAI.BehaviorSide.Middle:
			behaviorSide2 = (((position.AsVec2 - MidDefenderOrigin.AsVec2).Normalized().DotProduct(DefenderMidToDefenderRightDir) > 0f) ? FormationAI.BehaviorSide.Right : FormationAI.BehaviorSide.Left);
			break;
		case FormationAI.BehaviorSide.Right:
			if ((position.AsVec2 - RightDefenderOrigin.AsVec2).Normalized().DotProduct(DefenderMidToDefenderRightDir) < 0f)
			{
				behaviorSide2 = FormationAI.BehaviorSide.Middle;
			}
			break;
		}
		int num4 = 1 << (int)behaviorSide;
		if (behaviorSide2 != FormationAI.BehaviorSide.BehaviorSideNotSet)
		{
			num4 |= 1 << (int)behaviorSide2;
		}
		return num4;
	}

	public static bool AreSidesRelated(FormationAI.BehaviorSide side, int connectedSides)
	{
		return ((1 << (int)side) & connectedSides) != 0;
	}

	public static int SideDistance(int connectedSides, int side)
	{
		while (connectedSides != 0 && side != 0)
		{
			connectedSides >>= 1;
			side >>= 1;
		}
		int num = ((connectedSides != 0) ? connectedSides : side);
		int num2 = 0;
		while (num > 0)
		{
			num2++;
			if ((num & 1) == 1)
			{
				break;
			}
			num >>= 1;
		}
		return num2;
	}
}
