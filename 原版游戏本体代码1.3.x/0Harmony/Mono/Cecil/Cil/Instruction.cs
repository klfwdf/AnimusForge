using System;
using System.Globalization;
using System.Text;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002F0 RID: 752
	internal sealed class Instruction
	{
		// Token: 0x170004EE RID: 1262
		// (get) Token: 0x06001387 RID: 4999 RVA: 0x0003DA59 File Offset: 0x0003BC59
		// (set) Token: 0x06001388 RID: 5000 RVA: 0x0003DA61 File Offset: 0x0003BC61
		public int Offset
		{
			get
			{
				return this.offset;
			}
			set
			{
				this.offset = value;
			}
		}

		// Token: 0x170004EF RID: 1263
		// (get) Token: 0x06001389 RID: 5001 RVA: 0x0003DA6A File Offset: 0x0003BC6A
		// (set) Token: 0x0600138A RID: 5002 RVA: 0x0003DA72 File Offset: 0x0003BC72
		public OpCode OpCode
		{
			get
			{
				return this.opcode;
			}
			set
			{
				this.opcode = value;
			}
		}

		// Token: 0x170004F0 RID: 1264
		// (get) Token: 0x0600138B RID: 5003 RVA: 0x0003DA7B File Offset: 0x0003BC7B
		// (set) Token: 0x0600138C RID: 5004 RVA: 0x0003DA83 File Offset: 0x0003BC83
		public object Operand
		{
			get
			{
				return this.operand;
			}
			set
			{
				this.operand = value;
			}
		}

		// Token: 0x170004F1 RID: 1265
		// (get) Token: 0x0600138D RID: 5005 RVA: 0x0003DA8C File Offset: 0x0003BC8C
		// (set) Token: 0x0600138E RID: 5006 RVA: 0x0003DA94 File Offset: 0x0003BC94
		public Instruction Previous
		{
			get
			{
				return this.previous;
			}
			set
			{
				this.previous = value;
			}
		}

		// Token: 0x170004F2 RID: 1266
		// (get) Token: 0x0600138F RID: 5007 RVA: 0x0003DA9D File Offset: 0x0003BC9D
		// (set) Token: 0x06001390 RID: 5008 RVA: 0x0003DAA5 File Offset: 0x0003BCA5
		public Instruction Next
		{
			get
			{
				return this.next;
			}
			set
			{
				this.next = value;
			}
		}

		// Token: 0x06001391 RID: 5009 RVA: 0x0003DAAE File Offset: 0x0003BCAE
		internal Instruction(int offset, OpCode opCode)
		{
			this.offset = offset;
			this.opcode = opCode;
		}

		// Token: 0x06001392 RID: 5010 RVA: 0x0003DAC4 File Offset: 0x0003BCC4
		internal Instruction(OpCode opcode, object operand)
		{
			this.opcode = opcode;
			this.operand = operand;
		}

		// Token: 0x06001393 RID: 5011 RVA: 0x0003DADA File Offset: 0x0003BCDA
		public Instruction GetPrototype()
		{
			return new Instruction(this.opcode, this.operand);
		}

		// Token: 0x06001394 RID: 5012 RVA: 0x0003DAF0 File Offset: 0x0003BCF0
		public int GetSize()
		{
			int size = this.opcode.Size;
			switch (this.opcode.OperandType)
			{
			case OperandType.InlineBrTarget:
			case OperandType.InlineField:
			case OperandType.InlineI:
			case OperandType.InlineMethod:
			case OperandType.InlineSig:
			case OperandType.InlineString:
			case OperandType.InlineTok:
			case OperandType.InlineType:
			case OperandType.ShortInlineR:
				return size + 4;
			case OperandType.InlineI8:
			case OperandType.InlineR:
				return size + 8;
			case OperandType.InlineSwitch:
				return size + (1 + ((Instruction[])this.operand).Length) * 4;
			case OperandType.InlineVar:
			case OperandType.InlineArg:
				return size + 2;
			case OperandType.ShortInlineBrTarget:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineVar:
			case OperandType.ShortInlineArg:
				return size + 1;
			}
			return size;
		}

		// Token: 0x06001395 RID: 5013 RVA: 0x0003DB94 File Offset: 0x0003BD94
		public override string ToString()
		{
			StringBuilder instruction = new StringBuilder();
			Instruction.AppendLabel(instruction, this);
			instruction.Append(':');
			instruction.Append(' ');
			instruction.Append(this.opcode.Name);
			if (this.operand == null)
			{
				return instruction.ToString();
			}
			instruction.Append(' ');
			OperandType operandType = this.opcode.OperandType;
			if (operandType <= OperandType.InlineString)
			{
				if (operandType != OperandType.InlineBrTarget)
				{
					if (operandType != OperandType.InlineString)
					{
						goto IL_D4;
					}
					instruction.Append('"');
					instruction.Append(this.operand);
					instruction.Append('"');
					goto IL_EB;
				}
			}
			else
			{
				if (operandType == OperandType.InlineSwitch)
				{
					Instruction[] labels = (Instruction[])this.operand;
					for (int i = 0; i < labels.Length; i++)
					{
						if (i > 0)
						{
							instruction.Append(',');
						}
						Instruction.AppendLabel(instruction, labels[i]);
					}
					goto IL_EB;
				}
				if (operandType != OperandType.ShortInlineBrTarget)
				{
					goto IL_D4;
				}
			}
			Instruction.AppendLabel(instruction, (Instruction)this.operand);
			goto IL_EB;
			IL_D4:
			instruction.Append(Convert.ToString(this.operand, CultureInfo.InvariantCulture));
			IL_EB:
			return instruction.ToString();
		}

		// Token: 0x06001396 RID: 5014 RVA: 0x0003DC92 File Offset: 0x0003BE92
		private static void AppendLabel(StringBuilder builder, Instruction instruction)
		{
			builder.Append("IL_");
			builder.Append(instruction.offset.ToString("x4"));
		}

		// Token: 0x06001397 RID: 5015 RVA: 0x0003DCB7 File Offset: 0x0003BEB7
		public static Instruction Create(OpCode opcode)
		{
			if (opcode.OperandType != OperandType.InlineNone)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, null);
		}

		// Token: 0x06001398 RID: 5016 RVA: 0x0003DCD5 File Offset: 0x0003BED5
		public static Instruction Create(OpCode opcode, TypeReference type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (opcode.OperandType != OperandType.InlineType && opcode.OperandType != OperandType.InlineTok)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, type);
		}

		// Token: 0x06001399 RID: 5017 RVA: 0x0003DD0D File Offset: 0x0003BF0D
		public static Instruction Create(OpCode opcode, CallSite site)
		{
			if (site == null)
			{
				throw new ArgumentNullException("site");
			}
			if (opcode.Code != Code.Calli)
			{
				throw new ArgumentException("code");
			}
			return new Instruction(opcode, site);
		}

		// Token: 0x0600139A RID: 5018 RVA: 0x0003DD3A File Offset: 0x0003BF3A
		public static Instruction Create(OpCode opcode, MethodReference method)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			if (opcode.OperandType != OperandType.InlineMethod && opcode.OperandType != OperandType.InlineTok)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, method);
		}

		// Token: 0x0600139B RID: 5019 RVA: 0x0003DD71 File Offset: 0x0003BF71
		public static Instruction Create(OpCode opcode, FieldReference field)
		{
			if (field == null)
			{
				throw new ArgumentNullException("field");
			}
			if (opcode.OperandType != OperandType.InlineField && opcode.OperandType != OperandType.InlineTok)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, field);
		}

		// Token: 0x0600139C RID: 5020 RVA: 0x0003DDA8 File Offset: 0x0003BFA8
		public static Instruction Create(OpCode opcode, string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (opcode.OperandType != OperandType.InlineString)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, value);
		}

		// Token: 0x0600139D RID: 5021 RVA: 0x0003DDD5 File Offset: 0x0003BFD5
		public static Instruction Create(OpCode opcode, sbyte value)
		{
			if (opcode.OperandType != OperandType.ShortInlineI && opcode != OpCodes.Ldc_I4_S)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, value);
		}

		// Token: 0x0600139E RID: 5022 RVA: 0x0003DE06 File Offset: 0x0003C006
		public static Instruction Create(OpCode opcode, byte value)
		{
			if (opcode.OperandType != OperandType.ShortInlineI || opcode == OpCodes.Ldc_I4_S)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, value);
		}

		// Token: 0x0600139F RID: 5023 RVA: 0x0003DE37 File Offset: 0x0003C037
		public static Instruction Create(OpCode opcode, int value)
		{
			if (opcode.OperandType != OperandType.InlineI)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, value);
		}

		// Token: 0x060013A0 RID: 5024 RVA: 0x0003DE5A File Offset: 0x0003C05A
		public static Instruction Create(OpCode opcode, long value)
		{
			if (opcode.OperandType != OperandType.InlineI8)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, value);
		}

		// Token: 0x060013A1 RID: 5025 RVA: 0x0003DE7D File Offset: 0x0003C07D
		public static Instruction Create(OpCode opcode, float value)
		{
			if (opcode.OperandType != OperandType.ShortInlineR)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, value);
		}

		// Token: 0x060013A2 RID: 5026 RVA: 0x0003DEA1 File Offset: 0x0003C0A1
		public static Instruction Create(OpCode opcode, double value)
		{
			if (opcode.OperandType != OperandType.InlineR)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, value);
		}

		// Token: 0x060013A3 RID: 5027 RVA: 0x0003DEC4 File Offset: 0x0003C0C4
		public static Instruction Create(OpCode opcode, Instruction target)
		{
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}
			if (opcode.OperandType != OperandType.InlineBrTarget && opcode.OperandType != OperandType.ShortInlineBrTarget)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, target);
		}

		// Token: 0x060013A4 RID: 5028 RVA: 0x0003DEFA File Offset: 0x0003C0FA
		public static Instruction Create(OpCode opcode, Instruction[] targets)
		{
			if (targets == null)
			{
				throw new ArgumentNullException("targets");
			}
			if (opcode.OperandType != OperandType.InlineSwitch)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, targets);
		}

		// Token: 0x060013A5 RID: 5029 RVA: 0x0003DF27 File Offset: 0x0003C127
		public static Instruction Create(OpCode opcode, VariableDefinition variable)
		{
			if (variable == null)
			{
				throw new ArgumentNullException("variable");
			}
			if (opcode.OperandType != OperandType.ShortInlineVar && opcode.OperandType != OperandType.InlineVar)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, variable);
		}

		// Token: 0x060013A6 RID: 5030 RVA: 0x0003DF5F File Offset: 0x0003C15F
		public static Instruction Create(OpCode opcode, ParameterDefinition parameter)
		{
			if (parameter == null)
			{
				throw new ArgumentNullException("parameter");
			}
			if (opcode.OperandType != OperandType.ShortInlineArg && opcode.OperandType != OperandType.InlineArg)
			{
				throw new ArgumentException("opcode");
			}
			return new Instruction(opcode, parameter);
		}

		// Token: 0x040008AB RID: 2219
		internal int offset;

		// Token: 0x040008AC RID: 2220
		internal OpCode opcode;

		// Token: 0x040008AD RID: 2221
		internal object operand;

		// Token: 0x040008AE RID: 2222
		internal Instruction previous;

		// Token: 0x040008AF RID: 2223
		internal Instruction next;
	}
}
