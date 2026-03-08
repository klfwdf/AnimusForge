using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006D7 RID: 1751
	[StructLayout(LayoutKind.Sequential)]
	internal class FileEntry : IDisposable
	{
		// Token: 0x06005079 RID: 20601 RVA: 0x0011DCF8 File Offset: 0x0011BEF8
		~FileEntry()
		{
			this.Dispose(false);
		}

		// Token: 0x0600507A RID: 20602 RVA: 0x0011DD28 File Offset: 0x0011BF28
		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x0600507B RID: 20603 RVA: 0x0011DD34 File Offset: 0x0011BF34
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
				if (this.MuiMapping != null)
				{
					this.MuiMapping.Dispose(true);
					this.MuiMapping = null;
				}
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x0400230A RID: 8970
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Name;

		// Token: 0x0400230B RID: 8971
		public uint HashAlgorithm;

		// Token: 0x0400230C RID: 8972
		[MarshalAs(UnmanagedType.LPWStr)]
		public string LoadFrom;

		// Token: 0x0400230D RID: 8973
		[MarshalAs(UnmanagedType.LPWStr)]
		public string SourcePath;

		// Token: 0x0400230E RID: 8974
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ImportPath;

		// Token: 0x0400230F RID: 8975
		[MarshalAs(UnmanagedType.LPWStr)]
		public string SourceName;

		// Token: 0x04002310 RID: 8976
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Location;

		// Token: 0x04002311 RID: 8977
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr HashValue;

		// Token: 0x04002312 RID: 8978
		public uint HashValueSize;

		// Token: 0x04002313 RID: 8979
		public ulong Size;

		// Token: 0x04002314 RID: 8980
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Group;

		// Token: 0x04002315 RID: 8981
		public uint Flags;

		// Token: 0x04002316 RID: 8982
		public MuiResourceMapEntry MuiMapping;

		// Token: 0x04002317 RID: 8983
		public uint WritableType;

		// Token: 0x04002318 RID: 8984
		public ISection HashElements;
	}
}
