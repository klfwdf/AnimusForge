using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace NavalDLC.DWA;

internal class DWAKdTree
{
	private const int MaxLeafSize = 10;

	private DWAAgent[] _agents;

	private DWAAgentTreeNode[] _agentTree;

	private DWAObstacleTreeNode _obstacleTree;

	private DWASimulator _simulator;

	internal DWAKdTree(DWASimulator simulator)
	{
		_simulator = simulator;
	}

	internal void BuildAgentTree()
	{
		if (_agents == null || _agents.Length != _simulator.NumAgents)
		{
			_agents = new DWAAgent[_simulator.NumAgents];
			int num = 0;
			for (int i = 0; i < _simulator.AgentsIncludingRemoved.Count; i++)
			{
				DWAAgent dWAAgent = _simulator.AgentsIncludingRemoved[i];
				if (dWAAgent != null)
				{
					_agents[num] = dWAAgent;
					num++;
				}
			}
			_agentTree = new DWAAgentTreeNode[2 * _agents.Length];
			for (int j = 0; j < _agentTree.Length; j++)
			{
				_agentTree[j] = default(DWAAgentTreeNode);
			}
		}
		if (_agents.Length != 0)
		{
			BuildAgentTreeRecursive(0, _agents.Length, 0);
		}
	}

	internal void BuildObstacleTree()
	{
		_obstacleTree = new DWAObstacleTreeNode();
		IList<DWAObstacleVertex> list = new List<DWAObstacleVertex>(_simulator.NumObstacles);
		for (int i = 0; i < _simulator.NumObstacles; i++)
		{
			list.Add(_simulator.Obstacles[i]);
		}
		_obstacleTree = BuildObstacleTreeRecursive(list);
	}

	internal void ComputeAgentNeighbors(DWAAgent agent, float rangeSq, ushort parity)
	{
		QueryAgentTreeRecursive(agent, ref rangeSq, 0, parity);
	}

	internal void ComputeObstacleNeighbors(DWAAgent agent, float rangeSq)
	{
		QueryObstacleTreeRecursive(agent, ref rangeSq, _obstacleTree);
	}

	internal bool QueryVisibility(in Vec2 point1, in Vec2 point2, float radius)
	{
		return QueryVisibilityRecursive(in point1, in point2, radius, _obstacleTree);
	}

	private void BuildAgentTreeRecursive(int begin, int end, int node)
	{
		_agentTree[node].Begin = begin;
		_agentTree[node].End = end;
		_agentTree[node].MinX = (_agentTree[node].MaxX = _agents[begin].State.Position.x);
		_agentTree[node].MinY = (_agentTree[node].MaxY = _agents[begin].State.Position.y);
		for (int i = begin + 1; i < end; i++)
		{
			_agentTree[node].MaxX = Math.Max(_agentTree[node].MaxX, _agents[i].State.Position.x);
			_agentTree[node].MinX = Math.Min(_agentTree[node].MinX, _agents[i].State.Position.x);
			_agentTree[node].MaxY = Math.Max(_agentTree[node].MaxY, _agents[i].State.Position.y);
			_agentTree[node].MinY = Math.Min(_agentTree[node].MinY, _agents[i].State.Position.y);
		}
		if (end - begin <= 10)
		{
			return;
		}
		bool flag = _agentTree[node].MaxX - _agentTree[node].MinX > _agentTree[node].MaxY - _agentTree[node].MinY;
		float num = 0.5f * (flag ? (_agentTree[node].MaxX + _agentTree[node].MinX) : (_agentTree[node].MaxY + _agentTree[node].MinY));
		int j = begin;
		int num2 = end;
		while (j < num2)
		{
			for (; j < num2 && (flag ? _agents[j].State.Position.x : _agents[j].State.Position.y) < num; j++)
			{
			}
			while (num2 > j && (flag ? _agents[num2 - 1].State.Position.x : _agents[num2 - 1].State.Position.y) >= num)
			{
				num2--;
			}
			if (j < num2)
			{
				DWAAgent dWAAgent = _agents[j];
				_agents[j] = _agents[num2 - 1];
				_agents[num2 - 1] = dWAAgent;
				j++;
				num2--;
			}
		}
		int num3 = j - begin;
		if (num3 == 0)
		{
			num3++;
			j++;
			num2++;
		}
		_agentTree[node].Left = node + 1;
		_agentTree[node].Right = node + 2 * num3;
		BuildAgentTreeRecursive(begin, j, _agentTree[node].Left);
		BuildAgentTreeRecursive(j, end, _agentTree[node].Right);
	}

