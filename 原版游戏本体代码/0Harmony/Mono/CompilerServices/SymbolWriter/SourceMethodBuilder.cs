using System;
using System.Collections.Generic;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000349 RID: 841
	internal class SourceMethodBuilder
	{
		// Token: 0x06001591 RID: 5521 RVA: 0x00044EC0 File Offset: 0x000430C0
		public SourceMethodBuilder(ICompileUnit comp_unit)
		{
			this._comp_unit = comp_unit;
			this.method_lines = new List<LineNumberEntry>();
		}

		// Token: 0x06001592 RID: 5522 RVA: 0x00044EDA File Offset: 0x000430DA
		public SourceMethodBuilder(ICompileUnit comp_unit, int ns_id, IMethodDef method)
			: this(comp_unit)
		{
			this.ns_id = ns_id;
			this.method = method;
		}

		// Token: 0x06001593 RID: 5523 RVA: 0x00044EF1 File Offset: 0x000430F1
		public void MarkSequencePoint(int offset, SourceFileEntry file, int line, int column, bool is_hidden)
		{
			this.MarkSequencePoint(offset, file, line, column, -1, -1, is_hidden);
		}

		// Token: 0x06001594 RID: 5524 RVA: 0x00044F04 File Offset: 0x00043104
		public void MarkSequencePoint(int offset, SourceFileEntry file, int line, int column, int end_line, int end_column, bool is_hidden)
		{
			LineNumberEntry lne = new LineNumberEntry((file != null) ? file.Index : 0, line, column, end_line, end_column, offset, is_hidden);
			if (this.method_lines.Count > 0)
			{
				LineNumberEntry prev = this.method_lines[this.method_lines.Count - 1];
				if (prev.Offset == offset)
				{
					if (LineNumberEntry.LocationComparer.Default.Compare(lne, prev) > 0)
					{
						this.method_lines[this.method_lines.Count - 1] = lne;
					}
					return;
				}
			}
			this.method_lines.Add(lne);
		}

		// Token: 0x06001595 RID: 5525 RVA: 0x00044F92 File Offset: 0x00043192
		public void StartBlock(CodeBlockEntry.Type type, int start_offset)
		{
			this.StartBlock(type, start_offset, (this._blocks == null) ? 1 : (this._blocks.Count + 1));
		}

		// Token: 0x06001596 RID: 5526 RVA: 0x00044FB4 File Offset: 0x000431B4
		public void StartBlock(CodeBlockEntry.Type type, int start_offset, int scopeIndex)
		{
			if (this._block_stack == null)
			{
				this._block_stack = new Stack<CodeBlockEntry>();
			}
			if (this._blocks == null)
			{
				this._blocks = new List<CodeBlockEntry>();
			}
			int parent = ((this.CurrentBlock != null) ? this.CurrentBlock.Index : (-1));
			CodeBlockEntry block = new CodeBlockEntry(scopeIndex, parent, type, start_offset);
			this._block_stack.Push(block);
			this._blocks.Add(block);
		}

		// Token: 0x06001597 RID: 5527 RVA: 0x00045020 File Offset: 0x00043220
		public void EndBlock(int end_offset)
		{
			this._block_stack.Pop().Close(end_offset);
		}

		// Token: 0x17000576 RID: 1398
		// (get) Token: 0x06001598 RID: 5528 RVA: 0x00045034 File Offset: 0x00043234
		public CodeBlockEntry[] Blocks
		{
			get
			{
				if (this._blocks == null)
				{
					return new CodeBlockEntry[0];
				}
				CodeBlockEntry[] retval = new CodeBlockEntry[this._blocks.Count];
				this._blocks.CopyTo(retval, 0);
				return retval;
			}
		}

		// Token: 0x17000577 RID: 1399
		// (get) Token: 0x06001599 RID: 5529 RVA: 0x0004506F File Offset: 0x0004326F
		public CodeBlockEntry CurrentBlock
		{
			get
			{
				if (this._block_stack != null && this._block_stack.Count > 0)
				{
					return this._block_stack.Peek();
				}
				return null;
			}
		}

		// Token: 0x17000578 RID: 1400
		// (get) Token: 0x0600159A RID: 5530 RVA: 0x00045094 File Offset: 0x00043294
		public LocalVariableEntry[] Locals
		{
			get
			{
				if (this._locals == null)
				{
					return new LocalVariableEntry[0];
				}
				return this._locals.ToArray();
			}
		}

		// Token: 0x17000579 RID: 1401
		// (get) Token: 0x0600159B RID: 5531 RVA: 0x000450B0 File Offset: 0x000432B0
		public ICompileUnit SourceFile
		{
			get
			{
				return this._comp_unit;
			}
		}

		// Token: 0x0600159C RID: 5532 RVA: 0x000450B8 File Offset: 0x000432B8
		public void AddLocal(int index, string name)
		{
			if (this._locals == null)
			{
				this._locals = new List<LocalVariableEntry>();
			}
			int block_idx = ((this.CurrentBlock != null) ? this.CurrentBlock.Index : 0);
			this._locals.Add(new LocalVariableEntry(index, name, block_idx));
		}

		// Token: 0x1700057A RID: 1402
		// (get) Token: 0x0600159D RID: 5533 RVA: 0x00045102 File Offset: 0x00043302
		public ScopeVariable[] ScopeVariables
		{
			get
			{
				if (this._scope_vars == null)
				{
					return new ScopeVariable[0];
				}
				return this._scope_vars.ToArray();
			}
		}

		// Token: 0x0600159E RID: 5534 RVA: 0x0004511E File Offset: 0x0004331E
		public void AddScopeVariable(int scope, int index)
		{
			if (this._scope_vars == null)
			{
				this._scope_vars = new List<ScopeVariable>();
			}
			this._scope_vars.Add(new ScopeVariable(scope, index));
		}

		// Token: 0x0600159F RID: 5535 RVA: 0x00045145 File Offset: 0x00043345
		public void DefineMethod(MonoSymbolFile file)
		{
			this.DefineMethod(file, this.method.Token);
		}

		// Token: 0x060015A0 RID: 5536 RVA: 0x0004515C File Offset: 0x0004335C
		public void DefineMethod(MonoSymbolFile file, int token)
		{
			CodeBlockEntry[] blocks = this.Blocks;
			if (blocks.Length != 0)
			{
				List<CodeBlockEntry> sorted = new List<CodeBlockEntry>(blocks.Length);
				int max_index = 0;
				for (int i = 0; i < blocks.Length; i++)
				{
					max_index = Math.Max(max_index, blocks[i].Index);
				}
				for (int j = 0; j < max_index; j++)
				{
					int scope_index = j + 1;
					if (j < blocks.Length && blocks[j].Index == scope_index)
					{
						sorted.Add(blocks[j]);
					}
					else
					{
						bool found = false;
						for (int ii = 0; ii < blocks.Length; ii++)
						{
							if (blocks[ii].Index == scope_index)
							{
								sorted.Add(blocks[ii]);
								found = true;
								break;
							}
						}
						if (!found)
						{
							sorted.Add(new CodeBlockEntry(scope_index, -1, CodeBlockEntry.Type.CompilerGenerated, 0));
						}
					}
				}
				blocks = sorted.ToArray();
			}
			MethodEntry entry = new MethodEntry(file, this._comp_unit.Entry, token, this.ScopeVariables, this.Locals, this.method_lines.ToArray(), blocks, null, MethodEntry.Flags.ColumnsInfoIncluded, this.ns_id);
			file.AddMethod(entry);
		}

		// Token: 0x04000B0C RID: 2828
		private List<LocalVariableEntry> _locals;

		// Token: 0x04000B0D RID: 2829
		private List<CodeBlockEntry> _blocks;

		// Token: 0x04000B0E RID: 2830
		private List<ScopeVariable> _scope_vars;

		// Token: 0x04000B0F RID: 2831
		private Stack<CodeBlockEntry> _block_stack;

		// Token: 0x04000B10 RID: 2832
		private readonly List<LineNumberEntry> method_lines;

		// Token: 0x04000B11 RID: 2833
		private readonly ICompileUnit _comp_unit;

		// Token: 0x04000B12 RID: 2834
		private readonly int ns_id;

		// Token: 0x04000B13 RID: 2835
		private readonly IMethodDef method;
	}
}
