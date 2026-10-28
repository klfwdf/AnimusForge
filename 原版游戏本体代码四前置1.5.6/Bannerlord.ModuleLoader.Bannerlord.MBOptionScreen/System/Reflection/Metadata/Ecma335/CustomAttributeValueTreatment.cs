namespace System.Reflection.Metadata.Ecma335;

[Flags]
internal enum CustomAttributeValueTreatment : byte
{
	None = 0,
	AttributeUsageAllowSingle = 1,
	AttributeUsageAllowMultiple = 2,
	AttributeUsageVersionAttribute = AttributeUsageAllowSingle | AttributeUsageAllowMultiple,
	AttributeUsageDeprecatedAttribute = 4
}
