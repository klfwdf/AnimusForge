using System.Collections.Immutable;

namespace System.Reflection.Metadata;

internal readonly struct CustomAttributeValue<TType>
{
	public System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TType>> FixedArguments { get; }

	public System.Collections.Immutable.ImmutableArray<CustomAttributeNamedArgument<TType>> NamedArguments { get; }

	public CustomAttributeValue(System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TType>> fixedArguments, System.Collections.Immutable.ImmutableArray<CustomAttributeNamedArgument<TType>> namedArguments)
	{
		FixedArguments = fixedArguments;
		NamedArguments = namedArguments;
	}
}
