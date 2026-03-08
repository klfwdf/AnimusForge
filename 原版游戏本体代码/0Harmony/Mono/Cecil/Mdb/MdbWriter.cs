using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Mono.CompilerServices.SymbolWriter;

namespace Mono.Cecil.Mdb
{
	// Token: 0x02000353 RID: 851
	internal sealed class MdbWriter : ISymbolWriter, IDisposable
	{
		// Token: 0x060015D9 RID: 5593 RVA: 0x00045AA8 File Offset: 0x00043CA8
		public MdbWriter(ModuleDefinition module, string assembly)
		{
			this.module = module;
			this.writer = new MonoSymbolWriter(assembly);
			this.source_files = new Dictionary<string, MdbWriter.SourceFile>();
		}

		// Token: 0x060015DA RID: 5594 RVA: 0x00045ACE File Offset: 0x00043CCE
		public ISymbolReaderProvider GetReaderProvider()
		{
			return new MdbReaderProvider();
		}

		// Token: 0x060015DB RID: 5595 RVA: 0x00045AD8 File Offset: 0x00043CD8
		private MdbWriter.SourceFile GetSourceFile(Document document)
		{
			string url = document.Url;
			MdbWriter.SourceFile source_file;
			if (this.source_files.TryGetValue(url, out source_file))
			{
				return source_file;
			}
			SourceFileEntry entry = this.writer.DefineDocument(url, null, (document.Hash != null && document.Hash.Length == 16) ? document.Hash : null);
			source_file = new MdbWriter.SourceFile(this.writer.DefineCompilationUnit(entry), entry);
			this.source_files.Add(url, source_file);
			return source_file;
		}

		// Token: 0x060015DC RID: 5596 RVA: 0x00045B4C File Offset: 0x00043D4C
		private void Populate(Collection<SequencePoint> sequencePoints, int[] offsets, int[] startRows, int[] endRows, int[] startCols, int[] endCols, out MdbWriter.SourceFile file)
		{
			MdbWriter.SourceFile source_file = null;
			for (int i = 0; i < sequencePoints.Count; i++)
			{
				SequencePoint sequence_point = sequencePoints[i];
				offsets[i] = sequence_point.Offset;
				if (source_file == null)
				{
					source_file = this.GetSourceFile(sequence_point.Document);
				}
				startRows[i] = sequence_point.StartLine;
				endRows[i] = sequence_point.EndLine;
				startCols[i] = sequence_point.StartColumn;
				endCols[i] = sequence_point.EndColumn;
			}
			file = source_file;
		}

		// Token: 0x060015DD RID: 5597 RVA: 0x00045BB8 File Offset: 0x00043DB8
		public void Write(MethodDebugInformation info)
		{
			MdbWriter.SourceMethod method = new MdbWriter.SourceMethod(info.method);
			Collection<SequencePoint> sequence_points = info.SequencePoints;
			int count = sequence_points.Count;
			if (count == 0)
			{
				return;
			}
			int[] offsets = new int[count];
			int[] start_rows = new int[count];
			int[] end_rows = new int[count];
			int[] start_cols = new int[count];
			int[] end_cols = new int[count];
			MdbWriter.SourceFile file;
			this.Populate(sequence_points, offsets, start_rows, end_rows, start_cols, end_cols, out file);
			SourceMethodBuilder builder = this.writer.OpenMethod(file.CompilationUnit, 0, method);
			for (int i = 0; i < count; i++)
			{
				builder.MarkSequencePoint(offsets[i], file.CompilationUnit.SourceFile, start_rows[i], start_cols[i], end_rows[i], end_cols[i], false);
			}
			if (info.scope != null)
			{
				this.WriteRootScope(info.scope, info);
			}
			this.writer.CloseMethod();
		}

		// Token: 0x060015DE RID: 5598 RVA: 0x00045C8E File Offset: 0x00043E8E
		private void WriteRootScope(ScopeDebugInformation scope, MethodDebugInformation info)
		{
			this.WriteScopeVariables(scope);
			if (scope.HasScopes)
			{
				this.WriteScopes(scope.Scopes, info);
			}
		}

