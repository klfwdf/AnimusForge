using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006A7 RID: 1703
	internal struct StoreOperationMetadataProperty
	{
		// Token: 0x06004FD6 RID: 20438 RVA: 0x0011CA9E File Offset: 0x0011AC9E
		public StoreOperationMetadataProperty(Guid PropertySet, string Name)
		{
			this = new StoreOperationMetadataProperty(PropertySet, Name, null);
		}

		// Token: 0x06004FD7 RID: 20439 RVA: 0x0011CAA9 File Offset: 0x0011ACA9
		public StoreOperationMetadataProperty(Guid PropertySet, string Name, string Value)
		{
			this.GuidPropertySet = PropertySet;
			this.Name = Name;
			this.Value = Value;
			this.ValueSize = ((Value != null) ? new IntPtr((Value.Length + 1) * 2) : IntPtr.Zero);
		}

		// Token: 0x04002245 RID: 8773
		public Guid GuidPropertySet;

		// Token: 0x04002246 RID: 8774
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Name;

		// Token: 0x04002247 RID: 8775
		[MarshalAs(UnmanagedType.SysUInt)]
		public IntPtr ValueSize;

		// Token: 0x04002248 RID: 8776
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Value;
	}
}
