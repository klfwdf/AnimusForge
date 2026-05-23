using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000677 RID: 1655
	internal struct STORE_ASSEMBLY
	{
		// Token: 0x040021E9 RID: 8681
		public uint Status;

		// Token: 0x040021EA RID: 8682
		public IDefinitionIdentity DefinitionIdentity;

		// Token: 0x040021EB RID: 8683
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ManifestPath;

		// Token: 0x040021EC RID: 8684
		public ulong AssemblySize;

		// Token: 0x040021ED RID: 8685
		public ulong ChangeId;
	}
}
