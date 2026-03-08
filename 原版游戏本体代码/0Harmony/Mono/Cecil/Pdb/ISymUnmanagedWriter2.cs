using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Pdb
{
	// Token: 0x02000357 RID: 855
	[Guid("0B97726E-9E6D-4f05-9A26-424022093CAA")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface ISymUnmanagedWriter2
	{
		// Token: 0x060015EE RID: 5614
		void DefineDocument([MarshalAs(UnmanagedType.LPWStr)] [In] string url, [In] ref Guid langauge, [In] ref Guid languageVendor, [In] ref Guid documentType, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedDocumentWriter pRetVal);

		// Token: 0x060015EF RID: 5615
		void SetUserEntryPoint([In] int methodToken);

		// Token: 0x060015F0 RID: 5616
		void OpenMethod([In] int methodToken);

		// Token: 0x060015F1 RID: 5617
		void CloseMethod();

		// Token: 0x060015F2 RID: 5618
		void OpenScope([In] int startOffset, out int pRetVal);

		// Token: 0x060015F3 RID: 5619
		void CloseScope([In] int endOffset);

		// Token: 0x060015F4 RID: 5620
		void SetScopeRange_Placeholder();

		// Token: 0x060015F5 RID: 5621
		void DefineLocalVariable_Placeholder();

		// Token: 0x060015F6 RID: 5622
		void DefineParameter_Placeholder();

		// Token: 0x060015F7 RID: 5623
		void DefineField_Placeholder();

		// Token: 0x060015F8 RID: 5624
		void DefineGlobalVariable_Placeholder();

		// Token: 0x060015F9 RID: 5625
		void Close();

		// Token: 0x060015FA RID: 5626
		void SetSymAttribute(uint parent, string name, uint data, IntPtr signature);

		// Token: 0x060015FB RID: 5627
		void OpenNamespace([MarshalAs(UnmanagedType.LPWStr)] [In] string name);

		// Token: 0x060015FC RID: 5628
		void CloseNamespace();

		// Token: 0x060015FD RID: 5629
		void UsingNamespace([MarshalAs(UnmanagedType.LPWStr)] [In] string fullName);

		// Token: 0x060015FE RID: 5630
		void SetMethodSourceRange_Placeholder();

		// Token: 0x060015FF RID: 5631
		void Initialize([MarshalAs(UnmanagedType.IUnknown)] [In] object emitter, [MarshalAs(UnmanagedType.LPWStr)] [In] string filename, [In] IStream pIStream, [In] bool fFullBuild);

		// Token: 0x06001600 RID: 5632
		void GetDebugInfo(out ImageDebugDirectory pIDD, [In] int cData, out int pcData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] [In] [Out] byte[] data);

		// Token: 0x06001601 RID: 5633
		void DefineSequencePoints([MarshalAs(UnmanagedType.Interface)] [In] ISymUnmanagedDocumentWriter document, [In] int spCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] [In] int[] offsets, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] [In] int[] lines, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] [In] int[] columns, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] [In] int[] endLines, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] [In] int[] endColumns);

		// Token: 0x06001602 RID: 5634
		void RemapToken_Placeholder();

		// Token: 0x06001603 RID: 5635
		void Initialize2_Placeholder();

		// Token: 0x06001604 RID: 5636
		void DefineConstant_Placeholder();

		// Token: 0x06001605 RID: 5637
		void Abort_Placeholder();

		// Token: 0x06001606 RID: 5638
		void DefineLocalVariable2([MarshalAs(UnmanagedType.LPWStr)] [In] string name, [In] int attributes, [In] int sigToken, [In] int addrKind, [In] int addr1, [In] int addr2, [In] int addr3, [In] int startOffset, [In] int endOffset);

		// Token: 0x06001607 RID: 5639
		void DefineGlobalVariable2_Placeholder();

		// Token: 0x06001608 RID: 5640
		void DefineConstant2([MarshalAs(UnmanagedType.LPWStr)] [In] string name, [MarshalAs(UnmanagedType.Struct)] [In] object variant, [In] int sigToken);
	}
}
