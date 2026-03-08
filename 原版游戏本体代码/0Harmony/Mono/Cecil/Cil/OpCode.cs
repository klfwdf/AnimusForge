using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002F9 RID: 761
	internal struct OpCode : IEquatable<OpCode>
	{
		// Token: 0x170004FF RID: 1279
		// (get) Token: 0x060013CF RID: 5071 RVA: 0x0003E950 File Offset: 0x0003CB50
		public string Name
		{
			get
			{
				return OpCodeNames.names[(int)this.Code];
			}
		}

		// Token: 0x17000500 RID: 1280
		// (get) Token: 0x060013D0 RID: 5072 RVA: 0x0003E95E File Offset: 0x0003CB5E
		public int Size
		{
			get
			{
				if (this.op1 != 255)
				{
					return 2;
				}
				return 1;
			}
		}

		// Token: 0x17000501 RID: 1281
		// (get) Token: 0x060013D1 RID: 5073 RVA: 0x0003E970 File Offset: 0x0003CB70
		public byte Op1
		{
			get
			{
				return this.op1;
			}
		}

		// Token: 0x17000502 RID: 1282
		// (get) Token: 0x060013D2 RID: 5074 RVA: 0x0003E978 File Offset: 0x0003CB78
		public byte Op2
		{
			get
			{
				return this.op2;
			}
		}

		// Token: 0x17000503 RID: 1283
		// (get) Token: 0x060013D3 RID: 5075 RVA: 0x0003E980 File Offset: 0x0003CB80
		public short Value
		{
			get
			{
				if (this.op1 != 255)
				{
					return (short)(((int)this.op1 << 8) | (int)this.op2);
				}
				return (short)this.op2;
			}
		}

		// Token: 0x17000504 RID: 1284
		// (get) Token: 0x060013D4 RID: 5076 RVA: 0x0003E9A6 File Offset: 0x0003CBA6
		public Code Code
		{
			get
			{
				return (Code)this.code;
			}
		}

		// Token: 0x17000505 RID: 1285
		// (get) Token: 0x060013D5 RID: 5077 RVA: 0x0003E9AE File Offset: 0x0003CBAE
		public FlowControl FlowControl
		{
			get
			{
				return (FlowControl)this.flow_control;
			}
		}

		// Token: 0x17000506 RID: 1286
		// (get) Token: 0x060013D6 RID: 5078 RVA: 0x0003E9B6 File Offset: 0x0003CBB6
		public OpCodeType OpCodeType
		{
			get
			{
				return (OpCodeType)this.opcode_type;
			}
		}

		// Token: 0x17000507 RID: 1287
		// (get) Token: 0x060013D7 RID: 5079 RVA: 0x0003E9BE File Offset: 0x0003CBBE
		public OperandType OperandType
		{
			get
			{
				return (OperandType)this.operand_type;
			}
		}

		// Token: 0x17000508 RID: 1288
		// (get) Token: 0x060013D8 RID: 5080 RVA: 0x0003E9C6 File Offset: 0x0003CBC6
		public StackBehaviour StackBehaviourPop
		{
			get
			{
				return (StackBehaviour)this.stack_behavior_pop;
			}
		}

		// Token: 0x17000509 RID: 1289
		// (get) Token: 0x060013D9 RID: 5081 RVA: 0x0003E9CE File Offset: 0x0003CBCE
		public StackBehaviour StackBehaviourPush
		{
			get
			{
				return (StackBehaviour)this.stack_behavior_push;
			}
		}

		// Token: 0x060013DA RID: 5082 RVA: 0x0003E9D8 File Offset: 0x0003CBD8
		internal OpCode(int x, int y)
		{
			this.op1 = (byte)(x & 255);
			this.op2 = (byte)((x >> 8) & 255);
			this.code = (byte)((x >> 16) & 255);
			this.flow_control = (byte)((x >> 24) & 255);
			this.opcode_type = (byte)(y & 255);
			this.operand_type = (byte)((y >> 8) & 255);
			this.stack_behavior_pop = (byte)((y >> 16) & 255);
			this.stack_behavior_push = (byte)((y >> 24) & 255);
			if (this.op1 == 255)
			{
				OpCodes.OneByteOpCode[(int)this.op2] = this;
				return;
			}
			OpCodes.TwoBytesOpCode[(int)this.op2] = this;
		}

		// Token: 0x060013DB RID: 5083 RVA: 0x0003EA9F File Offset: 0x0003CC9F
		public override int GetHashCode()
		{
			return (int)this.Value;
		}

		// Token: 0x060013DC RID: 5084 RVA: 0x0003EAA8 File Offset: 0x0003CCA8
		public override bool Equals(object obj)
		{
			if (!(obj is OpCode))
			{
				return false;
			}
			OpCode opcode = (OpCode)obj;
			return this.op1 == opcode.op1 && this.op2 == opcode.op2;
		}

		// Token: 0x060013DD RID: 5085 RVA: 0x0003EAE4 File Offset: 0x0003CCE4
		public bool Equals(OpCode opcode)
		{
			return this.op1 == opcode.op1 && this.op2 == opcode.op2;
		}

		// Token: 0x060013DE RID: 5086 RVA: 0x0003EAE4 File Offset: 0x0003CCE4
		public static bool operator ==(OpCode one, OpCode other)
		{
			return one.op1 == other.op1 && one.op2 == other.op2;
		}

		// Token: 0x060013DF RID: 5087 RVA: 0x0003EB04 File Offset: 0x0003CD04
		public static bool operator !=(OpCode one, OpCode other)
		{
			return one.op1 != other.op1 || one.op2 != other.op2;
		}

		// Token: 0x060013E0 RID: 5088 RVA: 0x0003EB27 File Offset: 0x0003CD27
		public override string ToString()
		{
			return this.Name;
		}

		// Token: 0x04000905 RID: 2309
		private readonly byte op1;

		// Token: 0x04000906 RID: 2310
		private readonly byte op2;

		// Token: 0x04000907 RID: 2311
		private readonly byte code;

		// Token: 0x04000908 RID: 2312
		private readonly byte flow_control;

		// Token: 0x04000909 RID: 2313
		private readonly byte opcode_type;

		// Token: 0x0400090A RID: 2314
		private readonly byte operand_type;

		// Token: 0x0400090B RID: 2315
		private readonly byte stack_behavior_pop;

		// Token: 0x0400090C RID: 2316
		private readonly byte stack_behavior_push;
	}
}
