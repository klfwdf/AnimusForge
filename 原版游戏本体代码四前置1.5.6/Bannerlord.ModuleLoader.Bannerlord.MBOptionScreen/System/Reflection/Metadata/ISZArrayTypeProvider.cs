namespace System.Reflection.Metadata;

internal interface ISZArrayTypeProvider<TType>
{
	TType GetSZArrayType(TType elementType);
}
