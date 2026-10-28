using TaleWorlds.Library;

namespace NavalDLC.DWA;

public class DWAThread
{
	private Vec2[] _tempObstaclePoly = new Vec2[32];

	public int Index { get; private set; }

	public float DV { get; private set; }

	public float DOmega { get; private set; }

	public DWAAgent Owner { get; private set; }

	public float DT { get; private set; }

	public int TimeSamples { get; private set; }

	public float Cost { get; private set; }

	public bool HasCollision { get; private set; }

	public int CollisionSampleIndex { get; private set; }

	public DWAAgent CollidedAgent { get; private set; }

	public DWAObstacleVertex CollidedObstacle { get; private set; }

	public bool IsFinished { get; private set; }

	public DWAThread(int index)
	{
		Index = index;
		Owner = null;
		DV = 0f;
		DOmega = 0f;
		DT = 0f;
		TimeSamples = 0;
		ClearAux();
	}

	public void Initialize(DWAAgent owner, float dV, float dOmega, float dt, int timeSamples)
	{
		Owner = owner;
		DV = dV;
		DOmega = dOmega;
		DT = dt;
		TimeSamples = timeSamples;
		ClearAux();
	}

	internal void Clear()
	{
		Owner = null;
		DV = 0f;
		DOmega = 0f;
		DT = 0f;
		TimeSamples = 0;
		ClearAux();
	}

	public void Run()
	{
		DWAAgentState curState = Owner.State;
		curState.LinearAcceleration = DV;
		curState.AngularAcceleration = DOmega;
		DWAAgentState newState = default(DWAAgentState);
		bool flag = false;
		DWAAgent dWAAgent = null;
		DWAObstacleVertex dWAObstacleVertex = null;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		for (int i = 0; i < TimeSamples; i++)
		{
			Owner.IntegrateState(in curState, DT, ref newState);
			Owner.EvaluateState(in newState, i, out var hasCollision, out var collidedAgent, out var collidedObstacle, out var goalCost, out var proxCost, out var maxPenetration, _tempObstaclePoly);
			if (hasCollision)
			{
				num4 += DT;
				num3 = MathF.Max(num3, maxPenetration);
				if (!flag)
				{
					flag = true;
					dWAAgent = collidedAgent;
					dWAObstacleVertex = collidedObstacle;
				}
			}
			num2 += proxCost;
			num += goalCost;
			curState = newState;
		}
		float num5 = 0.5f;
		float num6 = 1.5f;
		float num7 = (float)TimeSamples * DT;
		float num8 = MathF.Clamp(num3 / Owner.State.MaxExtent, 0f, 1f);
		float num9 = MathF.Clamp(num4 / num7, 0f, 1f);
		float num10 = num8 * num6;
		float num11 = num9 * num5;
		float num12 = (1f + num11 + num10 * num10) * num2;
		Cost = (num + num12) / (float)TimeSamples;
		HasCollision = flag;
		if (flag && dWAAgent != null)
		{
			CollidedAgent = dWAAgent;
		}
		if (flag && dWAObstacleVertex != null)
		{
			CollidedObstacle = dWAObstacleVertex;
		}
		IsFinished = true;
	}

	private void ClearAux()
	{
		IsFinished = false;
		Cost = 0f;
		HasCollision = false;
		CollisionSampleIndex = -1;
		CollidedAgent = null;
		CollidedObstacle = null;
	}
}
