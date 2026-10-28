namespace TaleWorlds.Core;

public class MBUnderFlowException : MBException
{
	public MBUnderFlowException()
		: base("The given value is less than the expected value.")
	{
	}

	public MBUnderFlowException(string parameterName)
		: base("The given value is less than the expected value : " + parameterName)
	{
	}
}
