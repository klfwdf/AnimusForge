using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using MonoMod.Utils.Interop;

namespace MonoMod.Utils
{
	// Token: 0x020008D8 RID: 2264
	internal static class PlatformDetection
	{
		// Token: 0x06002F03 RID: 12035 RVA: 0x000A21BA File Offset: 0x000A03BA
		private static void EnsurePlatformInfoInitialized()
		{
			if (PlatformDetection.platInitState != 0)
			{
				return;
			}
			ValueTuple<OSKind, ArchitectureKind> valueTuple = PlatformDetection.DetectPlatformInfo();
			PlatformDetection.os = valueTuple.Item1;
			PlatformDetection.arch = valueTuple.Item2;
			Thread.MemoryBarrier();
			Interlocked.Exchange(ref PlatformDetection.platInitState, 1);
		}

		// Token: 0x17000853 RID: 2131
		// (get) Token: 0x06002F04 RID: 12036 RVA: 0x000A21EF File Offset: 0x000A03EF
		public static OSKind OS
		{
			get
			{
				PlatformDetection.EnsurePlatformInfoInitialized();
				return PlatformDetection.os;
			}
		}

		// Token: 0x17000854 RID: 2132
		// (get) Token: 0x06002F05 RID: 12037 RVA: 0x000A21FB File Offset: 0x000A03FB
		public static ArchitectureKind Architecture
		{
			get
			{
				PlatformDetection.EnsurePlatformInfoInitialized();
				return PlatformDetection.arch;
			}
		}

		// Token: 0x06002F06 RID: 12038 RVA: 0x000A2208 File Offset: 0x000A0408
		[return: TupleElementNames(new string[] { "OS", "Arch" })]
		private static ValueTuple<OSKind, ArchitectureKind> DetectPlatformInfo()
		{
			OSKind os = OSKind.Unknown;
			ArchitectureKind arch = ArchitectureKind.Unknown;
			PropertyInfo p_Platform = typeof(Environment).GetProperty("Platform", BindingFlags.Static | BindingFlags.NonPublic);
			string platID;
			if (p_Platform != null)
			{
				object value = p_Platform.GetValue(null, null);
				platID = ((value != null) ? value.ToString() : null);
			}
			else
			{
				platID = Environment.OSVersion.Platform.ToString();
			}
			platID = ((platID != null) ? platID.ToUpperInvariant() : null) ?? "";
			if (platID.Contains("WIN", StringComparison.Ordinal))
			{
				os = OSKind.Windows;
			}
			else if (platID.Contains("MAC", StringComparison.Ordinal) || platID.Contains("OSX", StringComparison.Ordinal))
			{
				os = OSKind.OSX;
			}
			else if (platID.Contains("LIN", StringComparison.Ordinal))
			{
				os = OSKind.Linux;
			}
			else if (platID.Contains("BSD", StringComparison.Ordinal))
			{
				os = OSKind.BSD;
			}
			else if (platID.Contains("UNIX", StringComparison.Ordinal))
			{
				os = OSKind.Posix;
			}
			if (os == OSKind.Windows)
			{
				PlatformDetection.DetectInfoWindows(ref os, ref arch);
			}
			else if ((os & OSKind.Posix) != OSKind.Unknown)
			{
				PlatformDetection.DetectInfoPosix(ref os, ref arch);
			}
			if (os != OSKind.Unknown)
			{
				if (os == OSKind.Linux && Directory.Exists("/data") && File.Exists("/system/build.prop"))
				{
					os = OSKind.Android;
				}
				else if (os == OSKind.Posix && Directory.Exists("/Applications") && Directory.Exists("/System") && Directory.Exists("/User") && !Directory.Exists("/Users"))
				{
					os = OSKind.IOS;
				}
				else if (os == OSKind.Windows && PlatformDetection.CheckWine())
				{
					os = OSKind.Wine;
				}
			}
			bool flag;
			MMDbgLog.DebugLogInfoStringHandler debugLogInfoStringHandler = new MMDbgLog.DebugLogInfoStringHandler(16, 2, ref flag);
			if (flag)
			{
				debugLogInfoStringHandler.AppendLiteral("Platform info: ");
				debugLogInfoStringHandler.AppendFormatted<OSKind>(os);
				debugLogInfoStringHandler.AppendLiteral(" ");
				debugLogInfoStringHandler.AppendFormatted<ArchitectureKind>(arch);
			}
			MMDbgLog.Info(ref debugLogInfoStringHandler);
			return new ValueTuple<OSKind, ArchitectureKind>(os, arch);
		}