		// Token: 0x060015DF RID: 5599 RVA: 0x00045CAC File Offset: 0x00043EAC
		private void WriteScope(ScopeDebugInformation scope, MethodDebugInformation info)
		{
			this.writer.OpenScope(scope.Start.Offset);
			this.WriteScopeVariables(scope);
			if (scope.HasScopes)
			{
				this.WriteScopes(scope.Scopes, info);
			}
			this.writer.CloseScope(scope.End.IsEndOfMethod ? info.code_size : scope.End.Offset);
		}

		// Token: 0x060015E0 RID: 5600 RVA: 0x00045D20 File Offset: 0x00043F20
		private void WriteScopes(Collection<ScopeDebugInformation> scopes, MethodDebugInformation info)
		{
			for (int i = 0; i < scopes.Count; i++)
			{
				this.WriteScope(scopes[i], info);
			}
		}

		// Token: 0x060015E1 RID: 5601 RVA: 0x00045D4C File Offset: 0x00043F4C
		private void WriteScopeVariables(ScopeDebugInformation scope)
		{
			if (!scope.HasVariables)
			{
				return;
			}
			foreach (VariableDebugInformation variable in scope.variables)
			{
				if (!string.IsNullOrEmpty(variable.Name))
				{
					this.writer.DefineLocalVariable(variable.Index, variable.Name);
				}
			}
		}

		// Token: 0x060015E2 RID: 5602 RVA: 0x00045DC8 File Offset: 0x00043FC8
		public ImageDebugHeader GetDebugHeader()
		{
			return new ImageDebugHeader();
		}

		// Token: 0x060015E3 RID: 5603 RVA: 0x0001B842 File Offset: 0x00019A42
		public void Write()
		{
		}

		// Token: 0x060015E4 RID: 5604 RVA: 0x0001B842 File Offset: 0x00019A42
		public void Write(ICustomDebugInformationProvider provider)
		{
		}

		// Token: 0x060015E5 RID: 5605 RVA: 0x00045DCF File Offset: 0x00043FCF
		public void Dispose()
		{
			this.writer.WriteSymbolFile(this.module.Mvid);
		}

		// Token: 0x04000B28 RID: 2856
		private readonly ModuleDefinition module;

		// Token: 0x04000B29 RID: 2857
		private readonly MonoSymbolWriter writer;

		// Token: 0x04000B2A RID: 2858
		private readonly Dictionary<string, MdbWriter.SourceFile> source_files;

		// Token: 0x02000354 RID: 852
		private class SourceFile : ISourceFile
		{
			// Token: 0x17000580 RID: 1408
			// (get) Token: 0x060015E6 RID: 5606 RVA: 0x00045DE7 File Offset: 0x00043FE7
			public SourceFileEntry Entry
			{
				get
				{
					return this.entry;
				}
			}

			// Token: 0x17000581 RID: 1409
			// (get) Token: 0x060015E7 RID: 5607 RVA: 0x00045DEF File Offset: 0x00043FEF
			public CompileUnitEntry CompilationUnit
			{
				get
				{
					return this.compilation_unit;
				}
			}

			// Token: 0x060015E8 RID: 5608 RVA: 0x00045DF7 File Offset: 0x00043FF7
			public SourceFile(CompileUnitEntry comp_unit, SourceFileEntry entry)
			{
				this.compilation_unit = comp_unit;
				this.entry = entry;
			}

			// Token: 0x04000B2B RID: 2859
			private readonly CompileUnitEntry compilation_unit;

			// Token: 0x04000B2C RID: 2860
			private readonly SourceFileEntry entry;
		}

		// Token: 0x02000355 RID: 853
		private class SourceMethod : IMethodDef
		{
			// Token: 0x17000582 RID: 1410
			// (get) Token: 0x060015E9 RID: 5609 RVA: 0x00045E0D File Offset: 0x0004400D
			public string Name
			{
				get
				{
					return this.method.Name;
				}
			}

			// Token: 0x17000583 RID: 1411
			// (get) Token: 0x060015EA RID: 5610 RVA: 0x00045E1C File Offset: 0x0004401C
			public int Token
			{
				get
				{
					return this.method.MetadataToken.ToInt32();
				}
			}

			// Token: 0x060015EB RID: 5611 RVA: 0x00045E3C File Offset: 0x0004403C
			public SourceMethod(MethodDefinition method)
			{
				this.method = method;
			}

			// Token: 0x04000B2D RID: 2861
			private readonly MethodDefinition method;
		}
	}
}
