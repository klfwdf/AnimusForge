using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000335 RID: 821
	internal class MonoSymbolFile : IDisposable
	{
		// Token: 0x060014F0 RID: 5360 RVA: 0x00041DAC File Offset: 0x0003FFAC
		public MonoSymbolFile()
		{
			this.ot = new OffsetTable();
		}

		// Token: 0x060014F1 RID: 5361 RVA: 0x00041DE8 File Offset: 0x0003FFE8
		public int AddSource(SourceFileEntry source)
		{
			this.sources.Add(source);
			return this.sources.Count;
		}

		// Token: 0x060014F2 RID: 5362 RVA: 0x00041E01 File Offset: 0x00040001
		public int AddCompileUnit(CompileUnitEntry entry)
		{
			this.comp_units.Add(entry);
			return this.comp_units.Count;
		}

		// Token: 0x060014F3 RID: 5363 RVA: 0x00041E1A File Offset: 0x0004001A
		public void AddMethod(MethodEntry entry)
		{
			this.methods.Add(entry);
		}

		// Token: 0x060014F4 RID: 5364 RVA: 0x00041E28 File Offset: 0x00040028
		public MethodEntry DefineMethod(CompileUnitEntry comp_unit, int token, ScopeVariable[] scope_vars, LocalVariableEntry[] locals, LineNumberEntry[] lines, CodeBlockEntry[] code_blocks, string real_name, MethodEntry.Flags flags, int namespace_id)
		{
			if (this.reader != null)
			{
				throw new InvalidOperationException();
			}
			MethodEntry method = new MethodEntry(this, comp_unit, token, scope_vars, locals, lines, code_blocks, real_name, flags, namespace_id);
			this.AddMethod(method);
			return method;
		}

		// Token: 0x060014F5 RID: 5365 RVA: 0x00041E61 File Offset: 0x00040061
		internal void DefineAnonymousScope(int id)
		{
			if (this.reader != null)
			{
				throw new InvalidOperationException();
			}
			if (this.anonymous_scopes == null)
			{
				this.anonymous_scopes = new Dictionary<int, AnonymousScopeEntry>();
			}
			this.anonymous_scopes.Add(id, new AnonymousScopeEntry(id));
		}

		// Token: 0x060014F6 RID: 5366 RVA: 0x00041E96 File Offset: 0x00040096
		internal void DefineCapturedVariable(int scope_id, string name, string captured_name, CapturedVariable.CapturedKind kind)
		{
			if (this.reader != null)
			{
				throw new InvalidOperationException();
			}
			this.anonymous_scopes[scope_id].AddCapturedVariable(name, captured_name, kind);
		}

		// Token: 0x060014F7 RID: 5367 RVA: 0x00041EBB File Offset: 0x000400BB
		internal void DefineCapturedScope(int scope_id, int id, string captured_name)
		{
			if (this.reader != null)
			{
				throw new InvalidOperationException();
			}
			this.anonymous_scopes[scope_id].AddCapturedScope(id, captured_name);
		}

		// Token: 0x060014F8 RID: 5368 RVA: 0x00041EE0 File Offset: 0x000400E0
		internal int GetNextTypeIndex()
		{
			int result = this.last_type_index + 1;
			this.last_type_index = result;
			return result;
		}

		// Token: 0x060014F9 RID: 5369 RVA: 0x00041F00 File Offset: 0x00040100
		internal int GetNextMethodIndex()
		{
			int result = this.last_method_index + 1;
			this.last_method_index = result;
			return result;
		}

		// Token: 0x060014FA RID: 5370 RVA: 0x00041F20 File Offset: 0x00040120
		internal int GetNextNamespaceIndex()
		{
			int result = this.last_namespace_index + 1;
			this.last_namespace_index = result;
			return result;
		}

		// Token: 0x060014FB RID: 5371 RVA: 0x00041F40 File Offset: 0x00040140
		private void Write(MyBinaryWriter bw, Guid guid)
		{
			bw.Write(5037318119232611860L);
			bw.Write(this.MajorVersion);
			bw.Write(this.MinorVersion);
			bw.Write(guid.ToByteArray());
			long offset_table_offset = bw.BaseStream.Position;
			this.ot.Write(bw, this.MajorVersion, this.MinorVersion);
			this.methods.Sort();
			for (int i = 0; i < this.methods.Count; i++)
			{
				this.methods[i].Index = i + 1;
			}
			this.ot.DataSectionOffset = (int)bw.BaseStream.Position;
			foreach (SourceFileEntry sourceFileEntry in this.sources)
			{
				sourceFileEntry.WriteData(bw);
			}
			foreach (CompileUnitEntry compileUnitEntry in this.comp_units)
			{
				compileUnitEntry.WriteData(bw);
			}
			foreach (MethodEntry methodEntry in this.methods)
			{
				methodEntry.WriteData(this, bw);
			}
			this.ot.DataSectionSize = (int)bw.BaseStream.Position - this.ot.DataSectionOffset;
			this.ot.MethodTableOffset = (int)bw.BaseStream.Position;
			for (int j = 0; j < this.methods.Count; j++)
			{
				this.methods[j].Write(bw);
			}
			this.ot.MethodTableSize = (int)bw.BaseStream.Position - this.ot.MethodTableOffset;
			this.ot.SourceTableOffset = (int)bw.BaseStream.Position;
			for (int k = 0; k < this.sources.Count; k++)
			{
				this.sources[k].Write(bw);
			}
			this.ot.SourceTableSize = (int)bw.BaseStream.Position - this.ot.SourceTableOffset;
			this.ot.CompileUnitTableOffset = (int)bw.BaseStream.Position;
			for (int l = 0; l < this.comp_units.Count; l++)
			{
				this.comp_units[l].Write(bw);
			}
			this.ot.CompileUnitTableSize = (int)bw.BaseStream.Position - this.ot.CompileUnitTableOffset;
			this.ot.AnonymousScopeCount = ((this.anonymous_scopes != null) ? this.anonymous_scopes.Count : 0);
			this.ot.AnonymousScopeTableOffset = (int)bw.BaseStream.Position;
			if (this.anonymous_scopes != null)
			{
				foreach (AnonymousScopeEntry anonymousScopeEntry in this.anonymous_scopes.Values)
				{
					anonymousScopeEntry.Write(bw);
				}
			}
			this.ot.AnonymousScopeTableSize = (int)bw.BaseStream.Position - this.ot.AnonymousScopeTableOffset;
			this.ot.TypeCount = this.last_type_index;
			this.ot.MethodCount = this.methods.Count;
			this.ot.SourceCount = this.sources.Count;
			this.ot.CompileUnitCount = this.comp_units.Count;
			this.ot.TotalFileSize = (int)bw.BaseStream.Position;
			bw.Seek((int)offset_table_offset, SeekOrigin.Begin);
			this.ot.Write(bw, this.MajorVersion, this.MinorVersion);
			bw.Seek(0, SeekOrigin.End);
		}

		// Token: 0x060014FC RID: 5372 RVA: 0x00042350 File Offset: 0x00040550
		public void CreateSymbolFile(Guid guid, FileStream fs)
		{
			if (this.reader != null)
			{
				throw new InvalidOperationException();
			}
			this.Write(new MyBinaryWriter(fs), guid);
		}

		// Token: 0x060014FD RID: 5373 RVA: 0x00042370 File Offset: 0x00040570
		private MonoSymbolFile(Stream stream)
		{
			this.reader = new MyBinaryReader(stream);
			try
			{
				long num = this.reader.ReadInt64();
				int major_version = this.reader.ReadInt32();
				int minor_version = this.reader.ReadInt32();
				if (num != 5037318119232611860L)
				{
					throw new MonoSymbolFileException("Symbol file is not a valid", new object[0]);
				}
				if (major_version != 50)
				{
					throw new MonoSymbolFileException("Symbol file has version {0} but expected {1}", new object[] { major_version, 50 });
				}
				if (minor_version != 0)
				{
					throw new MonoSymbolFileException("Symbol file has version {0}.{1} but expected {2}.{3}", new object[] { major_version, minor_version, 50, 0 });
				}
				this.MajorVersion = major_version;
				this.MinorVersion = minor_version;
				this.guid = new Guid(this.reader.ReadBytes(16));
				this.ot = new OffsetTable(this.reader, major_version, minor_version);
			}
			catch (Exception e)
			{
				throw new MonoSymbolFileException("Cannot read symbol file", e);
			}
			this.source_file_hash = new Dictionary<int, SourceFileEntry>();
			this.compile_unit_hash = new Dictionary<int, CompileUnitEntry>();
		}

		// Token: 0x060014FE RID: 5374 RVA: 0x000424C8 File Offset: 0x000406C8
		public static MonoSymbolFile ReadSymbolFile(Assembly assembly)
		{
			string mdbFilename = assembly.Location + ".mdb";
			Guid assembly_guid = assembly.GetModules()[0].ModuleVersionId;
			return MonoSymbolFile.ReadSymbolFile(mdbFilename, assembly_guid);
		}

		// Token: 0x060014FF RID: 5375 RVA: 0x000424F9 File Offset: 0x000406F9
		public static MonoSymbolFile ReadSymbolFile(string mdbFilename)
		{
			return MonoSymbolFile.ReadSymbolFile(new FileStream(mdbFilename, FileMode.Open, FileAccess.Read));
		}

		// Token: 0x06001500 RID: 5376 RVA: 0x00042508 File Offset: 0x00040708
		public static MonoSymbolFile ReadSymbolFile(string mdbFilename, Guid assemblyGuid)
		{
			MonoSymbolFile sf = MonoSymbolFile.ReadSymbolFile(mdbFilename);
			if (assemblyGuid != sf.guid)
			{
				throw new MonoSymbolFileException("Symbol file `{0}' does not match assembly", new object[] { mdbFilename });
			}
			return sf;
		}

		// Token: 0x06001501 RID: 5377 RVA: 0x00042540 File Offset: 0x00040740
		public static MonoSymbolFile ReadSymbolFile(Stream stream)
		{
			return new MonoSymbolFile(stream);
		}

		// Token: 0x1700055B RID: 1371
		// (get) Token: 0x06001502 RID: 5378 RVA: 0x00042548 File Offset: 0x00040748
		public int CompileUnitCount
		{
			get
			{
				return this.ot.CompileUnitCount;
			}
		}

		// Token: 0x1700055C RID: 1372
		// (get) Token: 0x06001503 RID: 5379 RVA: 0x00042555 File Offset: 0x00040755
		public int SourceCount
		{
			get
			{
				return this.ot.SourceCount;
			}
		}

		// Token: 0x1700055D RID: 1373
		// (get) Token: 0x06001504 RID: 5380 RVA: 0x00042562 File Offset: 0x00040762
		public int MethodCount
		{
			get
			{
				return this.ot.MethodCount;
			}
		}

		// Token: 0x1700055E RID: 1374
		// (get) Token: 0x06001505 RID: 5381 RVA: 0x0004256F File Offset: 0x0004076F
		public int TypeCount
		{
			get
			{
				return this.ot.TypeCount;
			}
		}

		// Token: 0x1700055F RID: 1375
		// (get) Token: 0x06001506 RID: 5382 RVA: 0x0004257C File Offset: 0x0004077C
		public int AnonymousScopeCount
		{
			get
			{
				return this.ot.AnonymousScopeCount;
			}
		}

		// Token: 0x17000560 RID: 1376
		// (get) Token: 0x06001507 RID: 5383 RVA: 0x00042589 File Offset: 0x00040789
		public int NamespaceCount
		{
			get
			{
				return this.last_namespace_index;
			}
		}

		// Token: 0x17000561 RID: 1377
		// (get) Token: 0x06001508 RID: 5384 RVA: 0x00042591 File Offset: 0x00040791
		public Guid Guid
		{
			get
			{
				return this.guid;
			}
		}

		// Token: 0x17000562 RID: 1378
		// (get) Token: 0x06001509 RID: 5385 RVA: 0x00042599 File Offset: 0x00040799
		public OffsetTable OffsetTable
		{
			get
			{
				return this.ot;
			}
		}

		// Token: 0x0600150A RID: 5386 RVA: 0x000425A4 File Offset: 0x000407A4
		public SourceFileEntry GetSourceFile(int index)
		{
			if (index < 1 || index > this.ot.SourceCount)
			{
				throw new ArgumentException();
			}
			if (this.reader == null)
			{
				throw new InvalidOperationException();
			}
			SourceFileEntry result;
			lock (this)
			{
				SourceFileEntry source;
				if (this.source_file_hash.TryGetValue(index, out source))
				{
					result = source;
				}
				else
				{
					long old_pos = this.reader.BaseStream.Position;
					this.reader.BaseStream.Position = (long)(this.ot.SourceTableOffset + SourceFileEntry.Size * (index - 1));
					source = new SourceFileEntry(this, this.reader);
					this.source_file_hash.Add(index, source);
					this.reader.BaseStream.Position = old_pos;
					result = source;
				}
			}
			return result;
		}

		// Token: 0x17000563 RID: 1379
		// (get) Token: 0x0600150B RID: 5387 RVA: 0x0004267C File Offset: 0x0004087C
		public SourceFileEntry[] Sources
		{
			get
			{
				if (this.reader == null)
				{
					throw new InvalidOperationException();
				}
				SourceFileEntry[] retval = new SourceFileEntry[this.SourceCount];
				for (int i = 0; i < this.SourceCount; i++)
				{
					retval[i] = this.GetSourceFile(i + 1);
				}
				return retval;
			}
		}

		// Token: 0x0600150C RID: 5388 RVA: 0x000426C4 File Offset: 0x000408C4
		public CompileUnitEntry GetCompileUnit(int index)
		{
			if (index < 1 || index > this.ot.CompileUnitCount)
			{
				throw new ArgumentException();
			}
			if (this.reader == null)
			{
				throw new InvalidOperationException();
			}
			CompileUnitEntry result;
			lock (this)
			{
				CompileUnitEntry unit;
				if (this.compile_unit_hash.TryGetValue(index, out unit))
				{
					result = unit;
				}
				else
				{
					long old_pos = this.reader.BaseStream.Position;
					this.reader.BaseStream.Position = (long)(this.ot.CompileUnitTableOffset + CompileUnitEntry.Size * (index - 1));
					unit = new CompileUnitEntry(this, this.reader);
					this.compile_unit_hash.Add(index, unit);
					this.reader.BaseStream.Position = old_pos;
					result = unit;
				}
			}
			return result;
		}

		// Token: 0x17000564 RID: 1380
		// (get) Token: 0x0600150D RID: 5389 RVA: 0x0004279C File Offset: 0x0004099C
		public CompileUnitEntry[] CompileUnits
		{
			get
			{
				if (this.reader == null)
				{
					throw new InvalidOperationException();
				}
				CompileUnitEntry[] retval = new CompileUnitEntry[this.CompileUnitCount];
				for (int i = 0; i < this.CompileUnitCount; i++)
				{
					retval[i] = this.GetCompileUnit(i + 1);
				}
				return retval;
			}
		}

		// Token: 0x0600150E RID: 5390 RVA: 0x000427E4 File Offset: 0x000409E4
		private void read_methods()
		{
			lock (this)
			{
				if (this.method_token_hash == null)
				{
					this.method_token_hash = new Dictionary<int, MethodEntry>();
					this.method_list = new List<MethodEntry>();
					long old_pos = this.reader.BaseStream.Position;
					this.reader.BaseStream.Position = (long)this.ot.MethodTableOffset;
					for (int i = 0; i < this.MethodCount; i++)
					{
						MethodEntry entry = new MethodEntry(this, this.reader, i + 1);
						this.method_token_hash.Add(entry.Token, entry);
						this.method_list.Add(entry);
					}
					this.reader.BaseStream.Position = old_pos;
				}
			}
		}

		// Token: 0x0600150F RID: 5391 RVA: 0x000428C0 File Offset: 0x00040AC0
		public MethodEntry GetMethodByToken(int token)
		{
			if (this.reader == null)
			{
				throw new InvalidOperationException();
			}
			MethodEntry result;
			lock (this)
			{
				this.read_methods();
				MethodEntry me;
				this.method_token_hash.TryGetValue(token, out me);
				result = me;
			}
			return result;
		}

		// Token: 0x06001510 RID: 5392 RVA: 0x0004291C File Offset: 0x00040B1C
		public MethodEntry GetMethod(int index)
		{
			if (index < 1 || index > this.ot.MethodCount)
			{
				throw new ArgumentException();
			}
			if (this.reader == null)
			{
				throw new InvalidOperationException();
			}
			MethodEntry result;
			lock (this)
			{
				this.read_methods();
				result = this.method_list[index - 1];
			}
			return result;
		}

		// Token: 0x17000565 RID: 1381
		// (get) Token: 0x06001511 RID: 5393 RVA: 0x00042990 File Offset: 0x00040B90
		public MethodEntry[] Methods
		{
			get
			{
				if (this.reader == null)
				{
					throw new InvalidOperationException();
				}
				MethodEntry[] result;
				lock (this)
				{
					this.read_methods();
					MethodEntry[] retval = new MethodEntry[this.MethodCount];
					this.method_list.CopyTo(retval, 0);
					result = retval;
				}
				return result;
			}
		}

		// Token: 0x06001512 RID: 5394 RVA: 0x000429F8 File Offset: 0x00040BF8
		public int FindSource(string file_name)
		{
			if (this.reader == null)
			{
				throw new InvalidOperationException();
			}
			int result;
			lock (this)
			{
				if (this.source_name_hash == null)
				{
					this.source_name_hash = new Dictionary<string, int>();
					for (int i = 0; i < this.ot.SourceCount; i++)
					{
						SourceFileEntry source = this.GetSourceFile(i + 1);
						this.source_name_hash.Add(source.FileName, i);
					}
				}
				int value;
				if (!this.source_name_hash.TryGetValue(file_name, out value))
				{
					result = -1;
				}
				else
				{
					result = value;
				}
			}
			return result;
		}

		// Token: 0x06001513 RID: 5395 RVA: 0x00042A9C File Offset: 0x00040C9C
		public AnonymousScopeEntry GetAnonymousScope(int id)
		{
			if (this.reader == null)
			{
				throw new InvalidOperationException();
			}
			AnonymousScopeEntry result;
			lock (this)
			{
				if (this.anonymous_scopes != null)
				{
					AnonymousScopeEntry scope;
					this.anonymous_scopes.TryGetValue(id, out scope);
					result = scope;
				}
				else
				{
					this.anonymous_scopes = new Dictionary<int, AnonymousScopeEntry>();
					this.reader.BaseStream.Position = (long)this.ot.AnonymousScopeTableOffset;
					for (int i = 0; i < this.ot.AnonymousScopeCount; i++)
					{
						AnonymousScopeEntry scope = new AnonymousScopeEntry(this.reader);
						this.anonymous_scopes.Add(scope.ID, scope);
					}
					result = this.anonymous_scopes[id];
				}
			}
			return result;
		}

		// Token: 0x17000566 RID: 1382
		// (get) Token: 0x06001514 RID: 5396 RVA: 0x00042B68 File Offset: 0x00040D68
		internal MyBinaryReader BinaryReader
		{
			get
			{
				if (this.reader == null)
				{
					throw new InvalidOperationException();
				}
				return this.reader;
			}
		}

		// Token: 0x06001515 RID: 5397 RVA: 0x00042B7E File Offset: 0x00040D7E
		public void Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x06001516 RID: 5398 RVA: 0x00042B87 File Offset: 0x00040D87
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && this.reader != null)
			{
				this.reader.Close();
				this.reader = null;
			}
		}

		// Token: 0x04000A74 RID: 2676
		private List<MethodEntry> methods = new List<MethodEntry>();

		// Token: 0x04000A75 RID: 2677
		private List<SourceFileEntry> sources = new List<SourceFileEntry>();

		// Token: 0x04000A76 RID: 2678
		private List<CompileUnitEntry> comp_units = new List<CompileUnitEntry>();

		// Token: 0x04000A77 RID: 2679
		private Dictionary<int, AnonymousScopeEntry> anonymous_scopes;

		// Token: 0x04000A78 RID: 2680
		private OffsetTable ot;

		// Token: 0x04000A79 RID: 2681
		private int last_type_index;

		// Token: 0x04000A7A RID: 2682
		private int last_method_index;

		// Token: 0x04000A7B RID: 2683
		private int last_namespace_index;

		// Token: 0x04000A7C RID: 2684
		public readonly int MajorVersion = 50;

		// Token: 0x04000A7D RID: 2685
		public readonly int MinorVersion;

		// Token: 0x04000A7E RID: 2686
		public int NumLineNumbers;

		// Token: 0x04000A7F RID: 2687
		private MyBinaryReader reader;

		// Token: 0x04000A80 RID: 2688
		private Dictionary<int, SourceFileEntry> source_file_hash;

		// Token: 0x04000A81 RID: 2689
		private Dictionary<int, CompileUnitEntry> compile_unit_hash;

		// Token: 0x04000A82 RID: 2690
		private List<MethodEntry> method_list;

		// Token: 0x04000A83 RID: 2691
		private Dictionary<int, MethodEntry> method_token_hash;

		// Token: 0x04000A84 RID: 2692
		private Dictionary<string, int> source_name_hash;

		// Token: 0x04000A85 RID: 2693
		private Guid guid;

		// Token: 0x04000A86 RID: 2694
		internal int LineNumberCount;

		// Token: 0x04000A87 RID: 2695
		internal int LocalCount;

		// Token: 0x04000A88 RID: 2696
		internal int StringSize;

		// Token: 0x04000A89 RID: 2697
		internal int LineNumberSize;

		// Token: 0x04000A8A RID: 2698
		internal int ExtendedLineNumberSize;
	}
}
