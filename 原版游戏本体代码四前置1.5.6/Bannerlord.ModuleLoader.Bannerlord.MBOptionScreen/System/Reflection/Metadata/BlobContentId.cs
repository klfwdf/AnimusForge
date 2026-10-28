using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Internal;

namespace System.Reflection.Metadata;

internal readonly struct BlobContentId : IEquatable<System.Reflection.Metadata.BlobContentId>
{
	private const int Size = 20;

	private readonly Guid _guid;

	private readonly uint _stamp;

	public Guid Guid => _guid;

	public uint Stamp => _stamp;

	public bool IsDefault
	{
		get
		{
			if (Guid == default(Guid))
			{
				return Stamp == 0;
			}
			return false;
		}
	}

	public BlobContentId(Guid guid, uint stamp)
	{
		_guid = guid;
		_stamp = stamp;
	}

	public BlobContentId(System.Collections.Immutable.ImmutableArray<byte> id)
	{
		if (id.IsDefault)
		{
			System.Reflection.Throw.ArgumentNull("id");
		}
		Initialize(id.AsSpan(), out _guid, out _stamp);
	}

	public BlobContentId(byte[] id)
	{
		if (id == null)
		{
			System.Reflection.Throw.ArgumentNull("id");
		}
		Initialize(id, out _guid, out _stamp);
	}

	private unsafe static void Initialize(ReadOnlySpan<byte> id, out Guid guid, out uint stamp)
	{
		if (id.Length != 20)
		{
			throw new ArgumentException(System.SR.Format(System.SR.UnexpectedArrayLength, 20), "id");
		}
		fixed (byte* buffer = &id[0])
		{
			System.Reflection.Metadata.BlobReader blobReader = new System.Reflection.Metadata.BlobReader(buffer, id.Length);
			guid = blobReader.ReadGuid();
			stamp = blobReader.ReadUInt32();
		}
	}

	public static System.Reflection.Metadata.BlobContentId FromHash(System.Collections.Immutable.ImmutableArray<byte> hashCode)
	{
		if (hashCode.IsDefault)
		{
			System.Reflection.Throw.ArgumentNull("hashCode");
		}
		return FromHash(hashCode.AsSpan());
	}

	public static System.Reflection.Metadata.BlobContentId FromHash(byte[] hashCode)
	{
		if (hashCode == null)
		{
			System.Reflection.Throw.ArgumentNull("hashCode");
		}
		return FromHash(hashCode.AsSpan());
	}

	private static System.Reflection.Metadata.BlobContentId FromHash(ReadOnlySpan<byte> hashCode)
	{
		if (hashCode.Length < 20)
		{
			throw new ArgumentException(System.SR.Format(System.SR.HashTooShort, 20), "hashCode");
		}
		uint a = (uint)((hashCode[3] << 24) | (hashCode[2] << 16) | (hashCode[1] << 8) | hashCode[0]);
		ushort num = (ushort)((hashCode[5] << 8) | hashCode[4]);
		ushort num2 = (ushort)((hashCode[7] << 8) | hashCode[6]);
		byte b = hashCode[8];
		byte e = hashCode[9];
		byte f = hashCode[10];
		byte g = hashCode[11];
		byte h = hashCode[12];
		byte i = hashCode[13];
		byte j = hashCode[14];
		byte k = hashCode[15];
		num2 = (ushort)((num2 & 0xFFF) | 0x4000);
		b = (byte)((b & 0x3F) | 0x80);
		Guid guid = new Guid((int)a, (short)num, (short)num2, b, e, f, g, h, i, j, k);
		uint stamp = (uint)(int.MinValue | ((hashCode[19] << 24) | (hashCode[18] << 16) | (hashCode[17] << 8) | hashCode[16]));
		return new System.Reflection.Metadata.BlobContentId(guid, stamp);
	}

	public static Func<IEnumerable<Blob>, System.Reflection.Metadata.BlobContentId> GetTimeBasedProvider()
	{
		uint timestamp = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
		return (IEnumerable<Blob> content) => new System.Reflection.Metadata.BlobContentId(Guid.NewGuid(), timestamp);
	}

	public bool Equals(System.Reflection.Metadata.BlobContentId other)
	{
		if (Guid == other.Guid)
		{
			return Stamp == other.Stamp;
		}
		return false;
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is System.Reflection.Metadata.BlobContentId other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return System.Reflection.Internal.Hash.Combine(Stamp, Guid.GetHashCode());
	}

	public static bool operator ==(System.Reflection.Metadata.BlobContentId left, System.Reflection.Metadata.BlobContentId right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(System.Reflection.Metadata.BlobContentId left, System.Reflection.Metadata.BlobContentId right)
	{
		return !left.Equals(right);
	}
}
