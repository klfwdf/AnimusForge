using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond;

[Serializable]
[DataContract]
public struct SessionKey
{
	[DataMember]
	private readonly Guid _guid;

	public Guid Guid => _guid;

	public static SessionKey Empty => new SessionKey(Guid.Empty);

	public SessionKey(Guid guid)
	{
		_guid = guid;
	}

	public SessionKey(byte[] b)
	{
		_guid = new Guid(b);
	}

	public static SessionKey NewGuid()
	{
		return new SessionKey(Guid.NewGuid());
	}

	public override string ToString()
	{
		return _guid.ToString();
	}

	public byte[] ToByteArray()
	{
		Guid guid = _guid;
		return guid.ToByteArray();
	}

	public static bool operator ==(SessionKey a, SessionKey b)
	{
		return a._guid == b._guid;
	}

	public static bool operator !=(SessionKey a, SessionKey b)
	{
		return a._guid != b._guid;
	}

	public override bool Equals(object o)
	{
		if (o != null && o is SessionKey sessionKey)
		{
			Guid guid = _guid;
			return guid.Equals(sessionKey.Guid);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _guid.GetHashCode();
	}
}
