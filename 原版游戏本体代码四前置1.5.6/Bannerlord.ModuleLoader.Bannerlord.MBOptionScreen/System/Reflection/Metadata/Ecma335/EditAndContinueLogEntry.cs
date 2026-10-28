using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct EditAndContinueLogEntry : IEquatable<EditAndContinueLogEntry>
{
	public EntityHandle Handle { get; }

	public EditAndContinueOperation Operation { get; }

	public EditAndContinueLogEntry(EntityHandle handle, EditAndContinueOperation operation)
	{
		Handle = handle;
		Operation = operation;
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is EditAndContinueLogEntry other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(EditAndContinueLogEntry other)
	{
		if (Operation == other.Operation)
		{
			return Handle == other.Handle;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (int)Operation ^ Handle.GetHashCode();
	}
}
