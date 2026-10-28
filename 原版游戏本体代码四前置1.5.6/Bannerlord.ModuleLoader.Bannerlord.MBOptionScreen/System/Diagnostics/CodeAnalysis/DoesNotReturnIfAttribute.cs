namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class DoesNotReturnIfAttribute : Attribute
{
	public bool ParameterValue { get; }

	public DoesNotReturnIfAttribute(bool parameterValue)
	{
		ParameterValue = parameterValue;
	}
}
