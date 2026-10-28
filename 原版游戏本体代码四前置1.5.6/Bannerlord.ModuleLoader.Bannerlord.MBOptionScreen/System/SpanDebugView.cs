using System.Diagnostics;

namespace System;

internal sealed class SpanDebugView<T>
{
	private readonly T[] _array;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items => _array;

	public SpanDebugView(Span<T> span)
	{
		_array = span.ToArray();
	}

	public SpanDebugView(ReadOnlySpan<T> span)
	{
		_array = span.ToArray();
	}
}
