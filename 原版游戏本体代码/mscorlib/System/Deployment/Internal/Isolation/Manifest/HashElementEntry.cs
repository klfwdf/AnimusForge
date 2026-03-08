using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006D4 RID: 1748
	[StructLayout(LayoutKind.Sequential)]
	internal class HashElementEntry : IDisposable
	{
		// Token: 0x0600506E RID: 20590 RVA: 0x0011DC4C File Offset: 0x0011BE4C
		~HashElementEntry()
		{
			this.Dispose(false);
		}

		// Token: 0x0600506F RID: 20591 RVA: 0x0011DC7C File Offset: 0x0011BE7C
		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x06005070 RID: 20592 RVA: 0x0011DC88 File Offset: 0x0011BE88
		[SecuritySafeCritical]
		public void Dispose(bool fDisposing)
		{
			if (this.TransformMetadata != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.TransformMetadata);
				this.TransformMetadata = IntPtr.Zero;
			}
			if (this.DigestValue != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.DigestValue);
				this.DigestValue = IntPtr.Zero;
			}
			if (fDisposing)
			{
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x040022FA RID: 8954
		public uint index;

		// Token: 0x040022FB RID: 8955
		public byte Transform;

		// Token: 0x040022FC RID: 8956
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr TransformMetadata;

		// Token: 0x040022FD RID: 8957
		public uint TransformMetadataSize;

		// Token: 0x040022FE RID: 8958
		public byte DigestMethod;

		// Token: 0x040022FF RID: 8959
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr DigestValue;

		// Token: 0x04002300 RID: 8960
		public uint DigestValueSize;

		// Token: 0x04002301 RID: 8961
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Xml;
	}
}
