namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C5daae966_002D1fcb_002D4b16_002D85cf_002D44ffb9c91e9e_003ENotNullIfNotNullAttribute : Attribute
{
	public string ParameterName { get; }

	public _003C5daae966_002D1fcb_002D4b16_002D85cf_002D44ffb9c91e9e_003ENotNullIfNotNullAttribute(string parameterName)
	{
		ParameterName = parameterName;
	}
}
