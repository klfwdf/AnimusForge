using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006A0 RID: 1696
	internal struct StoreOperationStageComponent
	{
		// Token: 0x06004FC3 RID: 20419 RVA: 0x0011C84C File Offset: 0x0011AA4C
		public void Destroy()
		{
		}

		// Token: 0x06004FC4 RID: 20420 RVA: 0x0011C84E File Offset: 0x0011AA4E
		public StoreOperationStageComponent(IDefinitionAppId app, string Manifest)
		{
			this = new StoreOperationStageComponent(app, null, Manifest);
		}

		// Token: 0x06004FC5 RID: 20421 RVA: 0x0011C859 File Offset: 0x0011AA59
		public StoreOperationStageComponent(IDefinitionAppId app, IDefinitionIdentity comp, string Manifest)
		{
			this.Size = (uint)Marshal.SizeOf(typeof(StoreOperationStageComponent));
			this.Flags = StoreOperationStageComponent.OpFlags.Nothing;
			this.Application = app;
			this.Component = comp;
			this.ManifestPath = Manifest;
		}

		// Token: 0x04002224 RID: 8740
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x04002225 RID: 8741
		[MarshalAs(UnmanagedType.U4)]
		public StoreOperationStageComponent.OpFlags Flags;

		// Token: 0x04002226 RID: 8742
		[MarshalAs(UnmanagedType.Interface)]
		public IDefinitionAppId Application;

		// Token: 0x04002227 RID: 8743
		[MarshalAs(UnmanagedType.Interface)]
		public IDefinitionIdentity Component;

		// Token: 0x04002228 RID: 8744
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ManifestPath;

		// Token: 0x02000C44 RID: 3140
		[Flags]
		public enum OpFlags
		{
			// Token: 0x04003760 RID: 14176
			Nothing = 0
		}

		// Token: 0x02000C45 RID: 3141
		public enum Disposition
		{
			// Token: 0x04003762 RID: 14178
			Failed,
			// Token: 0x04003763 RID: 14179
			Installed,
			// Token: 0x04003764 RID: 14180
			Refreshed,
			// Token: 0x04003765 RID: 14181
			AlreadyInstalled
		}
	}
}
