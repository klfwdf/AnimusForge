using System;
using System.Collections.Generic;
using System.IO;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000342 RID: 834
	internal class CompileUnitEntry : ICompileUnit
	{
		// Token: 0x17000569 RID: 1385
		// (get) Token: 0x06001541 RID: 5441 RVA: 0x0004350F File Offset: 0x0004170F
		public static int Size
		{
			get
			{
				return 8;
			}
		}

		// Token: 0x1700056A RID: 1386
		// (get) Token: 0x06001542 RID: 5442 RVA: 0x0001B6A2 File Offset: 0x000198A2
		CompileUnitEntry ICompileUnit.Entry
		{
			get
			{
				return this;
			}
		}

		// Token: 0x06001543 RID: 5443 RVA: 0x00043512 File Offset: 0x00041712
		public CompileUnitEntry(MonoSymbolFile file, SourceFileEntry source)
		{
			this.file = file;
			this.source = source;
			this.Index = file.AddCompileUnit(this);
			this.creating = true;
			this.namespaces = new List<NamespaceEntry>();
		}

		// Token: 0x06001544 RID: 5444 RVA: 0x00043547 File Offset: 0x00041747
		public void AddFile(SourceFileEntry file)
		{
			if (!this.creating)
			{
				throw new InvalidOperationException();
			}
			if (this.include_files == null)
			{
				this.include_files = new List<SourceFileEntry>();
			}
			this.include_files.Add(file);
		}

		// Token: 0x1700056B RID: 1387
		// (get) Token: 0x06001545 RID: 5445 RVA: 0x00043576 File Offset: 0x00041776
		public SourceFileEntry SourceFile
		{
			get
			{
				if (this.creating)
				{
					return this.source;
				}
				this.ReadData();
				return this.source;
			}
		}

		// Token: 0x06001546 RID: 5446 RVA: 0x00043594 File Offset: 0x00041794
		public int DefineNamespace(string name, string[] using_clauses, int parent)
		{
			if (!this.creating)
			{
				throw new InvalidOperationException();
			}
			int index = this.file.GetNextNamespaceIndex();
			NamespaceEntry ns = new NamespaceEntry(name, index, using_clauses, parent);
			this.namespaces.Add(ns);
			return index;
		}

		// Token: 0x06001547 RID: 5447 RVA: 0x000435D4 File Offset: 0x000417D4
		internal void WriteData(MyBinaryWriter bw)
		{
			this.DataOffset = (int)bw.BaseStream.Position;
			bw.WriteLeb128(this.source.Index);
			int count_includes = ((this.include_files != null) ? this.include_files.Count : 0);
			bw.WriteLeb128(count_includes);
			if (this.include_files != null)
			{
				foreach (SourceFileEntry entry in this.include_files)
				{
					bw.WriteLeb128(entry.Index);
				}
			}
			bw.WriteLeb128(this.namespaces.Count);
			foreach (NamespaceEntry ns in this.namespaces)
			{
				ns.Write(this.file, bw);
			}
		}

		// Token: 0x06001548 RID: 5448 RVA: 0x000436D0 File Offset: 0x000418D0
		internal void Write(BinaryWriter bw)
		{
			bw.Write(this.Index);
			bw.Write(this.DataOffset);
		}

		// Token: 0x06001549 RID: 5449 RVA: 0x000436EA File Offset: 0x000418EA
		internal CompileUnitEntry(MonoSymbolFile file, MyBinaryReader reader)
		{
			this.file = file;
			this.Index = reader.ReadInt32();
			this.DataOffset = reader.ReadInt32();
		}

		// Token: 0x0600154A RID: 5450 RVA: 0x00043711 File Offset: 0x00041911
		public void ReadAll()
		{
			this.ReadData();
		}

		// Token: 0x0600154B RID: 5451 RVA: 0x0004371C File Offset: 0x0004191C
		private void ReadData()
		{
			if (this.creating)
			{
				throw new InvalidOperationException();
			}
			MonoSymbolFile obj = this.file;
			lock (obj)
			{
				if (this.namespaces == null)
				{
					MyBinaryReader reader = this.file.BinaryReader;
					int old_pos = (int)reader.BaseStream.Position;
					reader.BaseStream.Position = (long)this.DataOffset;
					int source_idx = reader.ReadLeb128();
					this.source = this.file.GetSourceFile(source_idx);
					int count_includes = reader.ReadLeb128();
					if (count_includes > 0)
					{
						this.include_files = new List<SourceFileEntry>();
						for (int i = 0; i < count_includes; i++)
						{
							this.include_files.Add(this.file.GetSourceFile(reader.ReadLeb128()));
						}
					}
					int count_ns = reader.ReadLeb128();
					this.namespaces = new List<NamespaceEntry>();
					for (int j = 0; j < count_ns; j++)
					{
						this.namespaces.Add(new NamespaceEntry(this.file, reader));
					}
					reader.BaseStream.Position = (long)old_pos;
				}
			}
		}

		// Token: 0x1700056C RID: 1388
		// (get) Token: 0x0600154C RID: 5452 RVA: 0x00043844 File Offset: 0x00041A44
		public NamespaceEntry[] Namespaces
		{
			get
			{
				this.ReadData();
				NamespaceEntry[] retval = new NamespaceEntry[this.namespaces.Count];
				this.namespaces.CopyTo(retval, 0);
				return retval;
			}
		}

		// Token: 0x1700056D RID: 1389
		// (get) Token: 0x0600154D RID: 5453 RVA: 0x00043878 File Offset: 0x00041A78
		public SourceFileEntry[] IncludeFiles
		{
			get
			{
				this.ReadData();
				if (this.include_files == null)
				{
					return new SourceFileEntry[0];
				}
				SourceFileEntry[] retval = new SourceFileEntry[this.include_files.Count];
				this.include_files.CopyTo(retval, 0);
				return retval;
			}
		}

		// Token: 0x04000AC9 RID: 2761
		public readonly int Index;

		// Token: 0x04000ACA RID: 2762
		private int DataOffset;

		// Token: 0x04000ACB RID: 2763
		private MonoSymbolFile file;

		// Token: 0x04000ACC RID: 2764
		private SourceFileEntry source;

		// Token: 0x04000ACD RID: 2765
		private List<SourceFileEntry> include_files;

		// Token: 0x04000ACE RID: 2766
		private List<NamespaceEntry> namespaces;

		// Token: 0x04000ACF RID: 2767
		private bool creating;
	}
}
