using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace NavalDLC.DWA;

public class DWASimulator
{
	internal const int MaxObstacleVertexCount = 32;

	private MBList<DWAAgent> _agentsData;

	private MBList<DWAObstacleVertex> _obstaclesData;

	private DWAKdTree _kdTree;

	private MBList<int> _obstacleIndices;

	private MBList<int> _removedAgentIndices;

	private bool _isInitialized;

	private int _currentAgentIndexToProcess;

	private DWAAgent[] _agentsToProcess;

	private int _agentsToProcessCount;

	private DWAThread[] _processThreads;

	private DWASimulatorParameters _parameters;

	private ushort _parity;

	private readonly TWParallel.ParallelForAuxPredicate RunSampleThreadsAuxParallelPredicate;

	public bool IsInitialized => _isInitialized;

	internal ref readonly DWASimulatorParameters Parameters => ref _parameters;

	public int NumAgents => _agentsData.Count - _removedAgentIndices.Count;

	public int NumObstacles => _obstaclesData.Count;

	internal MBReadOnlyList<DWAAgent> AgentsIncludingRemoved => _agentsData;

	internal MBReadOnlyList<DWAObstacleVertex> Obstacles => _obstaclesData;

	public DWASimulator()
	{
		_agentsData = new MBList<DWAAgent>();
		_obstaclesData = new MBList<DWAObstacleVertex>();
		_obstacleIndices = new MBList<int>();
		_removedAgentIndices = new MBList<int>();
		SetParameters(DWASimulatorParameters.Create());
		_kdTree = new DWAKdTree(this);
		RunSampleThreadsAuxParallelPredicate = RunSampleThreadsAuxParallel;
		_parity = 0;
	}

	public void SetParameters(in DWASimulatorParameters newParameters)
	{
		_parameters.CopyFrom(in newParameters);
		if (!_parameters.CheckRequiresUpdate(reset: true))
		{
			return;
		}
		_agentsToProcessCount = 0;
		_agentsToProcess = new DWAAgent[_parameters.AgentsToProcessPerTick];
		_currentAgentIndexToProcess = 0;
		_processThreads = new DWAThread[_parameters.TotalNumAccelerationSamples];
		for (int i = 0; i < _processThreads.Length; i++)
		{
			_processThreads[i] = new DWAThread(i);
		}
		foreach (DWAAgent agentsDatum in _agentsData)
		{
			agentsDatum?.SetForecastStates(_parameters.NumTimeSamples);
		}
	}

	public DWAAgentState GetAgentAgentNeighbor(int agentId, int neighborIndex)
	{
		return _agentsData[agentId].AgentNeighbors[neighborIndex].Value.State;
	}

	public IDWAObstacleVertex GetAgentObstacleNeighbor(int agentId, int neighborIndex)
	{
		return _agentsData[agentId].ObstacleNeighbors[neighborIndex].Value;
	}

	public DWAAgentState GetAgentState(int agentId)
	{
		return _agentsData[agentId].State;
	}

	public int GetAgentNumAgentNeighbors(int agentId)
	{
		return _agentsData[agentId].AgentNeighbors.Count;
	}

	public int GetAgentNumObstacleNeighbors(int agentId)
	{
		return _agentsData[agentId].ObstacleNeighbors.Count;
	}

	public IDWAObstacleVertex GetObstacle(int obstacleId)
	{
		return _obstaclesData[obstacleId];
	}

	public IDWAObstacleVertex GetNextObstacleOfObstacle(int obstacleId)
	{
		return _obstaclesData[obstacleId].Next;
	}

	public IDWAObstacleVertex GetPrevObstacleOfObstacle(int obstacleId)
	{
		return _obstaclesData[obstacleId].Previous;
	}

	public int AddAgent(IDWAAgentDelegate agentDelegate)
	{
		DWAAgent dWAAgent = null;
		int num;
		if (_removedAgentIndices.Count > 0)
		{
			num = _removedAgentIndices.Last();
			_removedAgentIndices.RemoveAt(_removedAgentIndices.Count - 1);
			dWAAgent = new DWAAgent(this, num, agentDelegate);
			_agentsData[num] = dWAAgent;
		}
		else
		{
			num = _agentsData.Count;
			dWAAgent = new DWAAgent(this, num, agentDelegate);
			_agentsData.Add(dWAAgent);
		}
		dWAAgent.SetForecastStates(_parameters.NumTimeSamples);
		dWAAgent.Delegate.Initialize(num);
		return num;
	}

	public bool RemoveAgent(IDWAAgentDelegate agentDelegate)
	{
		for (int i = 0; i < _agentsData.Count; i++)
		{
			if (_agentsData[i] != null && AgentsIncludingRemoved[i].Delegate == agentDelegate)
			{
				RemoveAgent(i);
				return true;
			}
		}
		return false;
	}

	public void RemoveAgent(int agentIndex)
	{
		_agentsData[agentIndex] = null;
		InsertRemovedIndex(agentIndex);
	}

