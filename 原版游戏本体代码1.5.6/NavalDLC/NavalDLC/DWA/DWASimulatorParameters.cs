namespace NavalDLC.DWA;

public struct DWASimulatorParameters
{
	public const int DefaultMaxNumTimeSamples = 12;

	public const int DefaultSamplesPerSecond = 4;

	public const int DefaultAgentsToProcessPerTick = 1;

	public const int DefaultLinearAccelerationResolution = 3;

	public const int DefaultAngularAccelerationResolution = 3;

	public const bool DefaultIgnoreZeroAction = true;

	public const int DefaultMaxAgentNeighbors = 3;

	public const int DefaultMaxObstacleNeighbors = 3;

	private int _numLinearAccelerationSamples;

	private int _numAngularAccelerationSamples;

	private int _totalNumAccelerationSamples;

	private bool _requiresUpdate;

	public int NumTimeSamples { get; private set; }

	public int SamplesPerSecond { get; private set; }

	public int AgentsToProcessPerTick { get; private set; }

	public int LinearAccelerationResolution { get; private set; }

	public int AngularAccelerationResolution { get; private set; }

	public int MaxAgentNeighbors { get; private set; }

	public int MaxObstacleNeighbors { get; private set; }

	public bool IgnoreZeroAction { get; private set; }

	public int NumLinearAccelerationSamples => _numLinearAccelerationSamples;

	public int NumAngularAccelerationSamples => _numAngularAccelerationSamples;

	public int TotalNumAccelerationSamples => _totalNumAccelerationSamples;

	public float TimeHorizon => (float)NumTimeSamples * DeltaTime;

	public float DeltaTime => 1f / (float)SamplesPerSecond;

	private DWASimulatorParameters(int numTimeSamples, int samplesPerSecond, int agentsToProcessPerTick, int linearAccelerationResolution, int angularAccelerationResolution, bool ignoreZeroAction, int maxAgentNeighbors, int maxObstacleNeighbors, int numLinearAccelerationSamples, int numAngularAccelerationSamples, int totalNumAccelerationSamples, bool requiresUpdate)
	{
		NumTimeSamples = numTimeSamples;
		SamplesPerSecond = samplesPerSecond;
		AgentsToProcessPerTick = agentsToProcessPerTick;
		LinearAccelerationResolution = linearAccelerationResolution;
		AngularAccelerationResolution = angularAccelerationResolution;
		IgnoreZeroAction = ignoreZeroAction;
		MaxAgentNeighbors = maxAgentNeighbors;
		MaxObstacleNeighbors = maxObstacleNeighbors;
		_numLinearAccelerationSamples = numLinearAccelerationSamples;
		_numAngularAccelerationSamples = numAngularAccelerationSamples;
		_totalNumAccelerationSamples = totalNumAccelerationSamples;
		_requiresUpdate = requiresUpdate;
	}

	public bool CheckRequiresUpdate(bool reset)
	{
		bool requiresUpdate = _requiresUpdate;
		if (reset)
		{
			_requiresUpdate = false;
		}
		return requiresUpdate;
	}

	public void SetNumTimeSamples(int numTimeSamples)
	{
		if (NumTimeSamples != numTimeSamples)
		{
			NumTimeSamples = numTimeSamples;
			RecomputeDerivedParameters();
			_requiresUpdate = true;
		}
	}

	public void SetSamplesPerSecond(int samplesPerSecond)
	{
		if (SamplesPerSecond != samplesPerSecond)
		{
			SamplesPerSecond = samplesPerSecond;
			_requiresUpdate = true;
		}
	}

	public void SetAgentsToProcessPerTick(int agentsToProcessPerTick)
	{
		if (AgentsToProcessPerTick != agentsToProcessPerTick)
		{
			AgentsToProcessPerTick = agentsToProcessPerTick;
			_requiresUpdate = true;
		}
	}

	public void SetLinearAccelerationResolution(int linearAccelerationResolution)
	{
		if (LinearAccelerationResolution != linearAccelerationResolution)
		{
			LinearAccelerationResolution = linearAccelerationResolution;
			RecomputeDerivedParameters();
			_requiresUpdate = true;
		}
	}

	public void SetAngularAccelerationResolution(int angularAccelerationResolution)
	{
		if (AngularAccelerationResolution != angularAccelerationResolution)
		{
			AngularAccelerationResolution = angularAccelerationResolution;
			RecomputeDerivedParameters();
			_requiresUpdate = true;
		}
	}

	public void SetIgnoreZeroAction(bool ignoreZeroAction)
	{
		if (IgnoreZeroAction != ignoreZeroAction)
		{
			IgnoreZeroAction = ignoreZeroAction;
			RecomputeDerivedParameters();
			_requiresUpdate = true;
		}
	}

	public void SetMaxAgentNeighbors(int maxAgentNeighbors)
	{
		if (MaxAgentNeighbors != maxAgentNeighbors)
		{
			MaxAgentNeighbors = maxAgentNeighbors;
			_requiresUpdate = true;
		}
	}

	public void SetMaxObstacleNeighbors(int maxObstacleNeighbors)
	{
		if (MaxObstacleNeighbors != maxObstacleNeighbors)
		{
			MaxObstacleNeighbors = maxObstacleNeighbors;
			_requiresUpdate = true;
		}
	}

	public void CopyFrom(in DWASimulatorParameters otherParameters)
	{
		SetNumTimeSamples(otherParameters.NumTimeSamples);
		SetSamplesPerSecond(otherParameters.SamplesPerSecond);
		SetAgentsToProcessPerTick(otherParameters.AgentsToProcessPerTick);
		SetLinearAccelerationResolution(otherParameters.LinearAccelerationResolution);
		SetAngularAccelerationResolution(otherParameters.AngularAccelerationResolution);
		SetIgnoreZeroAction(otherParameters.IgnoreZeroAction);
		SetMaxAgentNeighbors(otherParameters.MaxAgentNeighbors);
		SetMaxObstacleNeighbors(otherParameters.MaxObstacleNeighbors);
	}

	private void RecomputeDerivedParameters()
	{
		ComputeDerivedParameters(LinearAccelerationResolution, AngularAccelerationResolution, IgnoreZeroAction, out _numLinearAccelerationSamples, out _numAngularAccelerationSamples, out _totalNumAccelerationSamples);
	}

	public static DWASimulatorParameters Create()
	{
		ComputeDerivedParameters(3, 3, ignoreZeroAction: true, out var numLinearAccelerationSamples, out var numAngularAccelerationSamples, out var numTotalAccelerationSamples);
		return new DWASimulatorParameters(12, 4, 1, 3, 3, ignoreZeroAction: true, 3, 3, numLinearAccelerationSamples, numAngularAccelerationSamples, numTotalAccelerationSamples, requiresUpdate: false);
	}

	public static void ComputeDerivedParameters(int linearAccelerationResolution, int angularAccelerationResolution, bool ignoreZeroAction, out int numLinearAccelerationSamples, out int numAngularAccelerationSamples, out int numTotalAccelerationSamples)
	{
		numLinearAccelerationSamples = 2 * linearAccelerationResolution + 1;
		numAngularAccelerationSamples = 2 * angularAccelerationResolution + 1;
		numTotalAccelerationSamples = numLinearAccelerationSamples * numAngularAccelerationSamples;
		if (ignoreZeroAction)
		{
			numTotalAccelerationSamples--;
		}
	}
}
