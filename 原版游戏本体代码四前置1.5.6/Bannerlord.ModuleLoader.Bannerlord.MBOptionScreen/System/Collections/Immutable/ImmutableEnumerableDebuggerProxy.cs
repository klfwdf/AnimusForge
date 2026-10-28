using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Immutable;

internal class ImmutableEnumerableDebuggerProxy<T>
{
	private readonly IEnumerable<T> _enumerable;

	private T[] _cachedContents;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Contents => _cachedContents ?? (_cachedContents = _enumerable.ToArray());

	public ImmutableEnumerableDebuggerProxy(IEnumerable<T> enumerable)
	{
		Requires.NotNull(enumerable, "enumerable");
		_enumerable = enumerable;
	}
}
