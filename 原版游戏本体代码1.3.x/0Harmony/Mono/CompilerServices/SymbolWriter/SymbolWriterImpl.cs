using System;
using System.Collections;
using System.Diagnostics.SymbolStore;
using System.Reflection;
using System.Text;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x0200034A RID: 842
	internal class SymbolWriterImpl : ISymbolWriter
	{
		// Token: 0x060015A1 RID: 5537 RVA: 0x00045260 File Offset: 0x00043460
		public SymbolWriterImpl(Guid guid)
		{
			this.guid = guid;
		}

		// Token: 0x060015A2 RID: 5538 RVA: 0x00045285 File Offset: 0x00043485
		public void Close()
		{
			this.msw.WriteSymbolFile(this.guid);
		}

		// Token: 0x060015A3 RID: 5539 RVA: 0x00045298 File Offset: 0x00043498
		public void CloseMethod()
		{
			if (this.methodOpened)
			{
				this.methodOpened = false;
				this.nextLocalIndex = 0;
				this.msw.CloseMethod();
			}
		}

		// Token: 0x060015A4 RID: 5540 RVA: 0x000452BB File Offset: 0x000434BB
		public void CloseNamespace()
		{
			this.namespaceStack.Pop();
			this.msw.CloseNamespace();
		}

		// Token: 0x060015A5 RID: 5541 RVA: 0x000452D4 File Offset: 0x000434D4
		public void CloseScope(int endOffset)
		{
			this.msw.CloseScope(endOffset);
		}

		// Token: 0x060015A6 RID: 5542 RVA: 0x000452E4 File Offset: 0x000434E4
		public ISymbolDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType)
		{
			SymbolDocumentWriterImpl doc = (SymbolDocumentWriterImpl)this.documents[url];
			if (doc == null)
			{
				SourceFileEntry entry = this.msw.DefineDocument(url);
				doc = new SymbolDocumentWriterImpl(this.msw.DefineCompilationUnit(entry));
				this.documents[url] = doc;
			}
			return doc;
		}

		// Token: 0x060015A7 RID: 5543 RVA: 0x0001B842 File Offset: 0x00019A42
		public void DefineField(SymbolToken parent, string name, FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3)
		{
		}

		// Token: 0x060015A8 RID: 5544 RVA: 0x0001B842 File Offset: 0x00019A42
		public void DefineGlobalVariable(string name, FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3)
		{
		}

		// Token: 0x060015A9 RID: 5545 RVA: 0x00045334 File Offset: 0x00043534
		public void DefineLocalVariable(string name, FieldAttributes attributes, byte[] signature, SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset)
		{
			MonoSymbolWriter monoSymbolWriter = this.msw;
			int num = this.nextLocalIndex;
			this.nextLocalIndex = num + 1;
			monoSymbolWriter.DefineLocalVariable(num, name);
		}

		// Token: 0x060015AA RID: 5546 RVA: 0x0001B842 File Offset: 0x00019A42
		public void DefineParameter(string name, ParameterAttributes attributes, int sequence, SymAddressKind addrKind, int addr1, int addr2, int addr3)
		{
		}

		// Token: 0x060015AB RID: 5547 RVA: 0x00045360 File Offset: 0x00043560
		public void DefineSequencePoints(ISymbolDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns)
		{
			SymbolDocumentWriterImpl doc = (SymbolDocumentWriterImpl)document;
			SourceFileEntry file = ((doc != null) ? doc.Entry.SourceFile : null);
			for (int i = 0; i < offsets.Length; i++)
			{
				if (i <= 0 || offsets[i] != offsets[i - 1] || lines[i] != lines[i - 1] || columns[i] != columns[i - 1])
				{
					this.msw.MarkSequencePoint(offsets[i], file, lines[i], columns[i], false);
				}
			}
		}

		// Token: 0x060015AC RID: 5548 RVA: 0x000453CF File Offset: 0x000435CF
		public void Initialize(IntPtr emitter, string filename, bool fFullBuild)
		{
			this.msw = new MonoSymbolWriter(filename);
		}

		// Token: 0x060015AD RID: 5549 RVA: 0x000453DD File Offset: 0x000435DD
		public void OpenMethod(SymbolToken method)
		{
			this.currentToken = method.GetToken();
		}

		// Token: 0x060015AE RID: 5550 RVA: 0x000453EC File Offset: 0x000435EC
		public void OpenNamespace(string name)
		{
			NamespaceInfo i = new NamespaceInfo();
			i.NamespaceID = -1;
			i.Name = name;
			this.namespaceStack.Push(i);
		}

		// Token: 0x060015AF RID: 5551 RVA: 0x00045419 File Offset: 0x00043619
		public int OpenScope(int startOffset)
		{
			return this.msw.OpenScope(startOffset);
		}

		// Token: 0x060015B0 RID: 5552 RVA: 0x00045428 File Offset: 0x00043628
		public void SetMethodSourceRange(ISymbolDocumentWriter startDoc, int startLine, int startColumn, ISymbolDocumentWriter endDoc, int endLine, int endColumn)
		{
			int nsId = this.GetCurrentNamespace(startDoc);
			SourceMethodImpl sm = new SourceMethodImpl(this.methodName, this.currentToken, nsId);
			this.msw.OpenMethod(((ICompileUnit)startDoc).Entry, nsId, sm);
			this.methodOpened = true;
		}

		// Token: 0x060015B1 RID: 5553 RVA: 0x0001B842 File Offset: 0x00019A42
		public void SetScopeRange(int scopeID, int startOffset, int endOffset)
		{
		}

		// Token: 0x060015B2 RID: 5554 RVA: 0x00045470 File Offset: 0x00043670
		public void SetSymAttribute(SymbolToken parent, string name, byte[] data)
		{
			if (name == "__name")
			{
				this.methodName = Encoding.UTF8.GetString(data);
			}
		}

		// Token: 0x060015B3 RID: 5555 RVA: 0x0001B842 File Offset: 0x00019A42
		public void SetUnderlyingWriter(IntPtr underlyingWriter)
		{
		}

		// Token: 0x060015B4 RID: 5556 RVA: 0x0001B842 File Offset: 0x00019A42
		public void SetUserEntryPoint(SymbolToken entryMethod)
		{
		}

		// Token: 0x060015B5 RID: 5557 RVA: 0x00045490 File Offset: 0x00043690
		public void UsingNamespace(string fullName)
		{
			if (this.namespaceStack.Count == 0)
			{
				this.OpenNamespace("");
			}
			NamespaceInfo ni = (NamespaceInfo)this.namespaceStack.Peek();
			if (ni.NamespaceID != -1)
			{
				NamespaceInfo old = ni;
				this.CloseNamespace();
				this.OpenNamespace(old.Name);
				ni = (NamespaceInfo)this.namespaceStack.Peek();
				ni.UsingClauses = old.UsingClauses;
			}
			ni.UsingClauses.Add(fullName);
		}

		// Token: 0x060015B6 RID: 5558 RVA: 0x00045510 File Offset: 0x00043710
		private int GetCurrentNamespace(ISymbolDocumentWriter doc)
		{
			if (this.namespaceStack.Count == 0)
			{
				this.OpenNamespace("");
			}
			NamespaceInfo ni = (NamespaceInfo)this.namespaceStack.Peek();
			if (ni.NamespaceID == -1)
			{
				string[] usings = (string[])ni.UsingClauses.ToArray(typeof(string));
				int parentId = 0;
				if (this.namespaceStack.Count > 1)
				{
					this.namespaceStack.Pop();
					parentId = ((NamespaceInfo)this.namespaceStack.Peek()).NamespaceID;
					this.namespaceStack.Push(ni);
				}
				ni.NamespaceID = this.msw.DefineNamespace(ni.Name, ((ICompileUnit)doc).Entry, usings, parentId);
			}
			return ni.NamespaceID;
		}

		// Token: 0x04000B14 RID: 2836
		private MonoSymbolWriter msw;

		// Token: 0x04000B15 RID: 2837
		private int nextLocalIndex;

		// Token: 0x04000B16 RID: 2838
		private int currentToken;

		// Token: 0x04000B17 RID: 2839
		private string methodName;

		// Token: 0x04000B18 RID: 2840
		private Stack namespaceStack = new Stack();

		// Token: 0x04000B19 RID: 2841
		private bool methodOpened;

		// Token: 0x04000B1A RID: 2842
		private Hashtable documents = new Hashtable();

		// Token: 0x04000B1B RID: 2843
		private Guid guid;
	}
}
