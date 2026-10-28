namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class _003C5daae966_002D1fcb_002D4b16_002D85cf_002D44ffb9c91e9e_003EMemberNotNullAttribute : Attribute
{
	public string[] Members { get; }

	public _003C5daae966_002D1fcb_002D4b16_002D85cf_002D44ffb9c91e9e_003EMemberNotNullAttribute(string member)
	{
		Members = new string[1] { member };
	}

	public _003C5daae966_002D1fcb_002D4b16_002D85cf_002D44ffb9c91e9e_003EMemberNotNullAttribute(params string[] members)
	{
		Members = members;
	}
}
