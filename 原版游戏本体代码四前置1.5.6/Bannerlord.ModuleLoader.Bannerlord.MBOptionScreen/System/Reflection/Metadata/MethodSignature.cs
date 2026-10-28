using System.Collections.Immutable;

namespace System.Reflection.Metadata;

internal readonly struct MethodSignature<TType>
{
	public SignatureHeader Header { get; }

	public TType ReturnType { get; }

	public int RequiredParameterCount { get; }

	public int GenericParameterCount { get; }

	public System.Collections.Immutable.ImmutableArray<TType> ParameterTypes { get; }

	public MethodSignature(SignatureHeader header, TType returnType, int requiredParameterCount, int genericParameterCount, System.Collections.Immutable.ImmutableArray<TType> parameterTypes)
	{
		Header = header;
		ReturnType = returnType;
		GenericParameterCount = genericParameterCount;
		RequiredParameterCount = requiredParameterCount;
		ParameterTypes = parameterTypes;
	}
}
