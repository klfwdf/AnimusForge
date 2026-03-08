using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoMod.Utils.Interop;

namespace MonoMod.Utils
{
	// Token: 0x020008AB RID: 2219
	[NullableContext(1)]
	[Nullable(0)]
	internal static class DynDll
	{
		// Token: 0x06002D9D RID: 11677 RVA: 0x00099614 File Offset: 0x00097814
		private static DynDll.BackendImpl CreateCrossplatBackend()
		{
			OSKind os = PlatformDetection.OS;
			if (os.Is(OSKind.Windows))
			{
				return new DynDll.WindowsBackend();
			}
			if (os.Is(OSKind.Linux) || os.Is(OSKind.OSX))
			{
				return new DynDll.LinuxOSXBackend(os.Is(OSKind.Linux));
			}
			bool flag;
			MMDbgLog.DebugLogWarningStringHandler debugLogWarningStringHandler = new MMDbgLog.DebugLogWarningStringHandler(55, 1, ref flag);
			if (flag)
			{
				debugLogWarningStringHandler.AppendLiteral("Unknown OS ");
				debugLogWarningStringHandler.AppendFormatted<OSKind>(os);
				debugLogWarningStringHandler.AppendLiteral(" when setting up DynDll; assuming posix-like");
			}
			MMDbgLog.Warning(ref debugLogWarningStringHandler);
			return new DynDll.UnknownPosixBackend();
		}

		// Token: 0x06002D9E RID: 11678 RVA: 0x00099691 File Offset: 0x00097891
		[NullableContext(2)]
		public static IntPtr OpenLibrary(string name)
		{
			return DynDll.Backend.OpenLibrary(name, Assembly.GetCallingAssembly());
		}

		// Token: 0x06002D9F RID: 11679 RVA: 0x000996A3 File Offset: 0x000978A3
		[NullableContext(2)]
		public static bool TryOpenLibrary(string name, out IntPtr libraryPtr)
		{
			return DynDll.Backend.TryOpenLibrary(name, Assembly.GetCallingAssembly(), out libraryPtr);
		}

		// Token: 0x06002DA0 RID: 11680 RVA: 0x000996B6 File Offset: 0x000978B6
		public static void CloseLibrary(IntPtr lib)
		{
			DynDll.Backend.CloseLibrary(lib);
		}

		// Token: 0x06002DA1 RID: 11681 RVA: 0x000996C3 File Offset: 0x000978C3
		public static bool TryCloseLibrary(IntPtr lib)
		{
			return DynDll.Backend.TryCloseLibrary(lib);
		}

		// Token: 0x06002DA2 RID: 11682 RVA: 0x000996D0 File Offset: 0x000978D0
		public static IntPtr GetExport(this IntPtr libraryPtr, string name)
		{
			return DynDll.Backend.GetExport(libraryPtr, name);
		}

		// Token: 0x06002DA3 RID: 11683 RVA: 0x000996DE File Offset: 0x000978DE
		public static bool TryGetExport(this IntPtr libraryPtr, string name, out IntPtr functionPtr)
		{
			return DynDll.Backend.TryGetExport(libraryPtr, name, out functionPtr);
		}

		// Token: 0x04003AE8 RID: 15080
		private static readonly DynDll.BackendImpl Backend = DynDll.CreateCrossplatBackend();

		// Token: 0x020008AC RID: 2220
		[Nullable(0)]
		private abstract class BackendImpl
		{
			// Token: 0x06002DA6 RID: 11686
			protected abstract bool TryOpenLibraryCore([Nullable(2)] string name, Assembly assembly, out IntPtr handle);

			// Token: 0x06002DA7 RID: 11687
			public abstract bool TryCloseLibrary(IntPtr handle);

			// Token: 0x06002DA8 RID: 11688
			public abstract bool TryGetExport(IntPtr handle, string name, out IntPtr ptr);

			// Token: 0x06002DA9 RID: 11689
			protected abstract void CheckAndThrowError();

			// Token: 0x06002DAA RID: 11690 RVA: 0x000996FC File Offset: 0x000978FC
			public virtual bool TryOpenLibrary([Nullable(2)] string name, Assembly assembly, out IntPtr handle)
			{
				if (name != null)
				{
					foreach (string path in this.GetLibrarySearchOrder(name))
					{
						if (this.TryOpenLibraryCore(path, assembly, out handle))
						{
							return true;
						}
					}
					handle = IntPtr.Zero;
					return false;
				}
				return this.TryOpenLibraryCore(null, assembly, out handle);
			}

