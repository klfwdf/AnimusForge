using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006DC RID: 1756
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("0C66F299-E08E-48c5-9264-7CCBEB4D5CBB")]
	[ComImport]
	internal interface IFileAssociationEntry
	{
		// Token: 0x17000CE6 RID: 3302
		// (get) Token: 0x0600508D RID: 20621
		FileAssociationEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CE7 RID: 3303
		// (get) Token: 0x0600508E RID: 20622
		string Extension
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000CE8 RID: 3304
		// (get) Token: 0x0600508F RID: 20623
		string Description
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000CE9 RID: 3305
		// (get) Token: 0x06005090 RID: 20624
		string ProgID
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000CEA RID: 3306
		// (get) Token: 0x06005091 RID: 20625
		string DefaultIcon
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}

		// Token: 0x17000CEB RID: 3307
		// (get) Token: 0x06005092 RID: 20626
		string Parameter
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}
	}
}
