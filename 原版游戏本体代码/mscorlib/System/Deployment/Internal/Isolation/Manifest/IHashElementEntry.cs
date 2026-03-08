using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x020006D6 RID: 1750
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("9D46FB70-7B54-4f4f-9331-BA9E87833FF5")]
	[ComImport]
	internal interface IHashElementEntry
	{
		// Token: 0x17000CD0 RID: 3280
		// (get) Token: 0x06005072 RID: 20594
		HashElementEntry AllData
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CD1 RID: 3281
		// (get) Token: 0x06005073 RID: 20595
		uint index
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CD2 RID: 3282
		// (get) Token: 0x06005074 RID: 20596
		byte Transform
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CD3 RID: 3283
		// (get) Token: 0x06005075 RID: 20597
		object TransformMetadata
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}

		// Token: 0x17000CD4 RID: 3284
		// (get) Token: 0x06005076 RID: 20598
		byte DigestMethod
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x17000CD5 RID: 3285
		// (get) Token: 0x06005077 RID: 20599
		object DigestValue
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
		}

		// Token: 0x17000CD6 RID: 3286
		// (get) Token: 0x06005078 RID: 20600
		string Xml
		{
			[SecurityCritical]
			[return: MarshalAs(UnmanagedType.LPWStr)]
			get;
		}
	}
}
