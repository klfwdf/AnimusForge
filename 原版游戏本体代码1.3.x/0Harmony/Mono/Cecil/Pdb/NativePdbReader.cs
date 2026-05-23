using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Cci.Pdb;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Cecil.Pdb
{
	// Token: 0x0200035B RID: 859
	internal class NativePdbReader : ISymbolReader, IDisposable
	{
		// Token: 0x060016ED RID: 5869 RVA: 0x00046110 File Offset: 0x00044310
		internal NativePdbReader(Disposable<Stream> file)
		{
			this.pdb_file = file;
		}

		// Token: 0x060016EE RID: 5870 RVA: 0x00046140 File Offset: 0x00044340
		public ISymbolWriterProvider GetWriterProvider()
		{
			return new NativePdbWriterProvider();
		}

		// Token: 0x060016EF RID: 5871 RVA: 0x00046148 File Offset: 0x00044348
		public bool ProcessDebugHeader(ImageDebugHeader header)
		{
			if (!header.HasEntries)
			{
				return false;
			}
			using (this.pdb_file)
			{
				PdbInfo info = PdbFile.LoadFunctions(this.pdb_file.value);
				foreach (ImageDebugHeaderEntry entry in header.Entries)
				{
					if (NativePdbReader.IsMatchingEntry(info, entry))
					{
						foreach (PdbFunction function in info.Functions)
						{
							this.functions.Add(function.token, function);
						}
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x060016F0 RID: 5872 RVA: 0x000461FC File Offset: 0x000443FC
		private static bool IsMatchingEntry(PdbInfo info, ImageDebugHeaderEntry entry)
		{
			if (entry.Directory.Type != ImageDebugType.CodeView)
			{
				return false;
			}
			byte[] data = entry.Data;
			if (data.Length < 24)
			{
				return false;
			}
			if (NativePdbReader.ReadInt32(data, 0) != 1396986706)
			{
				return false;
			}
			byte[] guid_bytes = new byte[16];
			Buffer.BlockCopy(data, 4, guid_bytes, 0, 16);
			return info.Guid == new Guid(guid_bytes);
		}

		// Token: 0x060016F1 RID: 5873 RVA: 0x0003FE90 File Offset: 0x0003E090
		private static int ReadInt32(byte[] bytes, int start)
		{
			return (int)bytes[start] | ((int)bytes[start + 1] << 8) | ((int)bytes[start + 2] << 16) | ((int)bytes[start + 3] << 24);
		}

		// Token: 0x060016F2 RID: 5874 RVA: 0x00046260 File Offset: 0x00044460
		public MethodDebugInformation Read(MethodDefinition method)
		{
			MetadataToken method_token = method.MetadataToken;
			PdbFunction function;
			if (!this.functions.TryGetValue(method_token.ToUInt32(), out function))
			{
				return null;
			}
			MethodDebugInformation symbol = new MethodDebugInformation(method);
			this.ReadSequencePoints(function, symbol);
			MethodDebugInformation methodDebugInformation = symbol;
			ScopeDebugInformation scope;
			if (function.scopes.IsNullOrEmpty<PdbScope>())
			{
				ScopeDebugInformation scopeDebugInformation = new ScopeDebugInformation();
				scopeDebugInformation.Start = new InstructionOffset(0);
				scope = scopeDebugInformation;
				scopeDebugInformation.End = new InstructionOffset((int)function.length);
			}
			else
			{
				scope = this.ReadScopeAndLocals(function.scopes[0], symbol);
			}
			methodDebugInformation.scope = scope;
			if (function.tokenOfMethodWhoseUsingInfoAppliesToThisMethod != method.MetadataToken.ToUInt32() && function.tokenOfMethodWhoseUsingInfoAppliesToThisMethod != 0U)
			{
				symbol.scope.import = this.GetImport(function.tokenOfMethodWhoseUsingInfoAppliesToThisMethod, method.Module);
			}
			if (function.scopes.Length > 1)
			{
				for (int i = 1; i < function.scopes.Length; i++)
				{
					ScopeDebugInformation s = this.ReadScopeAndLocals(function.scopes[i], symbol);
					if (!NativePdbReader.AddScope(symbol.scope.Scopes, s))
					{
						symbol.scope.Scopes.Add(s);
					}
				}
			}
			if (function.iteratorScopes != null)
			{
				StateMachineScopeDebugInformation state_machine = new StateMachineScopeDebugInformation();
				foreach (ILocalScope iterator_scope in function.iteratorScopes)
				{
					state_machine.Scopes.Add(new StateMachineScope((int)iterator_scope.Offset, (int)(iterator_scope.Offset + iterator_scope.Length + 1U)));
				}
				symbol.CustomDebugInformations.Add(state_machine);
			}
			if (function.synchronizationInformation != null)
			{
				AsyncMethodBodyDebugInformation async_debug_info = new AsyncMethodBodyDebugInformation((int)function.synchronizationInformation.GeneratedCatchHandlerOffset);
				foreach (PdbSynchronizationPoint synchronization_point in function.synchronizationInformation.synchronizationPoints)
				{
					async_debug_info.Yields.Add(new InstructionOffset((int)synchronization_point.SynchronizeOffset));
					async_debug_info.Resumes.Add(new InstructionOffset((int)synchronization_point.ContinuationOffset));
					async_debug_info.ResumeMethods.Add(method);
				}
				symbol.CustomDebugInformations.Add(async_debug_info);
				symbol.StateMachineKickOffMethod = (MethodDefinition)method.Module.LookupToken((int)function.synchronizationInformation.kickoffMethodToken);
			}
			return symbol;
		}

		// Token: 0x060016F3 RID: 5875 RVA: 0x000464AC File Offset: 0x000446AC
		private Collection<ScopeDebugInformation> ReadScopeAndLocals(PdbScope[] scopes, MethodDebugInformation info)
		{
			Collection<ScopeDebugInformation> symbols = new Collection<ScopeDebugInformation>(scopes.Length);
			foreach (PdbScope scope in scopes)
			{
				if (scope != null)
				{
					symbols.Add(this.ReadScopeAndLocals(scope, info));
				}
			}
			return symbols;
		}

		// Token: 0x060016F4 RID: 5876 RVA: 0x000464E8 File Offset: 0x000446E8
		private ScopeDebugInformation ReadScopeAndLocals(PdbScope scope, MethodDebugInformation info)
		{
			ScopeDebugInformation parent = new ScopeDebugInformation();
			parent.Start = new InstructionOffset((int)scope.offset);
			parent.End = new InstructionOffset((int)(scope.offset + scope.length));
			if (!scope.slots.IsNullOrEmpty<PdbSlot>())
			{
				parent.variables = new Collection<VariableDebugInformation>(scope.slots.Length);
				foreach (PdbSlot slot in scope.slots)
				{
					if ((slot.flags & 1) == 0)
					{
						VariableDebugInformation variable = new VariableDebugInformation((int)slot.slot, slot.name);
						if ((slot.flags & 4) != 0)
						{
							variable.IsDebuggerHidden = true;
						}
						parent.variables.Add(variable);
					}
				}
			}
			if (!scope.constants.IsNullOrEmpty<PdbConstant>())
			{
				parent.constants = new Collection<ConstantDebugInformation>(scope.constants.Length);
				foreach (PdbConstant constant in scope.constants)
				{
					TypeReference type = info.Method.Module.Read<PdbConstant, TypeReference>(constant, (PdbConstant c, MetadataReader r) => r.ReadConstantSignature(new MetadataToken(c.token)));
					object value = constant.value;
					if (type != null && !type.IsValueType && value is int && (int)value == 0)
					{
						value = null;
					}
					parent.constants.Add(new ConstantDebugInformation(constant.name, type, value));
				}
			}
			if (!scope.usedNamespaces.IsNullOrEmpty<string>())
			{
				ImportDebugInformation import;
				if (this.imports.TryGetValue(scope, out import))
				{
					parent.import = import;
				}
				else
				{
					import = NativePdbReader.GetImport(scope, info.Method.Module);
					this.imports.Add(scope, import);
					parent.import = import;
				}
			}
			parent.scopes = this.ReadScopeAndLocals(scope.scopes, info);
			return parent;
		}

		// Token: 0x060016F5 RID: 5877 RVA: 0x000466BC File Offset: 0x000448BC
		private static bool AddScope(Collection<ScopeDebugInformation> scopes, ScopeDebugInformation scope)
		{
			foreach (ScopeDebugInformation sub_scope in scopes)
			{
				if (sub_scope.HasScopes && NativePdbReader.AddScope(sub_scope.Scopes, scope))
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

		// Token: 0x060016F6 RID: 5878 RVA: 0x0004676C File Offset: 0x0004496C
		private ImportDebugInformation GetImport(uint token, ModuleDefinition module)
		{
			PdbFunction function;
			if (!this.functions.TryGetValue(token, out function))
			{
				return null;
			}
			if (function.scopes.Length != 1)
			{
				return null;
			}
			PdbScope scope = function.scopes[0];
			ImportDebugInformation import;
			if (this.imports.TryGetValue(scope, out import))
			{
				return import;
			}
			import = NativePdbReader.GetImport(scope, module);
			this.imports.Add(scope, import);
			return import;
		}

		// Token: 0x060016F7 RID: 5879 RVA: 0x000467CC File Offset: 0x000449CC
		private static ImportDebugInformation GetImport(PdbScope scope, ModuleDefinition module)
		{
			if (scope.usedNamespaces.IsNullOrEmpty<string>())
			{
				return null;
			}
			ImportDebugInformation import = new ImportDebugInformation();
			foreach (string used_namespace in scope.usedNamespaces)
			{
				if (!string.IsNullOrEmpty(used_namespace))
				{
					ImportTarget target = null;
					string value = used_namespace.Substring(1);
					char c = used_namespace[0];
					if (c <= '@')
					{
						if (c != '*')
						{
							if (c == '@')
							{
								if (!value.StartsWith("P:"))
								{
									goto IL_194;
								}
								target = new ImportTarget(ImportTargetKind.ImportNamespace)
								{
									@namespace = value.Substring(2)
								};
							}
						}
						else
						{
							target = new ImportTarget(ImportTargetKind.ImportNamespace)
							{
								@namespace = value
							};
						}
					}
					else if (c != 'A')
					{
						if (c != 'T')
						{
							if (c == 'U')
							{
								target = new ImportTarget(ImportTargetKind.ImportNamespace)
								{
									@namespace = value
								};
							}
						}
						else
						{
							TypeReference type = TypeParser.ParseType(module, value, false);
							if (type != null)
							{
								target = new ImportTarget(ImportTargetKind.ImportType)
								{
									type = type
								};
							}
						}
					}
					else
					{
						int index = used_namespace.IndexOf(' ');
						if (index < 0)
						{
							target = new ImportTarget(ImportTargetKind.ImportNamespace)
							{
								@namespace = used_namespace
							};
						}
						else
						{
							string alias_value = used_namespace.Substring(1, index - 1);
							string alias_target_value = used_namespace.Substring(index + 2);
							char c2 = used_namespace[index + 1];
							if (c2 != 'T')
							{
								if (c2 == 'U')
								{
									target = new ImportTarget(ImportTargetKind.DefineNamespaceAlias)
									{
										alias = alias_value,
										@namespace = alias_target_value
									};
								}
							}
							else
							{
								TypeReference type2 = TypeParser.ParseType(module, alias_target_value, false);
								if (type2 != null)
								{
									target = new ImportTarget(ImportTargetKind.DefineTypeAlias)
									{
										alias = alias_value,
										type = type2
									};
								}
							}
						}
					}
					if (target != null)
					{
						import.Targets.Add(target);
					}
				}
				IL_194:;
			}
			return import;
		}

		// Token: 0x060016F8 RID: 5880 RVA: 0x0004697C File Offset: 0x00044B7C
		private void ReadSequencePoints(PdbFunction function, MethodDebugInformation info)
		{
			if (function.lines == null)
			{
				return;
			}
			info.sequence_points = new Collection<SequencePoint>();
			foreach (PdbLines lines in function.lines)
			{
				this.ReadLines(lines, info);
			}
		}

		// Token: 0x060016F9 RID: 5881 RVA: 0x000469C0 File Offset: 0x00044BC0
		private void ReadLines(PdbLines lines, MethodDebugInformation info)
		{
			Document document = this.GetDocument(lines.file);
			PdbLine[] lines2 = lines.lines;
			for (int i = 0; i < lines2.Length; i++)
			{
				NativePdbReader.ReadLine(lines2[i], document, info);
			}
		}

		// Token: 0x060016FA RID: 5882 RVA: 0x00046A00 File Offset: 0x00044C00
		private static void ReadLine(PdbLine line, Document document, MethodDebugInformation info)
		{
			SequencePoint sequence_point = new SequencePoint((int)line.offset, document);
			sequence_point.StartLine = (int)line.lineBegin;
			sequence_point.StartColumn = (int)line.colBegin;
			sequence_point.EndLine = (int)line.lineEnd;
			sequence_point.EndColumn = (int)line.colEnd;
			info.sequence_points.Add(sequence_point);
		}

		// Token: 0x060016FB RID: 5883 RVA: 0x00046A58 File Offset: 0x00044C58
		private Document GetDocument(PdbSource source)
		{
			string name = source.name;
			Document document;
			if (this.documents.TryGetValue(name, out document))
			{
				return document;
			}
			document = new Document(name)
			{
				LanguageGuid = source.language,
				LanguageVendorGuid = source.vendor,
				TypeGuid = source.doctype,
				HashAlgorithmGuid = source.checksumAlgorithm,
				Hash = source.checksum
			};
			this.documents.Add(name, document);
			return document;
		}

		// Token: 0x060016FC RID: 5884 RVA: 0x00045A49 File Offset: 0x00043C49
		public Collection<CustomDebugInformation> Read(ICustomDebugInformationProvider provider)
		{
			return new Collection<CustomDebugInformation>();
		}

		// Token: 0x060016FD RID: 5885 RVA: 0x00046AD0 File Offset: 0x00044CD0
		public void Dispose()
		{
			this.pdb_file.Dispose();
		}

		// Token: 0x04000B33 RID: 2867
		private readonly Disposable<Stream> pdb_file;

		// Token: 0x04000B34 RID: 2868
		private readonly Dictionary<string, Document> documents = new Dictionary<string, Document>();

		// Token: 0x04000B35 RID: 2869
		private readonly Dictionary<uint, PdbFunction> functions = new Dictionary<uint, PdbFunction>();

		// Token: 0x04000B36 RID: 2870
		private readonly Dictionary<PdbScope, ImportDebugInformation> imports = new Dictionary<PdbScope, ImportDebugInformation>();
	}
}
