using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Mono.CompilerServices.SymbolWriter;

namespace Mono.Cecil.Mdb
{
	// Token: 0x0200034F RID: 847
	internal sealed class MdbReader : ISymbolReader, IDisposable
	{
		// Token: 0x060015C4 RID: 5572 RVA: 0x00045677 File Offset: 0x00043877
		public MdbReader(ModuleDefinition module, MonoSymbolFile symFile)
		{
			this.module = module;
			this.symbol_file = symFile;
			this.documents = new Dictionary<string, Document>();
		}

		// Token: 0x060015C5 RID: 5573 RVA: 0x00045698 File Offset: 0x00043898
		public ISymbolWriterProvider GetWriterProvider()
		{
			return new MdbWriterProvider();
		}

		// Token: 0x060015C6 RID: 5574 RVA: 0x0004569F File Offset: 0x0004389F
		public bool ProcessDebugHeader(ImageDebugHeader header)
		{
			return this.symbol_file.Guid == this.module.Mvid;
		}

		// Token: 0x060015C7 RID: 5575 RVA: 0x000456BC File Offset: 0x000438BC
		public MethodDebugInformation Read(MethodDefinition method)
		{
			MetadataToken method_token = method.MetadataToken;
			MethodEntry entry = this.symbol_file.GetMethodByToken(method_token.ToInt32());
			if (entry == null)
			{
				return null;
			}
			MethodDebugInformation info = new MethodDebugInformation(method);
			info.code_size = MdbReader.ReadCodeSize(method);
			ScopeDebugInformation[] scopes = MdbReader.ReadScopes(entry, info);
			this.ReadLineNumbers(entry, info);
			MdbReader.ReadLocalVariables(entry, scopes);
			return info;
		}

		// Token: 0x060015C8 RID: 5576 RVA: 0x00045713 File Offset: 0x00043913
		private static int ReadCodeSize(MethodDefinition method)
		{
			return method.Module.Read<MethodDefinition, int>(method, (MethodDefinition m, MetadataReader reader) => reader.ReadCodeSize(m));
		}

		// Token: 0x060015C9 RID: 5577 RVA: 0x00045740 File Offset: 0x00043940
		private static void ReadLocalVariables(MethodEntry entry, ScopeDebugInformation[] scopes)
		{
			foreach (LocalVariableEntry local in entry.GetLocals())
			{
				VariableDebugInformation variable = new VariableDebugInformation(local.Index, local.Name);
				int index = local.BlockIndex;
				if (index >= 0 && index < scopes.Length)
				{
					ScopeDebugInformation scope = scopes[index];
					if (scope != null)
					{
						scope.Variables.Add(variable);
					}
				}
			}
		}

		// Token: 0x060015CA RID: 5578 RVA: 0x000457A8 File Offset: 0x000439A8
		private void ReadLineNumbers(MethodEntry entry, MethodDebugInformation info)
		{
			LineNumberTable table = entry.GetLineNumberTable();
			info.sequence_points = new Collection<SequencePoint>(table.LineNumbers.Length);
			for (int i = 0; i < table.LineNumbers.Length; i++)
			{
				LineNumberEntry line = table.LineNumbers[i];
				if (i <= 0 || table.LineNumbers[i - 1].Offset != line.Offset)
				{
					info.sequence_points.Add(this.LineToSequencePoint(line));
				}
			}
		}

		// Token: 0x060015CB RID: 5579 RVA: 0x00045818 File Offset: 0x00043A18
		private Document GetDocument(SourceFileEntry file)
		{
			string file_name = file.FileName;
			Document document;
			if (this.documents.TryGetValue(file_name, out document))
			{
				return document;
			}
			document = new Document(file_name)
			{
				Hash = file.Checksum
			};
			this.documents.Add(file_name, document);
			return document;
		}

		// Token: 0x060015CC RID: 5580 RVA: 0x00045860 File Offset: 0x00043A60
		private static ScopeDebugInformation[] ReadScopes(MethodEntry entry, MethodDebugInformation info)
		{
			CodeBlockEntry[] codeBlocks = entry.GetCodeBlocks();
			ScopeDebugInformation[] scopes = new ScopeDebugInformation[codeBlocks.Length + 1];
			ScopeDebugInformation[] array = scopes;
			int num = 0;
			ScopeDebugInformation scopeDebugInformation = new ScopeDebugInformation();
			scopeDebugInformation.Start = new InstructionOffset(0);
			scopeDebugInformation.End = new InstructionOffset(info.code_size);
			ScopeDebugInformation scope2 = scopeDebugInformation;
			array[num] = scopeDebugInformation;
			info.scope = scope2;
			foreach (CodeBlockEntry block in codeBlocks)
			{
				if (block.BlockType == CodeBlockEntry.Type.Lexical || block.BlockType == CodeBlockEntry.Type.CompilerGenerated)
				{
					ScopeDebugInformation scope = new ScopeDebugInformation();
					scope.Start = new InstructionOffset(block.StartOffset);
					scope.End = new InstructionOffset(block.EndOffset);
					scopes[block.Index + 1] = scope;
					if (!MdbReader.AddScope(info.scope.Scopes, scope))
					{
						info.scope.Scopes.Add(scope);
					}
				}
			}
			return scopes;
		}

		// Token: 0x060015CD RID: 5581 RVA: 0x00045938 File Offset: 0x00043B38
		private static bool AddScope(Collection<ScopeDebugInformation> scopes, ScopeDebugInformation scope)
		{
			foreach (ScopeDebugInformation sub_scope in scopes)
			{
				if (sub_scope.HasScopes && MdbReader.AddScope(sub_scope.Scopes, scope))
				{
					return true;
				}
				if (scope.Start.Offset >= sub_scope.Start.Offset && scope.End.Offset <= sub_scope.End.Offset)
				{
					sub_scope.Scopes.Add(scope);
					return true;
				}
			}
			return false;
		}

		// Token: 0x060015CE RID: 5582 RVA: 0x000459E8 File Offset: 0x00043BE8
		private SequencePoint LineToSequencePoint(LineNumberEntry line)
		{
			SourceFileEntry source = this.symbol_file.GetSourceFile(line.File);
			return new SequencePoint(line.Offset, this.GetDocument(source))
			{
				StartLine = line.Row,
				EndLine = line.EndRow,
				StartColumn = line.Column,
				EndColumn = line.EndColumn
			};
		}

		// Token: 0x060015CF RID: 5583 RVA: 0x00045A49 File Offset: 0x00043C49
		public Collection<CustomDebugInformation> Read(ICustomDebugInformationProvider provider)
		{
			return new Collection<CustomDebugInformation>();
		}

		// Token: 0x060015D0 RID: 5584 RVA: 0x00045A50 File Offset: 0x00043C50
		public void Dispose()
		{
			this.symbol_file.Dispose();
		}

		// Token: 0x04000B23 RID: 2851
		private readonly ModuleDefinition module;

		// Token: 0x04000B24 RID: 2852
		private readonly MonoSymbolFile symbol_file;

		// Token: 0x04000B25 RID: 2853
		private readonly Dictionary<string, Document> documents;
	}
}
