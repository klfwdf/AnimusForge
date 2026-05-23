using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Runtime.Hosting
{
	// Token: 0x0200002E RID: 46
	[SecurityCritical]
	[ComConversionLoss]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D")]
	[ComImport]
	internal interface IClrStrongNameUsingIntPtr
	{
		// Token: 0x060001AA RID: 426
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int GetHashFromAssemblyFile([MarshalAs(UnmanagedType.LPStr)] [In] string pszFilePath, [MarshalAs(UnmanagedType.U4)] [In] [Out] ref int piHashAlg, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] [Out] byte[] pbHash, [MarshalAs(UnmanagedType.U4)] [In] int cchHash, [MarshalAs(UnmanagedType.U4)] out int pchHash);

		// Token: 0x060001AB RID: 427
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int GetHashFromAssemblyFileW([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzFilePath, [MarshalAs(UnmanagedType.U4)] [In] [Out] ref int piHashAlg, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] [Out] byte[] pbHash, [MarshalAs(UnmanagedType.U4)] [In] int cchHash, [MarshalAs(UnmanagedType.U4)] out int pchHash);

		// Token: 0x060001AC RID: 428
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int GetHashFromBlob([In] IntPtr pbBlob, [MarshalAs(UnmanagedType.U4)] [In] int cchBlob, [MarshalAs(UnmanagedType.U4)] [In] [Out] ref int piHashAlg, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] [Out] byte[] pbHash, [MarshalAs(UnmanagedType.U4)] [In] int cchHash, [MarshalAs(UnmanagedType.U4)] out int pchHash);

		// Token: 0x060001AD RID: 429
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int GetHashFromFile([MarshalAs(UnmanagedType.LPStr)] [In] string pszFilePath, [MarshalAs(UnmanagedType.U4)] [In] [Out] ref int piHashAlg, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] [Out] byte[] pbHash, [MarshalAs(UnmanagedType.U4)] [In] int cchHash, [MarshalAs(UnmanagedType.U4)] out int pchHash);

		// Token: 0x060001AE RID: 430
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int GetHashFromFileW([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzFilePath, [MarshalAs(UnmanagedType.U4)] [In] [Out] ref int piHashAlg, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] [Out] byte[] pbHash, [MarshalAs(UnmanagedType.U4)] [In] int cchHash, [MarshalAs(UnmanagedType.U4)] out int pchHash);

		// Token: 0x060001AF RID: 431
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int GetHashFromHandle([In] IntPtr hFile, [MarshalAs(UnmanagedType.U4)] [In] [Out] ref int piHashAlg, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] [Out] byte[] pbHash, [MarshalAs(UnmanagedType.U4)] [In] int cchHash, [MarshalAs(UnmanagedType.U4)] out int pchHash);

		// Token: 0x060001B0 RID: 432
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.U4)]
		int StrongNameCompareAssemblies([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzAssembly1, [MarshalAs(UnmanagedType.LPWStr)] [In] string pwzAssembly2, [MarshalAs(UnmanagedType.U4)] out int dwResult);

		// Token: 0x060001B1 RID: 433
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameFreeBuffer([In] IntPtr pbMemory);

		// Token: 0x060001B2 RID: 434
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameGetBlob([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzFilePath, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] [Out] byte[] pbBlob, [MarshalAs(UnmanagedType.U4)] [In] [Out] ref int pcbBlob);

		// Token: 0x060001B3 RID: 435
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameGetBlobFromImage([In] IntPtr pbBase, [MarshalAs(UnmanagedType.U4)] [In] int dwLength, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] [Out] byte[] pbBlob, [MarshalAs(UnmanagedType.U4)] [In] [Out] ref int pcbBlob);

		// Token: 0x060001B4 RID: 436
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameGetPublicKey([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzKeyContainer, [In] IntPtr pbKeyBlob, [MarshalAs(UnmanagedType.U4)] [In] int cbKeyBlob, out IntPtr ppbPublicKeyBlob, [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

		// Token: 0x060001B5 RID: 437
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.U4)]
		int StrongNameHashSize([MarshalAs(UnmanagedType.U4)] [In] int ulHashAlg, [MarshalAs(UnmanagedType.U4)] out int cbSize);

		// Token: 0x060001B6 RID: 438
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameKeyDelete([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzKeyContainer);

		// Token: 0x060001B7 RID: 439
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameKeyGen([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzKeyContainer, [MarshalAs(UnmanagedType.U4)] [In] int dwFlags, out IntPtr ppbKeyBlob, [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

		// Token: 0x060001B8 RID: 440
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameKeyGenEx([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzKeyContainer, [MarshalAs(UnmanagedType.U4)] [In] int dwFlags, [MarshalAs(UnmanagedType.U4)] [In] int dwKeySize, out IntPtr ppbKeyBlob, [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

		// Token: 0x060001B9 RID: 441
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameKeyInstall([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzKeyContainer, [In] IntPtr pbKeyBlob, [MarshalAs(UnmanagedType.U4)] [In] int cbKeyBlob);

		// Token: 0x060001BA RID: 442
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameSignatureGeneration([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzFilePath, [MarshalAs(UnmanagedType.LPWStr)] [In] string pwzKeyContainer, [In] IntPtr pbKeyBlob, [MarshalAs(UnmanagedType.U4)] [In] int cbKeyBlob, [In] [Out] IntPtr ppbSignatureBlob, [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob);

		// Token: 0x060001BB RID: 443
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameSignatureGenerationEx([MarshalAs(UnmanagedType.LPWStr)] [In] string wszFilePath, [MarshalAs(UnmanagedType.LPWStr)] [In] string wszKeyContainer, [In] IntPtr pbKeyBlob, [MarshalAs(UnmanagedType.U4)] [In] int cbKeyBlob, [In] [Out] IntPtr ppbSignatureBlob, [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob, [MarshalAs(UnmanagedType.U4)] [In] int dwFlags);

		// Token: 0x060001BC RID: 444
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameSignatureSize([In] IntPtr pbPublicKeyBlob, [MarshalAs(UnmanagedType.U4)] [In] int cbPublicKeyBlob, [MarshalAs(UnmanagedType.U4)] out int pcbSize);

		// Token: 0x060001BD RID: 445
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.U4)]
		int StrongNameSignatureVerification([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzFilePath, [MarshalAs(UnmanagedType.U4)] [In] int dwInFlags, [MarshalAs(UnmanagedType.U4)] out int dwOutFlags);

		// Token: 0x060001BE RID: 446
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.U4)]
		int StrongNameSignatureVerificationEx([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzFilePath, [MarshalAs(UnmanagedType.I1)] [In] bool fForceVerification, [MarshalAs(UnmanagedType.I1)] out bool fWasVerified);

		// Token: 0x060001BF RID: 447
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.U4)]
		int StrongNameSignatureVerificationFromImage([In] IntPtr pbBase, [MarshalAs(UnmanagedType.U4)] [In] int dwLength, [MarshalAs(UnmanagedType.U4)] [In] int dwInFlags, [MarshalAs(UnmanagedType.U4)] out int dwOutFlags);

		// Token: 0x060001C0 RID: 448
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameTokenFromAssembly([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzFilePath, out IntPtr ppbStrongNameToken, [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);

		// Token: 0x060001C1 RID: 449
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameTokenFromAssemblyEx([MarshalAs(UnmanagedType.LPWStr)] [In] string pwzFilePath, out IntPtr ppbStrongNameToken, [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken, out IntPtr ppbPublicKeyBlob, [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

		// Token: 0x060001C2 RID: 450
		[MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
		int StrongNameTokenFromPublicKey([In] IntPtr pbPublicKeyBlob, [MarshalAs(UnmanagedType.U4)] [In] int cbPublicKeyBlob, out IntPtr ppbStrongNameToken, [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);
	}
}
