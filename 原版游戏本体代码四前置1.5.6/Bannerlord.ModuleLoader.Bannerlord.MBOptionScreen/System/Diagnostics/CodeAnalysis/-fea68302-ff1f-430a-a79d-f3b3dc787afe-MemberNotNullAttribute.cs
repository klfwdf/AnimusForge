namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
internal sealed class _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EMemberNotNullAttribute : Attribute
{
	public string[] Members { get; }

	public _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EMemberNotNullAttribute(string member)
	{
		Members = new string[1] { member };
	}

	public _003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003EMemberNotNullAttribute(params string[] members)
	{
		Members = members;
	}
}
