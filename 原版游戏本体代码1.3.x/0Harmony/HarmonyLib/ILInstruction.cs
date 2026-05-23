using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
	// Token: 0x02000020 RID: 32
	internal class ILInstruction
	{
		// Token: 0x060000A3 RID: 163 RVA: 0x00005008 File Offset: 0x00003208
		internal ILInstruction(OpCode opcode, object operand = null)
		{
			this.opcode = opcode;
			this.operand = operand;
			this.argument = operand;
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x0000503C File Offset: 0x0000323C
		internal CodeInstruction GetCodeInstruction()
		{
			CodeInstruction instr = new CodeInstruction(this.opcode, this.argument);
			if (this.opcode.OperandType == OperandType.InlineNone)
			{
				instr.operand = null;
			}
			instr.labels = this.labels;
			instr.blocks = this.blocks;
			return instr;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x0000508C File Offset: 0x0000328C
		internal int GetSize()
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
				size += 4;
				break;
			case OperandType.InlineI8:
			case OperandType.InlineR:
				size += 8;
				break;
			case OperandType.InlineSwitch:
				size += (1 + ((Array)this.operand).Length) * 4;
				break;
			case OperandType.InlineVar:
				size += 2;
				break;
			case OperandType.ShortInlineBrTarget:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineVar:
				size++;
				break;
			}
			return size;
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00005138 File Offset: 0x00003338
		public override string ToString()
		{
			string instruction = "";
			ILInstruction.AppendLabel(ref instruction, this);
			instruction = instruction + ": " + this.opcode.Name;
			if (this.operand == null)
			{
				return instruction;
			}
			instruction += " ";
			OperandType operandType = this.opcode.OperandType;
			if (operandType <= OperandType.InlineString)
			{
				if (operandType != OperandType.InlineBrTarget)
				{
					if (operandType != OperandType.InlineString)
					{
						goto IL_EC;
					}
					string str = instruction;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
					defaultInterpolatedStringHandler.AppendLiteral("\"");
					defaultInterpolatedStringHandler.AppendFormatted<object>(this.operand);
					defaultInterpolatedStringHandler.AppendLiteral("\"");
					return str + defaultInterpolatedStringHandler.ToStringAndClear();
				}
			}
			else
			{
				if (operandType == OperandType.InlineSwitch)
				{
					ILInstruction[] switchLabels = (ILInstruction[])this.operand;
					for (int i = 0; i < switchLabels.Length; i++)
					{
						if (i > 0)
						{
							instruction += ",";
						}
						ILInstruction.AppendLabel(ref instruction, switchLabels[i]);
					}
					return instruction;
				}
				if (operandType != OperandType.ShortInlineBrTarget)
				{
					goto IL_EC;
				}
			}
			ILInstruction.AppendLabel(ref instruction, this.operand);
			return instruction;
			IL_EC:
			string str2 = instruction;
			object obj = this.operand;
			instruction = str2 + ((obj != null) ? obj.ToString() : null);
			return instruction;
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x0000524C File Offset: 0x0000344C
		private static void AppendLabel(ref string str, object argument)
		{
			ILInstruction instruction = argument as ILInstruction;
			string str2 = str;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 1);
			defaultInterpolatedStringHandler.AppendLiteral("IL_");
			defaultInterpolatedStringHandler.AppendFormatted<object>(((instruction != null) ? instruction.offset.ToString("X4") : null) ?? argument);
			str = str2 + defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x04000051 RID: 81
		internal int offset;

		// Token: 0x04000052 RID: 82
		internal OpCode opcode;

		// Token: 0x04000053 RID: 83
		internal object operand;

		// Token: 0x04000054 RID: 84
		internal object argument;

		// Token: 0x04000055 RID: 85
		internal List<Label> labels = new List<Label>();

		// Token: 0x04000056 RID: 86
		internal List<ExceptionBlock> blocks = new List<ExceptionBlock>();
	}
}
