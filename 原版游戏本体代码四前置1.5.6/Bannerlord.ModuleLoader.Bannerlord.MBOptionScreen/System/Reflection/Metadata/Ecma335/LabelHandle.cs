using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct LabelHandle : IEquatable<LabelHandle>
{
	public int Id { get; }

	public bool IsNil => Id == 0;

	internal LabelHandle(int id)
	{
		Id = id;
	}

	public bool Equals(LabelHandle other)
	{
		return Id == other.Id;
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is LabelHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}

	public static bool operator ==(LabelHandle left, LabelHandle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(LabelHandle left, LabelHandle right)
	{
		return !left.Equals(right);
	}
}
