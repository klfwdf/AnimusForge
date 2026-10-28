namespace System.Reflection.Metadata;

internal readonly struct Blob
{
	internal readonly byte[] Buffer;

	internal readonly int Start;

	public int Length { get; }

	public bool IsDefault => Buffer == null;

	internal Blob(byte[] buffer, int start, int length)
	{
		Buffer = buffer;
		Start = start;
		Length = length;
	}

	public ArraySegment<byte> GetBytes()
	{
		return new ArraySegment<byte>(Buffer, Start, Length);
	}
}
