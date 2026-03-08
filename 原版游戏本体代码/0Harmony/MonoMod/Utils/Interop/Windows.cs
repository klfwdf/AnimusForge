using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoMod.Utils.Interop
{
	// Token: 0x020008EE RID: 2286
	internal static class Windows
	{
		// Token: 0x06002F8F RID: 12175
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("kernel32", ExactSpelling = true)]
		public unsafe static extern void GetSystemInfo(Windows.SYSTEM_INFO* lpSystemInfo);

		// Token: 0x06002F90 RID: 12176
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("kernel32", ExactSpelling = true)]
		public unsafe static extern Windows.HMODULE GetModuleHandleW(ushort* lpModuleName);

		// Token: 0x06002F91 RID: 12177
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("kernel32", ExactSpelling = true)]
		public unsafe static extern IntPtr GetProcAddress(Windows.HMODULE hModule, sbyte* lpProcName);

		// Token: 0x06002F92 RID: 12178
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("kernel32", ExactSpelling = true)]
		public unsafe static extern Windows.HMODULE LoadLibraryW(ushort* lpLibFileName);

		// Token: 0x06002F93 RID: 12179
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("kernel32", ExactSpelling = true)]
		public static extern Windows.BOOL FreeLibrary(Windows.HMODULE hLibModule);

		// Token: 0x06002F94 RID: 12180
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("kernel32", ExactSpelling = true)]
		public static extern uint GetLastError();

		// Token: 0x04003BC1 RID: 15297
		public const int PROCESSOR_ARCHITECTURE_INTEL = 0;

		// Token: 0x04003BC2 RID: 15298
		public const int PROCESSOR_ARCHITECTURE_MIPS = 1;

		// Token: 0x04003BC3 RID: 15299
		public const int PROCESSOR_ARCHITECTURE_ALPHA = 2;

		// Token: 0x04003BC4 RID: 15300
		public const int PROCESSOR_ARCHITECTURE_PPC = 3;

		// Token: 0x04003BC5 RID: 15301
		public const int PROCESSOR_ARCHITECTURE_SHX = 4;

		// Token: 0x04003BC6 RID: 15302
		public const int PROCESSOR_ARCHITECTURE_ARM = 5;

		// Token: 0x04003BC7 RID: 15303
		public const int PROCESSOR_ARCHITECTURE_IA64 = 6;

		// Token: 0x04003BC8 RID: 15304
		public const int PROCESSOR_ARCHITECTURE_ALPHA64 = 7;

		// Token: 0x04003BC9 RID: 15305
		public const int PROCESSOR_ARCHITECTURE_MSIL = 8;

		// Token: 0x04003BCA RID: 15306
		public const int PROCESSOR_ARCHITECTURE_AMD64 = 9;

		// Token: 0x04003BCB RID: 15307
		public const int PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 = 10;

		// Token: 0x04003BCC RID: 15308
		public const int PROCESSOR_ARCHITECTURE_NEUTRAL = 11;

		// Token: 0x04003BCD RID: 15309
		public const int PROCESSOR_ARCHITECTURE_ARM64 = 12;

		// Token: 0x04003BCE RID: 15310
		public const int PROCESSOR_ARCHITECTURE_ARM32_ON_WIN64 = 13;

		// Token: 0x04003BCF RID: 15311
		public const int PROCESSOR_ARCHITECTURE_IA32_ON_ARM64 = 14;

		// Token: 0x04003BD0 RID: 15312
		public const int PROCESSOR_ARCHITECTURE_UNKNOWN = 65535;

		// Token: 0x020008EF RID: 2287
		[Conditional("NEVER")]
		[AttributeUsage(AttributeTargets.All)]
		private sealed class SetsLastSystemErrorAttribute : Attribute
		{
		}

		// Token: 0x020008F0 RID: 2288
		[Conditional("NEVER")]
		[AttributeUsage(AttributeTargets.All)]
		private sealed class NativeTypeNameAttribute : Attribute
		{
			// Token: 0x06002F96 RID: 12182 RVA: 0x00002057 File Offset: 0x00000257
			[NullableContext(1)]
			public NativeTypeNameAttribute(string x)
			{
			}
		}

		// Token: 0x020008F1 RID: 2289
		public struct SYSTEM_INFO
		{
			// Token: 0x1700085C RID: 2140
			// (get) Token: 0x06002F97 RID: 12183 RVA: 0x000A56D8 File Offset: 0x000A38D8
			[UnscopedRef]
			public ref uint dwOemId
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return ref this.Anonymous.dwOemId;
				}
			}

			// Token: 0x1700085D RID: 2141
			// (get) Token: 0x06002F98 RID: 12184 RVA: 0x000A56E5 File Offset: 0x000A38E5
			[UnscopedRef]
			public ref ushort wProcessorArchitecture
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return ref this.Anonymous.Anonymous.wProcessorArchitecture;
				}
			}

			// Token: 0x1700085E RID: 2142
			// (get) Token: 0x06002F99 RID: 12185 RVA: 0x000A56F7 File Offset: 0x000A38F7
			[UnscopedRef]
			public ref ushort wReserved
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get
				{
					return ref this.Anonymous.Anonymous.wReserved;
				}
			}

			// Token: 0x04003BD1 RID: 15313
			public Windows.SYSTEM_INFO._Anonymous_e__Union Anonymous;

			// Token: 0x04003BD2 RID: 15314
			public uint dwPageSize;

			// Token: 0x04003BD3 RID: 15315
			public unsafe void* lpMinimumApplicationAddress;

			// Token: 0x04003BD4 RID: 15316
			public unsafe void* lpMaximumApplicationAddress;

			// Token: 0x04003BD5 RID: 15317
			[NativeInteger]
			public UIntPtr dwActiveProcessorMask;

			// Token: 0x04003BD6 RID: 15318
			public uint dwNumberOfProcessors;

			// Token: 0x04003BD7 RID: 15319
			public uint dwProcessorType;

			// Token: 0x04003BD8 RID: 15320
			public uint dwAllocationGranularity;

			// Token: 0x04003BD9 RID: 15321
			public ushort wProcessorLevel;

			// Token: 0x04003BDA RID: 15322
			public ushort wProcessorRevision;

			// Token: 0x020008F2 RID: 2290
			[StructLayout(LayoutKind.Explicit)]
			public struct _Anonymous_e__Union
			{
				// Token: 0x04003BDB RID: 15323
				[FieldOffset(0)]
				public uint dwOemId;

				// Token: 0x04003BDC RID: 15324
				[FieldOffset(0)]
				public Windows.SYSTEM_INFO._Anonymous_e__Union._Anonymous_e__Struct Anonymous;

				// Token: 0x020008F3 RID: 2291
				public struct _Anonymous_e__Struct
				{
					// Token: 0x04003BDD RID: 15325
					public ushort wProcessorArchitecture;

					// Token: 0x04003BDE RID: 15326
					public ushort wReserved;
				}
			}
		}

		// Token: 0x020008F4 RID: 2292
		public readonly struct BOOL : IComparable, IComparable<Windows.BOOL>, IEquatable<Windows.BOOL>, IFormattable
		{
			// Token: 0x06002F9A RID: 12186 RVA: 0x000A5709 File Offset: 0x000A3909
			public BOOL(int value)
			{
				this.Value = value;
			}

			// Token: 0x1700085F RID: 2143
			// (get) Token: 0x06002F9B RID: 12187 RVA: 0x000A5712 File Offset: 0x000A3912
			public static Windows.BOOL FALSE
			{
				get
				{
					return new Windows.BOOL(0);
				}
			}

			// Token: 0x17000860 RID: 2144
			// (get) Token: 0x06002F9C RID: 12188 RVA: 0x000A571A File Offset: 0x000A391A
			public static Windows.BOOL TRUE
			{
				get
				{
					return new Windows.BOOL(1);
				}
			}

			// Token: 0x06002F9D RID: 12189 RVA: 0x000A5722 File Offset: 0x000A3922
			public static bool operator ==(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value == right.Value;
			}

			// Token: 0x06002F9E RID: 12190 RVA: 0x000A5732 File Offset: 0x000A3932
			public static bool operator !=(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value != right.Value;
			}

			// Token: 0x06002F9F RID: 12191 RVA: 0x000A5745 File Offset: 0x000A3945
			public static bool operator <(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value < right.Value;
			}

			// Token: 0x06002FA0 RID: 12192 RVA: 0x000A5755 File Offset: 0x000A3955
			public static bool operator <=(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value <= right.Value;
			}

			// Token: 0x06002FA1 RID: 12193 RVA: 0x000A5768 File Offset: 0x000A3968
			public static bool operator >(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value > right.Value;
			}

			// Token: 0x06002FA2 RID: 12194 RVA: 0x000A5778 File Offset: 0x000A3978
			public static bool operator >=(Windows.BOOL left, Windows.BOOL right)
			{
				return left.Value >= right.Value;
			}

			// Token: 0x06002FA3 RID: 12195 RVA: 0x000A578B File Offset: 0x000A398B
			public static implicit operator bool(Windows.BOOL value)
			{
				return value.Value != 0;
			}

			// Token: 0x06002FA4 RID: 12196 RVA: 0x000A5796 File Offset: 0x000A3996
			public static implicit operator Windows.BOOL(bool value)
			{
				return new Windows.BOOL((value > false) ? 1 : 0);
			}

			// Token: 0x06002FA5 RID: 12197 RVA: 0x000A57A1 File Offset: 0x000A39A1
			public static bool operator false(Windows.BOOL value)
			{
				return value.Value == 0;
			}

			// Token: 0x06002FA6 RID: 12198 RVA: 0x000A578B File Offset: 0x000A398B
			public static bool operator true(Windows.BOOL value)
			{
				return value.Value != 0;
			}

			// Token: 0x06002FA7 RID: 12199 RVA: 0x000A57AC File Offset: 0x000A39AC
			public static implicit operator Windows.BOOL(byte value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002FA8 RID: 12200 RVA: 0x000A57B4 File Offset: 0x000A39B4
			public static explicit operator byte(Windows.BOOL value)
			{
				return (byte)value.Value;
			}

			// Token: 0x06002FA9 RID: 12201 RVA: 0x000A57AC File Offset: 0x000A39AC
			public static implicit operator Windows.BOOL(short value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002FAA RID: 12202 RVA: 0x000A57BD File Offset: 0x000A39BD
			public static explicit operator short(Windows.BOOL value)
			{
				return (short)value.Value;
			}

			// Token: 0x06002FAB RID: 12203 RVA: 0x000A57AC File Offset: 0x000A39AC
			public static implicit operator Windows.BOOL(int value)
			{
				return new Windows.BOOL(value);
			}

			// Token: 0x06002FAC RID: 12204 RVA: 0x000A57C6 File Offset: 0x000A39C6
			public static implicit operator int(Windows.BOOL value)
			{
				return value.Value;
			}

			// Token: 0x06002FAD RID: 12205 RVA: 0x000A57CE File Offset: 0x000A39CE
			public static explicit operator Windows.BOOL(long value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002FAE RID: 12206 RVA: 0x000A57D7 File Offset: 0x000A39D7
			public static implicit operator long(Windows.BOOL value)
			{
				return (long)value.Value;
			}

			// Token: 0x06002FAF RID: 12207 RVA: 0x000A57CE File Offset: 0x000A39CE
			public static explicit operator Windows.BOOL([NativeInteger] IntPtr value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002FB0 RID: 12208 RVA: 0x000A57E0 File Offset: 0x000A39E0
			[return: NativeInteger]
			public static implicit operator IntPtr(Windows.BOOL value)
			{
				return (IntPtr)value.Value;
			}

			// Token: 0x06002FB1 RID: 12209 RVA: 0x000A57AC File Offset: 0x000A39AC
			public static implicit operator Windows.BOOL(sbyte value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002FB2 RID: 12210 RVA: 0x000A57E9 File Offset: 0x000A39E9
			public static explicit operator sbyte(Windows.BOOL value)
			{
				return (sbyte)value.Value;
			}

			// Token: 0x06002FB3 RID: 12211 RVA: 0x000A57AC File Offset: 0x000A39AC
			public static implicit operator Windows.BOOL(ushort value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002FB4 RID: 12212 RVA: 0x000A57F2 File Offset: 0x000A39F2
			public static explicit operator ushort(Windows.BOOL value)
			{
				return (ushort)value.Value;
			}

			// Token: 0x06002FB5 RID: 12213 RVA: 0x000A57AC File Offset: 0x000A39AC
			public static explicit operator Windows.BOOL(uint value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002FB6 RID: 12214 RVA: 0x000A57C6 File Offset: 0x000A39C6
			public static explicit operator uint(Windows.BOOL value)
			{
				return (uint)value.Value;
			}

			// Token: 0x06002FB7 RID: 12215 RVA: 0x000A57CE File Offset: 0x000A39CE
			public static explicit operator Windows.BOOL(ulong value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002FB8 RID: 12216 RVA: 0x000A57D7 File Offset: 0x000A39D7
			public static explicit operator ulong(Windows.BOOL value)
			{
				return (ulong)((long)value.Value);
			}

			// Token: 0x06002FB9 RID: 12217 RVA: 0x000A57CE File Offset: 0x000A39CE
			public static explicit operator Windows.BOOL([NativeInteger] UIntPtr value)
			{
				return new Windows.BOOL((int)value);
			}

			// Token: 0x06002FBA RID: 12218 RVA: 0x000A57E0 File Offset: 0x000A39E0
			[return: NativeInteger]
			public static explicit operator UIntPtr(Windows.BOOL value)
			{
				return (UIntPtr)((IntPtr)value.Value);
			}

			// Token: 0x06002FBB RID: 12219 RVA: 0x000A57FC File Offset: 0x000A39FC
			[NullableContext(2)]
			public int CompareTo(object obj)
			{
				if (obj is Windows.BOOL)
				{
					Windows.BOOL other = (Windows.BOOL)obj;
					return this.CompareTo(other);
				}
				if (obj != null)
				{
					throw new ArgumentException("obj is not an instance of BOOL.");
				}
				return 1;
			}

			// Token: 0x06002FBC RID: 12220 RVA: 0x000A5830 File Offset: 0x000A3A30
			public int CompareTo(Windows.BOOL other)
			{
				return this.Value.CompareTo(other.Value);
			}

			// Token: 0x06002FBD RID: 12221 RVA: 0x000A5854 File Offset: 0x000A3A54
			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				if (obj is Windows.BOOL)
				{
					Windows.BOOL other = (Windows.BOOL)obj;
					return this.Equals(other);
				}
				return false;
			}

			// Token: 0x06002FBE RID: 12222 RVA: 0x000A587C File Offset: 0x000A3A7C
			public bool Equals(Windows.BOOL other)
			{
				return this.Value.Equals(other.Value);
			}

			// Token: 0x06002FBF RID: 12223 RVA: 0x000A58A0 File Offset: 0x000A3AA0
			public override int GetHashCode()
			{
				return this.Value.GetHashCode();
			}

			// Token: 0x06002FC0 RID: 12224 RVA: 0x000A58BC File Offset: 0x000A3ABC
			[NullableContext(1)]
			public override string ToString()
			{
				return this.Value.ToString(null);
			}

			// Token: 0x06002FC1 RID: 12225 RVA: 0x000A58D8 File Offset: 0x000A3AD8
			[NullableContext(2)]
			[return: Nullable(1)]
			public string ToString(string format, IFormatProvider formatProvider)
			{
				return this.Value.ToString(format, formatProvider);
			}

			// Token: 0x04003BDF RID: 15327
			public readonly int Value;
		}

		// Token: 0x020008F5 RID: 2293
		public readonly struct HANDLE : IComparable, IComparable<Windows.HANDLE>, IEquatable<Windows.HANDLE>, IFormattable
		{
			// Token: 0x06002FC2 RID: 12226 RVA: 0x000A58F5 File Offset: 0x000A3AF5
			public unsafe HANDLE(void* value)
			{
				this.Value = value;
			}

			// Token: 0x17000861 RID: 2145
			// (get) Token: 0x06002FC3 RID: 12227 RVA: 0x000A58FE File Offset: 0x000A3AFE
			public static Windows.HANDLE INVALID_VALUE
			{
				get
				{
					return new Windows.HANDLE(-1);
				}
			}

			// Token: 0x17000862 RID: 2146
			// (get) Token: 0x06002FC4 RID: 12228 RVA: 0x000A5907 File Offset: 0x000A3B07
			public static Windows.HANDLE NULL
			{
				get
				{
					return new Windows.HANDLE(null);
				}
			}

			// Token: 0x06002FC5 RID: 12229 RVA: 0x000A5910 File Offset: 0x000A3B10
			public static bool operator ==(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value == right.Value;
			}

			// Token: 0x06002FC6 RID: 12230 RVA: 0x000A5920 File Offset: 0x000A3B20
			public static bool operator !=(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value != right.Value;
			}

			// Token: 0x06002FC7 RID: 12231 RVA: 0x000A5933 File Offset: 0x000A3B33
			public static bool operator <(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value < right.Value;
			}

			// Token: 0x06002FC8 RID: 12232 RVA: 0x000A5943 File Offset: 0x000A3B43
			public static bool operator <=(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value == right.Value;
			}

			// Token: 0x06002FC9 RID: 12233 RVA: 0x000A5956 File Offset: 0x000A3B56
			public static bool operator >(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value != right.Value;
			}

			// Token: 0x06002FCA RID: 12234 RVA: 0x000A5966 File Offset: 0x000A3B66
			public static bool operator >=(Windows.HANDLE left, Windows.HANDLE right)
			{
				return left.Value >= right.Value;
			}

			// Token: 0x06002FCB RID: 12235 RVA: 0x000A5979 File Offset: 0x000A3B79
			public unsafe static explicit operator Windows.HANDLE(void* value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FCC RID: 12236 RVA: 0x000A5981 File Offset: 0x000A3B81
			public unsafe static implicit operator void*(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FCD RID: 12237 RVA: 0x000A5989 File Offset: 0x000A3B89
			public static explicit operator Windows.HANDLE(byte value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FCE RID: 12238 RVA: 0x000A5992 File Offset: 0x000A3B92
			public static explicit operator byte(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FCF RID: 12239 RVA: 0x000A599B File Offset: 0x000A3B9B
			public static explicit operator Windows.HANDLE(short value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FD0 RID: 12240 RVA: 0x000A59A4 File Offset: 0x000A3BA4
			public static explicit operator short(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FD1 RID: 12241 RVA: 0x000A599B File Offset: 0x000A3B9B
			public static explicit operator Windows.HANDLE(int value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FD2 RID: 12242 RVA: 0x000A59AD File Offset: 0x000A3BAD
			public static explicit operator int(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FD3 RID: 12243 RVA: 0x000A5989 File Offset: 0x000A3B89
			public static explicit operator Windows.HANDLE(long value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FD4 RID: 12244 RVA: 0x000A59B6 File Offset: 0x000A3BB6
			public static explicit operator long(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FD5 RID: 12245 RVA: 0x000A5979 File Offset: 0x000A3B79
			public static explicit operator Windows.HANDLE([NativeInteger] IntPtr value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FD6 RID: 12246 RVA: 0x000A5981 File Offset: 0x000A3B81
			[return: NativeInteger]
			public static implicit operator IntPtr(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FD7 RID: 12247 RVA: 0x000A599B File Offset: 0x000A3B9B
			public static explicit operator Windows.HANDLE(sbyte value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FD8 RID: 12248 RVA: 0x000A59BF File Offset: 0x000A3BBF
			public static explicit operator sbyte(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FD9 RID: 12249 RVA: 0x000A5989 File Offset: 0x000A3B89
			public static explicit operator Windows.HANDLE(ushort value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FDA RID: 12250 RVA: 0x000A59C8 File Offset: 0x000A3BC8
			public static explicit operator ushort(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FDB RID: 12251 RVA: 0x000A5989 File Offset: 0x000A3B89
			public static explicit operator Windows.HANDLE(uint value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FDC RID: 12252 RVA: 0x000A59D1 File Offset: 0x000A3BD1
			public static explicit operator uint(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FDD RID: 12253 RVA: 0x000A5989 File Offset: 0x000A3B89
			public static explicit operator Windows.HANDLE(ulong value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FDE RID: 12254 RVA: 0x000A59B6 File Offset: 0x000A3BB6
			public static explicit operator ulong(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FDF RID: 12255 RVA: 0x000A5979 File Offset: 0x000A3B79
			public static explicit operator Windows.HANDLE([NativeInteger] UIntPtr value)
			{
				return new Windows.HANDLE(value);
			}

			// Token: 0x06002FE0 RID: 12256 RVA: 0x000A5981 File Offset: 0x000A3B81
			[return: NativeInteger]
			public static implicit operator UIntPtr(Windows.HANDLE value)
			{
				return value.Value;
			}

			// Token: 0x06002FE1 RID: 12257 RVA: 0x000A59DC File Offset: 0x000A3BDC
			[NullableContext(2)]
			public int CompareTo(object obj)
			{
				if (obj is Windows.HANDLE)
				{
					Windows.HANDLE other = (Windows.HANDLE)obj;
					return this.CompareTo(other);
				}
				if (obj != null)
				{
					throw new ArgumentException("obj is not an instance of HANDLE.");
				}
				return 1;
			}

			// Token: 0x06002FE2 RID: 12258 RVA: 0x000A5A10 File Offset: 0x000A3C10
			public int CompareTo(Windows.HANDLE other)
			{
				if (sizeof(IntPtr) != 4)
				{
					return this.Value.CompareTo(other.Value);
				}
				return this.Value.CompareTo(other.Value);
			}

			// Token: 0x06002FE3 RID: 12259 RVA: 0x000A5A54 File Offset: 0x000A3C54
			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				if (obj is Windows.HANDLE)
				{
					Windows.HANDLE other = (Windows.HANDLE)obj;
					return this.Equals(other);
				}
				return false;
			}

			// Token: 0x06002FE4 RID: 12260 RVA: 0x000A5A7C File Offset: 0x000A3C7C
			public bool Equals(Windows.HANDLE other)
			{
				return this.Value.Equals(other.Value);
			}

			// Token: 0x06002FE5 RID: 12261 RVA: 0x000A5AA4 File Offset: 0x000A3CA4
			public override int GetHashCode()
			{
				return this.Value.GetHashCode();
			}

			// Token: 0x06002FE6 RID: 12262 RVA: 0x000A5AC0 File Offset: 0x000A3CC0
			[NullableContext(1)]
			public override string ToString()
			{
				if (sizeof(UIntPtr) != 4)
				{
					return this.Value.ToString("X16", null);
				}
				return this.Value.ToString("X8", null);
			}

			// Token: 0x06002FE7 RID: 12263 RVA: 0x000A5B04 File Offset: 0x000A3D04
			[NullableContext(2)]
			[return: Nullable(1)]
			public string ToString(string format, IFormatProvider formatProvider)
			{
				if (sizeof(IntPtr) != 4)
				{
					return this.Value.ToString(format, formatProvider);
				}
				return this.Value.ToString(format, formatProvider);
			}

			// Token: 0x04003BE0 RID: 15328
			public unsafe readonly void* Value;
		}

		// Token: 0x020008F6 RID: 2294
		public readonly struct HMODULE : IComparable, IComparable<Windows.HMODULE>, IEquatable<Windows.HMODULE>, IFormattable
		{
			// Token: 0x06002FE8 RID: 12264 RVA: 0x000A5B3D File Offset: 0x000A3D3D
			public unsafe HMODULE(void* value)
			{
				this.Value = value;
			}

			// Token: 0x17000863 RID: 2147
			// (get) Token: 0x06002FE9 RID: 12265 RVA: 0x000A5B46 File Offset: 0x000A3D46
			public static Windows.HMODULE INVALID_VALUE
			{
				get
				{
					return new Windows.HMODULE(-1);
				}
			}

			// Token: 0x17000864 RID: 2148
			// (get) Token: 0x06002FEA RID: 12266 RVA: 0x000A5B4F File Offset: 0x000A3D4F
			public static Windows.HMODULE NULL
			{
				get
				{
					return new Windows.HMODULE(null);
				}
			}

			// Token: 0x06002FEB RID: 12267 RVA: 0x000A5B58 File Offset: 0x000A3D58
			public static bool operator ==(Windows.HMODULE left, Windows.HMODULE right)
			{
				return left.Value == right.Value;
			}

			// Token: 0x06002FEC RID: 12268 RVA: 0x000A5B68 File Offset: 0x000A3D68
			public static bool operator !=(Windows.HMODULE left, Windows.HMODULE right)
			{
				return left.Value != right.Value;
			}

			// Token: 0x06002FED RID: 12269 RVA: 0x000A5B7B File Offset: 0x000A3D7B
			public static bool operator <(Windows.HMODULE left, Windows.HMODULE right)
			{
				return left.Value < right.Value;
			}

			// Token: 0x06002FEE RID: 12270 RVA: 0x000A5B8B File Offset: 0x000A3D8B
			public static bool operator <=(Windows.HMODULE left, Windows.HMODULE right)
			{
				return left.Value == right.Value;
			}

			// Token: 0x06002FEF RID: 12271 RVA: 0x000A5B9E File Offset: 0x000A3D9E
			public static bool operator >(Windows.HMODULE left, Windows.HMODULE right)
			{
				return left.Value != right.Value;
			}

			// Token: 0x06002FF0 RID: 12272 RVA: 0x000A5BAE File Offset: 0x000A3DAE
			public static bool operator >=(Windows.HMODULE left, Windows.HMODULE right)
			{
				return left.Value >= right.Value;
			}

			// Token: 0x06002FF1 RID: 12273 RVA: 0x000A5BC1 File Offset: 0x000A3DC1
			public unsafe static explicit operator Windows.HMODULE(void* value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06002FF2 RID: 12274 RVA: 0x000A5BC9 File Offset: 0x000A3DC9
			public unsafe static implicit operator void*(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06002FF3 RID: 12275 RVA: 0x000A5BD1 File Offset: 0x000A3DD1
			public static explicit operator Windows.HMODULE(Windows.HANDLE value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06002FF4 RID: 12276 RVA: 0x000A5BDE File Offset: 0x000A3DDE
			public static implicit operator Windows.HANDLE(Windows.HMODULE value)
			{
				return new Windows.HANDLE(value.Value);
			}

			// Token: 0x06002FF5 RID: 12277 RVA: 0x000A5BEB File Offset: 0x000A3DEB
			public static explicit operator Windows.HMODULE(byte value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06002FF6 RID: 12278 RVA: 0x000A5BF4 File Offset: 0x000A3DF4
			public static explicit operator byte(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06002FF7 RID: 12279 RVA: 0x000A5BFD File Offset: 0x000A3DFD
			public static explicit operator Windows.HMODULE(short value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06002FF8 RID: 12280 RVA: 0x000A5C06 File Offset: 0x000A3E06
			public static explicit operator short(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06002FF9 RID: 12281 RVA: 0x000A5BFD File Offset: 0x000A3DFD
			public static explicit operator Windows.HMODULE(int value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06002FFA RID: 12282 RVA: 0x000A5C0F File Offset: 0x000A3E0F
			public static explicit operator int(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06002FFB RID: 12283 RVA: 0x000A5BEB File Offset: 0x000A3DEB
			public static explicit operator Windows.HMODULE(long value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06002FFC RID: 12284 RVA: 0x000A5C18 File Offset: 0x000A3E18
			public static explicit operator long(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06002FFD RID: 12285 RVA: 0x000A5BC1 File Offset: 0x000A3DC1
			public static explicit operator Windows.HMODULE([NativeInteger] IntPtr value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06002FFE RID: 12286 RVA: 0x000A5BC9 File Offset: 0x000A3DC9
			[return: NativeInteger]
			public static implicit operator IntPtr(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06002FFF RID: 12287 RVA: 0x000A5BFD File Offset: 0x000A3DFD
			public static explicit operator Windows.HMODULE(sbyte value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06003000 RID: 12288 RVA: 0x000A5C21 File Offset: 0x000A3E21
			public static explicit operator sbyte(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06003001 RID: 12289 RVA: 0x000A5BEB File Offset: 0x000A3DEB
			public static explicit operator Windows.HMODULE(ushort value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06003002 RID: 12290 RVA: 0x000A5C2A File Offset: 0x000A3E2A
			public static explicit operator ushort(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06003003 RID: 12291 RVA: 0x000A5BEB File Offset: 0x000A3DEB
			public static explicit operator Windows.HMODULE(uint value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06003004 RID: 12292 RVA: 0x000A5C33 File Offset: 0x000A3E33
			public static explicit operator uint(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06003005 RID: 12293 RVA: 0x000A5BEB File Offset: 0x000A3DEB
			public static explicit operator Windows.HMODULE(ulong value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06003006 RID: 12294 RVA: 0x000A5C18 File Offset: 0x000A3E18
			public static explicit operator ulong(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06003007 RID: 12295 RVA: 0x000A5BC1 File Offset: 0x000A3DC1
			public static explicit operator Windows.HMODULE([NativeInteger] UIntPtr value)
			{
				return new Windows.HMODULE(value);
			}

			// Token: 0x06003008 RID: 12296 RVA: 0x000A5BC9 File Offset: 0x000A3DC9
			[return: NativeInteger]
			public static implicit operator UIntPtr(Windows.HMODULE value)
			{
				return value.Value;
			}

			// Token: 0x06003009 RID: 12297 RVA: 0x000A5C3C File Offset: 0x000A3E3C
			[NullableContext(2)]
			public int CompareTo(object obj)
			{
				if (obj is Windows.HMODULE)
				{
					Windows.HMODULE other = (Windows.HMODULE)obj;
					return this.CompareTo(other);
				}
				if (obj != null)
				{
					throw new ArgumentException("obj is not an instance of HMODULE.");
				}
				return 1;
			}

			// Token: 0x0600300A RID: 12298 RVA: 0x000A5C70 File Offset: 0x000A3E70
			public int CompareTo(Windows.HMODULE other)
			{
				if (sizeof(IntPtr) != 4)
				{
					return this.Value.CompareTo(other.Value);
				}
				return this.Value.CompareTo(other.Value);
			}

			// Token: 0x0600300B RID: 12299 RVA: 0x000A5CB4 File Offset: 0x000A3EB4
			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				if (obj is Windows.HMODULE)
				{
					Windows.HMODULE other = (Windows.HMODULE)obj;
					return this.Equals(other);
				}
				return false;
			}

			// Token: 0x0600300C RID: 12300 RVA: 0x000A5CDC File Offset: 0x000A3EDC
			public bool Equals(Windows.HMODULE other)
			{
				return this.Value.Equals(other.Value);
			}

			// Token: 0x0600300D RID: 12301 RVA: 0x000A5D04 File Offset: 0x000A3F04
			public override int GetHashCode()
			{
				return this.Value.GetHashCode();
			}

			// Token: 0x0600300E RID: 12302 RVA: 0x000A5D20 File Offset: 0x000A3F20
			[NullableContext(1)]
			public override string ToString()
			{
				if (sizeof(UIntPtr) != 4)
				{
					return this.Value.ToString("X16", null);
				}
				return this.Value.ToString("X8", null);
			}

			// Token: 0x0600300F RID: 12303 RVA: 0x000A5D64 File Offset: 0x000A3F64
			[NullableContext(2)]
			[return: Nullable(1)]
			public string ToString(string format, IFormatProvider formatProvider)
			{
				if (sizeof(IntPtr) != 4)
				{
					return this.Value.ToString(format, formatProvider);
				}
				return this.Value.ToString(format, formatProvider);
			}

			// Token: 0x04003BE1 RID: 15329
			public unsafe readonly void* Value;
		}
	}
}