	private DWAObstacleTreeNode BuildObstacleTreeRecursive(IList<DWAObstacleVertex> obstacles)
	{
		if (obstacles.Count == 0)
		{
			return null;
		}
		DWAObstacleTreeNode dWAObstacleTreeNode = new DWAObstacleTreeNode();
		int num = 0;
		int num2 = obstacles.Count;
		int num3 = obstacles.Count;
		for (int i = 0; i < obstacles.Count; i++)
		{
			int num4 = 0;
			int num5 = 0;
			DWAObstacleVertex dWAObstacleVertex = obstacles[i];
			DWAObstacleVertex next = dWAObstacleVertex.Next;
			for (int j = 0; j < obstacles.Count; j++)
			{
				if (i != j)
				{
					DWAObstacleVertex dWAObstacleVertex2 = obstacles[j];
					DWAObstacleVertex next2 = dWAObstacleVertex2.Next;
					float signedDistanceOfPointToLineSegment = MBMath.GetSignedDistanceOfPointToLineSegment(dWAObstacleVertex.Point, next.Point, dWAObstacleVertex2.Point);
					float signedDistanceOfPointToLineSegment2 = MBMath.GetSignedDistanceOfPointToLineSegment(dWAObstacleVertex.Point, next.Point, next2.Point);
					if (signedDistanceOfPointToLineSegment >= -1E-05f && signedDistanceOfPointToLineSegment2 >= -1E-05f)
					{
						num4++;
					}
					else if (signedDistanceOfPointToLineSegment <= 1E-05f && signedDistanceOfPointToLineSegment2 <= 1E-05f)
					{
						num5++;
					}
					else
					{
						num4++;
						num5++;
					}
					if (new DWAFloatPair(Math.Max(num4, num5), Math.Min(num4, num5)) >= new DWAFloatPair(Math.Max(num2, num3), Math.Min(num2, num3)))
					{
						break;
					}
				}
			}
			if (new DWAFloatPair(Math.Max(num4, num5), Math.Min(num4, num5)) < new DWAFloatPair(Math.Max(num2, num3), Math.Min(num2, num3)))
			{
				num2 = num4;
				num3 = num5;
				num = i;
			}
		}
		IList<DWAObstacleVertex> list = new List<DWAObstacleVertex>(num2);
		for (int k = 0; k < num2; k++)
		{
			list.Add(null);
		}
		IList<DWAObstacleVertex> list2 = new List<DWAObstacleVertex>(num3);
		for (int l = 0; l < num3; l++)
		{
			list2.Add(null);
		}
		int num6 = 0;
		int num7 = 0;
		int num8 = num;
		DWAObstacleVertex dWAObstacleVertex3 = obstacles[num8];
		DWAObstacleVertex next3 = dWAObstacleVertex3.Next;
		for (int m = 0; m < obstacles.Count; m++)
		{
			if (num8 == m)
			{
				continue;
			}
			DWAObstacleVertex dWAObstacleVertex4 = obstacles[m];
			DWAObstacleVertex next4 = dWAObstacleVertex4.Next;
			float signedDistanceOfPointToLineSegment3 = MBMath.GetSignedDistanceOfPointToLineSegment(dWAObstacleVertex3.Point, next3.Point, dWAObstacleVertex4.Point);
			float signedDistanceOfPointToLineSegment4 = MBMath.GetSignedDistanceOfPointToLineSegment(dWAObstacleVertex3.Point, next3.Point, next4.Point);
			if (signedDistanceOfPointToLineSegment3 >= -1E-05f && signedDistanceOfPointToLineSegment4 >= -1E-05f)
			{
				list[num6++] = obstacles[m];
				continue;
			}
			if (signedDistanceOfPointToLineSegment3 <= 1E-05f && signedDistanceOfPointToLineSegment4 <= 1E-05f)
			{
				list2[num7++] = obstacles[m];
				continue;
			}
			float num9 = Vec2.Determinant(next3.Point - dWAObstacleVertex3.Point, dWAObstacleVertex4.Point - dWAObstacleVertex3.Point) / Vec2.Determinant(next3.Point - dWAObstacleVertex3.Point, dWAObstacleVertex4.Point - next4.Point);
			Vec2 point = dWAObstacleVertex4.Point + num9 * (next4.Point - dWAObstacleVertex4.Point);
			float pointZ = dWAObstacleVertex4.PointZ + num9 * (next4.PointZ - dWAObstacleVertex4.PointZ);
			DWAObstacleVertex dWAObstacleVertex5 = new DWAObstacleVertex(_simulator.NumObstacles);
			dWAObstacleVertex5.Point = point;
			dWAObstacleVertex5.PointZ = pointZ;
			dWAObstacleVertex5.Previous = dWAObstacleVertex4;
			dWAObstacleVertex5.Next = next4;
			dWAObstacleVertex5.IsConvex = true;
			dWAObstacleVertex5.Direction = dWAObstacleVertex4.Direction;
			_simulator.AddObstacleVertex(dWAObstacleVertex5);
			dWAObstacleVertex4.Next = dWAObstacleVertex5;
			next4.Previous = dWAObstacleVertex5;
			if (signedDistanceOfPointToLineSegment3 > 0f)
			{
				list[num6++] = dWAObstacleVertex4;
				list2[num7++] = dWAObstacleVertex5;
			}
			else
			{
				list2[num7++] = dWAObstacleVertex4;
				list[num6++] = dWAObstacleVertex5;
			}
		}
		dWAObstacleTreeNode.Obstacle = dWAObstacleVertex3;
		dWAObstacleTreeNode.Left = BuildObstacleTreeRecursive(list);
		dWAObstacleTreeNode.Right = BuildObstacleTreeRecursive(list2);
		return dWAObstacleTreeNode;
	}

