using System.ComponentModel;
using System.Numerics.Hashing;

namespace System;

internal readonly struct SequencePosition(object @object, int integer) : IEquatable<SequencePosition>
{
	private readonly object _object = @object;

	private readonly int _integer = integer;

	[EditorBrowsable(EditorBrowsableState.Never)]
	public object GetObject()
	{
		return _object;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public int GetInteger()
	{
		return _integer;
	}

	public bool Equals(SequencePosition other)
	{
		if (_integer == other._integer)
		{
			return object.Equals(_object, other._object);
		}
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool Equals(object obj)
	{
		if (obj is SequencePosition other)
		{
			return Equals(other);
		}
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override int GetHashCode()
	{
		return _003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003EHashHelpers.Combine(_object?.GetHashCode() ?? 0, _integer);
	}
}
