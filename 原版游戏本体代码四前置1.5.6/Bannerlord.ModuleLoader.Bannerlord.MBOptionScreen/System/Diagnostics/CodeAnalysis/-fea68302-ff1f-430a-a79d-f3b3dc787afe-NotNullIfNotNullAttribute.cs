namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
internal sealed class _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENotNullIfNotNullAttribute : Attribute
{
	public string ParameterName { get; }

	public _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ENotNullIfNotNullAttribute(string parameterName)
	{
		ParameterName = parameterName;
	}
}