	private void QueryAgentTreeRecursive(DWAAgent agent, ref float rangeSq, int node, ushort parity)
	{
		if (_agentTree[node].End - _agentTree[node].Begin <= 10)
		{
			for (int i = _agentTree[node].Begin; i < _agentTree[node].End; i++)
			{
				DWAAgent dWAAgent = _agents[i];
				if (agent.Id != dWAAgent.Id && agent.Delegate.IsAgentEligibleNeighbor(dWAAgent.Id, dWAAgent.Delegate))
				{
					agent.InsertAgentNeighbor(dWAAgent, ref rangeSq, parity);
				}
			}
			return;
		}
		Vec2 position = agent.State.Position;
		int left = _agentTree[node].Left;
		DWAAgentTreeNode dWAAgentTreeNode = _agentTree[left];
		float num = Math.Max(0f, dWAAgentTreeNode.MinX - position.x);
		float num2 = Math.Max(0f, position.x - dWAAgentTreeNode.MaxX);
		float num3 = Math.Max(0f, dWAAgentTreeNode.MinY - position.y);
		float num4 = Math.Max(0f, position.y - dWAAgentTreeNode.MaxY);
		float num5 = num * num + num2 * num2 + num3 * num3 + num4 * num4;
		int right = _agentTree[node].Right;
		DWAAgentTreeNode dWAAgentTreeNode2 = _agentTree[right];
		float num6 = Math.Max(0f, dWAAgentTreeNode2.MinX - position.x);
		float num7 = Math.Max(0f, position.x - dWAAgentTreeNode2.MaxX);
		float num8 = Math.Max(0f, dWAAgentTreeNode2.MinY - position.y);
		float num9 = Math.Max(0f, position.y - dWAAgentTreeNode2.MaxY);
		float num10 = num6 * num6 + num7 * num7 + num8 * num8 + num9 * num9;
		if (num5 < num10)
		{
			if (num5 < rangeSq)
			{
				QueryAgentTreeRecursive(agent, ref rangeSq, left, parity);
				if (num10 < rangeSq)
				{
					QueryAgentTreeRecursive(agent, ref rangeSq, right, parity);
				}
			}
		}
		else if (num10 < rangeSq)
		{
			QueryAgentTreeRecursive(agent, ref rangeSq, right, parity);
			if (num5 < rangeSq)
			{
				QueryAgentTreeRecursive(agent, ref rangeSq, left, parity);
			}
		}
	}