		// Token: 0x06002F07 RID: 12039 RVA: 0x000A23B7 File Offset: 0x000A05B7
		private unsafe static int PosixUname(OSKind os, byte* buf)
		{
			if (os != OSKind.OSX)
			{
				return PlatformDetection.<PosixUname>g__Libc|9_0(buf);
			}
			return PlatformDetection.<PosixUname>g__Osx|9_1(buf);
		}

		// Token: 0x06002F08 RID: 12040 RVA: 0x000A23CC File Offset: 0x000A05CC
		[return: Nullable(1)]
		private unsafe static string GetCString(ReadOnlySpan<byte> buffer, out int nullByte)
		{
			fixed (byte* pinnableReference = buffer.GetPinnableReference())
			{
				return Marshal.PtrToStringAnsi((IntPtr)((void*)pinnableReference), nullByte = buffer.IndexOf(0));
			}
		}

		// Token: 0x06002F09 RID: 12041 RVA: 0x000A23FC File Offset: 0x000A05FC
		private unsafe static void DetectInfoPosix(ref OSKind os, ref ArchitectureKind arch)
		{
			try
			{
				Span<byte> buffer = new byte[3078];
				bool flag;
				try
				{
					fixed (byte* ptr = buffer.GetPinnableReference())
					{
						byte* bufPtr = ptr;
						if (PlatformDetection.PosixUname(os, bufPtr) < 0)
						{
							string msg = new Win32Exception(Marshal.GetLastWin32Error()).Message;
							MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler = new MMDbgLog.DebugLogErrorStringHandler(24, 1, ref flag);
							if (flag)
							{
								debugLogErrorStringHandler.AppendLiteral("uname() syscall failed! ");
								debugLogErrorStringHandler.AppendFormatted(msg);
							}
							MMDbgLog.Error(ref debugLogErrorStringHandler);
							return;
						}
					}
				}
				finally
				{
					byte* ptr = null;
				}
				int nullByteOffs;
				string kernelName = PlatformDetection.GetCString(buffer, out nullByteOffs).ToUpperInvariant();
				buffer = buffer.Slice(nullByteOffs);
				MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new MMDbgLog.DebugLogTraceStringHandler(22, 1, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler.AppendLiteral("uname() call returned ");
					debugLogTraceStringHandler.AppendFormatted(kernelName);
				}
				MMDbgLog.Trace(ref debugLogTraceStringHandler);
				if (kernelName.Contains("LINUX", StringComparison.Ordinal))
				{
					os = OSKind.Linux;
				}
				else if (kernelName.Contains("DARWIN", StringComparison.Ordinal))
				{
					os = OSKind.OSX;
				}
				else if (kernelName.Contains("BSD", StringComparison.Ordinal))
				{
					os = OSKind.BSD;
				}
				string machineName = PlatformDetection.GetMachineNamePosix(os, buffer).ToUpperInvariant();
				if (machineName.Contains("X86_64", StringComparison.Ordinal) || machineName.Contains("AMD64", StringComparison.Ordinal))
				{
					arch = ArchitectureKind.x86_64;
				}
				else if (machineName.Contains("X86", StringComparison.Ordinal) || machineName.Contains("I686", StringComparison.Ordinal))
				{
					arch = ArchitectureKind.x86;
				}
				else if (machineName.Contains("AARCH64", StringComparison.Ordinal) || machineName.Contains("ARM64", StringComparison.Ordinal))
				{
					arch = ArchitectureKind.Arm64;
				}
				else if (machineName.Contains("ARM", StringComparison.Ordinal))
				{
					arch = ArchitectureKind.Arm;
				}
				MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler2 = new MMDbgLog.DebugLogTraceStringHandler(37, 2, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler2.AppendLiteral("uname() detected architecture info: ");
					debugLogTraceStringHandler2.AppendFormatted<OSKind>(os);
					debugLogTraceStringHandler2.AppendLiteral(" ");
					debugLogTraceStringHandler2.AppendFormatted<ArchitectureKind>(arch);
				}
				MMDbgLog.Trace(ref debugLogTraceStringHandler2);
			}
			catch (Exception e)
			{
				bool flag;
				MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler2 = new MMDbgLog.DebugLogErrorStringHandler(49, 1, ref flag);
				if (flag)
				{
					debugLogErrorStringHandler2.AppendLiteral("Error trying to detect info on POSIX-like system ");
					debugLogErrorStringHandler2.AppendFormatted<Exception>(e);
				}
				MMDbgLog.Error(ref debugLogErrorStringHandler2);
			}
		}

