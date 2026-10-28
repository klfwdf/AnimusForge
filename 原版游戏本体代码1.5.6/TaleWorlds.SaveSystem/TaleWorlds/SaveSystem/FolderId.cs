using System;

namespace TaleWorlds.SaveSystem;

public struct FolderId : IEquatable<FolderId>
{
	public int LocalId { get; private set; }

	public SaveFolderExtension Extension { get; private set; }

	public FolderId(int localId, SaveFolderExtension extension)
	{
		LocalId = localId;
		Extension = extension;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is FolderId folderId))
		{
			return false;
		}
		if (folderId.LocalId == LocalId)
		{
			return folderId.Extension == Extension;
		}
		return false;
	}

	public bool Equals(FolderId other)
	{
		if (other.LocalId == LocalId)
		{
			return other.Extension == Extension;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (LocalId.GetHashCode() * 397) ^ ((int)Extension).GetHashCode();
	}

	public static bool operator ==(FolderId a, FolderId b)
	{
		if (a.LocalId == b.LocalId)
		{
			return a.Extension == b.Extension;
		}
		return false;
	}

	public static bool operator !=(FolderId a, FolderId b)
	{
		return !(a == b);
	}
}
