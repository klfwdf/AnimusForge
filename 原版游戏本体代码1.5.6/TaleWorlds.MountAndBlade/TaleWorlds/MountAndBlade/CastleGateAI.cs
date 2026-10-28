namespace TaleWorlds.MountAndBlade;

public class CastleGateAI : UsableMachineAIBase
{
	private CastleGate.GateState _initialState;

	public override bool HasActionCompleted => ((CastleGate)UsableMachine).State != _initialState;

	public void ResetInitialGateState(CastleGate.GateState newInitialState)
	{
		_initialState = newInitialState;
	}

	public CastleGateAI(CastleGate gate)
		: base(gate)
	{
		_initialState = gate.State;
	}
}