		// Token: 0x06002F0A RID: 12042 RVA: 0x000A2628 File Offset: 0x000A0828
		[return: Nullable(1)]
		private unsafe static string GetMachineNamePosix(OSKind os, Span<byte> unameBuffer)
		{
			string machineName = null;
			bool flag;
			if (os == OSKind.Linux)
			{
				IntPtr getAuxVal;
				if (DynDll.OpenLibrary("libc").TryGetExport("getauxval", out getAuxVal))
				{
					method system.IntPtr_u0020(System.IntPtr) = (void*)getAuxVal;
					IntPtr result = calli(System.IntPtr(System.IntPtr), (IntPtr)15, system.IntPtr_u0020(System.IntPtr));
					if (result != 0)
					{
						machineName = Marshal.PtrToStringAnsi(result);
						MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new MMDbgLog.DebugLogTraceStringHandler(35, 1, ref flag);
						if (flag)
						{
							debugLogTraceStringHandler.AppendLiteral("Got architecture from getauxval(): ");
							debugLogTraceStringHandler.AppendFormatted(machineName);
						}
						MMDbgLog.Trace(ref debugLogTraceStringHandler);
					}
				}
				if (machineName == null)
				{
					try
					{
						Span<Unix.LinuxAuxvEntry> auxv = MemoryMarshal.Cast<byte, Unix.LinuxAuxvEntry>(Helpers.ReadAllBytes("/proc/self/auxv").AsSpan<byte>());
						machineName = string.Empty;
						Span<Unix.LinuxAuxvEntry> span = auxv;
						for (int k = 0; k < span.Length; k++)
						{
							Unix.LinuxAuxvEntry entry = *span[k];
							if (entry.Key == (IntPtr)15)
							{
								machineName = Marshal.PtrToStringAnsi(entry.Value) ?? string.Empty;
								break;
							}
						}
						if (machineName.Length == 0)
						{
							MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new MMDbgLog.DebugLogWarningStringHandler(56, 1, ref flag);
							if (flag)
							{
								debugLogWarningStringHandler.AppendLiteral("Auxv table did not inlcude useful AT_PLATFORM (0x");
								debugLogWarningStringHandler.AppendFormatted<int>(15, "x");
								debugLogWarningStringHandler.AppendLiteral(") entry");
							}
							MMDbgLog.Warning(ref debugLogWarningStringHandler);
							Span<Unix.LinuxAuxvEntry> span2 = auxv;
							for (int k = 0; k < span2.Length; k++)
							{
								Unix.LinuxAuxvEntry entry2 = *span2[k];
								MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler2 = new MMDbgLog.DebugLogTraceStringHandler(3, 2, ref flag);
								if (flag)
								{
									debugLogTraceStringHandler2.AppendFormatted<IntPtr>(entry2.Key, "x16");
									debugLogTraceStringHandler2.AppendLiteral(" = ");
									debugLogTraceStringHandler2.AppendFormatted<IntPtr>(entry2.Value, "x16");
								}
								MMDbgLog.Trace(ref debugLogTraceStringHandler2);
							}
							machineName = null;
						}
						else
						{
							MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler3 = new MMDbgLog.DebugLogTraceStringHandler(43, 1, ref flag);
							if (flag)
							{
								debugLogTraceStringHandler3.AppendLiteral("Got architecture name ");
								debugLogTraceStringHandler3.AppendFormatted(machineName);
								debugLogTraceStringHandler3.AppendLiteral(" from /proc/self/auxv");
							}
							MMDbgLog.Trace(ref debugLogTraceStringHandler3);
						}
					}
					catch (UnauthorizedAccessException ex)
					{
						MMDbgLog.Warning("Could not read /proc/self/auxv, and libc does not have getauxval");
						MMDbgLog.Warning("Falling back to parsing out of uname() result...");
						MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler2 = new MMDbgLog.DebugLogWarningStringHandler(0, 1, ref flag);
						if (flag)
						{
							debugLogWarningStringHandler2.AppendFormatted<UnauthorizedAccessException>(ex);
						}
						MMDbgLog.Warning(ref debugLogWarningStringHandler2);
					}
				}
			}
			if (machineName == null)
			{
				for (int i = 0; i < 4; i++)
				{
					if (i != 0)
					{
						int nullByteOffs = unameBuffer.IndexOf(0);
						unameBuffer = unameBuffer.Slice(nullByteOffs);
						if (i == 1 && nullByteOffs < 5 && unameBuffer.Length >= 2 && *unameBuffer[1] != 0)
						{
							nullByteOffs = unameBuffer.Slice(1).IndexOf(0);
							unameBuffer = unameBuffer.Slice(nullByteOffs + 1);
						}
					}
					int j = 0;
					while (j < unameBuffer.Length && *unameBuffer[j] == 0)
					{
						j++;
					}
					unameBuffer = unameBuffer.Slice(j);
				}
				int k;
				machineName = PlatformDetection.GetCString(unameBuffer, out k);
				MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler4 = new MMDbgLog.DebugLogTraceStringHandler(35, 1, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler4.AppendLiteral("Got architecture name ");
					debugLogTraceStringHandler4.AppendFormatted(machineName);
					debugLogTraceStringHandler4.AppendLiteral(" from uname()");
				}
				MMDbgLog.Trace(ref debugLogTraceStringHandler4);
			}
			return machineName;
		}

