namespace TaleWorlds.Network;

public class WaitForSpecialCase : CoroutineState
{
	public delegate bool IsConditionSatisfiedDelegate();

	private IsConditionSatisfiedDelegate _isConditionSatisfiedDelegate;

	protected internal override bool IsFinished => _isConditionSatisfiedDelegate();

	public WaitForSpecialCase(IsConditionSatisfiedDelegate isConditionSatisfiedDelegate)
	{
		_isConditionSatisfiedDelegate = isConditionSatisfiedDelegate;
	}
}
