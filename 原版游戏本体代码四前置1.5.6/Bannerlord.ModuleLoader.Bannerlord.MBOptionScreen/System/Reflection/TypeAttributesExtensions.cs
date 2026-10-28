namespace System.Reflection;

internal static class TypeAttributesExtensions
{
	private const TypeAttributes Forwarder = (TypeAttributes)2097152;

	private const TypeAttributes NestedMask = TypeAttributes.NestedFamANDAssem;

	public static bool IsForwarder(this TypeAttributes flags)
	{
		return (flags & (TypeAttributes)0x200000) != 0;
	}

	public static bool IsNested(this TypeAttributes flags)
	{
		return (flags & TypeAttributes.NestedFamANDAssem) != 0;
	}
}