			// Token: 0x06002DAB RID: 11691 RVA: 0x0009976C File Offset: 0x0009796C
			protected virtual IEnumerable<string> GetLibrarySearchOrder(string name)
			{
				DynDll.BackendImpl.<GetLibrarySearchOrder>d__6 <GetLibrarySearchOrder>d__ = new DynDll.BackendImpl.<GetLibrarySearchOrder>d__6(-2);
				<GetLibrarySearchOrder>d__.<>3__name = name;
				return <GetLibrarySearchOrder>d__;
			}

			// Token: 0x06002DAC RID: 11692 RVA: 0x0009977C File Offset: 0x0009797C
			public virtual IntPtr OpenLibrary([Nullable(2)] string name, Assembly assembly)
			{
				IntPtr result;
				if (!this.TryOpenLibrary(name, assembly, out result))
				{
					this.CheckAndThrowError();
				}
				return result;
			}

			// Token: 0x06002DAD RID: 11693 RVA: 0x0009979C File Offset: 0x0009799C
			public virtual void CloseLibrary(IntPtr handle)
			{
				if (!this.TryCloseLibrary(handle))
				{
					this.CheckAndThrowError();
				}
			}

			// Token: 0x06002DAE RID: 11694 RVA: 0x000997B0 File Offset: 0x000979B0
			public virtual IntPtr GetExport(IntPtr handle, string name)
			{
				IntPtr result;
				if (!this.TryGetExport(handle, name, out result))
				{
					this.CheckAndThrowError();
				}
				return result;
			}
		}

		// Token: 0x020008AE RID: 2222
		[Nullable(0)]
		private sealed class WindowsBackend : DynDll.BackendImpl
		{
			// Token: 0x06002DB7 RID: 11703 RVA: 0x0009988C File Offset: 0x00097A8C
			protected override void CheckAndThrowError()
			{
				uint lastError = Windows.GetLastError();
				if (lastError != 0U)
				{
					throw new Win32Exception((int)lastError);
				}
			}

			// Token: 0x06002DB8 RID: 11704 RVA: 0x000998AC File Offset: 0x00097AAC
			protected unsafe override bool TryOpenLibraryCore([Nullable(2)] string name, Assembly assembly, out IntPtr handle)
			{
				IntPtr result;
				if (name == null)
				{
					result = (handle = Windows.GetModuleHandleW(null));
				}
				else
				{
					fixed (char* pinnableReference = name.AsSpan().GetPinnableReference())
					{
						char* pName = pinnableReference;
						result = (handle = Windows.LoadLibraryW((ushort*)pName));
					}
				}
				return result != IntPtr.Zero;
			}

			// Token: 0x06002DB9 RID: 11705 RVA: 0x000998FD File Offset: 0x00097AFD
			public unsafe override bool TryCloseLibrary(IntPtr handle)
			{
				return Windows.FreeLibrary(new Windows.HMODULE((void*)handle));
			}

			// Token: 0x06002DBA RID: 11706 RVA: 0x00099914 File Offset: 0x00097B14
			public unsafe override bool TryGetExport(IntPtr handle, string name, out IntPtr ptr)
			{
				byte[] array2;
				byte[] array = (array2 = Unix.MarshalToUtf8(name));
				byte* pName;
				if (array == null || array2.Length == 0)
				{
					pName = null;
				}
				else
				{
					pName = &array2[0];
				}
				IntPtr result = (ptr = Windows.GetProcAddress(new Windows.HMODULE((void*)handle), (sbyte*)pName));
				array2 = null;
				Unix.FreeMarshalledArray(array);
				return result != IntPtr.Zero;
			}

			// Token: 0x06002DBB RID: 11707 RVA: 0x00099966 File Offset: 0x00097B66
			protected override IEnumerable<string> GetLibrarySearchOrder(string name)
			{
				DynDll.WindowsBackend.<GetLibrarySearchOrder>d__4 <GetLibrarySearchOrder>d__ = new DynDll.WindowsBackend.<GetLibrarySearchOrder>d__4(-2);
				<GetLibrarySearchOrder>d__.<>3__name = name;
				return <GetLibrarySearchOrder>d__;
			}
		}

