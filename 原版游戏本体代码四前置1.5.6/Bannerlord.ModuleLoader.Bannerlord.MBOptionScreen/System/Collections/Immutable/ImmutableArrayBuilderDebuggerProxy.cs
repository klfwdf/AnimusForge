using System.Diagnostics;

namespace System.Collections.Immutable;

internal sealed class ImmutableArrayBuilderDebuggerProxy<T>
{
	private readonly System.Collections.Immutable.ImmutableArray<T>.Builder _builder;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] A => _builder.ToArray();

	public ImmutableArrayBuilderDebuggerProxy(System.Collections.Immutable.ImmutableArray<T>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		_builder = builder;
	}
}
