using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using MonoMod.Logs;

namespace MonoMod.Utils
{
	// Token: 0x020008C8 RID: 2248
	[NullableContext(1)]
	[Nullable(0)]
	internal static class Helpers
	{
		// Token: 0x06002E9A RID: 11930 RVA: 0x000A0820 File Offset: 0x0009EA20
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Swap<[Nullable(2)] T>(ref T a, ref T b)
		{
			T tmp = a;
			a = b;
			b = tmp;
		}

		// Token: 0x06002E9B RID: 11931 RVA: 0x000A0848 File Offset: 0x0009EA48
		[NullableContext(0)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static bool Has<T>(this T value, T flag) where T : struct, Enum
		{
			if (Unsafe.SizeOf<T>() == 8)
			{
				long flagVal = *Unsafe.As<T, long>(ref flag);
				return (*Unsafe.As<T, long>(ref value) & flagVal) == flagVal;
			}
			if (Unsafe.SizeOf<T>() == 4)
			{
				int flagVal2 = *Unsafe.As<T, int>(ref flag);
				return (*Unsafe.As<T, int>(ref value) & flagVal2) == flagVal2;
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				short flagVal3 = *Unsafe.As<T, short>(ref flag);
				return (*Unsafe.As<T, short>(ref value) & flagVal3) == flagVal3;
			}
			if (Unsafe.SizeOf<T>() == 1)
			{
				byte flagVal4 = *Unsafe.As<T, byte>(ref flag);
				return (*Unsafe.As<T, byte>(ref value) & flagVal4) == flagVal4;
			}
			throw new InvalidOperationException("unknown enum size?");
		}

		// Token: 0x06002E9C RID: 11932 RVA: 0x000A08DB File Offset: 0x0009EADB
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfArgumentNull<T>([NotNull] T arg, [Nullable(1)] [CallerArgumentExpression("arg")] string name = "")
		{
			if (arg == null)
			{
				Helpers.ThrowArgumentNull(name);
			}
		}

		// Token: 0x06002E9D RID: 11933 RVA: 0x000A08EB File Offset: 0x0009EAEB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ThrowIfNull<[Nullable(2)] T>([Nullable(2)] [NotNull] T arg, [CallerArgumentExpression("arg")] string name = "")
		{
			if (arg == null)
			{
				Helpers.ThrowArgumentNull(name);
			}
			return arg;
		}

		// Token: 0x06002E9E RID: 11934 RVA: 0x000A08FC File Offset: 0x0009EAFC
		public static T EventAdd<[Nullable(0)] T>([Nullable(2)] ref T evt, T del) where T : Delegate
		{
			T orig;
			T newDel;
			do
			{
				orig = evt;
				newDel = (T)((object)Delegate.Combine(orig, del));
			}
			while (Interlocked.CompareExchange<T>(ref evt, newDel, orig) != orig);
			return newDel;
		}

		// Token: 0x06002E9F RID: 11935 RVA: 0x000A0944 File Offset: 0x0009EB44
		[return: Nullable(2)]
		public static T EventRemove<[Nullable(0)] T>([Nullable(2)] ref T evt, T del) where T : Delegate
		{
			T orig;
			T newDel;
			do
			{
				orig = evt;
				newDel = (T)((object)Delegate.Remove(orig, del));
			}
			while (Interlocked.CompareExchange<T>(ref evt, newDel, orig) != orig);
			return newDel;
		}

		// Token: 0x06002EA0 RID: 11936 RVA: 0x000A098A File Offset: 0x0009EB8A
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Assert([DoesNotReturnIf(false)] bool value, [Nullable(2)] string message = null, [CallerArgumentExpression("value")] string expr = "")
		{
			if (!value)
			{
				Helpers.ThrowAssertionFailed(message, expr);
			}
		}

		// Token: 0x06002EA1 RID: 11937 RVA: 0x000A098A File Offset: 0x0009EB8A
		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DAssert([DoesNotReturnIf(false)] bool value, [Nullable(2)] string message = null, [CallerArgumentExpression("value")] string expr = "")
		{
			if (!value)
			{
				Helpers.ThrowAssertionFailed(message, expr);
			}
		}

		// Token: 0x06002EA2 RID: 11938 RVA: 0x000A0996 File Offset: 0x0009EB96
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Assert([DoesNotReturnIf(false)] bool value, [InterpolatedStringHandlerArgument("value")] ref AssertionInterpolatedStringHandler message, [CallerArgumentExpression("value")] string expr = "")
		{
			if (!value)
			{
				Helpers.ThrowAssertionFailed(ref message, expr);
			}
		}

		// Token: 0x06002EA3 RID: 11939 RVA: 0x000A0996 File Offset: 0x0009EB96
		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DAssert([DoesNotReturnIf(false)] bool value, [InterpolatedStringHandlerArgument("value")] ref AssertionInterpolatedStringHandler message, [CallerArgumentExpression("value")] string expr = "")
		{
			if (!value)
			{
				Helpers.ThrowAssertionFailed(ref message, expr);
			}
		}

		// Token: 0x06002EA4 RID: 11940 RVA: 0x000A09A2 File Offset: 0x0009EBA2
		[DoesNotReturn]
		private static void ThrowArgumentNull(string argName)
		{
			throw new ArgumentNullException(argName);
		}

		// Token: 0x06002EA5 RID: 11941 RVA: 0x000A09AC File Offset: 0x0009EBAC
		[DoesNotReturn]
		private static void ThrowAssertionFailed([Nullable(2)] string msg, string expr)
		{
			LogLevel logLevel = LogLevel.Assert;
			LogLevel level = logLevel;
			bool flag;
			DebugLogInterpolatedStringHandler debugLogInterpolatedStringHandler = new DebugLogInterpolatedStringHandler(19, 2, logLevel, ref flag);
			if (flag)
			{
				debugLogInterpolatedStringHandler.AppendLiteral("Assertion failed! ");
				debugLogInterpolatedStringHandler.AppendFormatted(expr);
				debugLogInterpolatedStringHandler.AppendLiteral(" ");
				debugLogInterpolatedStringHandler.AppendFormatted(msg);
			}
			DebugLog.Log("MonoMod.Utils.Assert", level, ref debugLogInterpolatedStringHandler);
			throw new AssertionFailedException(msg, expr);
		}

		// Token: 0x06002EA6 RID: 11942 RVA: 0x000A0A08 File Offset: 0x0009EC08
		[DoesNotReturn]
		private static void ThrowAssertionFailed(ref AssertionInterpolatedStringHandler message, string expr)
		{
			string msg = message.ToStringAndClear();
			LogLevel logLevel = LogLevel.Assert;
			LogLevel level = logLevel;
			bool flag;
			DebugLogInterpolatedStringHandler debugLogInterpolatedStringHandler = new DebugLogInterpolatedStringHandler(19, 2, logLevel, ref flag);
			if (flag)
			{
				debugLogInterpolatedStringHandler.AppendLiteral("Assertion failed! ");
				debugLogInterpolatedStringHandler.AppendFormatted(expr);
				debugLogInterpolatedStringHandler.AppendLiteral(" ");
				debugLogInterpolatedStringHandler.AppendFormatted(msg);
			}
			DebugLog.Log("MonoMod.Utils.Assert", level, ref debugLogInterpolatedStringHandler);
			throw new AssertionFailedException(msg, expr);
		}

		// Token: 0x06002EA7 RID: 11943 RVA: 0x000A0A6C File Offset: 0x0009EC6C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetOrInit<T>([Nullable(2)] ref T location, Func<T> init) where T : class
		{
			if (location != null)
			{
				return location;
			}
			return Helpers.InitializeValue<T, Func<T>>(ref location, Helpers.FuncInvokeHolder<T>.InvokeFunc, init);
		}

		// Token: 0x06002EA8 RID: 11944 RVA: 0x000A0A8E File Offset: 0x0009EC8E
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetOrInitWithLock<T>([Nullable(2)] ref T location, object @lock, Func<T> init) where T : class
		{
			if (location != null)
			{
				return location;
			}
			return Helpers.InitializeValueWithLock<T, Func<T>>(ref location, @lock, Helpers.FuncInvokeHolder<T>.InvokeFunc, init);
		}

		// Token: 0x06002EA9 RID: 11945 RVA: 0x000A0AB1 File Offset: 0x0009ECB1
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetOrInit<[Nullable(2)] TParam, T>([Nullable(2)] ref T location, Func<TParam, T> init, TParam param) where T : class
		{
			Helpers.ThrowIfArgumentNull<Func<TParam, T>>(init, "init");
			if (location != null)
			{
				return location;
			}
			return Helpers.InitializeValue<T, TParam>(ref location, init, param);
		}

		// Token: 0x06002EAA RID: 11946 RVA: 0x000A0ADA File Offset: 0x0009ECDA
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetOrInitWithLock<[Nullable(2)] TParam, T>([Nullable(2)] ref T location, object @lock, Func<TParam, T> init, TParam param) where T : class
		{
			Helpers.ThrowIfArgumentNull<Func<TParam, T>>(init, "init");
			if (location != null)
			{
				return location;
			}
			return Helpers.InitializeValueWithLock<T, TParam>(ref location, @lock, init, param);
		}

		// Token: 0x06002EAB RID: 11947 RVA: 0x000A0B04 File Offset: 0x0009ED04
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetOrInit<T>([Nullable(2)] ref T location, [Nullable(new byte[] { 0, 1 })] method init) where T : class
		{
			if (location != null)
			{
				return location;
			}
			return Helpers.InitializeValue<T, IntPtr>(ref location, ldftn(TailCallDelegatePtr<T>), (IntPtr)init);
		}

		// Token: 0x06002EAC RID: 11948 RVA: 0x000A0B2C File Offset: 0x0009ED2C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetOrInitWithLock<T>([Nullable(2)] ref T location, object @lock, [Nullable(new byte[] { 0, 1 })] method init) where T : class
		{
			if (location != null)
			{
				return location;
			}
			return Helpers.InitializeValueWithLock<T, IntPtr>(ref location, @lock, ldftn(TailCallDelegatePtr<T>), (IntPtr)init);
		}

		// Token: 0x06002EAD RID: 11949 RVA: 0x000A0B55 File Offset: 0x0009ED55
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetOrInit<T, [Nullable(2)] TParam>([Nullable(2)] ref T location, [Nullable(new byte[] { 0, 1, 1 })] method init, TParam obj) where T : class
		{
			if (location != null)
			{
				return location;
			}
			return Helpers.InitializeValue<T, TParam>(ref location, init, obj);
		}

		// Token: 0x06002EAE RID: 11950 RVA: 0x000A0B73 File Offset: 0x0009ED73
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T GetOrInitWithLock<T, [Nullable(2)] TParam>([Nullable(2)] ref T location, object @lock, [Nullable(new byte[] { 0, 1, 1 })] method init, TParam obj) where T : class
		{
			if (location != null)
			{
				return location;
			}
			return Helpers.InitializeValueWithLock<T, TParam>(ref location, @lock, init, obj);
		}

		// Token: 0x06002EAF RID: 11951 RVA: 0x000A0B94 File Offset: 0x0009ED94
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T InitializeValue<T, [Nullable(2)] TParam>([Nullable(2)] ref T location, [Nullable(new byte[] { 0, 1, 1 })] method init, TParam obj) where T : class
		{
			Interlocked.CompareExchange<T>(ref location, calli(T(TParam), obj, init), default(T));
			return location;
		}

		// Token: 0x06002EB0 RID: 11952 RVA: 0x000A0BC0 File Offset: 0x0009EDC0
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T InitializeValue<T, [Nullable(2)] TParam>([Nullable(2)] ref T location, Func<TParam, T> init, TParam obj) where T : class
		{
			Interlocked.CompareExchange<T>(ref location, init(obj), default(T));
			return location;
		}

		// Token: 0x06002EB1 RID: 11953 RVA: 0x000A0BEC File Offset: 0x0009EDEC
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T InitializeValueWithLock<T, [Nullable(2)] TParam>([Nullable(2)] ref T location, object @lock, [Nullable(new byte[] { 0, 1, 1 })] method init, TParam obj) where T : class
		{
			T result;
			lock (@lock)
			{
				if (location != null)
				{
					result = location;
				}
				else
				{
					result = (location = calli(T(TParam), obj, init));
				}
			}
			return result;
		}

		// Token: 0x06002EB2 RID: 11954 RVA: 0x000A0C50 File Offset: 0x0009EE50
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static T InitializeValueWithLock<T, [Nullable(2)] TParam>([Nullable(2)] ref T location, object @lock, Func<TParam, T> init, TParam obj) where T : class
		{
			T result;
			lock (@lock)
			{
				if (location != null)
				{
					result = location;
				}
				else
				{
					result = (location = init(obj));
				}
			}
			return result;
		}

		// Token: 0x06002EB3 RID: 11955 RVA: 0x000A0CB0 File Offset: 0x0009EEB0
		[NullableContext(0)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool MaskedSequenceEqual(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second, ReadOnlySpan<byte> mask)
		{
			if (mask.Length < first.Length || mask.Length < second.Length)
			{
				Helpers.ThrowMaskTooShort();
			}
			return first.Length == second.Length && Helpers.MaskedSequenceEqualCore(MemoryMarshal.GetReference<byte>(first), MemoryMarshal.GetReference<byte>(second), MemoryMarshal.GetReference<byte>(mask), (UIntPtr)((IntPtr)first.Length));
		}

		// Token: 0x06002EB4 RID: 11956 RVA: 0x000A0D13 File Offset: 0x0009EF13
		[DoesNotReturn]
		private static void ThrowMaskTooShort()
		{
			throw new ArgumentException("Mask too short", "mask");
		}

		// Token: 0x06002EB5 RID: 11957 RVA: 0x000A0D24 File Offset: 0x0009EF24
		private unsafe static bool MaskedSequenceEqualCore(ref byte first, ref byte second, ref byte maskBytes, [NativeInteger] UIntPtr length)
		{
			if (!Unsafe.AreSame<byte>(ref first, ref second))
			{
				IntPtr i = (IntPtr)0;
				if (length >= (UIntPtr)((IntPtr)sizeof(UIntPtr)))
				{
					IntPtr j = (IntPtr)(length - (UIntPtr)((IntPtr)sizeof(UIntPtr)));
					UIntPtr mask;
					while (j > i)
					{
						mask = Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref maskBytes, i));
						if ((Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref first, i)) & mask) != (Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref second, i)) & mask))
						{
							return false;
						}
						i += (IntPtr)sizeof(UIntPtr);
					}
					mask = Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref maskBytes, i));
					return (Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref first, j)) & mask) == (Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref second, j)) & mask);
				}
				while (length > (UIntPtr)i)
				{
					byte mask2 = *Unsafe.AddByteOffset<byte>(ref maskBytes, i);
					if ((*Unsafe.AddByteOffset<byte>(ref first, i) & mask2) != (*Unsafe.AddByteOffset<byte>(ref second, i) & mask2))
					{
						return false;
					}
					i += (IntPtr)1;
				}
				return true;
			}
			return true;
		}

		// Token: 0x06002EB6 RID: 11958 RVA: 0x000A0DEC File Offset: 0x0009EFEC
		public static byte[] ReadAllBytes(string path)
		{
			byte[] result;
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1))
			{
				long fileLength = fs.Length;
				if (fileLength > 2147483647L)
				{
					throw new IOException("File is too long (more than 2GB)");
				}
				if (fileLength == 0L)
				{
					result = Helpers.ReadAllBytesUnknownLength(fs);
				}
				else
				{
					int index = 0;
					int count = (int)fileLength;
					byte[] bytes = new byte[count];
					while (count > 0)
					{
						int i = fs.Read(bytes, index, count);
						if (i == 0)
						{
							throw new IOException("Unexpected end of stream");
						}
						index += i;
						count -= i;
					}
					result = bytes;
				}
			}
			return result;
		}

		// Token: 0x06002EB7 RID: 11959 RVA: 0x000A0E88 File Offset: 0x0009F088
		private static byte[] ReadAllBytesUnknownLength(FileStream fs)
		{
			byte[] rentedArray = ArrayPool<byte>.Shared.Rent(256);
			byte[] result;
			try
			{
				int bytesRead = 0;
				for (;;)
				{
					if (bytesRead == rentedArray.Length)
					{
						uint newLength = (uint)(rentedArray.Length * 2);
						if ((ulong)newLength > (ulong)((long)ArrayEx.MaxLength))
						{
							newLength = (uint)Math.Max(ArrayEx.MaxLength, rentedArray.Length + 1);
						}
						byte[] tmp = ArrayPool<byte>.Shared.Rent((int)newLength);
						Array.Copy(rentedArray, tmp, rentedArray.Length);
						if (rentedArray != null)
						{
							ArrayPool<byte>.Shared.Return(rentedArray, false);
						}
						rentedArray = tmp;
					}
					int i = fs.Read(rentedArray, bytesRead, rentedArray.Length - bytesRead);
					if (i == 0)
					{
						break;
					}
					bytesRead += i;
				}
				result = rentedArray.AsSpan(0, bytesRead).ToArray();
			}
			finally
			{
				if (rentedArray != null)
				{
					ArrayPool<byte>.Shared.Return(rentedArray, false);
				}
			}
			return result;
		}

		// Token: 0x020008C9 RID: 2249
		[NullableContext(0)]
		private static class FuncInvokeHolder<[Nullable(2)] T>
		{
			// Token: 0x04003B3F RID: 15167
			[Nullable(1)]
			public static readonly Func<Func<T>, T> InvokeFunc = (Func<T> f) => f();
		}
	}
}
