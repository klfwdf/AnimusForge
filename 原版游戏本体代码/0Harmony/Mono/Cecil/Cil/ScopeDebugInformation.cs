using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200030A RID: 778
	internal sealed class ScopeDebugInformation : DebugInformation
	{
		// Token: 0x17000517 RID: 1303
		// (get) Token: 0x0600143D RID: 5181 RVA: 0x00040BFE File Offset: 0x0003EDFE
		// (set) Token: 0x0600143E RID: 5182 RVA: 0x00040C06 File Offset: 0x0003EE06
		public InstructionOffset Start
		{
			get
			{
				return this.start;
			}
			set
			{
				this.start = value;
			}
		}

		// Token: 0x17000518 RID: 1304
		// (get) Token: 0x0600143F RID: 5183 RVA: 0x00040C0F File Offset: 0x0003EE0F
		// (set) Token: 0x06001440 RID: 5184 RVA: 0x00040C17 File Offset: 0x0003EE17
		public InstructionOffset End
		{
			get
			{
				return this.end;
			}
			set
			{
				this.end = value;
			}
		}

		// Token: 0x17000519 RID: 1305
		// (get) Token: 0x06001441 RID: 5185 RVA: 0x00040C20 File Offset: 0x0003EE20
		// (set) Token: 0x06001442 RID: 5186 RVA: 0x00040C28 File Offset: 0x0003EE28
		public ImportDebugInformation Import
		{
			get
			{
				return this.import;
			}
			set
			{
				this.import = value;
			}
		}

		// Token: 0x1700051A RID: 1306
		// (get) Token: 0x06001443 RID: 5187 RVA: 0x00040C31 File Offset: 0x0003EE31
		public bool HasScopes
		{
			get
			{
				return !this.scopes.IsNullOrEmpty<ScopeDebugInformation>();
			}
		}

		// Token: 0x1700051B RID: 1307
		// (get) Token: 0x06001444 RID: 5188 RVA: 0x00040C41 File Offset: 0x0003EE41
		public Collection<ScopeDebugInformation> Scopes
		{
			get
			{
				if (this.scopes == null)
				{
					Interlocked.CompareExchange<Collection<ScopeDebugInformation>>(ref this.scopes, new Collection<ScopeDebugInformation>(), null);
				}
				return this.scopes;
			}
		}

		// Token: 0x1700051C RID: 1308
		// (get) Token: 0x06001445 RID: 5189 RVA: 0x00040C63 File Offset: 0x0003EE63
		public bool HasVariables
		{
			get
			{
				return !this.variables.IsNullOrEmpty<VariableDebugInformation>();
			}
		}

		// Token: 0x1700051D RID: 1309
		// (get) Token: 0x06001446 RID: 5190 RVA: 0x00040C73 File Offset: 0x0003EE73
		public Collection<VariableDebugInformation> Variables
		{
			get
			{
				if (this.variables == null)
				{
					Interlocked.CompareExchange<Collection<VariableDebugInformation>>(ref this.variables, new Collection<VariableDebugInformation>(), null);
				}
				return this.variables;
			}
		}

		// Token: 0x1700051E RID: 1310
		// (get) Token: 0x06001447 RID: 5191 RVA: 0x00040C95 File Offset: 0x0003EE95
		public bool HasConstants
		{
			get
			{
				return !this.constants.IsNullOrEmpty<ConstantDebugInformation>();
			}
		}

		// Token: 0x1700051F RID: 1311
		// (get) Token: 0x06001448 RID: 5192 RVA: 0x00040CA5 File Offset: 0x0003EEA5
		public Collection<ConstantDebugInformation> Constants
		{
			get
			{
				if (this.constants == null)
				{
					Interlocked.CompareExchange<Collection<ConstantDebugInformation>>(ref this.constants, new Collection<ConstantDebugInformation>(), null);
				}
				return this.constants;
			}
		}

		// Token: 0x06001449 RID: 5193 RVA: 0x00040CC7 File Offset: 0x0003EEC7
		internal ScopeDebugInformation()
		{
			this.token = new MetadataToken(TokenType.LocalScope);
		}

		// Token: 0x0600144A RID: 5194 RVA: 0x00040CDF File Offset: 0x0003EEDF
		public ScopeDebugInformation(Instruction start, Instruction end)
			: this()
		{
			if (start == null)
			{
				throw new ArgumentNullException("start");
			}
			this.start = new InstructionOffset(start);
			if (end != null)
			{
				this.end = new InstructionOffset(end);
			}
		}

		// Token: 0x0600144B RID: 5195 RVA: 0x00040D10 File Offset: 0x0003EF10
		public bool TryGetName(VariableDefinition variable, out string name)
		{
			name = null;
			if (this.variables == null || this.variables.Count == 0)
			{
				return false;
			}
			for (int i = 0; i < this.variables.Count; i++)
			{
				if (this.variables[i].Index == variable.Index)
				{
					name = this.variables[i].Name;
					return true;
				}
			}
			return false;
		}

		// Token: 0x04000A18 RID: 2584
		internal InstructionOffset start;

		// Token: 0x04000A19 RID: 2585
		internal InstructionOffset end;

		// Token: 0x04000A1A RID: 2586
		internal ImportDebugInformation import;

		// Token: 0x04000A1B RID: 2587
		internal Collection<ScopeDebugInformation> scopes;

		// Token: 0x04000A1C RID: 2588
		internal Collection<VariableDebugInformation> variables;

		// Token: 0x04000A1D RID: 2589
		internal Collection<ConstantDebugInformation> constants;
	}
}
