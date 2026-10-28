using System.Diagnostics;

namespace System.Collections.Immutable;

[DebuggerDisplay("{Value,nq}")]
internal struct RefAsValueType<T>
{
	internal T Value;

	internal RefAsValueType(T value)
	{
		Value = value;
	}
}
