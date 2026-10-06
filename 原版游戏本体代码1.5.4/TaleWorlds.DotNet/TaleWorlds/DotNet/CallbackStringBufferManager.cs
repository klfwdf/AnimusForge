using System;

namespace TaleWorlds.DotNet;

public static class CallbackStringBufferManager
{
	internal const int CallbackStringBufferMaxSize = 1024;

	[ThreadStatic]
	private static byte[] _stringBuffer0;

	[ThreadStatic]
	private static byte[] _stringBuffer1;

	[ThreadStatic]
	private static byte[] _stringBuffer2;

	[ThreadStatic]
	private static byte[] _stringBuffer3;

	[ThreadStatic]
	private static byte[] _stringBuffer4;

	[ThreadStatic]
	private static byte[] _stringBuffer5;

	public static byte[] StringBuffer0 => _stringBuffer0 ?? (_stringBuffer0 = new byte[1024]);

	public static byte[] StringBuffer1 => _stringBuffer1 ?? (_stringBuffer1 = new byte[1024]);

	public static byte[] StringBuffer2 => _stringBuffer2 ?? (_stringBuffer2 = new byte[1024]);

	public static byte[] StringBuffer3 => _stringBuffer3 ?? (_stringBuffer3 = new byte[1024]);

	public static byte[] StringBuffer4 => _stringBuffer4 ?? (_stringBuffer4 = new byte[1024]);

	public static byte[] StringBuffer5 => _stringBuffer5 ?? (_stringBuffer5 = new byte[1024]);
}
