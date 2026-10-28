using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal;

internal static class EncodingHelper
{
	public const int PooledBufferSize = 200;

	private static readonly System.Reflection.Internal.ObjectPool<byte[]> s_pool = new System.Reflection.Internal.ObjectPool<byte[]>(() => new byte[200]);

	public unsafe static string DecodeUtf8(byte* bytes, int byteCount, byte[] prefix, MetadataStringDecoder utf8Decoder)
	{
		if (prefix != null)
		{
			return DecodeUtf8Prefixed(bytes, byteCount, prefix, utf8Decoder);
		}
		if (byteCount == 0)
		{
			return string.Empty;
		}
		return utf8Decoder.GetString(bytes, byteCount);
	}

	private unsafe static string DecodeUtf8Prefixed(byte* bytes, int byteCount, byte[] prefix, MetadataStringDecoder utf8Decoder)
	{
		int num = byteCount + prefix.Length;
		if (num == 0)
		{
			return string.Empty;
		}
		byte[] array = AcquireBuffer(num);
		prefix.CopyTo(array, 0);
		Marshal.Copy((IntPtr)bytes, array, prefix.Length, byteCount);
		string result;
		fixed (byte* bytes2 = &array[0])
		{
			result = utf8Decoder.GetString(bytes2, num);
		}
		ReleaseBuffer(array);
		return result;
	}

	private static byte[] AcquireBuffer(int byteCount)
	{
		if (byteCount > 200)
		{
			return new byte[byteCount];
		}
		return s_pool.Allocate();
	}

	private static void ReleaseBuffer(byte[] buffer)
	{
		if (buffer.Length == 200)
		{
			s_pool.Free(buffer);
		}
	}
}
