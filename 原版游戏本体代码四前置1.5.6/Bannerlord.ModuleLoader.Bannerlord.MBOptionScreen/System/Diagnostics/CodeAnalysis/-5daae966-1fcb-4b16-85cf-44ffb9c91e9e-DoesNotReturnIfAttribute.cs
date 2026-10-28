namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C5daae966_002D1fcb_002D4b16_002D85cf_002D44ffb9c91e9e_003EDoesNotReturnIfAttribute : Attribute
{
	public bool ParameterValue { get; }

	public _003C5daae966_002D1fcb_002D4b16_002D85cf_002D44ffb9c91e9e_003EDoesNotReturnIfAttribute(bool parameterValue)
	{
		ParameterValue = parameterValue;
	}
}