		// Token: 0x06002F0B RID: 12043 RVA: 0x000A293C File Offset: 0x000A0B3C
		private unsafe static void DetectInfoWindows(ref OSKind os, ref ArchitectureKind arch)
		{
			Windows.SYSTEM_INFO sysInfo;
			Windows.GetSystemInfo(&sysInfo);
			ushort x = sysInfo.Anonymous.Anonymous.wProcessorArchitecture;
			ArchitectureKind architectureKind;
			if (x != 0)
			{
				switch (x)
				{
				case 5:
					architectureKind = ArchitectureKind.Arm;
					goto IL_85;
				case 6:
					throw new PlatformNotSupportedException("You're running .NET on an Itanium device!?!?");
				case 7:
				case 8:
					break;
				case 9:
					architectureKind = ArchitectureKind.x86_64;
					goto IL_85;
				default:
					if (x == 12)
					{
						architectureKind = ArchitectureKind.Arm64;
						goto IL_85;
					}
					break;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unknown Windows processor architecture ");
				defaultInterpolatedStringHandler.AppendFormatted<ushort>(x);
				throw new PlatformNotSupportedException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			architectureKind = ArchitectureKind.x86;
			IL_85:
			arch = architectureKind;
		}

		// Token: 0x06002F0C RID: 12044 RVA: 0x000A29D4 File Offset: 0x000A0BD4
		private unsafe static bool CheckWine()
		{
			bool runningWine;
			if (Switches.TryGetSwitchEnabled("RunningOnWine", out runningWine))
			{
				return runningWine;
			}
			string environmentVariable = Environment.GetEnvironmentVariable("XL_WINEONLINUX");
			string env = ((environmentVariable != null) ? environmentVariable.ToUpperInvariant() : null);
			if (env == "TRUE")
			{
				return true;
			}
			if (env == "FALSE")
			{
				return false;
			}
			fixed (char* pinnableReference = "ntdll.dll".AsSpan().GetPinnableReference())
			{
				Windows.HMODULE ntdll = Windows.GetModuleHandleW((ushort*)pinnableReference);
				if (ntdll != Windows.HMODULE.NULL && ntdll != Windows.HMODULE.INVALID_VALUE)
				{
					fixed (byte* pinnableReference2 = new ReadOnlySpan<byte>((void*)(&<PrivateImplementationDetails>.0A3EBE02DD250439043520A24AEF10F9F051F5747BD28A93500A5C734CC975A9), 14).GetPinnableReference())
					{
						byte* pWineGetVersion = pinnableReference2;
						if (Windows.GetProcAddress(ntdll, (sbyte*)pWineGetVersion) != IntPtr.Zero)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x06002F0D RID: 12045 RVA: 0x000A2A9C File Offset: 0x000A0C9C
		[MemberNotNull("runtimeVersion")]
		private static void EnsureRuntimeInitialized()
		{
			if (PlatformDetection.runtimeInitState == 0)
			{
				ValueTuple<RuntimeKind, CorelibKind, Version> valueTuple = PlatformDetection.DetermineRuntimeInfo();
				PlatformDetection.runtime = valueTuple.Item1;
				PlatformDetection.corelib = valueTuple.Item2;
				PlatformDetection.runtimeVersion = valueTuple.Item3;
				Thread.MemoryBarrier();
				Interlocked.Exchange(ref PlatformDetection.runtimeInitState, 1);
				return;
			}
			if (PlatformDetection.runtimeVersion == null)
			{
				throw new InvalidOperationException("Despite runtimeInitState being set, runtimeVersion was somehow null");
			}
		}

		// Token: 0x17000855 RID: 2133
		// (get) Token: 0x06002F0E RID: 12046 RVA: 0x000A2AF9 File Offset: 0x000A0CF9
		public static RuntimeKind Runtime
		{
			get
			{
				PlatformDetection.EnsureRuntimeInitialized();
				return PlatformDetection.runtime;
			}
		}

		// Token: 0x17000856 RID: 2134
		// (get) Token: 0x06002F0F RID: 12047 RVA: 0x000A2B05 File Offset: 0x000A0D05
		public static CorelibKind Corelib
		{
			get
			{
				PlatformDetection.EnsureRuntimeInitialized();
				return PlatformDetection.corelib;
			}
		}

		// Token: 0x17000857 RID: 2135
		// (get) Token: 0x06002F10 RID: 12048 RVA: 0x000A2B11 File Offset: 0x000A0D11
		[Nullable(1)]
		public static Version RuntimeVersion
		{
			[NullableContext(1)]
			get
			{
				PlatformDetection.EnsureRuntimeInitialized();
				return PlatformDetection.runtimeVersion;
			}
		}

		// Token: 0x06002F11 RID: 12049 RVA: 0x000A2B20 File Offset: 0x000A0D20
		[return: TupleElementNames(new string[] { "Rt", "Cor", "Ver" })]
		[return: Nullable(new byte[] { 0, 1 })]
		private static ValueTuple<RuntimeKind, CorelibKind, Version> DetermineRuntimeInfo()
		{
			Version version = null;
			bool isMono = Type.GetType("Mono.Runtime") != null || Type.GetType("Mono.RuntimeStructs") != null;
			bool isCoreBcl = typeof(object).Assembly.GetName().Name == "System.Private.CoreLib";
			CorelibKind corelib = (isCoreBcl ? CorelibKind.Core : CorelibKind.Framework);
			RuntimeKind runtime;
			if (isMono)
			{
				runtime = RuntimeKind.Mono;
			}
			else if (isCoreBcl && !isMono)
			{
				runtime = RuntimeKind.CoreCLR;
			}
			else
			{
				runtime = RuntimeKind.Framework;
			}
			bool flag;
			MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new MMDbgLog.DebugLogTraceStringHandler(21, 2, ref flag);
			if (flag)
			{
				debugLogTraceStringHandler.AppendLiteral("IsMono: ");
				debugLogTraceStringHandler.AppendFormatted<bool>(isMono);
				debugLogTraceStringHandler.AppendLiteral(", IsCoreBcl: ");
				debugLogTraceStringHandler.AppendFormatted<bool>(isCoreBcl);
			}
			MMDbgLog.Trace(ref debugLogTraceStringHandler);
			Version sysVer = Environment.Version;
			MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler2 = new MMDbgLog.DebugLogTraceStringHandler(25, 1, ref flag);
			if (flag)
			{
				debugLogTraceStringHandler2.AppendLiteral("Returned system version: ");
				debugLogTraceStringHandler2.AppendFormatted<Version>(sysVer);
			}
			MMDbgLog.Trace(ref debugLogTraceStringHandler2);
			Type rti = Type.GetType("System.Runtime.InteropServices.RuntimeInformation");
			if (rti == null)
			{
				rti = Type.GetType("System.Runtime.InteropServices.RuntimeInformation, System.Runtime.InteropServices.RuntimeInformation");
			}
			object obj;
			if (rti == null)
			{
				obj = null;
			}
			else
			{
				PropertyInfo property = rti.GetProperty("FrameworkDescription");
				obj = ((property != null) ? property.GetValue(null, null) : null);
			}
			string fxDesc = (string)obj;
			MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler3 = new MMDbgLog.DebugLogTraceStringHandler(22, 1, ref flag);
			if (flag)
			{
				debugLogTraceStringHandler3.AppendLiteral("FrameworkDescription: ");
				debugLogTraceStringHandler3.AppendFormatted(fxDesc ?? "(null)");
			}
			MMDbgLog.Trace(ref debugLogTraceStringHandler3);
			if (fxDesc != null)
			{
				int prefixLength;
				if (fxDesc.StartsWith("Mono ", StringComparison.Ordinal))
				{
					runtime = RuntimeKind.Mono;
					prefixLength = "Mono ".Length;
				}
				else if (fxDesc.StartsWith(".NET Core ", StringComparison.Ordinal))
				{
					runtime = RuntimeKind.CoreCLR;
					prefixLength = ".NET Core ".Length;
				}
				else if (fxDesc.StartsWith(".NET Framework ", StringComparison.Ordinal))
				{
					runtime = RuntimeKind.Framework;
					prefixLength = ".NET Framework ".Length;
				}
				else if (fxDesc.StartsWith(".NET ", StringComparison.Ordinal))
				{
					runtime = (isMono ? RuntimeKind.Mono : RuntimeKind.CoreCLR);
					prefixLength = ".NET ".Length;
				}
				else
				{
					runtime = RuntimeKind.Unknown;
					prefixLength = fxDesc.Length;
				}
				int space = fxDesc.IndexOfAny(new char[] { ' ', '-' }, prefixLength);
				if (space < 0)
				{
					space = fxDesc.Length;
				}
				string versionString = fxDesc.Substring(prefixLength, space - prefixLength);
				try
				{
					version = new Version(versionString);
				}
				catch (Exception e)
				{
					MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler = new MMDbgLog.DebugLogErrorStringHandler(61, 2, ref flag);
					if (flag)
					{
						debugLogErrorStringHandler.AppendLiteral("Invalid version string pulled from FrameworkDescription ('");
						debugLogErrorStringHandler.AppendFormatted(fxDesc);
						debugLogErrorStringHandler.AppendLiteral("') ");
						debugLogErrorStringHandler.AppendFormatted<Exception>(e);
					}
					MMDbgLog.Error(ref debugLogErrorStringHandler);
				}
			}
			if (runtime == RuntimeKind.Framework && version == null)
			{
				version = sysVer;
			}
			MMDbgLog.DebugLogInfoStringHandler debugLogInfoStringHandler = new MMDbgLog.DebugLogInfoStringHandler(34, 3, ref flag);
			if (flag)
			{
				debugLogInfoStringHandler.AppendLiteral("Detected runtime: ");
				debugLogInfoStringHandler.AppendFormatted<RuntimeKind>(runtime);
				debugLogInfoStringHandler.AppendLiteral(" ");
				debugLogInfoStringHandler.AppendFormatted(((version != null) ? version.ToString() : null) ?? "(null)");
				debugLogInfoStringHandler.AppendLiteral(" using ");
				debugLogInfoStringHandler.AppendFormatted<CorelibKind>(corelib);
				debugLogInfoStringHandler.AppendLiteral(" corelib");
			}
			MMDbgLog.Info(ref debugLogInfoStringHandler);
			return new ValueTuple<RuntimeKind, CorelibKind, Version>(runtime, corelib, version ?? new Version(0, 0));
		}

		// Token: 0x06002F12 RID: 12050 RVA: 0x000A2E40 File Offset: 0x000A1040
		[CompilerGenerated]
		internal unsafe static int <PosixUname>g__Libc|9_0(byte* buf)
		{
			return Unix.Uname(buf);
		}

		// Token: 0x06002F13 RID: 12051 RVA: 0x000A2E48 File Offset: 0x000A1048
		[CompilerGenerated]
		internal unsafe static int <PosixUname>g__Osx|9_1(byte* buf)
		{
			return OSX.Uname(buf);
		}

		// Token: 0x04003B66 RID: 15206
		private static int platInitState;

		// Token: 0x04003B67 RID: 15207
		private static OSKind os;

		// Token: 0x04003B68 RID: 15208
		private static ArchitectureKind arch;

		// Token: 0x04003B69 RID: 15209
		private static int runtimeInitState;

		// Token: 0x04003B6A RID: 15210
		private static RuntimeKind runtime;

		// Token: 0x04003B6B RID: 15211
		private static CorelibKind corelib;

		// Token: 0x04003B6C RID: 15212
		[Nullable(2)]
		private static Version runtimeVersion;
	}
}
