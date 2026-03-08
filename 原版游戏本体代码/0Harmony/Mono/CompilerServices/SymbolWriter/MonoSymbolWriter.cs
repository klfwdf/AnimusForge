using System;
using System.Collections.Generic;
using System.IO;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000348 RID: 840
	internal class MonoSymbolWriter
	{
		// Token: 0x06001577 RID: 5495 RVA: 0x00044BB4 File Offset: 0x00042DB4
		public MonoSymbolWriter(string filename)
		{
			this.methods = new List<SourceMethodBuilder>();
			this.sources = new List<SourceFileEntry>();
			this.comp_units = new List<CompileUnitEntry>();
			this.file = new MonoSymbolFile();
			this.filename = filename + ".mdb";
		}

		// Token: 0x17000575 RID: 1397
		// (get) Token: 0x06001578 RID: 5496 RVA: 0x00044C0F File Offset: 0x00042E0F
		public MonoSymbolFile SymbolFile
		{
			get
			{
				return this.file;
			}
		}

		// Token: 0x06001579 RID: 5497 RVA: 0x0001B842 File Offset: 0x00019A42
		public void CloseNamespace()
		{
		}

		// Token: 0x0600157A RID: 5498 RVA: 0x00044C17 File Offset: 0x00042E17
		public void DefineLocalVariable(int index, string name)
		{
			if (this.current_method == null)
			{
				return;
			}
			this.current_method.AddLocal(index, name);
		}

		// Token: 0x0600157B RID: 5499 RVA: 0x00044C2F File Offset: 0x00042E2F
		public void DefineCapturedLocal(int scope_id, string name, string captured_name)
		{
			this.file.DefineCapturedVariable(scope_id, name, captured_name, CapturedVariable.CapturedKind.Local);
		}

		// Token: 0x0600157C RID: 5500 RVA: 0x00044C40 File Offset: 0x00042E40
		public void DefineCapturedParameter(int scope_id, string name, string captured_name)
		{
			this.file.DefineCapturedVariable(scope_id, name, captured_name, CapturedVariable.CapturedKind.Parameter);
		}

		// Token: 0x0600157D RID: 5501 RVA: 0x00044C51 File Offset: 0x00042E51
		public void DefineCapturedThis(int scope_id, string captured_name)
		{
			this.file.DefineCapturedVariable(scope_id, "this", captured_name, CapturedVariable.CapturedKind.This);
		}

		// Token: 0x0600157E RID: 5502 RVA: 0x00044C66 File Offset: 0x00042E66
		public void DefineCapturedScope(int scope_id, int id, string captured_name)
		{
			this.file.DefineCapturedScope(scope_id, id, captured_name);
		}

		// Token: 0x0600157F RID: 5503 RVA: 0x00044C76 File Offset: 0x00042E76
		public void DefineScopeVariable(int scope, int index)
		{
			if (this.current_method == null)
			{
				return;
			}
			this.current_method.AddScopeVariable(scope, index);
		}

		// Token: 0x06001580 RID: 5504 RVA: 0x00044C8E File Offset: 0x00042E8E
		public void MarkSequencePoint(int offset, SourceFileEntry file, int line, int column, bool is_hidden)
		{
			if (this.current_method == null)
			{
				return;
			}
			this.current_method.MarkSequencePoint(offset, file, line, column, is_hidden);
		}

		// Token: 0x06001581 RID: 5505 RVA: 0x00044CAC File Offset: 0x00042EAC
		public SourceMethodBuilder OpenMethod(ICompileUnit file, int ns_id, IMethodDef method)
		{
			SourceMethodBuilder builder = new SourceMethodBuilder(file, ns_id, method);
			this.current_method_stack.Push(this.current_method);
			this.current_method = builder;
			this.methods.Add(this.current_method);
			return builder;
		}

		// Token: 0x06001582 RID: 5506 RVA: 0x00044CEC File Offset: 0x00042EEC
		public void CloseMethod()
		{
			this.current_method = this.current_method_stack.Pop();
		}

		// Token: 0x06001583 RID: 5507 RVA: 0x00044D00 File Offset: 0x00042F00
		public SourceFileEntry DefineDocument(string url)
		{
			SourceFileEntry entry = new SourceFileEntry(this.file, url);
			this.sources.Add(entry);
			return entry;
		}

		// Token: 0x06001584 RID: 5508 RVA: 0x00044D28 File Offset: 0x00042F28
		public SourceFileEntry DefineDocument(string url, byte[] guid, byte[] checksum)
		{
			SourceFileEntry entry = new SourceFileEntry(this.file, url, guid, checksum);
			this.sources.Add(entry);
			return entry;
		}

		// Token: 0x06001585 RID: 5509 RVA: 0x00044D54 File Offset: 0x00042F54
		public CompileUnitEntry DefineCompilationUnit(SourceFileEntry source)
		{
			CompileUnitEntry entry = new CompileUnitEntry(this.file, source);
			this.comp_units.Add(entry);
			return entry;
		}

		// Token: 0x06001586 RID: 5510 RVA: 0x00044D7B File Offset: 0x00042F7B
		public int DefineNamespace(string name, CompileUnitEntry unit, string[] using_clauses, int parent)
		{
			if (unit == null || using_clauses == null)
			{
				throw new NullReferenceException();
			}
			return unit.DefineNamespace(name, using_clauses, parent);
		}

		// Token: 0x06001587 RID: 5511 RVA: 0x00044D93 File Offset: 0x00042F93
		public int OpenScope(int start_offset)
		{
			if (this.current_method == null)
			{
				return 0;
			}
			this.current_method.StartBlock(CodeBlockEntry.Type.Lexical, start_offset);
			return 0;
		}

		// Token: 0x06001588 RID: 5512 RVA: 0x00044DAD File Offset: 0x00042FAD
		public void CloseScope(int end_offset)
		{
			if (this.current_method == null)
			{
				return;
			}
			this.current_method.EndBlock(end_offset);
		}

		// Token: 0x06001589 RID: 5513 RVA: 0x00044DC4 File Offset: 0x00042FC4
		public void OpenCompilerGeneratedBlock(int start_offset)
		{
			if (this.current_method == null)
			{
				return;
			}
			this.current_method.StartBlock(CodeBlockEntry.Type.CompilerGenerated, start_offset);
		}

		// Token: 0x0600158A RID: 5514 RVA: 0x00044DAD File Offset: 0x00042FAD
		public void CloseCompilerGeneratedBlock(int end_offset)
		{
			if (this.current_method == null)
			{
				return;
			}
			this.current_method.EndBlock(end_offset);
		}

		// Token: 0x0600158B RID: 5515 RVA: 0x00044DDC File Offset: 0x00042FDC
		public void StartIteratorBody(int start_offset)
		{
			this.current_method.StartBlock(CodeBlockEntry.Type.IteratorBody, start_offset);
		}

		// Token: 0x0600158C RID: 5516 RVA: 0x00044DEB File Offset: 0x00042FEB
		public void EndIteratorBody(int end_offset)
		{
			this.current_method.EndBlock(end_offset);
		}

		// Token: 0x0600158D RID: 5517 RVA: 0x00044DF9 File Offset: 0x00042FF9
		public void StartIteratorDispatcher(int start_offset)
		{
			this.current_method.StartBlock(CodeBlockEntry.Type.IteratorDispatcher, start_offset);
		}

		// Token: 0x0600158E RID: 5518 RVA: 0x00044DEB File Offset: 0x00042FEB
		public void EndIteratorDispatcher(int end_offset)
		{
			this.current_method.EndBlock(end_offset);
		}

		// Token: 0x0600158F RID: 5519 RVA: 0x00044E08 File Offset: 0x00043008
		public void DefineAnonymousScope(int id)
		{
			this.file.DefineAnonymousScope(id);
		}

		// Token: 0x06001590 RID: 5520 RVA: 0x00044E18 File Offset: 0x00043018
		public void WriteSymbolFile(Guid guid)
		{
			foreach (SourceMethodBuilder sourceMethodBuilder in this.methods)
			{
				sourceMethodBuilder.DefineMethod(this.file);
			}
			try
			{
				File.Delete(this.filename);
			}
			catch
			{
			}
			using (FileStream fs = new FileStream(this.filename, FileMode.Create, FileAccess.Write))
			{
				this.file.CreateSymbolFile(guid, fs);
			}
		}

		// Token: 0x04000B05 RID: 2821
		private List<SourceMethodBuilder> methods;

		// Token: 0x04000B06 RID: 2822
		private List<SourceFileEntry> sources;

		// Token: 0x04000B07 RID: 2823
		private List<CompileUnitEntry> comp_units;

		// Token: 0x04000B08 RID: 2824
		protected readonly MonoSymbolFile file;

		// Token: 0x04000B09 RID: 2825
		private string filename;

		// Token: 0x04000B0A RID: 2826
		private SourceMethodBuilder current_method;

		// Token: 0x04000B0B RID: 2827
		private Stack<SourceMethodBuilder> current_method_stack = new Stack<SourceMethodBuilder>();
	}
}