		// Token: 0x020008B0 RID: 2224
		[Nullable(0)]
		private abstract class LibdlBackend : DynDll.BackendImpl
		{
			// Token: 0x06002DC5 RID: 11717 RVA: 0x00099A8F File Offset: 0x00097C8F
			protected LibdlBackend()
			{
				Unix.DlError();
			}

			// Token: 0x06002DC6 RID: 11718 RVA: 0x00099A9D File Offset: 0x00097C9D
			[DoesNotReturn]
			private static void ThrowError(IntPtr dlerr)
			{
				throw new Win32Exception(Marshal.PtrToStringAnsi(dlerr));
			}

			// Token: 0x06002DC7 RID: 11719 RVA: 0x00099AAC File Offset: 0x00097CAC
			protected override void CheckAndThrowError()
			{
				IntPtr lastError = DynDll.LibdlBackend.lastDlErrorReturn;
				IntPtr errorCode;
				if (lastError == IntPtr.Zero)
				{
					errorCode = Unix.DlError();
				}
				else
				{
					errorCode = lastError;
					DynDll.LibdlBackend.lastDlErrorReturn = IntPtr.Zero;
				}
				if (errorCode != IntPtr.Zero)
				{
					DynDll.LibdlBackend.ThrowError(errorCode);
				}
			}

			// Token: 0x06002DC8 RID: 11720 RVA: 0x00099AF4 File Offset: 0x00097CF4
			protected override bool TryOpenLibraryCore([Nullable(2)] string name, Assembly assembly, out IntPtr handle)
			{
				Unix.DlopenFlags flags = (Unix.DlopenFlags)258;
				return (handle = Unix.DlOpen(name, flags)) != IntPtr.Zero;
			}

			// Token: 0x06002DC9 RID: 11721 RVA: 0x00099B1D File Offset: 0x00097D1D
			public override bool TryCloseLibrary(IntPtr handle)
			{
				return Unix.DlClose(handle);
			}

			// Token: 0x06002DCA RID: 11722 RVA: 0x00099B25 File Offset: 0x00097D25
			public override bool TryGetExport(IntPtr handle, string name, out IntPtr ptr)
			{
				Unix.DlError();
				ptr = Unix.DlSym(handle, name);
				return (DynDll.LibdlBackend.lastDlErrorReturn = Unix.DlError()) == IntPtr.Zero;
			}

			// Token: 0x06002DCB RID: 11723 RVA: 0x00099B4C File Offset: 0x00097D4C
			public override IntPtr GetExport(IntPtr handle, string name)
			{
				Unix.DlError();
				IntPtr result = Unix.DlSym(handle, name);
				IntPtr dlerr = Unix.DlError();
				if (dlerr != IntPtr.Zero)
				{
					DynDll.LibdlBackend.ThrowError(dlerr);
				}
				return result;
			}

			// Token: 0x04003AF3 RID: 15091
			[ThreadStatic]
			private static IntPtr lastDlErrorReturn;
		}

		// Token: 0x020008B1 RID: 2225
		[NullableContext(0)]
		private sealed class LinuxOSXBackend : DynDll.LibdlBackend
		{
			// Token: 0x06002DCC RID: 11724 RVA: 0x00099B7F File Offset: 0x00097D7F
			public LinuxOSXBackend(bool isLinux)
			{
				this.isLinux = isLinux;
			}

			// Token: 0x06002DCD RID: 11725 RVA: 0x00099B8E File Offset: 0x00097D8E
			[NullableContext(1)]
			protected override IEnumerable<string> GetLibrarySearchOrder(string name)
			{
				DynDll.LinuxOSXBackend.<GetLibrarySearchOrder>d__2 <GetLibrarySearchOrder>d__ = new DynDll.LinuxOSXBackend.<GetLibrarySearchOrder>d__2(-2);
				<GetLibrarySearchOrder>d__.<>4__this = this;
				<GetLibrarySearchOrder>d__.<>3__name = name;
				return <GetLibrarySearchOrder>d__;
			}

			// Token: 0x04003AF4 RID: 15092
			private readonly bool isLinux;
		}

		// Token: 0x020008B3 RID: 2227
		[NullableContext(0)]
		private sealed class UnknownPosixBackend : DynDll.LibdlBackend
		{
		}
	}
}
