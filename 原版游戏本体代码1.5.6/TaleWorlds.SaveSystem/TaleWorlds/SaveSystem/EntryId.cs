using System;

namespace TaleWorlds.SaveSystem;

public struct EntryId : IEquatable<EntryId>
{
	public int Id { get; private set; }

	public SaveEntryExtension Extension { get; private set; }

	public EntryId(int id, SaveEntryExtension extension)
	{
		Id = id;
		Extension = extension;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is EntryId entryId))
		{
			return false;
		}
		if (entryId.Id == Id)
		{
			return entryId.Extension == Extension;
		}
		return false;
	}

	public bool Equals(EntryId other)
	{
		if (other.Id == Id)
		{
			return other.Extension == Extension;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Id.GetHashCode() * 397) ^ ((int)Extension).GetHashCode();
	}

	public static bool operator ==(EntryId a, EntryId b)
	{
		if (a.Id == b.Id)
		{
			return a.Extension == b.Extension;
		}
		return false;
	}

	public static bool operator !=(EntryId a, EntryId b)
	{
		return !(a == b);
	}
}