	public int AddObstacle(MBList<Vec3> vertices)
	{
		if (vertices.Count < 2)
		{
			Debug.FailedAssert("Obstacle vertex count must be greater than one", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\DWACollision\\DWASimulator.cs", "AddObstacle", 329);
			return -1;
		}
		int count = _obstaclesData.Count;
		for (int i = 0; i < vertices.Count; i++)
		{
			DWAObstacleVertex dWAObstacleVertex = new DWAObstacleVertex(_obstaclesData.Count);
			dWAObstacleVertex.Point = vertices[i].AsVec2;
			dWAObstacleVertex.PointZ = vertices[i].z;
			if (i != 0)
			{
				dWAObstacleVertex.Previous = _obstaclesData[_obstaclesData.Count - 1];
				dWAObstacleVertex.Previous.Next = dWAObstacleVertex;
			}
			if (i == vertices.Count - 1)
			{
				dWAObstacleVertex.Next = _obstaclesData[count];
				dWAObstacleVertex.Next.Previous = dWAObstacleVertex;
			}
			dWAObstacleVertex.Direction = (vertices[(i != vertices.Count - 1) ? (i + 1) : 0].AsVec2 - vertices[i].AsVec2).Normalized();
			if (vertices.Count == 2)
			{
				dWAObstacleVertex.IsConvex = true;
			}
			else
			{
				dWAObstacleVertex.IsConvex = MBMath.GetSignedDistanceOfPointToLineSegment(vertices[(i == 0) ? (vertices.Count - 1) : (i - 1)].AsVec2, vertices[i].AsVec2, vertices[(i != vertices.Count - 1) ? (i + 1) : 0].AsVec2) >= 0f;
			}
			_obstaclesData.Add(dWAObstacleVertex);
		}
		_obstacleIndices.Add(count);
		return count;
	}

	public void Clear()
	{
		_agentsData.Clear();
		_obstaclesData.Clear();
		_obstacleIndices.Clear();
		_kdTree = new DWAKdTree(this);
		_removedAgentIndices.Clear();
		_currentAgentIndexToProcess = 0;
		_agentsToProcessCount = 0;
		for (int i = 0; i < _agentsToProcess.Length; i++)
		{
			_agentsToProcess[i] = null;
		}
		for (int j = 0; j < _processThreads.Length; j++)
		{
			_processThreads[j].Clear();
		}
		_isInitialized = false;
	}

	public void Tick(float dt)
	{
		if (!_isInitialized)
		{
			return;
		}
		_kdTree.BuildAgentTree();
		ComputeAndUpdateAgentsToProcess(_parity, ref _currentAgentIndexToProcess, out _agentsToProcessCount);
		if (_agentsToProcessCount > 0)
		{
			ComputeAndForecastNeighbors(_parity);
			DWAAgent[] agentsToProcess = _agentsToProcess;
			foreach (DWAAgent dWAAgent in agentsToProcess)
			{
				if (dWAAgent != null)
				{
					dWAAgent.InitializeThreads(in _parameters, _processThreads);
					dWAAgent.ComputeTargetOcclusion();
					TWParallel.For(0, _processThreads.Length, RunSampleThreadsAuxParallelPredicate);
					var (dV, dOmega) = dWAAgent.SelectAction(_processThreads, out var _, out var _);
					dWAAgent.Delegate.UpdateSelectedAction(dV, dOmega);
				}
			}
		}
		ClearProcessThreads();
		_parity++;
	}

	public bool QueryVisibility(Vec2 point1, Vec2 point2, float radius)
	{
		return _kdTree.QueryVisibility(in point1, in point2, radius);
	}

	private void RunSampleThreadsAuxParallel(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			_processThreads[i].Run();
		}
	}

	internal void AddObstacleVertex(DWAObstacleVertex newObstacle)
	{
		_obstaclesData.Add(newObstacle);
	}

	internal void ComputeAgentNeighbors(DWAAgent agent, float rangeSq, ushort parity)
	{
		_kdTree.ComputeAgentNeighbors(agent, rangeSq, parity);
	}

	internal void ComputeObstacleNeighbors(DWAAgent agent, float rangeSq)
	{
		_kdTree.ComputeObstacleNeighbors(agent, rangeSq);
	}

	internal void Initialize()
	{
		_kdTree.BuildObstacleTree();
		_isInitialized = true;
	}

	private void ComputeAndUpdateAgentsToProcess(ushort parity, ref int currentAgentIndexToProcess, out int agentsToProcessCount)
	{
		agentsToProcessCount = 0;
		if (_agentsData.Count <= 0)
		{
			return;
		}
		int num = currentAgentIndexToProcess;
		do
		{
			DWAAgent dWAAgent = _agentsData[currentAgentIndexToProcess];
			if (dWAAgent != null && dWAAgent.Delegate.CanPlanTrajectory())
			{
				dWAAgent.TryUpdateState(parity);
				if (!dWAAgent.Delegate.HasArrivedAtTarget())
				{
					_agentsToProcess[agentsToProcessCount] = dWAAgent;
					agentsToProcessCount++;
				}
				else
				{
					dWAAgent.Delegate.UpdateSelectedAction(0f, 0f);
				}
			}
			currentAgentIndexToProcess = (currentAgentIndexToProcess + 1) % _agentsData.Count;
		}
		while (agentsToProcessCount < _agentsToProcess.Length && currentAgentIndexToProcess != num);
	}

	private void ComputeAndForecastNeighbors(ushort parity)
	{
		for (int i = 0; i < _agentsToProcessCount; i++)
		{
			_agentsToProcess[i].ComputeNeighbors(parity);
			foreach (KeyValuePair<float, DWAAgent> agentNeighbor in _agentsToProcess[i].AgentNeighbors)
			{
				DWAAgent value = agentNeighbor.Value;
				if (!value.IsForecast)
				{
					value.ForecastTrajectory(_parameters.DeltaTime, _parameters.NumTimeSamples);
				}
			}
		}
	}

	private void ClearProcessThreads()
	{
		for (int i = 0; i < _processThreads.Length; i++)
		{
			_processThreads[i].Clear();
		}
	}

	private void InsertRemovedIndex(int removedIndex)
	{
		int num = _removedAgentIndices.BinarySearch(removedIndex, Comparer<int>.Create((int a, int b) => b.CompareTo(a)));
		if (num < 0)
		{
			num = ~num;
		}
		_removedAgentIndices.Insert(num, removedIndex);
	}
}
