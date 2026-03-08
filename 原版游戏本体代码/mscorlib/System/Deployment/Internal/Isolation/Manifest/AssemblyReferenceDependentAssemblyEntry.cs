using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006EF RID: 1775
	[StructLayout(LayoutKind.Sequential)]
	internal class AssemblyReferenceDependentAssemblyEntry : IDisposable
	{
		// Token: 0x060050B3 RID: 20659 RVA: 0x0011DDD0 File Offset: 0x0011BFD0
		~AssemblyReferenceDependentAssemblyEntry()
		{
			this.Dispose(false);
		}

		// Token: 0x060050B4 RID: 20660 RVA: 0x0011DE00 File Offset: 0x0011C000
		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x060050B5 RID: 20661 RVA: 0x0011DE09 File Offset: 0x0011C009
		[SecuritySafeCritical]
		public void Dispose(bool fDisposing)
		{
			if (this.HashValue != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.HashValue);
				this.HashValue = IntPtr.Zero;
			}
			if (fDisposing)
			{
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x0400235A RID: 9050
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Group;

		// Token: 0x0400235B RID: 9051
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Codebase;

		// Token: 0x0400235C RID: 9052
		public ulong Size;

		// Token: 0x0400235D RID: 9053
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr HashValue;

		// Token: 0x0400235E RID: 9054
		public uint HashValueSize;

		// Token: 0x0400235F RID: 9055
		public uint HashAlgorithm;

		// Token: 0x04002360 RID: 9056
		public uint Flags;

		// Token: 0x04002361 RID: 9057
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ResourceFallbackCulture;

		// Token: 0x04002362 RID: 9058
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Description;

		// Token: 0x04002363 RID: 9059
		[MarshalAs(UnmanagedType.LPWStr)]
		public string SupportUrl;

		// Token: 0x04002364 RID: 9060
		public ISection HashElements;
	}
}
