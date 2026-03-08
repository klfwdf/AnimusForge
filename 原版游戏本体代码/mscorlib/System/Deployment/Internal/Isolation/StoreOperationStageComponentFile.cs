using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006A1 RID: 1697
	internal struct StoreOperationStageComponentFile
	{
		// Token: 0x06004FC6 RID: 20422 RVA: 0x0011C88C File Offset: 0x0011AA8C
		public StoreOperationStageComponentFile(IDefinitionAppId App, string CompRelPath, string SrcFile)
		{
			this = new StoreOperationStageComponentFile(App, null, CompRelPath, SrcFile);
		}

		// Token: 0x06004FC7 RID: 20423 RVA: 0x0011C898 File Offset: 0x0011AA98
		public StoreOperationStageComponentFile(IDefinitionAppId App, IDefinitionIdentity Component, string CompRelPath, string SrcFile)
		{
			this.Size = (uint)Marshal.SizeOf(typeof(StoreOperationStageComponentFile));
			this.Flags = StoreOperationStageComponentFile.OpFlags.Nothing;
			this.Application = App;
			this.Component = Component;
			this.ComponentRelativePath = CompRelPath;
			this.SourceFilePath = SrcFile;
		}

		// Token: 0x06004FC8 RID: 20424 RVA: 0x0011C8D3 File Offset: 0x0011AAD3
		public void Destroy()
		{
		}

		// Token: 0x04002229 RID: 8745
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x0400222A RID: 8746
		[MarshalAs(UnmanagedType.U4)]
		public StoreOperationStageComponentFile.OpFlags Flags;

		// Token: 0x0400222B RID: 8747
		[MarshalAs(UnmanagedType.Interface)]
		public IDefinitionAppId Application;

		// Token: 0x0400222C RID: 8748
		[MarshalAs(UnmanagedType.Interface)]
		public IDefinitionIdentity Component;

		// Token: 0x0400222D RID: 8749
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ComponentRelativePath;

		// Token: 0x0400222E RID: 8750
		[MarshalAs(UnmanagedType.LPWStr)]
		public string SourceFilePath;

		// Token: 0x02000C46 RID: 3142
		[Flags]
		public enum OpFlags
		{
			// Token: 0x04003767 RID: 14183
			Nothing = 0
		}

		// Token: 0x02000C47 RID: 3143
		public enum Disposition
		{
			// Token: 0x04003769 RID: 14185
			Failed,
			// Token: 0x0400376A RID: 14186
			Installed,
			// Token: 0x0400376B RID: 14187
			Refreshed,
			// Token: 0x0400376C RID: 14188
			AlreadyInstalled
		}
	}
}
