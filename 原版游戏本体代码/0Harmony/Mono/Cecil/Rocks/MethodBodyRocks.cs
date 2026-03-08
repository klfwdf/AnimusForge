using System;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Rocks
{
	// Token: 0x02000454 RID: 1108
	internal static class MethodBodyRocks
	{
		// Token: 0x0600181A RID: 6170 RVA: 0x0004BD38 File Offset: 0x00049F38
		public static void SimplifyMacros(this MethodBody self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			foreach (Instruction instruction in self.Instructions)
			{
				if (instruction.OpCode.OpCodeType == OpCodeType.Macro)
				{
					Code code = instruction.OpCode.Code;
					switch (code)
					{
					case Code.Ldarg_0:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldarg, self.GetParameter(0));
						break;
					case Code.Ldarg_1:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldarg, self.GetParameter(1));
						break;
					case Code.Ldarg_2:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldarg, self.GetParameter(2));
						break;
					case Code.Ldarg_3:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldarg, self.GetParameter(3));
						break;
					case Code.Ldloc_0:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldloc, self.Variables[0]);
						break;
					case Code.Ldloc_1:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldloc, self.Variables[1]);
						break;
					case Code.Ldloc_2:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldloc, self.Variables[2]);
						break;
					case Code.Ldloc_3:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldloc, self.Variables[3]);
						break;
					case Code.Stloc_0:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Stloc, self.Variables[0]);
						break;
					case Code.Stloc_1:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Stloc, self.Variables[1]);
						break;
					case Code.Stloc_2:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Stloc, self.Variables[2]);
						break;
					case Code.Stloc_3:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Stloc, self.Variables[3]);
						break;
					case Code.Ldarg_S:
						instruction.OpCode = OpCodes.Ldarg;
						break;
					case Code.Ldarga_S:
						instruction.OpCode = OpCodes.Ldarga;
						break;
					case Code.Starg_S:
						instruction.OpCode = OpCodes.Starg;
						break;
					case Code.Ldloc_S:
						instruction.OpCode = OpCodes.Ldloc;
						break;
					case Code.Ldloca_S:
						instruction.OpCode = OpCodes.Ldloca;
						break;
					case Code.Stloc_S:
						instruction.OpCode = OpCodes.Stloc;
						break;
					case Code.Ldnull:
					case Code.Ldc_I4:
					case Code.Ldc_I8:
					case Code.Ldc_R4:
					case Code.Ldc_R8:
					case Code.Dup:
					case Code.Pop:
					case Code.Jmp:
					case Code.Call:
					case Code.Calli:
					case Code.Ret:
						break;
					case Code.Ldc_I4_M1:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, -1);
						break;
					case Code.Ldc_I4_0:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, 0);
						break;
					case Code.Ldc_I4_1:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, 1);
						break;
					case Code.Ldc_I4_2:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, 2);
						break;
					case Code.Ldc_I4_3:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, 3);
						break;
					case Code.Ldc_I4_4:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, 4);
						break;
					case Code.Ldc_I4_5:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, 5);
						break;
					case Code.Ldc_I4_6:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, 6);
						break;
					case Code.Ldc_I4_7:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, 7);
						break;
					case Code.Ldc_I4_8:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, 8);
						break;
					case Code.Ldc_I4_S:
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, (int)((sbyte)instruction.Operand));
						break;
					case Code.Br_S:
						instruction.OpCode = OpCodes.Br;
						break;
					case Code.Brfalse_S:
						instruction.OpCode = OpCodes.Brfalse;
						break;
					case Code.Brtrue_S:
						instruction.OpCode = OpCodes.Brtrue;
						break;
					case Code.Beq_S:
						instruction.OpCode = OpCodes.Beq;
						break;
					case Code.Bge_S:
						instruction.OpCode = OpCodes.Bge;
						break;
					case Code.Bgt_S:
						instruction.OpCode = OpCodes.Bgt;
						break;
					case Code.Ble_S:
						instruction.OpCode = OpCodes.Ble;
						break;
					case Code.Blt_S:
						instruction.OpCode = OpCodes.Blt;
						break;
					case Code.Bne_Un_S:
						instruction.OpCode = OpCodes.Bne_Un;
						break;
					case Code.Bge_Un_S:
						instruction.OpCode = OpCodes.Bge_Un;
						break;
					case Code.Bgt_Un_S:
						instruction.OpCode = OpCodes.Bgt_Un;
						break;
					case Code.Ble_Un_S:
						instruction.OpCode = OpCodes.Ble_Un;
						break;
					case Code.Blt_Un_S:
						instruction.OpCode = OpCodes.Blt_Un;
						break;
					default:
						if (code == Code.Leave_S)
						{
							instruction.OpCode = OpCodes.Leave;
						}
						break;
					}
				}
			}
		}

		// Token: 0x0600181B RID: 6171 RVA: 0x0004C20C File Offset: 0x0004A40C
		private static void ExpandMacro(Instruction instruction, OpCode opcode, object operand)
		{
			instruction.OpCode = opcode;
			instruction.Operand = operand;
		}

		// Token: 0x0600181C RID: 6172 RVA: 0x0004C21C File Offset: 0x0004A41C
		private static void MakeMacro(Instruction instruction, OpCode opcode)
		{
			instruction.OpCode = opcode;
			instruction.Operand = null;
		}

		// Token: 0x0600181D RID: 6173 RVA: 0x0004C22C File Offset: 0x0004A42C
		public static void Optimize(this MethodBody self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			self.OptimizeLongs();
			self.OptimizeMacros();
		}

		// Token: 0x0600181E RID: 6174 RVA: 0x0004C248 File Offset: 0x0004A448
		private static void OptimizeLongs(this MethodBody self)
		{
			for (int i = 0; i < self.Instructions.Count; i++)
			{
				Instruction instruction = self.Instructions[i];
				if (instruction.OpCode.Code == Code.Ldc_I8)
				{
					long j = (long)instruction.Operand;
					if (j < 2147483647L && j > -2147483648L)
					{
						MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4, (int)j);
						self.Instructions.Insert(++i, Instruction.Create(OpCodes.Conv_I8));
					}
				}
			}
		}

		// Token: 0x0600181F RID: 6175 RVA: 0x0004C2D4 File Offset: 0x0004A4D4
		public static void OptimizeMacros(this MethodBody self)
		{
			if (self == null)
			{
				throw new ArgumentNullException("self");
			}
			MethodDefinition method = self.Method;
			foreach (Instruction instruction in self.Instructions)
			{
				Code code = instruction.OpCode.Code;
				if (code != Code.Ldc_I4)
				{
					switch (code)
					{
					case Code.Ldarg:
					{
						int index = ((ParameterDefinition)instruction.Operand).Index;
						if (index == -1 && instruction.Operand == self.ThisParameter)
						{
							index = 0;
						}
						else if (method.HasThis)
						{
							index++;
						}
						switch (index)
						{
						case 0:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldarg_0);
							break;
						case 1:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldarg_1);
							break;
						case 2:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldarg_2);
							break;
						case 3:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldarg_3);
							break;
						default:
							if (index < 256)
							{
								MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldarg_S, instruction.Operand);
							}
							break;
						}
						break;
					}
					case Code.Ldarga:
					{
						int index = ((ParameterDefinition)instruction.Operand).Index;
						if (index == -1 && instruction.Operand == self.ThisParameter)
						{
							index = 0;
						}
						else if (method.HasThis)
						{
							index++;
						}
						if (index < 256)
						{
							MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldarga_S, instruction.Operand);
						}
						break;
					}
					case Code.Ldloc:
					{
						int index = ((VariableDefinition)instruction.Operand).Index;
						switch (index)
						{
						case 0:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldloc_0);
							break;
						case 1:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldloc_1);
							break;
						case 2:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldloc_2);
							break;
						case 3:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldloc_3);
							break;
						default:
							if (index < 256)
							{
								MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldloc_S, instruction.Operand);
							}
							break;
						}
						break;
					}
					case Code.Ldloca:
						if (((VariableDefinition)instruction.Operand).Index < 256)
						{
							MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldloca_S, instruction.Operand);
						}
						break;
					case Code.Stloc:
					{
						int index = ((VariableDefinition)instruction.Operand).Index;
						switch (index)
						{
						case 0:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Stloc_0);
							break;
						case 1:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Stloc_1);
							break;
						case 2:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Stloc_2);
							break;
						case 3:
							MethodBodyRocks.MakeMacro(instruction, OpCodes.Stloc_3);
							break;
						default:
							if (index < 256)
							{
								MethodBodyRocks.ExpandMacro(instruction, OpCodes.Stloc_S, instruction.Operand);
							}
							break;
						}
						break;
					}
					}
				}
				else
				{
					int i = (int)instruction.Operand;
					switch (i)
					{
					case -1:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_M1);
						break;
					case 0:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_0);
						break;
					case 1:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_1);
						break;
					case 2:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_2);
						break;
					case 3:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_3);
						break;
					case 4:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_4);
						break;
					case 5:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_5);
						break;
					case 6:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_6);
						break;
					case 7:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_7);
						break;
					case 8:
						MethodBodyRocks.MakeMacro(instruction, OpCodes.Ldc_I4_8);
						break;
					default:
						if (i >= -128 && i < 128)
						{
							MethodBodyRocks.ExpandMacro(instruction, OpCodes.Ldc_I4_S, (sbyte)i);
						}
						break;
					}
				}
			}
			MethodBodyRocks.OptimizeBranches(self);
		}

		// Token: 0x06001820 RID: 6176 RVA: 0x0004C6C4 File Offset: 0x0004A8C4
		private static void OptimizeBranches(MethodBody body)
		{
			MethodBodyRocks.ComputeOffsets(body);
			foreach (Instruction instruction in body.Instructions)
			{
				if (instruction.OpCode.OperandType == OperandType.InlineBrTarget && MethodBodyRocks.OptimizeBranch(instruction))
				{
					MethodBodyRocks.ComputeOffsets(body);
				}
			}
		}

		// Token: 0x06001821 RID: 6177 RVA: 0x0004C734 File Offset: 0x0004A934
		private static bool OptimizeBranch(Instruction instruction)
		{
			int offset = ((Instruction)instruction.Operand).Offset - (instruction.Offset + instruction.OpCode.Size + 4);
			if (offset < -128 || offset > 127)
			{
				return false;
			}
			Code code = instruction.OpCode.Code;
			switch (code)
			{
			case Code.Br:
				instruction.OpCode = OpCodes.Br_S;
				break;
			case Code.Brfalse:
				instruction.OpCode = OpCodes.Brfalse_S;
				break;
			case Code.Brtrue:
				instruction.OpCode = OpCodes.Brtrue_S;
				break;
			case Code.Beq:
				instruction.OpCode = OpCodes.Beq_S;
				break;
			case Code.Bge:
				instruction.OpCode = OpCodes.Bge_S;
				break;
			case Code.Bgt:
				instruction.OpCode = OpCodes.Bgt_S;
				break;
			case Code.Ble:
				instruction.OpCode = OpCodes.Ble_S;
				break;
			case Code.Blt:
				instruction.OpCode = OpCodes.Blt_S;
				break;
			case Code.Bne_Un:
				instruction.OpCode = OpCodes.Bne_Un_S;
				break;
			case Code.Bge_Un:
				instruction.OpCode = OpCodes.Bge_Un_S;
				break;
			case Code.Bgt_Un:
				instruction.OpCode = OpCodes.Bgt_Un_S;
				break;
			case Code.Ble_Un:
				instruction.OpCode = OpCodes.Ble_Un_S;
				break;
			case Code.Blt_Un:
				instruction.OpCode = OpCodes.Blt_Un_S;
				break;
			default:
				if (code == Code.Leave)
				{
					instruction.OpCode = OpCodes.Leave_S;
				}
				break;
			}
			return true;
		}

		// Token: 0x06001822 RID: 6178 RVA: 0x0004C894 File Offset: 0x0004AA94
		private static void ComputeOffsets(MethodBody body)
		{
			int offset = 0;
			foreach (Instruction instruction in body.Instructions)
			{
				instruction.Offset = offset;
				offset += instruction.GetSize();
			}
		}
	}
}
