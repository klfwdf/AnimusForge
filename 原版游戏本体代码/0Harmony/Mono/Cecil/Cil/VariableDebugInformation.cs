using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200030F RID: 783
	internal sealed class VariableDebugInformation : DebugInformation
	{
		// Token: 0x1700052A RID: 1322
		// (get) Token: 0x0600145C RID: 5212 RVA: 0x00040EE6 File Offset: 0x0003F0E6
		public int Index
		{
			get
			{
				return this.index.Index;
			}
		}

		// Token: 0x1700052B RID: 1323
		// (get) Token: 0x0600145D RID: 5213 RVA: 0x00040EF3 File Offset: 0x0003F0F3
		// (set) Token: 0x0600145E RID: 5214 RVA: 0x00040EFB File Offset: 0x0003F0FB
		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
			}
		}

		// Token: 0x1700052C RID: 1324
		// (get) Token: 0x0600145F RID: 5215 RVA: 0x00040F04 File Offset: 0x0003F104
		// (set) Token: 0x06001460 RID: 5216 RVA: 0x00040F0C File Offset: 0x0003F10C
		public VariableAttributes Attributes
		{
			get
			{
				return (VariableAttributes)this.attributes;
			}
			set
			{
				this.attributes = (ushort)value;
			}
		}

		// Token: 0x1700052D RID: 1325
		// (get) Token: 0x06001461 RID: 5217 RVA: 0x00040F15 File Offset: 0x0003F115
		// (set) Token: 0x06001462 RID: 5218 RVA: 0x00040F23 File Offset: 0x0003F123
		public bool IsDebuggerHidden
		{
			get
			{
				return this.attributes.GetAttributes(1);
			}
			set
			{
				this.attributes = this.attributes.SetAttributes(1, value);
			}
		}

		// Token: 0x06001463 RID: 5219 RVA: 0x00040F38 File Offset: 0x0003F138
		internal VariableDebugInformation(int index, string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.index = new VariableIndex(index);
			this.name = name;
		}

		// Token: 0x06001464 RID: 5220 RVA: 0x00040F64 File Offset: 0x0003F164
		public VariableDebugInformation(VariableDefinition variable, string name)
		{
			if (variable == null)
			{
				throw new ArgumentNullException("variable");
			}
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.index = new VariableIndex(variable);
			this.name = name;
			this.token = new MetadataToken(TokenType.LocalVariable);
		}

		// Token: 0x04000A27 RID: 2599
		private string name;

		// Token: 0x04000A28 RID: 2600
		private ushort attributes;

		// Token: 0x04000A29 RID: 2601
		internal VariableIndex index;
	}
}
