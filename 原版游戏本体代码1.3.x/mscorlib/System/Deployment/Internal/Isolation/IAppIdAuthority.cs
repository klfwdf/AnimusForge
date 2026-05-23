using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x0200069C RID: 1692
	[Guid("8c87810c-2541-4f75-b2d0-9af515488e23")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IAppIdAuthority
	{
		// Token: 0x06004FB3 RID: 20403
		[SecurityCritical]
		IDefinitionAppId TextToDefinition([In] uint Flags, [MarshalAs(UnmanagedType.LPWStr)] [In] string Identity);

		// Token: 0x06004FB4 RID: 20404
		[SecurityCritical]
		IReferenceAppId TextToReference([In] uint Flags, [MarshalAs(UnmanagedType.LPWStr)] [In] string Identity);

		// Token: 0x06004FB5 RID: 20405
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string DefinitionToText([In] uint Flags, [In] IDefinitionAppId DefinitionAppId);

		// Token: 0x06004FB6 RID: 20406
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string ReferenceToText([In] uint Flags, [In] IReferenceAppId ReferenceAppId);

		// Token: 0x06004FB7 RID: 20407
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.Bool)]
		bool AreDefinitionsEqual([In] uint Flags, [In] IDefinitionAppId Definition1, [In] IDefinitionAppId Definition2);

		// Token: 0x06004FB8 RID: 20408
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.Bool)]
		bool AreReferencesEqual([In] uint Flags, [In] IReferenceAppId Reference1, [In] IReferenceAppId Reference2);

		// Token: 0x06004FB9 RID: 20409
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.Bool)]
		bool AreTextualDefinitionsEqual([In] uint Flags, [MarshalAs(UnmanagedType.LPWStr)] [In] string AppIdLeft, [MarshalAs(UnmanagedType.LPWStr)] [In] string AppIdRight);

		// Token: 0x06004FBA RID: 20410
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.Bool)]
		bool AreTextualReferencesEqual([In] uint Flags, [MarshalAs(UnmanagedType.LPWStr)] [In] string AppIdLeft, [MarshalAs(UnmanagedType.LPWStr)] [In] string AppIdRight);

		// Token: 0x06004FBB RID: 20411
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.Bool)]
		bool DoesDefinitionMatchReference([In] uint Flags, [In] IDefinitionAppId DefinitionIdentity, [In] IReferenceAppId ReferenceIdentity);

		// Token: 0x06004FBC RID: 20412
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.Bool)]
		bool DoesTextualDefinitionMatchTextualReference([In] uint Flags, [MarshalAs(UnmanagedType.LPWStr)] [In] string Definition, [MarshalAs(UnmanagedType.LPWStr)] [In] string Reference);

		// Token: 0x06004FBD RID: 20413
		[SecurityCritical]
		ulong HashReference([In] uint Flags, [In] IReferenceAppId ReferenceIdentity);

		// Token: 0x06004FBE RID: 20414
		[SecurityCritical]
		ulong HashDefinition([In] uint Flags, [In] IDefinitionAppId DefinitionIdentity);

		// Token: 0x06004FBF RID: 20415
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string GenerateDefinitionKey([In] uint Flags, [In] IDefinitionAppId DefinitionIdentity);

		// Token: 0x06004FC0 RID: 20416
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string GenerateReferenceKey([In] uint Flags, [In] IReferenceAppId ReferenceIdentity);

		// Token: 0x06004FC1 RID: 20417
		[SecurityCritical]
		IDefinitionAppId CreateDefinition();

		// Token: 0x06004FC2 RID: 20418
		[SecurityCritical]
		IReferenceAppId CreateReference();
	}
}
