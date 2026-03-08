using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006B6 RID: 1718
	[Guid("0af57545-a72a-4fbe-813c-8554ed7d4528")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IActContext
	{
		// Token: 0x0600502A RID: 20522
		[SecurityCritical]
		void GetAppId([MarshalAs(UnmanagedType.Interface)] out object AppId);

		// Token: 0x0600502B RID: 20523
		[SecurityCritical]
		void EnumCategories([In] uint Flags, [In] IReferenceIdentity CategoryToMatch, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object EnumOut);

		// Token: 0x0600502C RID: 20524
		[SecurityCritical]
		void EnumSubcategories([In] uint Flags, [In] IDefinitionIdentity CategoryId, [MarshalAs(UnmanagedType.LPWStr)] [In] string SubcategoryPattern, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object EnumOut);

		// Token: 0x0600502D RID: 20525
		[SecurityCritical]
		void EnumCategoryInstances([In] uint Flags, [In] IDefinitionIdentity CategoryId, [MarshalAs(UnmanagedType.LPWStr)] [In] string Subcategory, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object EnumOut);

		// Token: 0x0600502E RID: 20526
		[SecurityCritical]
		void ReplaceStringMacros([In] uint Flags, [MarshalAs(UnmanagedType.LPWStr)] [In] string Culture, [MarshalAs(UnmanagedType.LPWStr)] [In] string ReplacementPattern, [MarshalAs(UnmanagedType.LPWStr)] out string Replaced);

		// Token: 0x0600502F RID: 20527
		[SecurityCritical]
		void GetComponentStringTableStrings([In] uint Flags, [MarshalAs(UnmanagedType.SysUInt)] [In] IntPtr ComponentIndex, [MarshalAs(UnmanagedType.SysUInt)] [In] IntPtr StringCount, [MarshalAs(UnmanagedType.LPArray)] [Out] string[] SourceStrings, [MarshalAs(UnmanagedType.LPArray)] out string[] DestinationStrings, [MarshalAs(UnmanagedType.SysUInt)] [In] IntPtr CultureFallbacks);

		// Token: 0x06005030 RID: 20528
		[SecurityCritical]
		void GetApplicationProperties([In] uint Flags, [In] UIntPtr cProperties, [MarshalAs(UnmanagedType.LPArray)] [In] string[] PropertyNames, [MarshalAs(UnmanagedType.LPArray)] out string[] PropertyValues, [MarshalAs(UnmanagedType.LPArray)] out UIntPtr[] ComponentIndicies);

		// Token: 0x06005031 RID: 20529
		[SecurityCritical]
		void ApplicationBasePath([In] uint Flags, [MarshalAs(UnmanagedType.LPWStr)] out string ApplicationPath);

		// Token: 0x06005032 RID: 20530
		[SecurityCritical]
		void GetComponentManifest([In] uint Flags, [In] IDefinitionIdentity ComponentId, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ManifestInteface);

		// Token: 0x06005033 RID: 20531
		[SecurityCritical]
		void GetComponentPayloadPath([In] uint Flags, [In] IDefinitionIdentity ComponentId, [MarshalAs(UnmanagedType.LPWStr)] out string PayloadPath);

		// Token: 0x06005034 RID: 20532
		[SecurityCritical]
		void FindReferenceInContext([In] uint dwFlags, [In] IReferenceIdentity Reference, [MarshalAs(UnmanagedType.Interface)] out object MatchedDefinition);

		// Token: 0x06005035 RID: 20533
		[SecurityCritical]
		void CreateActContextFromCategoryInstance([In] uint dwFlags, [In] ref CATEGORY_INSTANCE CategoryInstance, [MarshalAs(UnmanagedType.Interface)] out object ppCreatedAppContext);

		// Token: 0x06005036 RID: 20534
		[SecurityCritical]
		void EnumComponents([In] uint dwFlags, [MarshalAs(UnmanagedType.Interface)] out object ppIdentityEnum);

		// Token: 0x06005037 RID: 20535
		[SecurityCritical]
		void PrepareForExecution([MarshalAs(UnmanagedType.SysInt)] [In] IntPtr Inputs, [MarshalAs(UnmanagedType.SysInt)] [In] IntPtr Outputs);

		// Token: 0x06005038 RID: 20536
		[SecurityCritical]
		void SetApplicationRunningState([In] uint dwFlags, [In] uint ulState, out uint ulDisposition);

		// Token: 0x06005039 RID: 20537
		[SecurityCritical]
		void GetApplicationStateFilesystemLocation([In] uint dwFlags, [In] UIntPtr Component, [MarshalAs(UnmanagedType.SysInt)] [In] IntPtr pCoordinateList, [MarshalAs(UnmanagedType.LPWStr)] out string ppszPath);

		// Token: 0x0600503A RID: 20538
		[SecurityCritical]
		void FindComponentsByDefinition([In] uint dwFlags, [In] UIntPtr ComponentCount, [MarshalAs(UnmanagedType.LPArray)] [In] IDefinitionIdentity[] Components, [MarshalAs(UnmanagedType.LPArray)] [Out] UIntPtr[] Indicies, [MarshalAs(UnmanagedType.LPArray)] [Out] uint[] Dispositions);

		// Token: 0x0600503B RID: 20539
		[SecurityCritical]
		void FindComponentsByReference([In] uint dwFlags, [In] UIntPtr Components, [MarshalAs(UnmanagedType.LPArray)] [In] IReferenceIdentity[] References, [MarshalAs(UnmanagedType.LPArray)] [Out] UIntPtr[] Indicies, [MarshalAs(UnmanagedType.LPArray)] [Out] uint[] Dispositions);
	}
}
