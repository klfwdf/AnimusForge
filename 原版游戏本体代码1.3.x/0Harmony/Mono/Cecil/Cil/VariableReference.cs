using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200032A RID: 810
	internal abstract class VariableReference
	{
		// Token: 0x17000555 RID: 1365
		// (get) Token: 0x060014DE RID: 5342 RVA: 0x00041CFF File Offset: 0x0003FEFF
		// (set) Token: 0x060014DF RID: 5343 RVA: 0x00041D07 File Offset: 0x0003FF07
		public TypeReference VariableType
		{
			get
			{
				return this.variable_type;
			}
			set
			{
				this.variable_type = value;
			}
		}

		// Token: 0x17000556 RID: 1366
		// (get) Token: 0x060014E0 RID: 5344 RVA: 0x00041D10 File Offset: 0x0003FF10
		public int Index
		{
			get
			{
				return this.index;
			}
		}

		// Token: 0x060014E1 RID: 5345 RVA: 0x00041D18 File Offset: 0x0003FF18
		internal VariableReference(TypeReference variable_type)
		{
			this.variable_type = variable_type;
		}

		// Token: 0x060014E2 RID: 5346
		public abstract VariableDefinition Resolve();

		// Token: 0x060014E3 RID: 5347 RVA: 0x00041D2E File Offset: 0x0003FF2E
		public override string ToString()
		{
			if (this.index >= 0)
			{
				return "V_" + this.index.ToString();
			}
			return string.Empty;
		}

		// Token: 0x04000A6D RID: 2669
		internal int index = -1;

		// Token: 0x04000A6E RID: 2670
		protected TypeReference variable_type;
	}
}
