using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil.Pdb
{
	// Token: 0x0200035D RID: 861
	internal class NativePdbWriter : ISymbolWriter, IDisposable
	{
		// Token: 0x06001701 RID: 5889 RVA: 0x00046B0A File Offset: 0x00044D0A
		internal NativePdbWriter(ModuleDefinition module, SymWriter writer)
		{
			this.module = module;
			this.metadata = module.metadata_builder;
			this.writer = writer;
			this.documents = new Dictionary<string, SymDocumentWriter>();
			this.import_info_to_parent = new Dictionary<ImportDebugInformation, MetadataToken>();
		}

		// Token: 0x06001702 RID: 5890 RVA: 0x00046B42 File Offset: 0x00044D42
		public ISymbolReaderProvider GetReaderProvider()
		{
			return new NativePdbReaderProvider();
		}

		// Token: 0x06001703 RID: 5891 RVA: 0x00046B49 File Offset: 0x00044D49
		public ImageDebugHeader GetDebugHeader()
		{
			return new ImageDebugHeader(new ImageDebugHeaderEntry(this.debug_directory, this.debug_info));
		}

		// Token: 0x06001704 RID: 5892 RVA: 0x00046B64 File Offset: 0x00044D64
		public void Write(MethodDebugInformation info)
		{
			int sym_token = info.method.MetadataToken.ToInt32();
			if (!info.HasSequencePoints && info.scope == null && !info.HasCustomDebugInformations && info.StateMachineKickOffMethod == null)
			{
				return;
			}
			this.writer.OpenMethod(sym_token);
			if (!info.sequence_points.IsNullOrEmpty<SequencePoint>())
			{
				this.DefineSequencePoints(info.sequence_points);
			}
			MetadataToken import_parent = default(MetadataToken);
			if (info.scope != null)
			{
				this.DefineScope(info.scope, info, out import_parent);
			}
			this.DefineCustomMetadata(info, import_parent);
			this.writer.CloseMethod();
		}

		// Token: 0x06001705 RID: 5893 RVA: 0x00046C00 File Offset: 0x00044E00
		private void DefineCustomMetadata(MethodDebugInformation info, MetadataToken import_parent)
		{
			CustomMetadataWriter metadata = new CustomMetadataWriter(this.writer);
			if (import_parent.RID != 0U)
			{
				metadata.WriteForwardInfo(import_parent);
			}
			else if (info.scope != null && info.scope.Import != null && info.scope.Import.HasTargets)
			{
				metadata.WriteUsingInfo(info.scope.Import);
			}
			if (info.Method.HasCustomAttributes)
			{
				foreach (CustomAttribute attribute in info.Method.CustomAttributes)
				{
					TypeReference attribute_type = attribute.AttributeType;
					if (attribute_type.IsTypeOf("System.Runtime.CompilerServices", "IteratorStateMachineAttribute") || attribute_type.IsTypeOf("System.Runtime.CompilerServices", "AsyncStateMachineAttribute"))
					{
						TypeReference type = attribute.ConstructorArguments[0].Value as TypeReference;
						if (type != null)
						{
							metadata.WriteForwardIterator(type);
						}
					}
				}
			}
			if (info.HasCustomDebugInformations)
			{
				StateMachineScopeDebugInformation state_machine = info.CustomDebugInformations.FirstOrDefault((CustomDebugInformation cdi) => cdi.Kind == CustomDebugInformationKind.StateMachineScope) as StateMachineScopeDebugInformation;
				if (state_machine != null)
				{
					metadata.WriteIteratorScopes(state_machine, info);
				}
			}
			metadata.WriteCustomMetadata();
			this.DefineAsyncCustomMetadata(info);
		}

		// Token: 0x06001706 RID: 5894 RVA: 0x00046D60 File Offset: 0x00044F60
		private void DefineAsyncCustomMetadata(MethodDebugInformation info)
		{
			if (!info.HasCustomDebugInformations)
			{
				return;
			}
			foreach (CustomDebugInformation customDebugInformation in info.CustomDebugInformations)
			{
				AsyncMethodBodyDebugInformation async_debug_info = customDebugInformation as AsyncMethodBodyDebugInformation;
				if (async_debug_info != null)
				{
					using (MemoryStream stream = new MemoryStream())
					{
						BinaryStreamWriter async_metadata = new BinaryStreamWriter(stream);
						async_metadata.WriteUInt32((info.StateMachineKickOffMethod != null) ? info.StateMachineKickOffMethod.MetadataToken.ToUInt32() : 0U);
						async_metadata.WriteUInt32((uint)async_debug_info.CatchHandler.Offset);
						async_metadata.WriteUInt32((uint)async_debug_info.Resumes.Count);
						for (int i = 0; i < async_debug_info.Resumes.Count; i++)
						{
							async_metadata.WriteUInt32((uint)async_debug_info.Yields[i].Offset);
							async_metadata.WriteUInt32(async_debug_info.resume_methods[i].MetadataToken.ToUInt32());
							async_metadata.WriteUInt32((uint)async_debug_info.Resumes[i].Offset);
						}
						this.writer.DefineCustomMetadata("asyncMethodInfo", stream.ToArray());
					}
				}
			}
		}

		// Token: 0x06001707 RID: 5895 RVA: 0x00046ED8 File Offset: 0x000450D8
		private void DefineScope(ScopeDebugInformation scope, MethodDebugInformation info, out MetadataToken import_parent)
		{
			int start_offset = scope.Start.Offset;
			int end_offset = (scope.End.IsEndOfMethod ? info.code_size : scope.End.Offset);
			import_parent = new MetadataToken(0U);
			this.writer.OpenScope(start_offset);
			if (scope.Import != null && scope.Import.HasTargets && !this.import_info_to_parent.TryGetValue(info.scope.Import, out import_parent))
			{
				foreach (ImportTarget target in scope.Import.Targets)
				{
					ImportTargetKind kind = target.Kind;
					if (kind <= ImportTargetKind.ImportType)
					{
						if (kind != ImportTargetKind.ImportNamespace)
						{
							if (kind == ImportTargetKind.ImportType)
							{
								this.writer.UsingNamespace("T" + TypeParser.ToParseable(target.type, true));
							}
						}
						else
						{
							this.writer.UsingNamespace("U" + target.@namespace);
						}
					}
					else if (kind != ImportTargetKind.DefineNamespaceAlias)
					{
						if (kind == ImportTargetKind.DefineTypeAlias)
						{
							this.writer.UsingNamespace("A" + target.Alias + " T" + TypeParser.ToParseable(target.type, true));
						}
					}
					else
					{
						this.writer.UsingNamespace("A" + target.Alias + " U" + target.@namespace);
					}
				}
				this.import_info_to_parent.Add(info.scope.Import, info.method.MetadataToken);
			}
			int sym_token = info.local_var_token.ToInt32();
			if (!scope.variables.IsNullOrEmpty<VariableDebugInformation>())
			{
				for (int i = 0; i < scope.variables.Count; i++)
				{
					VariableDebugInformation variable = scope.variables[i];
					this.DefineLocalVariable(variable, sym_token, start_offset, end_offset);
				}
			}
			if (!scope.constants.IsNullOrEmpty<ConstantDebugInformation>())
			{
				for (int j = 0; j < scope.constants.Count; j++)
				{
					ConstantDebugInformation constant = scope.constants[j];
					this.DefineConstant(constant);
				}
			}
			if (!scope.scopes.IsNullOrEmpty<ScopeDebugInformation>())
			{
				for (int k = 0; k < scope.scopes.Count; k++)
				{
					MetadataToken _;
					this.DefineScope(scope.scopes[k], info, out _);
				}
			}
			this.writer.CloseScope(end_offset);
		}

		// Token: 0x06001708 RID: 5896 RVA: 0x00047174 File Offset: 0x00045374
		private void DefineSequencePoints(Collection<SequencePoint> sequence_points)
		{
			for (int i = 0; i < sequence_points.Count; i++)
			{
				SequencePoint sequence_point = sequence_points[i];
				this.writer.DefineSequencePoints(this.GetDocument(sequence_point.Document), new int[] { sequence_point.Offset }, new int[] { sequence_point.StartLine }, new int[] { sequence_point.StartColumn }, new int[] { sequence_point.EndLine }, new int[] { sequence_point.EndColumn });
			}
		}

		// Token: 0x06001709 RID: 5897 RVA: 0x000471FC File Offset: 0x000453FC
		private void DefineLocalVariable(VariableDebugInformation variable, int local_var_token, int start_offset, int end_offset)
		{
			this.writer.DefineLocalVariable2(variable.Name, variable.Attributes, local_var_token, variable.Index, 0, 0, start_offset, end_offset);
		}

		// Token: 0x0600170A RID: 5898 RVA: 0x0004722C File Offset: 0x0004542C
		private void DefineConstant(ConstantDebugInformation constant)
		{
			uint row = this.metadata.AddStandAloneSignature(this.metadata.GetConstantTypeBlobIndex(constant.ConstantType));
			MetadataToken token = new MetadataToken(TokenType.Signature, row);
			this.writer.DefineConstant2(constant.Name, constant.Value, token.ToInt32());
		}

		// Token: 0x0600170B RID: 5899 RVA: 0x00047284 File Offset: 0x00045484
		private SymDocumentWriter GetDocument(Document document)
		{
			if (document == null)
			{
				return null;
			}
			SymDocumentWriter doc_writer;
			if (this.documents.TryGetValue(document.Url, out doc_writer))
			{
				return doc_writer;
			}
			doc_writer = this.writer.DefineDocument(document.Url, document.LanguageGuid, document.LanguageVendorGuid, document.TypeGuid);
			if (!document.Hash.IsNullOrEmpty<byte>())
			{
				doc_writer.SetCheckSum(document.HashAlgorithmGuid, document.Hash);
			}
			this.documents[document.Url] = doc_writer;
			return doc_writer;
		}

		// Token: 0x0600170C RID: 5900 RVA: 0x00047304 File Offset: 0x00045504
		public void Write()
		{
			MethodDefinition entry_point = this.module.EntryPoint;
			if (entry_point != null)
			{
				this.writer.SetUserEntryPoint(entry_point.MetadataToken.ToInt32());
			}
			this.debug_info = this.writer.GetDebugInfo(out this.debug_directory);
			this.debug_directory.TimeDateStamp = (int)this.module.timestamp;
			this.writer.Close();
		}

		// Token: 0x0600170D RID: 5901 RVA: 0x0001B842 File Offset: 0x00019A42
		public void Write(ICustomDebugInformationProvider provider)
		{
		}

		// Token: 0x0600170E RID: 5902 RVA: 0x00047371 File Offset: 0x00045571
		public void Dispose()
		{
			this.writer.Close();
		}

		// Token: 0x04000B39 RID: 2873
		private readonly ModuleDefinition module;

		// Token: 0x04000B3A RID: 2874
		private readonly MetadataBuilder metadata;

		// Token: 0x04000B3B RID: 2875
		private readonly SymWriter writer;

		// Token: 0x04000B3C RID: 2876
		private readonly Dictionary<string, SymDocumentWriter> documents;

		// Token: 0x04000B3D RID: 2877
		private readonly Dictionary<ImportDebugInformation, MetadataToken> import_info_to_parent;

		// Token: 0x04000B3E RID: 2878
		private ImageDebugDirectory debug_directory;

		// Token: 0x04000B3F RID: 2879
		private byte[] debug_info;
	}
}