	private void QueryObstacleTreeRecursive(DWAAgent agent, ref float rangeSq, DWAObstacleTreeNode node)
	{
		if (node == null)
		{
			return;
		}
		DWAObstacleVertex obstacle = node.Obstacle;
		DWAObstacleVertex next = obstacle.Next;
		float signedDistanceOfPointToLineSegment = MBMath.GetSignedDistanceOfPointToLineSegment(obstacle.Point, next.Point, in agent.State.Position);
		QueryObstacleTreeRecursive(agent, ref rangeSq, (signedDistanceOfPointToLineSegment >= 0f) ? node.Left : node.Right);
		if (signedDistanceOfPointToLineSegment * signedDistanceOfPointToLineSegment / (next.Point - obstacle.Point).LengthSquared < rangeSq)
		{
			if (signedDistanceOfPointToLineSegment < 0f && agent.Delegate.IsObstacleSegmentEligibleNeighbor(obstacle, next))
			{
				agent.InsertObstacleNeighbor(node.Obstacle, ref rangeSq);
			}
			QueryObstacleTreeRecursive(agent, ref rangeSq, (signedDistanceOfPointToLineSegment >= 0f) ? node.Right : node.Left);
		}
	}

	private bool QueryVisibilityRecursive(in Vec2 q1, in Vec2 q2, float radius, DWAObstacleTreeNode node)
	{
		if (node == null)
		{
			return true;
		}
		DWAObstacleVertex obstacle = node.Obstacle;
		DWAObstacleVertex next = obstacle.Next;
		float signedDistanceOfPointToLineSegment = MBMath.GetSignedDistanceOfPointToLineSegment(obstacle.Point, next.Point, in q1);
		float signedDistanceOfPointToLineSegment2 = MBMath.GetSignedDistanceOfPointToLineSegment(obstacle.Point, next.Point, in q2);
		float num = 1f / (next.Point - obstacle.Point).LengthSquared;
		float num2 = signedDistanceOfPointToLineSegment * signedDistanceOfPointToLineSegment;
		float num3 = signedDistanceOfPointToLineSegment2 * signedDistanceOfPointToLineSegment2;
		float num4 = radius * radius;
		if (signedDistanceOfPointToLineSegment >= 0f && signedDistanceOfPointToLineSegment2 >= 0f)
		{
			if (QueryVisibilityRecursive(in q1, in q2, radius, node.Left))
			{
				if (!(num2 * num >= num4) || !(num3 * num >= num4))
				{
					return QueryVisibilityRecursive(in q1, in q2, radius, node.Right);
				}
				return true;
			}
			return false;
		}
		if (signedDistanceOfPointToLineSegment <= 0f && signedDistanceOfPointToLineSegment2 <= 0f)
		{
			if (QueryVisibilityRecursive(in q1, in q2, radius, node.Right))
			{
				if (!(num2 * num >= num4) || !(num3 * num >= num4))
				{
					return QueryVisibilityRecursive(in q1, in q2, radius, node.Left);
				}
				return true;
			}
			return false;
		}
		if (signedDistanceOfPointToLineSegment >= 0f && signedDistanceOfPointToLineSegment2 <= 0f)
		{
			if (QueryVisibilityRecursive(in q1, in q2, radius, node.Left))
			{
				return QueryVisibilityRecursive(in q1, in q2, radius, node.Right);
			}
			return false;
		}
		float signedDistanceOfPointToLineSegment3 = MBMath.GetSignedDistanceOfPointToLineSegment(in q1, in q2, obstacle.Point);
		float signedDistanceOfPointToLineSegment4 = MBMath.GetSignedDistanceOfPointToLineSegment(in q1, in q2, next.Point);
		float num5 = 1f / (q2 - q1).LengthSquared;
		float num6 = signedDistanceOfPointToLineSegment3 * signedDistanceOfPointToLineSegment3;
		float num7 = signedDistanceOfPointToLineSegment4 * signedDistanceOfPointToLineSegment4;
		if (signedDistanceOfPointToLineSegment3 * signedDistanceOfPointToLineSegment4 >= 0f && num6 * num5 > num4 && num7 * num5 > num4 && QueryVisibilityRecursive(in q1, in q2, radius, node.Left))
		{
			return QueryVisibilityRecursive(in q1, in q2, radius, node.Right);
		}
		return false;
	}
}
