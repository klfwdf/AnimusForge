using System;
using System.Runtime.InteropServices;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Cecil.Pdb
{
	// Token: 0x0200036A RID: 874
	internal class SymWriter
	{
		// Token: 0x06001734 RID: 5940
		[DllImport("ole32.dll")]
		private static extern int CoCreateInstance([In] ref Guid rclsid, [MarshalAs(UnmanagedType.IUnknown)] [In] object pUnkOuter, [In] uint dwClsContext, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

		// Token: 0x06001735 RID: 5941 RVA: 0x000478F4 File Offset: 0x00045AF4
		public SymWriter()
		{
			object objWriter;
			SymWriter.CoCreateInstance(ref SymWriter.s_CorSymWriter_SxS_ClassID, null, 1U, ref SymWriter.s_symUnmangedWriterIID, out objWriter);
			this.writer = (ISymUnmanagedWriter2)objWriter;
			this.documents = new Collection<ISymUnmanagedDocumentWriter>();
		}

		// Token: 0x06001736 RID: 5942 RVA: 0x00047934 File Offset: 0x00045B34
		public byte[] GetDebugInfo(out ImageDebugDirectory idd)
		{
			int size;
			this.writer.GetDebugInfo(out idd, 0, out size, null);
			byte[] debug_info = new byte[size];
			this.writer.GetDebugInfo(out idd, size, out size, debug_info);
			return debug_info;
		}

		// Token: 0x06001737 RID: 5943 RVA: 0x0004796C File Offset: 0x00045B6C
		public void DefineLocalVariable2(string name, VariableAttributes attributes, int sigToken, int addr1, int addr2, int addr3, int startOffset, int endOffset)
		{
			this.writer.DefineLocalVariable2(name, (int)attributes, sigToken, 1, addr1, addr2, addr3, startOffset, endOffset);
		}

		// Token: 0x06001738 RID: 5944 RVA: 0x00047992 File Offset: 0x00045B92
		public void DefineConstant2(string name, object value, int sigToken)
		{
			if (value == null)
			{
				this.writer.DefineConstant2(name, 0, sigToken);
				return;
			}
			this.writer.DefineConstant2(name, value, sigToken);
		}

		// Token: 0x06001739 RID: 5945 RVA: 0x000479BC File Offset: 0x00045BBC
		public void Close()
		{
			if (this.closed)
			{
				return;
			}
			this.closed = true;
			this.writer.Close();
			Marshal.ReleaseComObject(this.writer);
			foreach (ISymUnmanagedDocumentWriter o in this.documents)
			{
				Marshal.ReleaseComObject(o);
			}
		}

		// Token: 0x0600173A RID: 5946 RVA: 0x00047A34 File Offset: 0x00045C34
		public void CloseMethod()
		{
			this.writer.CloseMethod();
		}

		// Token: 0x0600173B RID: 5947 RVA: 0x00047A41 File Offset: 0x00045C41
		public void CloseNamespace()
		{
			this.writer.CloseNamespace();
		}

		// Token: 0x0600173C RID: 5948 RVA: 0x00047A4E File Offset: 0x00045C4E
		public void CloseScope(int endOffset)
		{
			this.writer.CloseScope(endOffset);
		}

		// Token: 0x0600173D RID: 5949 RVA: 0x00047A5C File Offset: 0x00045C5C
		public SymDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType)
		{
			ISymUnmanagedDocumentWriter doc_writer;
			this.writer.DefineDocument(url, ref language, ref languageVendor, ref documentType, out doc_writer);
			this.documents.Add(doc_writer);
			return new SymDocumentWriter(doc_writer);
		}

		// Token: 0x0600173E RID: 5950 RVA: 0x00047A8F File Offset: 0x00045C8F
		public void DefineSequencePoints(SymDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns)
		{
			this.writer.DefineSequencePoints(document.Writer, offsets.Length, offsets, lines, columns, endLines, endColumns);
		}

		// Token: 0x0600173F RID: 5951 RVA: 0x00047AAD File Offset: 0x00045CAD
		public void Initialize(object emitter, string filename, bool fFullBuild)
		{
			this.writer.Initialize(emitter, filename, null, fFullBuild);
		}

		// Token: 0x06001740 RID: 5952 RVA: 0x00047ABE File Offset: 0x00045CBE
		public void SetUserEntryPoint(int methodToken)
		{
			this.writer.SetUserEntryPoint(methodToken);
		}

		// Token: 0x06001741 RID: 5953 RVA: 0x00047ACC File Offset: 0x00045CCC
		public void OpenMethod(int methodToken)
		{
			this.writer.OpenMethod(methodToken);
		}

		// Token: 0x06001742 RID: 5954 RVA: 0x00047ADA File Offset: 0x00045CDA
		public void OpenNamespace(string name)
		{
			this.writer.OpenNamespace(name);
		}

		// Token: 0x06001743 RID: 5955 RVA: 0x00047AE8 File Offset: 0x00045CE8
		public int OpenScope(int startOffset)
		{
			int result;
			this.writer.OpenScope(startOffset, out result);
			return result;
		}

		// Token: 0x06001744 RID: 5956 RVA: 0x00047B04 File Offset: 0x00045D04
		public void UsingNamespace(string fullName)
		{
			this.writer.UsingNamespace(fullName);
		}

		// Token: 0x06001745 RID: 5957 RVA: 0x00047B14 File Offset: 0x00045D14
		public void DefineCustomMetadata(string name, byte[] metadata)
		{
			GCHandle handle = GCHandle.Alloc(metadata, GCHandleType.Pinned);
			this.writer.SetSymAttribute(0U, name, (uint)metadata.Length, handle.AddrOfPinnedObject());
			handle.Free();
		}

		// Token: 0x04000B56 RID: 2902
		private static Guid s_symUnmangedWriterIID = new Guid("0b97726e-9e6d-4f05-9a26-424022093caa");

		// Token: 0x04000B57 RID: 2903
		private static Guid s_CorSymWriter_SxS_ClassID = new Guid("108296c1-281e-11d3-bd22-0000f80849bd");

		// Token: 0x04000B58 RID: 2904
		private readonly ISymUnmanagedWriter2 writer;

		// Token: 0x04000B59 RID: 2905
		private readonly Collection<ISymUnmanagedDocumentWriter> documents;

		// Token: 0x04000B5A RID: 2906
		private bool closed;
	}
}
