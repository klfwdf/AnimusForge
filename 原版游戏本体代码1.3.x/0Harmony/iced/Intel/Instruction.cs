using System;
using System.Runtime.CompilerServices;
using Iced.Intel.EncoderInternal;

namespace Iced.Intel
{
	// Token: 0x02000648 RID: 1608
	[NullableContext(1)]
	[Nullable(0)]
	internal struct Instruction : IEquatable<Instruction>
	{
		// Token: 0x06002188 RID: 8584 RVA: 0x0006D248 File Offset: 0x0006B448
		private static void InitializeSignedImmediate(ref Instruction instruction, int operand, long immediate)
		{
			OpKind opKind = Instruction.GetImmediateOpKind(instruction.Code, operand);
			instruction.SetOpKind(operand, opKind);
			switch (opKind)
			{
			case OpKind.Immediate8:
				if (-128L > immediate || immediate > 255L)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8 = (uint)((byte)immediate);
				return;
			case OpKind.Immediate8_2nd:
				if (-128L > immediate || immediate > 255L)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8_2nd = (uint)((byte)immediate);
				return;
			case OpKind.Immediate16:
				if (-32768L > immediate || immediate > 65535L)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate16 = (uint)((ushort)immediate);
				return;
			case OpKind.Immediate32:
				if (-2147483648L > immediate || immediate > (long)((ulong)(-1)))
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.Immediate32 = (uint)immediate;
				return;
			case OpKind.Immediate64:
				instruction.Immediate64 = (ulong)immediate;
				return;
			case OpKind.Immediate8to16:
				if (-128L > immediate || immediate > 127L)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8 = (uint)((byte)immediate);
				return;
			case OpKind.Immediate8to32:
				if (-128L > immediate || immediate > 127L)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8 = (uint)((byte)immediate);
				return;
			case OpKind.Immediate8to64:
				if (-128L > immediate || immediate > 127L)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8 = (uint)((byte)immediate);
				return;
			case OpKind.Immediate32to64:
				if (-2147483648L > immediate || immediate > 2147483647L)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.Immediate32 = (uint)immediate;
				return;
			default:
				throw new ArgumentOutOfRangeException("instruction");
			}
		}

		// Token: 0x06002189 RID: 8585 RVA: 0x0006D3C4 File Offset: 0x0006B5C4
		private static void InitializeUnsignedImmediate(ref Instruction instruction, int operand, ulong immediate)
		{
			OpKind opKind = Instruction.GetImmediateOpKind(instruction.Code, operand);
			instruction.SetOpKind(operand, opKind);
			switch (opKind)
			{
			case OpKind.Immediate8:
				if (immediate > 255UL)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8 = (uint)((byte)immediate);
				return;
			case OpKind.Immediate8_2nd:
				if (immediate > 255UL)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8_2nd = (uint)((byte)immediate);
				return;
			case OpKind.Immediate16:
				if (immediate > 65535UL)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate16 = (uint)((ushort)immediate);
				return;
			case OpKind.Immediate32:
				if (immediate > (ulong)(-1))
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.Immediate32 = (uint)immediate;
				return;
			case OpKind.Immediate64:
				instruction.Immediate64 = immediate;
				return;
			case OpKind.Immediate8to16:
				if (immediate > 127UL && (65408UL > immediate || immediate > 65535UL))
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8 = (uint)((byte)immediate);
				return;
			case OpKind.Immediate8to32:
				if (immediate > 127UL && ((ulong)(-128) > immediate || immediate > (ulong)(-1)))
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8 = (uint)((byte)immediate);
				return;
			case OpKind.Immediate8to64:
				if (immediate + 128UL > 255UL)
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.InternalImmediate8 = (uint)((byte)immediate);
				return;
			case OpKind.Immediate32to64:
				if (immediate + (ulong)(-2147483648) > (ulong)(-1))
				{
					throw new ArgumentOutOfRangeException("immediate");
				}
				instruction.Immediate32 = (uint)immediate;
				return;
			default:
				throw new ArgumentOutOfRangeException("instruction");
			}
		}

		// Token: 0x0600218A RID: 8586 RVA: 0x0006D530 File Offset: 0x0006B730
		private static OpKind GetImmediateOpKind(Code code, int operand)
		{
			OpCodeHandler[] handlers = OpCodeHandlers.Handlers;
			if (code >= (Code)handlers.Length)
			{
				throw new ArgumentOutOfRangeException("code");
			}
			Op[] operands = handlers[(int)code].Operands;
			if (operand >= operands.Length)
			{
				string paramName = "operand";
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
				defaultInterpolatedStringHandler.AppendFormatted<Code>(code);
				defaultInterpolatedStringHandler.AppendLiteral(" doesn't have at least ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand + 1);
				defaultInterpolatedStringHandler.AppendLiteral(" operands");
				throw new ArgumentOutOfRangeException(paramName, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			OpKind opKind = operands[operand].GetImmediateOpKind();
			if (opKind == OpKind.Immediate8 && operand > 0 && operand + 1 == operands.Length)
			{
				OpKind opKindPrev = operands[operand - 1].GetImmediateOpKind();
				if (opKindPrev == OpKind.Immediate8 || opKindPrev == OpKind.Immediate16)
				{
					opKind = OpKind.Immediate8_2nd;
				}
			}
			if (opKind == (OpKind)(-1))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(32, 2);
				defaultInterpolatedStringHandler2.AppendFormatted<Code>(code);
				defaultInterpolatedStringHandler2.AppendLiteral("'s op");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler2.AppendLiteral(" isn't an immediate operand");
				throw new ArgumentException(defaultInterpolatedStringHandler2.ToStringAndClear());
			}
			return opKind;
		}

		// Token: 0x0600218B RID: 8587 RVA: 0x0006D61C File Offset: 0x0006B81C
		private static OpKind GetNearBranchOpKind(Code code, int operand)
		{
			OpCodeHandler[] handlers = OpCodeHandlers.Handlers;
			if (code >= (Code)handlers.Length)
			{
				throw new ArgumentOutOfRangeException("code");
			}
			Op[] operands = handlers[(int)code].Operands;
			if (operand >= operands.Length)
			{
				string paramName = "operand";
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
				defaultInterpolatedStringHandler.AppendFormatted<Code>(code);
				defaultInterpolatedStringHandler.AppendLiteral(" doesn't have at least ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand + 1);
				defaultInterpolatedStringHandler.AppendLiteral(" operands");
				throw new ArgumentOutOfRangeException(paramName, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			OpKind nearBranchOpKind = operands[operand].GetNearBranchOpKind();
			if (nearBranchOpKind == (OpKind)(-1))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(33, 2);
				defaultInterpolatedStringHandler2.AppendFormatted<Code>(code);
				defaultInterpolatedStringHandler2.AppendLiteral("'s op");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler2.AppendLiteral(" isn't a near branch operand");
				throw new ArgumentException(defaultInterpolatedStringHandler2.ToStringAndClear());
			}
			return nearBranchOpKind;
		}

		// Token: 0x0600218C RID: 8588 RVA: 0x0006D6E0 File Offset: 0x0006B8E0
		private static OpKind GetFarBranchOpKind(Code code, int operand)
		{
			OpCodeHandler[] handlers = OpCodeHandlers.Handlers;
			if (code >= (Code)handlers.Length)
			{
				throw new ArgumentOutOfRangeException("code");
			}
			Op[] operands = handlers[(int)code].Operands;
			if (operand >= operands.Length)
			{
				string paramName = "operand";
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
				defaultInterpolatedStringHandler.AppendFormatted<Code>(code);
				defaultInterpolatedStringHandler.AppendLiteral(" doesn't have at least ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand + 1);
				defaultInterpolatedStringHandler.AppendLiteral(" operands");
				throw new ArgumentOutOfRangeException(paramName, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			OpKind farBranchOpKind = operands[operand].GetFarBranchOpKind();
			if (farBranchOpKind == (OpKind)(-1))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(32, 2);
				defaultInterpolatedStringHandler2.AppendFormatted<Code>(code);
				defaultInterpolatedStringHandler2.AppendLiteral("'s op");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler2.AppendLiteral(" isn't a far branch operand");
				throw new ArgumentException(defaultInterpolatedStringHandler2.ToStringAndClear());
			}
			return farBranchOpKind;
		}

		// Token: 0x0600218D RID: 8589 RVA: 0x0006D7A4 File Offset: 0x0006B9A4
		private static Instruction CreateString_Reg_SegRSI(Code code, int addressSize, Register register, Register segmentPrefix, RepPrefixKind repPrefix)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			if (repPrefix == RepPrefixKind.Repe)
			{
				instruction.InternalSetHasRepePrefix();
			}
			else if (repPrefix == RepPrefixKind.Repne)
			{
				instruction.InternalSetHasRepnePrefix();
			}
			instruction.Op0Register = register;
			if (addressSize == 64)
			{
				instruction.Op1Kind = OpKind.MemorySegRSI;
			}
			else if (addressSize == 32)
			{
				instruction.Op1Kind = OpKind.MemorySegESI;
			}
			else
			{
				if (addressSize != 16)
				{
					throw new ArgumentOutOfRangeException("addressSize");
				}
				instruction.Op1Kind = OpKind.MemorySegSI;
			}
			instruction.SegmentPrefix = segmentPrefix;
			return instruction;
		}

		// Token: 0x0600218E RID: 8590 RVA: 0x0006D828 File Offset: 0x0006BA28
		private static Instruction CreateString_Reg_ESRDI(Code code, int addressSize, Register register, RepPrefixKind repPrefix)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			if (repPrefix == RepPrefixKind.Repe)
			{
				instruction.InternalSetHasRepePrefix();
			}
			else if (repPrefix == RepPrefixKind.Repne)
			{
				instruction.InternalSetHasRepnePrefix();
			}
			instruction.Op0Register = register;
			if (addressSize == 64)
			{
				instruction.Op1Kind = OpKind.MemoryESRDI;
			}
			else if (addressSize == 32)
			{
				instruction.Op1Kind = OpKind.MemoryESEDI;
			}
			else
			{
				if (addressSize != 16)
				{
					throw new ArgumentOutOfRangeException("addressSize");
				}
				instruction.Op1Kind = OpKind.MemoryESDI;
			}
			return instruction;
		}

		// Token: 0x0600218F RID: 8591 RVA: 0x0006D8A4 File Offset: 0x0006BAA4
		private static Instruction CreateString_ESRDI_Reg(Code code, int addressSize, Register register, RepPrefixKind repPrefix)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			if (repPrefix == RepPrefixKind.Repe)
			{
				instruction.InternalSetHasRepePrefix();
			}
			else if (repPrefix == RepPrefixKind.Repne)
			{
				instruction.InternalSetHasRepnePrefix();
			}
			if (addressSize == 64)
			{
				instruction.Op0Kind = OpKind.MemoryESRDI;
			}
			else if (addressSize == 32)
			{
				instruction.Op0Kind = OpKind.MemoryESEDI;
			}
			else
			{
				if (addressSize != 16)
				{
					throw new ArgumentOutOfRangeException("addressSize");
				}
				instruction.Op0Kind = OpKind.MemoryESDI;
			}
			instruction.Op1Register = register;
			return instruction;
		}

		// Token: 0x06002190 RID: 8592 RVA: 0x0006D920 File Offset: 0x0006BB20
		private static Instruction CreateString_SegRSI_ESRDI(Code code, int addressSize, Register segmentPrefix, RepPrefixKind repPrefix)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			if (repPrefix == RepPrefixKind.Repe)
			{
				instruction.InternalSetHasRepePrefix();
			}
			else if (repPrefix == RepPrefixKind.Repne)
			{
				instruction.InternalSetHasRepnePrefix();
			}
			if (addressSize == 64)
			{
				instruction.Op0Kind = OpKind.MemorySegRSI;
				instruction.Op1Kind = OpKind.MemoryESRDI;
			}
			else if (addressSize == 32)
			{
				instruction.Op0Kind = OpKind.MemorySegESI;
				instruction.Op1Kind = OpKind.MemoryESEDI;
			}
			else
			{
				if (addressSize != 16)
				{
					throw new ArgumentOutOfRangeException("addressSize");
				}
				instruction.Op0Kind = OpKind.MemorySegSI;
				instruction.Op1Kind = OpKind.MemoryESDI;
			}
			instruction.SegmentPrefix = segmentPrefix;
			return instruction;
		}

		// Token: 0x06002191 RID: 8593 RVA: 0x0006D9B4 File Offset: 0x0006BBB4
		private static Instruction CreateString_ESRDI_SegRSI(Code code, int addressSize, Register segmentPrefix, RepPrefixKind repPrefix)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			if (repPrefix == RepPrefixKind.Repe)
			{
				instruction.InternalSetHasRepePrefix();
			}
			else if (repPrefix == RepPrefixKind.Repne)
			{
				instruction.InternalSetHasRepnePrefix();
			}
			if (addressSize == 64)
			{
				instruction.Op0Kind = OpKind.MemoryESRDI;
				instruction.Op1Kind = OpKind.MemorySegRSI;
			}
			else if (addressSize == 32)
			{
				instruction.Op0Kind = OpKind.MemoryESEDI;
				instruction.Op1Kind = OpKind.MemorySegESI;
			}
			else
			{
				if (addressSize != 16)
				{
					throw new ArgumentOutOfRangeException("addressSize");
				}
				instruction.Op0Kind = OpKind.MemoryESDI;
				instruction.Op1Kind = OpKind.MemorySegSI;
			}
			instruction.SegmentPrefix = segmentPrefix;
			return instruction;
		}

		// Token: 0x06002192 RID: 8594 RVA: 0x0006DA48 File Offset: 0x0006BC48
		private static Instruction CreateMaskmov(Code code, int addressSize, Register register1, Register register2, Register segmentPrefix)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			if (addressSize == 64)
			{
				instruction.Op0Kind = OpKind.MemorySegRDI;
			}
			else if (addressSize == 32)
			{
				instruction.Op0Kind = OpKind.MemorySegEDI;
			}
			else
			{
				if (addressSize != 16)
				{
					throw new ArgumentOutOfRangeException("addressSize");
				}
				instruction.Op0Kind = OpKind.MemorySegDI;
			}
			instruction.Op1Register = register1;
			instruction.Op2Register = register2;
			instruction.SegmentPrefix = segmentPrefix;
			return instruction;
		}

		// Token: 0x06002193 RID: 8595 RVA: 0x0006DABC File Offset: 0x0006BCBC
		private static void InitMemoryOperand(ref Instruction instruction, in MemoryOperand memory)
		{
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement64 = (ulong)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;
		}

		// Token: 0x06002194 RID: 8596 RVA: 0x0006DB20 File Offset: 0x0006BD20
		public static Instruction Create(Code code)
		{
			return new Instruction
			{
				Code = code
			};
		}

		// Token: 0x06002195 RID: 8597 RVA: 0x0006DB40 File Offset: 0x0006BD40
		public static Instruction Create(Code code, Register register)
		{
			return new Instruction
			{
				Code = code,
				Op0Register = register
			};
		}

		// Token: 0x06002196 RID: 8598 RVA: 0x0006DB68 File Offset: 0x0006BD68
		public static Instruction Create(Code code, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			Instruction.InitializeSignedImmediate(ref instruction, 0, (long)immediate);
			return instruction;
		}

		// Token: 0x06002197 RID: 8599 RVA: 0x0006DB90 File Offset: 0x0006BD90
		public static Instruction Create(Code code, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			Instruction.InitializeUnsignedImmediate(ref instruction, 0, (ulong)immediate);
			return instruction;
		}

		// Token: 0x06002198 RID: 8600 RVA: 0x0006DBB8 File Offset: 0x0006BDB8
		public static Instruction Create(Code code, in MemoryOperand memory)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			return instruction;
		}

		// Token: 0x06002199 RID: 8601 RVA: 0x0006DBE8 File Offset: 0x0006BDE8
		public static Instruction Create(Code code, Register register1, Register register2)
		{
			return new Instruction
			{
				Code = code,
				Op0Register = register1,
				Op1Register = register2
			};
		}

		// Token: 0x0600219A RID: 8602 RVA: 0x0006DC18 File Offset: 0x0006BE18
		public static Instruction Create(Code code, Register register, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register;
			Instruction.InitializeSignedImmediate(ref instruction, 1, (long)immediate);
			return instruction;
		}

		// Token: 0x0600219B RID: 8603 RVA: 0x0006DC48 File Offset: 0x0006BE48
		public static Instruction Create(Code code, Register register, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register;
			Instruction.InitializeUnsignedImmediate(ref instruction, 1, (ulong)immediate);
			return instruction;
		}

		// Token: 0x0600219C RID: 8604 RVA: 0x0006DC78 File Offset: 0x0006BE78
		public static Instruction Create(Code code, Register register, long immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register;
			Instruction.InitializeSignedImmediate(ref instruction, 1, immediate);
			return instruction;
		}

		// Token: 0x0600219D RID: 8605 RVA: 0x0006DCA8 File Offset: 0x0006BEA8
		public static Instruction Create(Code code, Register register, ulong immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register;
			Instruction.InitializeUnsignedImmediate(ref instruction, 1, immediate);
			return instruction;
		}

		// Token: 0x0600219E RID: 8606 RVA: 0x0006DCD8 File Offset: 0x0006BED8
		public static Instruction Create(Code code, Register register, in MemoryOperand memory)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register;
			instruction.Op1Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			return instruction;
		}

		// Token: 0x0600219F RID: 8607 RVA: 0x0006DD10 File Offset: 0x0006BF10
		public static Instruction Create(Code code, int immediate, Register register)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			Instruction.InitializeSignedImmediate(ref instruction, 0, (long)immediate);
			instruction.Op1Register = register;
			return instruction;
		}

		// Token: 0x060021A0 RID: 8608 RVA: 0x0006DD40 File Offset: 0x0006BF40
		public static Instruction Create(Code code, uint immediate, Register register)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			Instruction.InitializeUnsignedImmediate(ref instruction, 0, (ulong)immediate);
			instruction.Op1Register = register;
			return instruction;
		}

		// Token: 0x060021A1 RID: 8609 RVA: 0x0006DD70 File Offset: 0x0006BF70
		public static Instruction Create(Code code, int immediate1, int immediate2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			Instruction.InitializeSignedImmediate(ref instruction, 0, (long)immediate1);
			Instruction.InitializeSignedImmediate(ref instruction, 1, (long)immediate2);
			return instruction;
		}

		// Token: 0x060021A2 RID: 8610 RVA: 0x0006DDA4 File Offset: 0x0006BFA4
		public static Instruction Create(Code code, uint immediate1, uint immediate2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			Instruction.InitializeUnsignedImmediate(ref instruction, 0, (ulong)immediate1);
			Instruction.InitializeUnsignedImmediate(ref instruction, 1, (ulong)immediate2);
			return instruction;
		}

		// Token: 0x060021A3 RID: 8611 RVA: 0x0006DDD8 File Offset: 0x0006BFD8
		public static Instruction Create(Code code, in MemoryOperand memory, Register register)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			instruction.Op1Register = register;
			return instruction;
		}

		// Token: 0x060021A4 RID: 8612 RVA: 0x0006DE10 File Offset: 0x0006C010
		public static Instruction Create(Code code, in MemoryOperand memory, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			Instruction.InitializeSignedImmediate(ref instruction, 1, (long)immediate);
			return instruction;
		}

		// Token: 0x060021A5 RID: 8613 RVA: 0x0006DE4C File Offset: 0x0006C04C
		public static Instruction Create(Code code, in MemoryOperand memory, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			Instruction.InitializeUnsignedImmediate(ref instruction, 1, (ulong)immediate);
			return instruction;
		}

		// Token: 0x060021A6 RID: 8614 RVA: 0x0006DE88 File Offset: 0x0006C088
		public static Instruction Create(Code code, Register register1, Register register2, Register register3)
		{
			return new Instruction
			{
				Code = code,
				Op0Register = register1,
				Op1Register = register2,
				Op2Register = register3
			};
		}

		// Token: 0x060021A7 RID: 8615 RVA: 0x0006DEC0 File Offset: 0x0006C0C0
		public static Instruction Create(Code code, Register register1, Register register2, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			Instruction.InitializeSignedImmediate(ref instruction, 2, (long)immediate);
			return instruction;
		}

		// Token: 0x060021A8 RID: 8616 RVA: 0x0006DEF8 File Offset: 0x0006C0F8
		public static Instruction Create(Code code, Register register1, Register register2, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			Instruction.InitializeUnsignedImmediate(ref instruction, 2, (ulong)immediate);
			return instruction;
		}

		// Token: 0x060021A9 RID: 8617 RVA: 0x0006DF30 File Offset: 0x0006C130
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			return instruction;
		}

		// Token: 0x060021AA RID: 8618 RVA: 0x0006DF70 File Offset: 0x0006C170
		public static Instruction Create(Code code, Register register, int immediate1, int immediate2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register;
			Instruction.InitializeSignedImmediate(ref instruction, 1, (long)immediate1);
			Instruction.InitializeSignedImmediate(ref instruction, 2, (long)immediate2);
			return instruction;
		}

		// Token: 0x060021AB RID: 8619 RVA: 0x0006DFAC File Offset: 0x0006C1AC
		public static Instruction Create(Code code, Register register, uint immediate1, uint immediate2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register;
			Instruction.InitializeUnsignedImmediate(ref instruction, 1, (ulong)immediate1);
			Instruction.InitializeUnsignedImmediate(ref instruction, 2, (ulong)immediate2);
			return instruction;
		}

		// Token: 0x060021AC RID: 8620 RVA: 0x0006DFE8 File Offset: 0x0006C1E8
		public static Instruction Create(Code code, Register register1, in MemoryOperand memory, Register register2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			instruction.Op2Register = register2;
			return instruction;
		}

		// Token: 0x060021AD RID: 8621 RVA: 0x0006E028 File Offset: 0x0006C228
		public static Instruction Create(Code code, Register register, in MemoryOperand memory, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register;
			instruction.Op1Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			Instruction.InitializeSignedImmediate(ref instruction, 2, (long)immediate);
			return instruction;
		}

		// Token: 0x060021AE RID: 8622 RVA: 0x0006E06C File Offset: 0x0006C26C
		public static Instruction Create(Code code, Register register, in MemoryOperand memory, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register;
			instruction.Op1Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			Instruction.InitializeUnsignedImmediate(ref instruction, 2, (ulong)immediate);
			return instruction;
		}

		// Token: 0x060021AF RID: 8623 RVA: 0x0006E0B0 File Offset: 0x0006C2B0
		public static Instruction Create(Code code, in MemoryOperand memory, Register register1, Register register2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			instruction.Op1Register = register1;
			instruction.Op2Register = register2;
			return instruction;
		}

		// Token: 0x060021B0 RID: 8624 RVA: 0x0006E0F0 File Offset: 0x0006C2F0
		public static Instruction Create(Code code, in MemoryOperand memory, Register register, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			instruction.Op1Register = register;
			Instruction.InitializeSignedImmediate(ref instruction, 2, (long)immediate);
			return instruction;
		}

		// Token: 0x060021B1 RID: 8625 RVA: 0x0006E134 File Offset: 0x0006C334
		public static Instruction Create(Code code, in MemoryOperand memory, Register register, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			instruction.Op1Register = register;
			Instruction.InitializeUnsignedImmediate(ref instruction, 2, (ulong)immediate);
			return instruction;
		}

		// Token: 0x060021B2 RID: 8626 RVA: 0x0006E178 File Offset: 0x0006C378
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, Register register4)
		{
			return new Instruction
			{
				Code = code,
				Op0Register = register1,
				Op1Register = register2,
				Op2Register = register3,
				Op3Register = register4
			};
		}

		// Token: 0x060021B3 RID: 8627 RVA: 0x0006E1B8 File Offset: 0x0006C3B8
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Register = register3;
			Instruction.InitializeSignedImmediate(ref instruction, 3, (long)immediate);
			return instruction;
		}

		// Token: 0x060021B4 RID: 8628 RVA: 0x0006E1FC File Offset: 0x0006C3FC
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Register = register3;
			Instruction.InitializeUnsignedImmediate(ref instruction, 3, (ulong)immediate);
			return instruction;
		}

		// Token: 0x060021B5 RID: 8629 RVA: 0x0006E240 File Offset: 0x0006C440
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, in MemoryOperand memory)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Register = register3;
			instruction.Op3Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			return instruction;
		}

		// Token: 0x060021B6 RID: 8630 RVA: 0x0006E288 File Offset: 0x0006C488
		public static Instruction Create(Code code, Register register1, Register register2, int immediate1, int immediate2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			Instruction.InitializeSignedImmediate(ref instruction, 2, (long)immediate1);
			Instruction.InitializeSignedImmediate(ref instruction, 3, (long)immediate2);
			return instruction;
		}

		// Token: 0x060021B7 RID: 8631 RVA: 0x0006E2CC File Offset: 0x0006C4CC
		public static Instruction Create(Code code, Register register1, Register register2, uint immediate1, uint immediate2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			Instruction.InitializeUnsignedImmediate(ref instruction, 2, (ulong)immediate1);
			Instruction.InitializeUnsignedImmediate(ref instruction, 3, (ulong)immediate2);
			return instruction;
		}

		// Token: 0x060021B8 RID: 8632 RVA: 0x0006E310 File Offset: 0x0006C510
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, Register register3)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			instruction.Op3Register = register3;
			return instruction;
		}

		// Token: 0x060021B9 RID: 8633 RVA: 0x0006E358 File Offset: 0x0006C558
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			Instruction.InitializeSignedImmediate(ref instruction, 3, (long)immediate);
			return instruction;
		}

		// Token: 0x060021BA RID: 8634 RVA: 0x0006E3A4 File Offset: 0x0006C5A4
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			Instruction.InitializeUnsignedImmediate(ref instruction, 3, (ulong)immediate);
			return instruction;
		}

		// Token: 0x060021BB RID: 8635 RVA: 0x0006E3F0 File Offset: 0x0006C5F0
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, Register register4, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Register = register3;
			instruction.Op3Register = register4;
			Instruction.InitializeSignedImmediate(ref instruction, 4, (long)immediate);
			return instruction;
		}

		// Token: 0x060021BC RID: 8636 RVA: 0x0006E43C File Offset: 0x0006C63C
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, Register register4, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Register = register3;
			instruction.Op3Register = register4;
			Instruction.InitializeUnsignedImmediate(ref instruction, 4, (ulong)immediate);
			return instruction;
		}

		// Token: 0x060021BD RID: 8637 RVA: 0x0006E488 File Offset: 0x0006C688
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, in MemoryOperand memory, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Register = register3;
			instruction.Op3Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			Instruction.InitializeSignedImmediate(ref instruction, 4, (long)immediate);
			return instruction;
		}

		// Token: 0x060021BE RID: 8638 RVA: 0x0006E4DC File Offset: 0x0006C6DC
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, in MemoryOperand memory, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Register = register3;
			instruction.Op3Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			Instruction.InitializeUnsignedImmediate(ref instruction, 4, (ulong)immediate);
			return instruction;
		}

		// Token: 0x060021BF RID: 8639 RVA: 0x0006E530 File Offset: 0x0006C730
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, Register register3, int immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			instruction.Op3Register = register3;
			Instruction.InitializeSignedImmediate(ref instruction, 4, (long)immediate);
			return instruction;
		}

		// Token: 0x060021C0 RID: 8640 RVA: 0x0006E584 File Offset: 0x0006C784
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, Register register3, uint immediate)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = code;
			instruction.Op0Register = register1;
			instruction.Op1Register = register2;
			instruction.Op2Kind = OpKind.Memory;
			Instruction.InitMemoryOperand(ref instruction, memory);
			instruction.Op3Register = register3;
			Instruction.InitializeUnsignedImmediate(ref instruction, 4, (ulong)immediate);
			return instruction;
		}

		// Token: 0x060021C1 RID: 8641 RVA: 0x0006E5D8 File Offset: 0x0006C7D8
		public static Instruction CreateBranch(Code code, ulong target)
		{
			return new Instruction
			{
				Code = code,
				Op0Kind = Instruction.GetNearBranchOpKind(code, 0),
				NearBranch64 = target
			};
		}

		// Token: 0x060021C2 RID: 8642 RVA: 0x0006E60C File Offset: 0x0006C80C
		public static Instruction CreateBranch(Code code, ushort selector, uint offset)
		{
			return new Instruction
			{
				Code = code,
				Op0Kind = Instruction.GetFarBranchOpKind(code, 0),
				FarBranchSelector = selector,
				FarBranch32 = offset
			};
		}

		// Token: 0x060021C3 RID: 8643 RVA: 0x0006E648 File Offset: 0x0006C848
		public static Instruction CreateXbegin(int bitness, ulong target)
		{
			Instruction instruction = default(Instruction);
			if (bitness != 16)
			{
				if (bitness != 32)
				{
					if (bitness != 64)
					{
						throw new ArgumentOutOfRangeException("bitness");
					}
					instruction.Code = Code.Xbegin_rel32;
					instruction.Op0Kind = OpKind.NearBranch64;
					instruction.NearBranch64 = target;
				}
				else
				{
					instruction.Code = Code.Xbegin_rel32;
					instruction.Op0Kind = OpKind.NearBranch32;
					instruction.NearBranch32 = (uint)target;
				}
			}
			else
			{
				instruction.Code = Code.Xbegin_rel16;
				instruction.Op0Kind = OpKind.NearBranch32;
				instruction.NearBranch32 = (uint)target;
			}
			return instruction;
		}

		// Token: 0x060021C4 RID: 8644 RVA: 0x0006E6D6 File Offset: 0x0006C8D6
		public static Instruction CreateOutsb(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Outsb_DX_m8, addressSize, Register.DX, segmentPrefix, repPrefix);
		}

		// Token: 0x060021C5 RID: 8645 RVA: 0x0006E6E7 File Offset: 0x0006C8E7
		public static Instruction CreateRepOutsb(int addressSize)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Outsb_DX_m8, addressSize, Register.DX, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021C6 RID: 8646 RVA: 0x0006E6F8 File Offset: 0x0006C8F8
		public static Instruction CreateOutsw(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Outsw_DX_m16, addressSize, Register.DX, segmentPrefix, repPrefix);
		}

		// Token: 0x060021C7 RID: 8647 RVA: 0x0006E709 File Offset: 0x0006C909
		public static Instruction CreateRepOutsw(int addressSize)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Outsw_DX_m16, addressSize, Register.DX, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021C8 RID: 8648 RVA: 0x0006E71A File Offset: 0x0006C91A
		public static Instruction CreateOutsd(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Outsd_DX_m32, addressSize, Register.DX, segmentPrefix, repPrefix);
		}

		// Token: 0x060021C9 RID: 8649 RVA: 0x0006E72B File Offset: 0x0006C92B
		public static Instruction CreateRepOutsd(int addressSize)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Outsd_DX_m32, addressSize, Register.DX, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021CA RID: 8650 RVA: 0x0006E73C File Offset: 0x0006C93C
		public static Instruction CreateLodsb(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Lodsb_AL_m8, addressSize, Register.AL, segmentPrefix, repPrefix);
		}

		// Token: 0x060021CB RID: 8651 RVA: 0x0006E74C File Offset: 0x0006C94C
		public static Instruction CreateRepLodsb(int addressSize)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Lodsb_AL_m8, addressSize, Register.AL, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021CC RID: 8652 RVA: 0x0006E75C File Offset: 0x0006C95C
		public static Instruction CreateLodsw(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Lodsw_AX_m16, addressSize, Register.AX, segmentPrefix, repPrefix);
		}

		// Token: 0x060021CD RID: 8653 RVA: 0x0006E76D File Offset: 0x0006C96D
		public static Instruction CreateRepLodsw(int addressSize)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Lodsw_AX_m16, addressSize, Register.AX, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021CE RID: 8654 RVA: 0x0006E77E File Offset: 0x0006C97E
		public static Instruction CreateLodsd(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Lodsd_EAX_m32, addressSize, Register.EAX, segmentPrefix, repPrefix);
		}

		// Token: 0x060021CF RID: 8655 RVA: 0x0006E78F File Offset: 0x0006C98F
		public static Instruction CreateRepLodsd(int addressSize)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Lodsd_EAX_m32, addressSize, Register.EAX, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021D0 RID: 8656 RVA: 0x0006E7A0 File Offset: 0x0006C9A0
		public static Instruction CreateLodsq(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Lodsq_RAX_m64, addressSize, Register.RAX, segmentPrefix, repPrefix);
		}

		// Token: 0x060021D1 RID: 8657 RVA: 0x0006E7B1 File Offset: 0x0006C9B1
		public static Instruction CreateRepLodsq(int addressSize)
		{
			return Instruction.CreateString_Reg_SegRSI(Code.Lodsq_RAX_m64, addressSize, Register.RAX, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021D2 RID: 8658 RVA: 0x0006E7C2 File Offset: 0x0006C9C2
		public static Instruction CreateScasb(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasb_AL_m8, addressSize, Register.AL, repPrefix);
		}

		// Token: 0x060021D3 RID: 8659 RVA: 0x0006E7D1 File Offset: 0x0006C9D1
		public static Instruction CreateRepeScasb(int addressSize)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasb_AL_m8, addressSize, Register.AL, RepPrefixKind.Repe);
		}

		// Token: 0x060021D4 RID: 8660 RVA: 0x0006E7E0 File Offset: 0x0006C9E0
		public static Instruction CreateRepneScasb(int addressSize)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasb_AL_m8, addressSize, Register.AL, RepPrefixKind.Repne);
		}

		// Token: 0x060021D5 RID: 8661 RVA: 0x0006E7EF File Offset: 0x0006C9EF
		public static Instruction CreateScasw(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasw_AX_m16, addressSize, Register.AX, repPrefix);
		}

		// Token: 0x060021D6 RID: 8662 RVA: 0x0006E7FF File Offset: 0x0006C9FF
		public static Instruction CreateRepeScasw(int addressSize)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasw_AX_m16, addressSize, Register.AX, RepPrefixKind.Repe);
		}

		// Token: 0x060021D7 RID: 8663 RVA: 0x0006E80F File Offset: 0x0006CA0F
		public static Instruction CreateRepneScasw(int addressSize)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasw_AX_m16, addressSize, Register.AX, RepPrefixKind.Repne);
		}

		// Token: 0x060021D8 RID: 8664 RVA: 0x0006E81F File Offset: 0x0006CA1F
		public static Instruction CreateScasd(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasd_EAX_m32, addressSize, Register.EAX, repPrefix);
		}

		// Token: 0x060021D9 RID: 8665 RVA: 0x0006E82F File Offset: 0x0006CA2F
		public static Instruction CreateRepeScasd(int addressSize)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasd_EAX_m32, addressSize, Register.EAX, RepPrefixKind.Repe);
		}

		// Token: 0x060021DA RID: 8666 RVA: 0x0006E83F File Offset: 0x0006CA3F
		public static Instruction CreateRepneScasd(int addressSize)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasd_EAX_m32, addressSize, Register.EAX, RepPrefixKind.Repne);
		}

		// Token: 0x060021DB RID: 8667 RVA: 0x0006E84F File Offset: 0x0006CA4F
		public static Instruction CreateScasq(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasq_RAX_m64, addressSize, Register.RAX, repPrefix);
		}

		// Token: 0x060021DC RID: 8668 RVA: 0x0006E85F File Offset: 0x0006CA5F
		public static Instruction CreateRepeScasq(int addressSize)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasq_RAX_m64, addressSize, Register.RAX, RepPrefixKind.Repe);
		}

		// Token: 0x060021DD RID: 8669 RVA: 0x0006E86F File Offset: 0x0006CA6F
		public static Instruction CreateRepneScasq(int addressSize)
		{
			return Instruction.CreateString_Reg_ESRDI(Code.Scasq_RAX_m64, addressSize, Register.RAX, RepPrefixKind.Repne);
		}

		// Token: 0x060021DE RID: 8670 RVA: 0x0006E87F File Offset: 0x0006CA7F
		public static Instruction CreateInsb(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Insb_m8_DX, addressSize, Register.DX, repPrefix);
		}

		// Token: 0x060021DF RID: 8671 RVA: 0x0006E88F File Offset: 0x0006CA8F
		public static Instruction CreateRepInsb(int addressSize)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Insb_m8_DX, addressSize, Register.DX, RepPrefixKind.Repe);
		}

		// Token: 0x060021E0 RID: 8672 RVA: 0x0006E89F File Offset: 0x0006CA9F
		public static Instruction CreateInsw(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Insw_m16_DX, addressSize, Register.DX, repPrefix);
		}

		// Token: 0x060021E1 RID: 8673 RVA: 0x0006E8AF File Offset: 0x0006CAAF
		public static Instruction CreateRepInsw(int addressSize)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Insw_m16_DX, addressSize, Register.DX, RepPrefixKind.Repe);
		}

		// Token: 0x060021E2 RID: 8674 RVA: 0x0006E8BF File Offset: 0x0006CABF
		public static Instruction CreateInsd(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Insd_m32_DX, addressSize, Register.DX, repPrefix);
		}

		// Token: 0x060021E3 RID: 8675 RVA: 0x0006E8CF File Offset: 0x0006CACF
		public static Instruction CreateRepInsd(int addressSize)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Insd_m32_DX, addressSize, Register.DX, RepPrefixKind.Repe);
		}

		// Token: 0x060021E4 RID: 8676 RVA: 0x0006E8DF File Offset: 0x0006CADF
		public static Instruction CreateStosb(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Stosb_m8_AL, addressSize, Register.AL, repPrefix);
		}

		// Token: 0x060021E5 RID: 8677 RVA: 0x0006E8EE File Offset: 0x0006CAEE
		public static Instruction CreateRepStosb(int addressSize)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Stosb_m8_AL, addressSize, Register.AL, RepPrefixKind.Repe);
		}

		// Token: 0x060021E6 RID: 8678 RVA: 0x0006E8FD File Offset: 0x0006CAFD
		public static Instruction CreateStosw(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Stosw_m16_AX, addressSize, Register.AX, repPrefix);
		}

		// Token: 0x060021E7 RID: 8679 RVA: 0x0006E90D File Offset: 0x0006CB0D
		public static Instruction CreateRepStosw(int addressSize)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Stosw_m16_AX, addressSize, Register.AX, RepPrefixKind.Repe);
		}

		// Token: 0x060021E8 RID: 8680 RVA: 0x0006E91D File Offset: 0x0006CB1D
		public static Instruction CreateStosd(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Stosd_m32_EAX, addressSize, Register.EAX, repPrefix);
		}

		// Token: 0x060021E9 RID: 8681 RVA: 0x0006E92D File Offset: 0x0006CB2D
		public static Instruction CreateRepStosd(int addressSize)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Stosd_m32_EAX, addressSize, Register.EAX, RepPrefixKind.Repe);
		}

		// Token: 0x060021EA RID: 8682 RVA: 0x0006E93D File Offset: 0x0006CB3D
		public static Instruction CreateStosq(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Stosq_m64_RAX, addressSize, Register.RAX, repPrefix);
		}

		// Token: 0x060021EB RID: 8683 RVA: 0x0006E94D File Offset: 0x0006CB4D
		public static Instruction CreateRepStosq(int addressSize)
		{
			return Instruction.CreateString_ESRDI_Reg(Code.Stosq_m64_RAX, addressSize, Register.RAX, RepPrefixKind.Repe);
		}

		// Token: 0x060021EC RID: 8684 RVA: 0x0006E95D File Offset: 0x0006CB5D
		public static Instruction CreateCmpsb(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsb_m8_m8, addressSize, segmentPrefix, repPrefix);
		}

		// Token: 0x060021ED RID: 8685 RVA: 0x0006E96C File Offset: 0x0006CB6C
		public static Instruction CreateRepeCmpsb(int addressSize)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsb_m8_m8, addressSize, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021EE RID: 8686 RVA: 0x0006E97B File Offset: 0x0006CB7B
		public static Instruction CreateRepneCmpsb(int addressSize)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsb_m8_m8, addressSize, Register.None, RepPrefixKind.Repne);
		}

		// Token: 0x060021EF RID: 8687 RVA: 0x0006E98A File Offset: 0x0006CB8A
		public static Instruction CreateCmpsw(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsw_m16_m16, addressSize, segmentPrefix, repPrefix);
		}

		// Token: 0x060021F0 RID: 8688 RVA: 0x0006E999 File Offset: 0x0006CB99
		public static Instruction CreateRepeCmpsw(int addressSize)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsw_m16_m16, addressSize, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021F1 RID: 8689 RVA: 0x0006E9A8 File Offset: 0x0006CBA8
		public static Instruction CreateRepneCmpsw(int addressSize)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsw_m16_m16, addressSize, Register.None, RepPrefixKind.Repne);
		}

		// Token: 0x060021F2 RID: 8690 RVA: 0x0006E9B7 File Offset: 0x0006CBB7
		public static Instruction CreateCmpsd(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsd_m32_m32, addressSize, segmentPrefix, repPrefix);
		}

		// Token: 0x060021F3 RID: 8691 RVA: 0x0006E9C6 File Offset: 0x0006CBC6
		public static Instruction CreateRepeCmpsd(int addressSize)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsd_m32_m32, addressSize, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021F4 RID: 8692 RVA: 0x0006E9D5 File Offset: 0x0006CBD5
		public static Instruction CreateRepneCmpsd(int addressSize)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsd_m32_m32, addressSize, Register.None, RepPrefixKind.Repne);
		}

		// Token: 0x060021F5 RID: 8693 RVA: 0x0006E9E4 File Offset: 0x0006CBE4
		public static Instruction CreateCmpsq(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsq_m64_m64, addressSize, segmentPrefix, repPrefix);
		}

		// Token: 0x060021F6 RID: 8694 RVA: 0x0006E9F3 File Offset: 0x0006CBF3
		public static Instruction CreateRepeCmpsq(int addressSize)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsq_m64_m64, addressSize, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021F7 RID: 8695 RVA: 0x0006EA02 File Offset: 0x0006CC02
		public static Instruction CreateRepneCmpsq(int addressSize)
		{
			return Instruction.CreateString_SegRSI_ESRDI(Code.Cmpsq_m64_m64, addressSize, Register.None, RepPrefixKind.Repne);
		}

		// Token: 0x060021F8 RID: 8696 RVA: 0x0006EA11 File Offset: 0x0006CC11
		public static Instruction CreateMovsb(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_SegRSI(Code.Movsb_m8_m8, addressSize, segmentPrefix, repPrefix);
		}

		// Token: 0x060021F9 RID: 8697 RVA: 0x0006EA20 File Offset: 0x0006CC20
		public static Instruction CreateRepMovsb(int addressSize)
		{
			return Instruction.CreateString_ESRDI_SegRSI(Code.Movsb_m8_m8, addressSize, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021FA RID: 8698 RVA: 0x0006EA2F File Offset: 0x0006CC2F
		public static Instruction CreateMovsw(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_SegRSI(Code.Movsw_m16_m16, addressSize, segmentPrefix, repPrefix);
		}

		// Token: 0x060021FB RID: 8699 RVA: 0x0006EA3E File Offset: 0x0006CC3E
		public static Instruction CreateRepMovsw(int addressSize)
		{
			return Instruction.CreateString_ESRDI_SegRSI(Code.Movsw_m16_m16, addressSize, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021FC RID: 8700 RVA: 0x0006EA4D File Offset: 0x0006CC4D
		public static Instruction CreateMovsd(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_SegRSI(Code.Movsd_m32_m32, addressSize, segmentPrefix, repPrefix);
		}

		// Token: 0x060021FD RID: 8701 RVA: 0x0006EA5C File Offset: 0x0006CC5C
		public static Instruction CreateRepMovsd(int addressSize)
		{
			return Instruction.CreateString_ESRDI_SegRSI(Code.Movsd_m32_m32, addressSize, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x060021FE RID: 8702 RVA: 0x0006EA6B File Offset: 0x0006CC6B
		public static Instruction CreateMovsq(int addressSize, Register segmentPrefix = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None)
		{
			return Instruction.CreateString_ESRDI_SegRSI(Code.Movsq_m64_m64, addressSize, segmentPrefix, repPrefix);
		}

		// Token: 0x060021FF RID: 8703 RVA: 0x0006EA7A File Offset: 0x0006CC7A
		public static Instruction CreateRepMovsq(int addressSize)
		{
			return Instruction.CreateString_ESRDI_SegRSI(Code.Movsq_m64_m64, addressSize, Register.None, RepPrefixKind.Repe);
		}

		// Token: 0x06002200 RID: 8704 RVA: 0x0006EA89 File Offset: 0x0006CC89
		public static Instruction CreateMaskmovq(int addressSize, Register register1, Register register2, Register segmentPrefix = Register.None)
		{
			return Instruction.CreateMaskmov(Code.Maskmovq_rDI_mm_mm, addressSize, register1, register2, segmentPrefix);
		}

		// Token: 0x06002201 RID: 8705 RVA: 0x0006EA99 File Offset: 0x0006CC99
		public static Instruction CreateMaskmovdqu(int addressSize, Register register1, Register register2, Register segmentPrefix = Register.None)
		{
			return Instruction.CreateMaskmov(Code.Maskmovdqu_rDI_xmm_xmm, addressSize, register1, register2, segmentPrefix);
		}

		// Token: 0x06002202 RID: 8706 RVA: 0x0006EAA9 File Offset: 0x0006CCA9
		public static Instruction CreateVmaskmovdqu(int addressSize, Register register1, Register register2, Register segmentPrefix = Register.None)
		{
			return Instruction.CreateMaskmov(Code.VEX_Vmaskmovdqu_rDI_xmm_xmm, addressSize, register1, register2, segmentPrefix);
		}

		// Token: 0x06002203 RID: 8707 RVA: 0x0006EABC File Offset: 0x0006CCBC
		public static Instruction CreateDeclareByte(byte b0)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 1U;
			instruction.SetDeclareByteValue(0, b0);
			return instruction;
		}

		// Token: 0x06002204 RID: 8708 RVA: 0x0006EAEC File Offset: 0x0006CCEC
		public static Instruction CreateDeclareByte(byte b0, byte b1)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 2U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			return instruction;
		}

		// Token: 0x06002205 RID: 8709 RVA: 0x0006EB24 File Offset: 0x0006CD24
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 3U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			return instruction;
		}

		// Token: 0x06002206 RID: 8710 RVA: 0x0006EB68 File Offset: 0x0006CD68
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 4U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			return instruction;
		}

		// Token: 0x06002207 RID: 8711 RVA: 0x0006EBB4 File Offset: 0x0006CDB4
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 5U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			return instruction;
		}

		// Token: 0x06002208 RID: 8712 RVA: 0x0006EC08 File Offset: 0x0006CE08
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 6U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			return instruction;
		}

		// Token: 0x06002209 RID: 8713 RVA: 0x0006EC68 File Offset: 0x0006CE68
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 7U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			return instruction;
		}

		// Token: 0x0600220A RID: 8714 RVA: 0x0006ECD0 File Offset: 0x0006CED0
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 8U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			instruction.SetDeclareByteValue(7, b7);
			return instruction;
		}

		// Token: 0x0600220B RID: 8715 RVA: 0x0006ED44 File Offset: 0x0006CF44
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 9U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			instruction.SetDeclareByteValue(7, b7);
			instruction.SetDeclareByteValue(8, b8);
			return instruction;
		}

		// Token: 0x0600220C RID: 8716 RVA: 0x0006EDC4 File Offset: 0x0006CFC4
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 10U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			instruction.SetDeclareByteValue(7, b7);
			instruction.SetDeclareByteValue(8, b8);
			instruction.SetDeclareByteValue(9, b9);
			return instruction;
		}

		// Token: 0x0600220D RID: 8717 RVA: 0x0006EE4C File Offset: 0x0006D04C
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 11U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			instruction.SetDeclareByteValue(7, b7);
			instruction.SetDeclareByteValue(8, b8);
			instruction.SetDeclareByteValue(9, b9);
			instruction.SetDeclareByteValue(10, b10);
			return instruction;
		}

		// Token: 0x0600220E RID: 8718 RVA: 0x0006EEE0 File Offset: 0x0006D0E0
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 12U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			instruction.SetDeclareByteValue(7, b7);
			instruction.SetDeclareByteValue(8, b8);
			instruction.SetDeclareByteValue(9, b9);
			instruction.SetDeclareByteValue(10, b10);
			instruction.SetDeclareByteValue(11, b11);
			return instruction;
		}

		// Token: 0x0600220F RID: 8719 RVA: 0x0006EF80 File Offset: 0x0006D180
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 13U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			instruction.SetDeclareByteValue(7, b7);
			instruction.SetDeclareByteValue(8, b8);
			instruction.SetDeclareByteValue(9, b9);
			instruction.SetDeclareByteValue(10, b10);
			instruction.SetDeclareByteValue(11, b11);
			instruction.SetDeclareByteValue(12, b12);
			return instruction;
		}

		// Token: 0x06002210 RID: 8720 RVA: 0x0006F02C File Offset: 0x0006D22C
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12, byte b13)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 14U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			instruction.SetDeclareByteValue(7, b7);
			instruction.SetDeclareByteValue(8, b8);
			instruction.SetDeclareByteValue(9, b9);
			instruction.SetDeclareByteValue(10, b10);
			instruction.SetDeclareByteValue(11, b11);
			instruction.SetDeclareByteValue(12, b12);
			instruction.SetDeclareByteValue(13, b13);
			return instruction;
		}

		// Token: 0x06002211 RID: 8721 RVA: 0x0006F0E0 File Offset: 0x0006D2E0
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12, byte b13, byte b14)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 15U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			instruction.SetDeclareByteValue(7, b7);
			instruction.SetDeclareByteValue(8, b8);
			instruction.SetDeclareByteValue(9, b9);
			instruction.SetDeclareByteValue(10, b10);
			instruction.SetDeclareByteValue(11, b11);
			instruction.SetDeclareByteValue(12, b12);
			instruction.SetDeclareByteValue(13, b13);
			instruction.SetDeclareByteValue(14, b14);
			return instruction;
		}

		// Token: 0x06002212 RID: 8722 RVA: 0x0006F1A0 File Offset: 0x0006D3A0
		public static Instruction CreateDeclareByte(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12, byte b13, byte b14, byte b15)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = 16U;
			instruction.SetDeclareByteValue(0, b0);
			instruction.SetDeclareByteValue(1, b1);
			instruction.SetDeclareByteValue(2, b2);
			instruction.SetDeclareByteValue(3, b3);
			instruction.SetDeclareByteValue(4, b4);
			instruction.SetDeclareByteValue(5, b5);
			instruction.SetDeclareByteValue(6, b6);
			instruction.SetDeclareByteValue(7, b7);
			instruction.SetDeclareByteValue(8, b8);
			instruction.SetDeclareByteValue(9, b9);
			instruction.SetDeclareByteValue(10, b10);
			instruction.SetDeclareByteValue(11, b11);
			instruction.SetDeclareByteValue(12, b12);
			instruction.SetDeclareByteValue(13, b13);
			instruction.SetDeclareByteValue(14, b14);
			instruction.SetDeclareByteValue(15, b15);
			return instruction;
		}

		// Token: 0x06002213 RID: 8723 RVA: 0x0006F26C File Offset: 0x0006D46C
		[NullableContext(0)]
		public unsafe static Instruction CreateDeclareByte(ReadOnlySpan<byte> data)
		{
			if (data.Length - 1 > 15)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_data();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = (uint)data.Length;
			for (int i = 0; i < data.Length; i++)
			{
				instruction.SetDeclareByteValue(i, *data[i]);
			}
			return instruction;
		}

		// Token: 0x06002214 RID: 8724 RVA: 0x0006F2CD File Offset: 0x0006D4CD
		public static Instruction CreateDeclareByte(byte[] data)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			return Instruction.CreateDeclareByte(data, 0, data.Length);
		}

		// Token: 0x06002215 RID: 8725 RVA: 0x0006F2E4 File Offset: 0x0006D4E4
		public static Instruction CreateDeclareByte(byte[] data, int index, int length)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			if (length - 1 > 15)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_length();
			}
			if ((ulong)index + (ulong)length > (ulong)data.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareByte;
			instruction.InternalDeclareDataCount = (uint)length;
			for (int i = 0; i < length; i++)
			{
				instruction.SetDeclareByteValue(i, data[index + i]);
			}
			return instruction;
		}

		// Token: 0x06002216 RID: 8726 RVA: 0x0006F348 File Offset: 0x0006D548
		public static Instruction CreateDeclareWord(ushort w0)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = 1U;
			instruction.SetDeclareWordValue(0, w0);
			return instruction;
		}

		// Token: 0x06002217 RID: 8727 RVA: 0x0006F378 File Offset: 0x0006D578
		public static Instruction CreateDeclareWord(ushort w0, ushort w1)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = 2U;
			instruction.SetDeclareWordValue(0, w0);
			instruction.SetDeclareWordValue(1, w1);
			return instruction;
		}

		// Token: 0x06002218 RID: 8728 RVA: 0x0006F3B0 File Offset: 0x0006D5B0
		public static Instruction CreateDeclareWord(ushort w0, ushort w1, ushort w2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = 3U;
			instruction.SetDeclareWordValue(0, w0);
			instruction.SetDeclareWordValue(1, w1);
			instruction.SetDeclareWordValue(2, w2);
			return instruction;
		}

		// Token: 0x06002219 RID: 8729 RVA: 0x0006F3F4 File Offset: 0x0006D5F4
		public static Instruction CreateDeclareWord(ushort w0, ushort w1, ushort w2, ushort w3)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = 4U;
			instruction.SetDeclareWordValue(0, w0);
			instruction.SetDeclareWordValue(1, w1);
			instruction.SetDeclareWordValue(2, w2);
			instruction.SetDeclareWordValue(3, w3);
			return instruction;
		}

		// Token: 0x0600221A RID: 8730 RVA: 0x0006F440 File Offset: 0x0006D640
		public static Instruction CreateDeclareWord(ushort w0, ushort w1, ushort w2, ushort w3, ushort w4)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = 5U;
			instruction.SetDeclareWordValue(0, w0);
			instruction.SetDeclareWordValue(1, w1);
			instruction.SetDeclareWordValue(2, w2);
			instruction.SetDeclareWordValue(3, w3);
			instruction.SetDeclareWordValue(4, w4);
			return instruction;
		}

		// Token: 0x0600221B RID: 8731 RVA: 0x0006F494 File Offset: 0x0006D694
		public static Instruction CreateDeclareWord(ushort w0, ushort w1, ushort w2, ushort w3, ushort w4, ushort w5)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = 6U;
			instruction.SetDeclareWordValue(0, w0);
			instruction.SetDeclareWordValue(1, w1);
			instruction.SetDeclareWordValue(2, w2);
			instruction.SetDeclareWordValue(3, w3);
			instruction.SetDeclareWordValue(4, w4);
			instruction.SetDeclareWordValue(5, w5);
			return instruction;
		}

		// Token: 0x0600221C RID: 8732 RVA: 0x0006F4F4 File Offset: 0x0006D6F4
		public static Instruction CreateDeclareWord(ushort w0, ushort w1, ushort w2, ushort w3, ushort w4, ushort w5, ushort w6)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = 7U;
			instruction.SetDeclareWordValue(0, w0);
			instruction.SetDeclareWordValue(1, w1);
			instruction.SetDeclareWordValue(2, w2);
			instruction.SetDeclareWordValue(3, w3);
			instruction.SetDeclareWordValue(4, w4);
			instruction.SetDeclareWordValue(5, w5);
			instruction.SetDeclareWordValue(6, w6);
			return instruction;
		}

		// Token: 0x0600221D RID: 8733 RVA: 0x0006F55C File Offset: 0x0006D75C
		public static Instruction CreateDeclareWord(ushort w0, ushort w1, ushort w2, ushort w3, ushort w4, ushort w5, ushort w6, ushort w7)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = 8U;
			instruction.SetDeclareWordValue(0, w0);
			instruction.SetDeclareWordValue(1, w1);
			instruction.SetDeclareWordValue(2, w2);
			instruction.SetDeclareWordValue(3, w3);
			instruction.SetDeclareWordValue(4, w4);
			instruction.SetDeclareWordValue(5, w5);
			instruction.SetDeclareWordValue(6, w6);
			instruction.SetDeclareWordValue(7, w7);
			return instruction;
		}

		// Token: 0x0600221E RID: 8734 RVA: 0x0006F5D0 File Offset: 0x0006D7D0
		[NullableContext(0)]
		public unsafe static Instruction CreateDeclareWord(ReadOnlySpan<byte> data)
		{
			if (data.Length - 1 > 15 || (data.Length & 1) != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_data();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = (uint)(data.Length / 2);
			for (int i = 0; i < data.Length; i += 2)
			{
				uint v = (uint)((int)(*data[i]) | ((int)(*data[i + 1]) << 8));
				instruction.SetDeclareWordValue(i / 2, (ushort)v);
			}
			return instruction;
		}

		// Token: 0x0600221F RID: 8735 RVA: 0x0006F651 File Offset: 0x0006D851
		public static Instruction CreateDeclareWord(byte[] data)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			return Instruction.CreateDeclareWord(data, 0, data.Length);
		}

		// Token: 0x06002220 RID: 8736 RVA: 0x0006F668 File Offset: 0x0006D868
		public static Instruction CreateDeclareWord(byte[] data, int index, int length)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			if (length - 1 > 15 || (length & 1) != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_length();
			}
			if ((ulong)index + (ulong)length > (ulong)data.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = (uint)(length / 2);
			for (int i = 0; i < length; i += 2)
			{
				uint v = (uint)((int)data[index + i] | ((int)data[index + i + 1] << 8));
				instruction.SetDeclareWordValue(i / 2, (ushort)v);
			}
			return instruction;
		}

		// Token: 0x06002221 RID: 8737 RVA: 0x0006F6E4 File Offset: 0x0006D8E4
		[NullableContext(0)]
		public unsafe static Instruction CreateDeclareWord(ReadOnlySpan<ushort> data)
		{
			if (data.Length - 1 > 7)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_data();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = (uint)data.Length;
			for (int i = 0; i < data.Length; i++)
			{
				instruction.SetDeclareWordValue(i, *data[i]);
			}
			return instruction;
		}

		// Token: 0x06002222 RID: 8738 RVA: 0x0006F744 File Offset: 0x0006D944
		public static Instruction CreateDeclareWord(ushort[] data)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			return Instruction.CreateDeclareWord(data, 0, data.Length);
		}

		// Token: 0x06002223 RID: 8739 RVA: 0x0006F758 File Offset: 0x0006D958
		public static Instruction CreateDeclareWord(ushort[] data, int index, int length)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			if (length - 1 > 7)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_length();
			}
			if ((ulong)index + (ulong)length > (ulong)data.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareWord;
			instruction.InternalDeclareDataCount = (uint)length;
			for (int i = 0; i < length; i++)
			{
				instruction.SetDeclareWordValue(i, data[index + i]);
			}
			return instruction;
		}

		// Token: 0x06002224 RID: 8740 RVA: 0x0006F7BC File Offset: 0x0006D9BC
		public static Instruction CreateDeclareDword(uint d0)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareDword;
			instruction.InternalDeclareDataCount = 1U;
			instruction.SetDeclareDwordValue(0, d0);
			return instruction;
		}

		// Token: 0x06002225 RID: 8741 RVA: 0x0006F7EC File Offset: 0x0006D9EC
		public static Instruction CreateDeclareDword(uint d0, uint d1)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareDword;
			instruction.InternalDeclareDataCount = 2U;
			instruction.SetDeclareDwordValue(0, d0);
			instruction.SetDeclareDwordValue(1, d1);
			return instruction;
		}

		// Token: 0x06002226 RID: 8742 RVA: 0x0006F824 File Offset: 0x0006DA24
		public static Instruction CreateDeclareDword(uint d0, uint d1, uint d2)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareDword;
			instruction.InternalDeclareDataCount = 3U;
			instruction.SetDeclareDwordValue(0, d0);
			instruction.SetDeclareDwordValue(1, d1);
			instruction.SetDeclareDwordValue(2, d2);
			return instruction;
		}

		// Token: 0x06002227 RID: 8743 RVA: 0x0006F868 File Offset: 0x0006DA68
		public static Instruction CreateDeclareDword(uint d0, uint d1, uint d2, uint d3)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareDword;
			instruction.InternalDeclareDataCount = 4U;
			instruction.SetDeclareDwordValue(0, d0);
			instruction.SetDeclareDwordValue(1, d1);
			instruction.SetDeclareDwordValue(2, d2);
			instruction.SetDeclareDwordValue(3, d3);
			return instruction;
		}

		// Token: 0x06002228 RID: 8744 RVA: 0x0006F8B4 File Offset: 0x0006DAB4
		[NullableContext(0)]
		public unsafe static Instruction CreateDeclareDword(ReadOnlySpan<byte> data)
		{
			if (data.Length - 1 > 15 || (data.Length & 3) != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_data();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareDword;
			instruction.InternalDeclareDataCount = (uint)(data.Length / 4);
			for (int i = 0; i < data.Length; i += 4)
			{
				uint v = (uint)((int)(*data[i]) | ((int)(*data[i + 1]) << 8) | ((int)(*data[i + 2]) << 16) | ((int)(*data[i + 3]) << 24));
				instruction.SetDeclareDwordValue(i / 4, v);
			}
			return instruction;
		}

		// Token: 0x06002229 RID: 8745 RVA: 0x0006F952 File Offset: 0x0006DB52
		public static Instruction CreateDeclareDword(byte[] data)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			return Instruction.CreateDeclareDword(data, 0, data.Length);
		}

		// Token: 0x0600222A RID: 8746 RVA: 0x0006F968 File Offset: 0x0006DB68
		public static Instruction CreateDeclareDword(byte[] data, int index, int length)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			if (length - 1 > 15 || (length & 3) != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_length();
			}
			if ((ulong)index + (ulong)length > (ulong)data.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareDword;
			instruction.InternalDeclareDataCount = (uint)(length / 4);
			for (int i = 0; i < length; i += 4)
			{
				uint v = (uint)((int)data[index + i] | ((int)data[index + i + 1] << 8) | ((int)data[index + i + 2] << 16) | ((int)data[index + i + 3] << 24));
				instruction.SetDeclareDwordValue(i / 4, v);
			}
			return instruction;
		}

		// Token: 0x0600222B RID: 8747 RVA: 0x0006F9F8 File Offset: 0x0006DBF8
		[NullableContext(0)]
		public unsafe static Instruction CreateDeclareDword(ReadOnlySpan<uint> data)
		{
			if (data.Length - 1 > 3)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_data();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareDword;
			instruction.InternalDeclareDataCount = (uint)data.Length;
			for (int i = 0; i < data.Length; i++)
			{
				instruction.SetDeclareDwordValue(i, *data[i]);
			}
			return instruction;
		}

		// Token: 0x0600222C RID: 8748 RVA: 0x0006FA58 File Offset: 0x0006DC58
		public static Instruction CreateDeclareDword(uint[] data)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			return Instruction.CreateDeclareDword(data, 0, data.Length);
		}

		// Token: 0x0600222D RID: 8749 RVA: 0x0006FA6C File Offset: 0x0006DC6C
		public static Instruction CreateDeclareDword(uint[] data, int index, int length)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			if (length - 1 > 3)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_length();
			}
			if ((ulong)index + (ulong)length > (ulong)data.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareDword;
			instruction.InternalDeclareDataCount = (uint)length;
			for (int i = 0; i < length; i++)
			{
				instruction.SetDeclareDwordValue(i, data[index + i]);
			}
			return instruction;
		}

		// Token: 0x0600222E RID: 8750 RVA: 0x0006FAD0 File Offset: 0x0006DCD0
		public static Instruction CreateDeclareQword(ulong q0)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareQword;
			instruction.InternalDeclareDataCount = 1U;
			instruction.SetDeclareQwordValue(0, q0);
			return instruction;
		}

		// Token: 0x0600222F RID: 8751 RVA: 0x0006FB00 File Offset: 0x0006DD00
		public static Instruction CreateDeclareQword(ulong q0, ulong q1)
		{
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareQword;
			instruction.InternalDeclareDataCount = 2U;
			instruction.SetDeclareQwordValue(0, q0);
			instruction.SetDeclareQwordValue(1, q1);
			return instruction;
		}

		// Token: 0x06002230 RID: 8752 RVA: 0x0006FB38 File Offset: 0x0006DD38
		[NullableContext(0)]
		public unsafe static Instruction CreateDeclareQword(ReadOnlySpan<byte> data)
		{
			if (data.Length - 1 > 15 || (data.Length & 7) != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_data();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareQword;
			instruction.InternalDeclareDataCount = (uint)(data.Length / 8);
			for (int i = 0; i < data.Length; i += 8)
			{
				uint v = (uint)((int)(*data[i]) | ((int)(*data[i + 1]) << 8) | ((int)(*data[i + 2]) << 16) | ((int)(*data[i + 3]) << 24));
				uint v2 = (uint)((int)(*data[i + 4]) | ((int)(*data[i + 5]) << 8) | ((int)(*data[i + 6]) << 16) | ((int)(*data[i + 7]) << 24));
				instruction.SetDeclareQwordValue(i / 8, (ulong)v | ((ulong)v2 << 32));
			}
			return instruction;
		}

		// Token: 0x06002231 RID: 8753 RVA: 0x0006FC1B File Offset: 0x0006DE1B
		public static Instruction CreateDeclareQword(byte[] data)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			return Instruction.CreateDeclareQword(data, 0, data.Length);
		}

		// Token: 0x06002232 RID: 8754 RVA: 0x0006FC30 File Offset: 0x0006DE30
		public static Instruction CreateDeclareQword(byte[] data, int index, int length)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			if (length - 1 > 15 || (length & 7) != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_length();
			}
			if ((ulong)index + (ulong)length > (ulong)data.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareQword;
			instruction.InternalDeclareDataCount = (uint)(length / 8);
			for (int i = 0; i < length; i += 8)
			{
				uint v = (uint)((int)data[index + i] | ((int)data[index + i + 1] << 8) | ((int)data[index + i + 2] << 16) | ((int)data[index + i + 3] << 24));
				uint v2 = (uint)((int)data[index + i + 4] | ((int)data[index + i + 5] << 8) | ((int)data[index + i + 6] << 16) | ((int)data[index + i + 7] << 24));
				instruction.SetDeclareQwordValue(i / 8, (ulong)v | ((ulong)v2 << 32));
			}
			return instruction;
		}

		// Token: 0x06002233 RID: 8755 RVA: 0x0006FCF0 File Offset: 0x0006DEF0
		[NullableContext(0)]
		public unsafe static Instruction CreateDeclareQword(ReadOnlySpan<ulong> data)
		{
			if (data.Length - 1 > 1)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_data();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareQword;
			instruction.InternalDeclareDataCount = (uint)data.Length;
			for (int i = 0; i < data.Length; i++)
			{
				instruction.SetDeclareQwordValue(i, (ulong)(*data[i]));
			}
			return instruction;
		}

		// Token: 0x06002234 RID: 8756 RVA: 0x0006FD50 File Offset: 0x0006DF50
		public static Instruction CreateDeclareQword(ulong[] data)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			return Instruction.CreateDeclareQword(data, 0, data.Length);
		}

		// Token: 0x06002235 RID: 8757 RVA: 0x0006FD64 File Offset: 0x0006DF64
		public static Instruction CreateDeclareQword(ulong[] data, int index, int length)
		{
			if (data == null)
			{
				ThrowHelper.ThrowArgumentNullException_data();
			}
			if (length - 1 > 1)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_length();
			}
			if ((ulong)index + (ulong)length > (ulong)data.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
			}
			Instruction instruction = default(Instruction);
			instruction.Code = Code.DeclareQword;
			instruction.InternalDeclareDataCount = (uint)length;
			for (int i = 0; i < length; i++)
			{
				instruction.SetDeclareQwordValue(i, data[index + i]);
			}
			return instruction;
		}

		// Token: 0x06002236 RID: 8758 RVA: 0x0006FDC6 File Offset: 0x0006DFC6
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(in Instruction left, in Instruction right)
		{
			return Instruction.EqualsInternal(left, right);
		}

		// Token: 0x06002237 RID: 8759 RVA: 0x0006FDCF File Offset: 0x0006DFCF
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(in Instruction left, in Instruction right)
		{
			return !Instruction.EqualsInternal(left, right);
		}

		// Token: 0x06002238 RID: 8760 RVA: 0x0006FDC6 File Offset: 0x0006DFC6
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool Equals(in Instruction other)
		{
			return Instruction.EqualsInternal(this, other);
		}

		// Token: 0x06002239 RID: 8761 RVA: 0x0006FDDB File Offset: 0x0006DFDB
		readonly bool IEquatable<Instruction>.Equals(Instruction other)
		{
			return Instruction.EqualsInternal(this, other);
		}

		// Token: 0x0600223A RID: 8762 RVA: 0x0006FDE8 File Offset: 0x0006DFE8
		private static bool EqualsInternal(in Instruction a, in Instruction b)
		{
			return a.memDispl == b.memDispl && ((a.flags1 ^ b.flags1) & 4294180863U) == 0U && a.immediate == b.immediate && a.code == b.code && a.memBaseReg == b.memBaseReg && a.memIndexReg == b.memIndexReg && a.reg0 == b.reg0 && a.reg1 == b.reg1 && a.reg2 == b.reg2 && a.reg3 == b.reg3 && a.opKind0 == b.opKind0 && a.opKind1 == b.opKind1 && a.opKind2 == b.opKind2 && a.opKind3 == b.opKind3 && a.scale == b.scale && a.displSize == b.displSize && a.pad == b.pad;
		}

		// Token: 0x0600223B RID: 8763 RVA: 0x0006FF04 File Offset: 0x0006E104
		public override readonly int GetHashCode()
		{
			return (int)((uint)this.memDispl ^ (uint)(this.memDispl >> 32) ^ (this.flags1 & 4294180863U) ^ this.immediate ^ (uint)((uint)this.code << 8) ^ (uint)((uint)this.memBaseReg << 16) ^ (uint)((uint)this.memIndexReg << 24) ^ (uint)this.reg3 ^ (uint)((uint)this.reg2 << 8) ^ (uint)((uint)this.reg1 << 16) ^ (uint)((uint)this.reg0 << 24) ^ (uint)this.opKind3 ^ (uint)((uint)this.opKind2 << 8) ^ (uint)((uint)this.opKind1 << 16) ^ (uint)((uint)this.opKind0 << 24) ^ (uint)this.scale ^ (uint)((uint)this.displSize << 8) ^ (uint)((uint)this.pad << 16));
		}

		// Token: 0x0600223C RID: 8764 RVA: 0x0006FFB8 File Offset: 0x0006E1B8
		[NullableContext(2)]
		public override readonly bool Equals(object obj)
		{
			if (obj is Instruction)
			{
				Instruction other = (Instruction)obj;
				return Instruction.EqualsInternal(this, other);
			}
			return false;
		}

		// Token: 0x0600223D RID: 8765 RVA: 0x0006FFE0 File Offset: 0x0006E1E0
		public static bool EqualsAllBits(in Instruction a, in Instruction b)
		{
			return a.nextRip == b.nextRip && a.memDispl == b.memDispl && a.flags1 == b.flags1 && a.immediate == b.immediate && a.code == b.code && a.memBaseReg == b.memBaseReg && a.memIndexReg == b.memIndexReg && a.reg0 == b.reg0 && a.reg1 == b.reg1 && a.reg2 == b.reg2 && a.reg3 == b.reg3 && a.opKind0 == b.opKind0 && a.opKind1 == b.opKind1 && a.opKind2 == b.opKind2 && a.opKind3 == b.opKind3 && a.scale == b.scale && a.displSize == b.displSize && a.len == b.len && a.pad == b.pad;
		}

		// Token: 0x17000784 RID: 1924
		// (get) Token: 0x0600223E RID: 8766 RVA: 0x00070114 File Offset: 0x0006E314
		// (set) Token: 0x0600223F RID: 8767 RVA: 0x00070125 File Offset: 0x0006E325
		public ushort IP16
		{
			readonly get
			{
				return (ushort)((uint)this.nextRip - (uint)this.Length);
			}
			set
			{
				this.nextRip = (ulong)((int)value + this.Length);
			}
		}

		// Token: 0x17000785 RID: 1925
		// (get) Token: 0x06002240 RID: 8768 RVA: 0x00070136 File Offset: 0x0006E336
		// (set) Token: 0x06002241 RID: 8769 RVA: 0x00070125 File Offset: 0x0006E325
		public uint IP32
		{
			readonly get
			{
				return (uint)this.nextRip - (uint)this.Length;
			}
			set
			{
				this.nextRip = (ulong)(value + (uint)this.Length);
			}
		}

		// Token: 0x17000786 RID: 1926
		// (get) Token: 0x06002242 RID: 8770 RVA: 0x00070146 File Offset: 0x0006E346
		// (set) Token: 0x06002243 RID: 8771 RVA: 0x00070156 File Offset: 0x0006E356
		public ulong IP
		{
			readonly get
			{
				return this.nextRip - (ulong)this.Length;
			}
			set
			{
				this.nextRip = value + (ulong)this.Length;
			}
		}

		// Token: 0x17000787 RID: 1927
		// (get) Token: 0x06002244 RID: 8772 RVA: 0x00070167 File Offset: 0x0006E367
		// (set) Token: 0x06002245 RID: 8773 RVA: 0x00070170 File Offset: 0x0006E370
		public ushort NextIP16
		{
			readonly get
			{
				return (ushort)this.nextRip;
			}
			set
			{
				this.nextRip = (ulong)value;
			}
		}

		// Token: 0x17000788 RID: 1928
		// (get) Token: 0x06002246 RID: 8774 RVA: 0x0007017A File Offset: 0x0006E37A
		// (set) Token: 0x06002247 RID: 8775 RVA: 0x00070170 File Offset: 0x0006E370
		public uint NextIP32
		{
			readonly get
			{
				return (uint)this.nextRip;
			}
			set
			{
				this.nextRip = (ulong)value;
			}
		}

		// Token: 0x17000789 RID: 1929
		// (get) Token: 0x06002248 RID: 8776 RVA: 0x00070183 File Offset: 0x0006E383
		// (set) Token: 0x06002249 RID: 8777 RVA: 0x0007018B File Offset: 0x0006E38B
		public ulong NextIP
		{
			readonly get
			{
				return this.nextRip;
			}
			set
			{
				this.nextRip = value;
			}
		}

		// Token: 0x1700078A RID: 1930
		// (get) Token: 0x0600224A RID: 8778 RVA: 0x00070194 File Offset: 0x0006E394
		// (set) Token: 0x0600224B RID: 8779 RVA: 0x000701A1 File Offset: 0x0006E3A1
		public CodeSize CodeSize
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (CodeSize)((this.flags1 >> 18) & 3U);
			}
			set
			{
				this.flags1 = (this.flags1 & 4294180863U) | (uint)((uint)(value & CodeSize.Code64) << 18);
			}
		}

		// Token: 0x1700078B RID: 1931
		// (set) Token: 0x0600224C RID: 8780 RVA: 0x000701BC File Offset: 0x0006E3BC
		internal CodeSize InternalCodeSize
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.flags1 |= (uint)((uint)value << 18);
			}
		}

		// Token: 0x1700078C RID: 1932
		// (get) Token: 0x0600224D RID: 8781 RVA: 0x000701CF File Offset: 0x0006E3CF
		public readonly bool IsInvalid
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this.code == 0;
			}
		}

		// Token: 0x1700078D RID: 1933
		// (get) Token: 0x0600224E RID: 8782 RVA: 0x000701DA File Offset: 0x0006E3DA
		// (set) Token: 0x0600224F RID: 8783 RVA: 0x000701E2 File Offset: 0x0006E3E2
		public Code Code
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (Code)this.code;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				if (value >= (Code)4936)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_value();
				}
				this.code = (ushort)value;
			}
		}

		// Token: 0x06002250 RID: 8784 RVA: 0x000701F9 File Offset: 0x0006E3F9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetCodeNoCheck(Code code)
		{
			this.code = (ushort)code;
		}

		// Token: 0x1700078E RID: 1934
		// (get) Token: 0x06002251 RID: 8785 RVA: 0x00070203 File Offset: 0x0006E403
		public readonly Mnemonic Mnemonic
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this.Code.Mnemonic();
			}
		}

		// Token: 0x1700078F RID: 1935
		// (get) Token: 0x06002252 RID: 8786 RVA: 0x00070210 File Offset: 0x0006E410
		public unsafe readonly int OpCount
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (int)(*InstructionOpCounts.OpCount[(int)this.code]);
			}
		}

		// Token: 0x17000790 RID: 1936
		// (get) Token: 0x06002253 RID: 8787 RVA: 0x00070231 File Offset: 0x0006E431
		// (set) Token: 0x06002254 RID: 8788 RVA: 0x00070239 File Offset: 0x0006E439
		public int Length
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (int)this.len;
			}
			set
			{
				this.len = (byte)value;
			}
		}

		// Token: 0x17000791 RID: 1937
		// (get) Token: 0x06002255 RID: 8789 RVA: 0x00070243 File Offset: 0x0006E443
		internal readonly bool Internal_HasRepeOrRepnePrefix
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (this.flags1 & 1610612736U) > 0U;
			}
		}

		// Token: 0x17000792 RID: 1938
		// (get) Token: 0x06002256 RID: 8790 RVA: 0x00070254 File Offset: 0x0006E454
		internal readonly uint HasAnyOf_Lock_Rep_Repne_Prefix
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this.flags1 & 3758096384U;
			}
		}

		// Token: 0x06002257 RID: 8791 RVA: 0x00070262 File Offset: 0x0006E462
		private readonly bool IsXacquireInstr()
		{
			if (this.Op0Kind != OpKind.Memory)
			{
				return false;
			}
			if (this.HasLockPrefix)
			{
				return this.Code != Code.Cmpxchg16b_m128;
			}
			return this.Mnemonic == Mnemonic.Xchg;
		}

		// Token: 0x06002258 RID: 8792 RVA: 0x00070298 File Offset: 0x0006E498
		private readonly bool IsXreleaseInstr()
		{
			if (this.Op0Kind != OpKind.Memory)
			{
				return false;
			}
			if (this.HasLockPrefix)
			{
				return this.Code != Code.Cmpxchg16b_m128;
			}
			Code code = this.Code;
			return code - Code.Xchg_rm8_r8 <= 7 || code == Code.Mov_rm8_imm8 || code - Code.Mov_rm16_imm16 <= 2;
		}

		// Token: 0x17000793 RID: 1939
		// (get) Token: 0x06002259 RID: 8793 RVA: 0x000702F0 File Offset: 0x0006E4F0
		// (set) Token: 0x0600225A RID: 8794 RVA: 0x00070308 File Offset: 0x0006E508
		public bool HasXacquirePrefix
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 1073741824U) != 0U && this.IsXacquireInstr();
			}
			set
			{
				if (value)
				{
					this.flags1 |= 1073741824U;
					return;
				}
				this.flags1 &= 3221225471U;
			}
		}

		// Token: 0x0600225B RID: 8795 RVA: 0x00070332 File Offset: 0x0006E532
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetHasXacquirePrefix()
		{
			this.flags1 |= 1073741824U;
		}

		// Token: 0x17000794 RID: 1940
		// (get) Token: 0x0600225C RID: 8796 RVA: 0x00070346 File Offset: 0x0006E546
		// (set) Token: 0x0600225D RID: 8797 RVA: 0x0007035E File Offset: 0x0006E55E
		public bool HasXreleasePrefix
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 536870912U) != 0U && this.IsXreleaseInstr();
			}
			set
			{
				if (value)
				{
					this.flags1 |= 536870912U;
					return;
				}
				this.flags1 &= 3758096383U;
			}
		}

		// Token: 0x0600225E RID: 8798 RVA: 0x00070388 File Offset: 0x0006E588
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetHasXreleasePrefix()
		{
			this.flags1 |= 536870912U;
		}

		// Token: 0x17000795 RID: 1941
		// (get) Token: 0x0600225F RID: 8799 RVA: 0x0007039C File Offset: 0x0006E59C
		// (set) Token: 0x06002260 RID: 8800 RVA: 0x0007035E File Offset: 0x0006E55E
		public bool HasRepPrefix
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 536870912U) > 0U;
			}
			set
			{
				if (value)
				{
					this.flags1 |= 536870912U;
					return;
				}
				this.flags1 &= 3758096383U;
			}
		}

		// Token: 0x17000796 RID: 1942
		// (get) Token: 0x06002261 RID: 8801 RVA: 0x0007039C File Offset: 0x0006E59C
		// (set) Token: 0x06002262 RID: 8802 RVA: 0x0007035E File Offset: 0x0006E55E
		public bool HasRepePrefix
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 536870912U) > 0U;
			}
			set
			{
				if (value)
				{
					this.flags1 |= 536870912U;
					return;
				}
				this.flags1 &= 3758096383U;
			}
		}

		// Token: 0x06002263 RID: 8803 RVA: 0x000703AD File Offset: 0x0006E5AD
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetHasRepePrefix()
		{
			this.flags1 = (this.flags1 & 3221225471U) | 536870912U;
		}

		// Token: 0x06002264 RID: 8804 RVA: 0x000703C7 File Offset: 0x0006E5C7
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalClearHasRepePrefix()
		{
			this.flags1 &= 3758096383U;
		}

		// Token: 0x06002265 RID: 8805 RVA: 0x000703DB File Offset: 0x0006E5DB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalClearHasRepeRepnePrefix()
		{
			this.flags1 &= 2684354559U;
		}

		// Token: 0x17000797 RID: 1943
		// (get) Token: 0x06002266 RID: 8806 RVA: 0x000703EF File Offset: 0x0006E5EF
		// (set) Token: 0x06002267 RID: 8807 RVA: 0x00070308 File Offset: 0x0006E508
		public bool HasRepnePrefix
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 1073741824U) > 0U;
			}
			set
			{
				if (value)
				{
					this.flags1 |= 1073741824U;
					return;
				}
				this.flags1 &= 3221225471U;
			}
		}

		// Token: 0x06002268 RID: 8808 RVA: 0x00070400 File Offset: 0x0006E600
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetHasRepnePrefix()
		{
			this.flags1 = (this.flags1 & 3758096383U) | 1073741824U;
		}

		// Token: 0x06002269 RID: 8809 RVA: 0x0007041A File Offset: 0x0006E61A
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalClearHasRepnePrefix()
		{
			this.flags1 &= 3221225471U;
		}

		// Token: 0x17000798 RID: 1944
		// (get) Token: 0x0600226A RID: 8810 RVA: 0x0007042E File Offset: 0x0006E62E
		// (set) Token: 0x0600226B RID: 8811 RVA: 0x0007043F File Offset: 0x0006E63F
		public bool HasLockPrefix
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 2147483648U) > 0U;
			}
			set
			{
				if (value)
				{
					this.flags1 |= 2147483648U;
					return;
				}
				this.flags1 &= 2147483647U;
			}
		}

		// Token: 0x0600226C RID: 8812 RVA: 0x00070469 File Offset: 0x0006E669
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetHasLockPrefix()
		{
			this.flags1 |= 2147483648U;
		}

		// Token: 0x0600226D RID: 8813 RVA: 0x0007047D File Offset: 0x0006E67D
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalClearHasLockPrefix()
		{
			this.flags1 &= 2147483647U;
		}

		// Token: 0x17000799 RID: 1945
		// (get) Token: 0x0600226E RID: 8814 RVA: 0x00070491 File Offset: 0x0006E691
		// (set) Token: 0x0600226F RID: 8815 RVA: 0x00070499 File Offset: 0x0006E699
		public OpKind Op0Kind
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (OpKind)this.opKind0;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.opKind0 = (byte)value;
			}
		}

		// Token: 0x1700079A RID: 1946
		// (get) Token: 0x06002270 RID: 8816 RVA: 0x000704A3 File Offset: 0x0006E6A3
		internal readonly bool Internal_Op0IsNotReg_or_Op1IsNotReg
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (this.opKind0 | this.opKind1) > 0;
			}
		}

		// Token: 0x1700079B RID: 1947
		// (get) Token: 0x06002271 RID: 8817 RVA: 0x000704B5 File Offset: 0x0006E6B5
		// (set) Token: 0x06002272 RID: 8818 RVA: 0x000704BD File Offset: 0x0006E6BD
		public OpKind Op1Kind
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (OpKind)this.opKind1;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.opKind1 = (byte)value;
			}
		}

		// Token: 0x1700079C RID: 1948
		// (get) Token: 0x06002273 RID: 8819 RVA: 0x000704C7 File Offset: 0x0006E6C7
		// (set) Token: 0x06002274 RID: 8820 RVA: 0x000704CF File Offset: 0x0006E6CF
		public OpKind Op2Kind
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (OpKind)this.opKind2;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.opKind2 = (byte)value;
			}
		}

		// Token: 0x1700079D RID: 1949
		// (get) Token: 0x06002275 RID: 8821 RVA: 0x000704D9 File Offset: 0x0006E6D9
		// (set) Token: 0x06002276 RID: 8822 RVA: 0x000704E1 File Offset: 0x0006E6E1
		public OpKind Op3Kind
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (OpKind)this.opKind3;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.opKind3 = (byte)value;
			}
		}

		// Token: 0x1700079E RID: 1950
		// (get) Token: 0x06002277 RID: 8823 RVA: 0x000413EB File Offset: 0x0003F5EB
		// (set) Token: 0x06002278 RID: 8824 RVA: 0x000704EB File Offset: 0x0006E6EB
		public OpKind Op4Kind
		{
			readonly get
			{
				return OpKind.Immediate8;
			}
			set
			{
				if (value != OpKind.Immediate8)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_value();
				}
			}
		}

		// Token: 0x06002279 RID: 8825 RVA: 0x000704F8 File Offset: 0x0006E6F8
		public readonly OpKind GetOpKind(int operand)
		{
			switch (operand)
			{
			case 0:
				return this.Op0Kind;
			case 1:
				return this.Op1Kind;
			case 2:
				return this.Op2Kind;
			case 3:
				return this.Op3Kind;
			case 4:
				return this.Op4Kind;
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_operand();
				return OpKind.Register;
			}
		}

		// Token: 0x0600227A RID: 8826 RVA: 0x0007054C File Offset: 0x0006E74C
		public readonly bool HasOpKind(OpKind opKind)
		{
			for (int i = 0; i < this.OpCount; i++)
			{
				if (this.GetOpKind(i) == opKind)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600227B RID: 8827 RVA: 0x00070578 File Offset: 0x0006E778
		public void SetOpKind(int operand, OpKind opKind)
		{
			switch (operand)
			{
			case 0:
				this.Op0Kind = opKind;
				return;
			case 1:
				this.Op1Kind = opKind;
				return;
			case 2:
				this.Op2Kind = opKind;
				return;
			case 3:
				this.Op3Kind = opKind;
				return;
			case 4:
				this.Op4Kind = opKind;
				return;
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_operand();
				return;
			}
		}

		// Token: 0x1700079F RID: 1951
		// (get) Token: 0x0600227C RID: 8828 RVA: 0x000705CE File Offset: 0x0006E7CE
		public readonly bool HasSegmentPrefix
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ((this.flags1 >> 5) & 7U) - 1U < 6U;
			}
		}

		// Token: 0x170007A0 RID: 1952
		// (get) Token: 0x0600227D RID: 8829 RVA: 0x000705E0 File Offset: 0x0006E7E0
		// (set) Token: 0x0600227E RID: 8830 RVA: 0x00070604 File Offset: 0x0006E804
		public Register SegmentPrefix
		{
			readonly get
			{
				uint index = ((this.flags1 >> 5) & 7U) - 1U;
				if (index >= 6U)
				{
					return Register.None;
				}
				return Register.ES + (int)index;
			}
			set
			{
				uint encValue;
				if (value == Register.None)
				{
					encValue = 0U;
				}
				else
				{
					encValue = (uint)((value - Register.ES + 1) & 7);
				}
				this.flags1 = (this.flags1 & 4294967071U) | (encValue << 5);
			}
		}

		// Token: 0x170007A1 RID: 1953
		// (get) Token: 0x0600227F RID: 8831 RVA: 0x00070638 File Offset: 0x0006E838
		public readonly Register MemorySegment
		{
			get
			{
				Register segReg = this.SegmentPrefix;
				if (segReg != Register.None)
				{
					return segReg;
				}
				Register baseReg = this.MemoryBase;
				if (baseReg == Register.BP || baseReg == Register.EBP || baseReg == Register.ESP || baseReg == Register.RBP || baseReg == Register.RSP)
				{
					return Register.SS;
				}
				return Register.DS;
			}
		}

		// Token: 0x170007A2 RID: 1954
		// (get) Token: 0x06002280 RID: 8832 RVA: 0x00070678 File Offset: 0x0006E878
		// (set) Token: 0x06002281 RID: 8833 RVA: 0x000706B8 File Offset: 0x0006E8B8
		public int MemoryDisplSize
		{
			readonly get
			{
				int result;
				switch (this.displSize)
				{
				case 0:
					result = 0;
					break;
				case 1:
					result = 1;
					break;
				case 2:
					result = 2;
					break;
				case 3:
					result = 4;
					break;
				default:
					result = 8;
					break;
				}
				return result;
			}
			set
			{
				byte b;
				switch (value)
				{
				case 0:
					b = 0;
					goto IL_2E;
				case 1:
					b = 1;
					goto IL_2E;
				case 2:
					b = 2;
					goto IL_2E;
				case 4:
					b = 3;
					goto IL_2E;
				}
				b = 4;
				IL_2E:
				this.displSize = b;
			}
		}

		// Token: 0x06002282 RID: 8834 RVA: 0x000706FA File Offset: 0x0006E8FA
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetMemoryDisplSize(uint scale)
		{
			this.displSize = (byte)scale;
		}

		// Token: 0x170007A3 RID: 1955
		// (get) Token: 0x06002283 RID: 8835 RVA: 0x00070704 File Offset: 0x0006E904
		// (set) Token: 0x06002284 RID: 8836 RVA: 0x00070715 File Offset: 0x0006E915
		public bool IsBroadcast
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 67108864U) > 0U;
			}
			set
			{
				if (value)
				{
					this.flags1 |= 67108864U;
					return;
				}
				this.flags1 &= 4227858431U;
			}
		}

		// Token: 0x06002285 RID: 8837 RVA: 0x0007073F File Offset: 0x0006E93F
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetIsBroadcast()
		{
			this.flags1 |= 67108864U;
		}

		// Token: 0x170007A4 RID: 1956
		// (get) Token: 0x06002286 RID: 8838 RVA: 0x00070754 File Offset: 0x0006E954
		public unsafe readonly MemorySize MemorySize
		{
			get
			{
				int index = (int)this.Code;
				if (this.IsBroadcast)
				{
					return (MemorySize)(*InstructionMemorySizes.SizesBcst[index]);
				}
				return (MemorySize)(*InstructionMemorySizes.SizesNormal[index]);
			}
		}

		// Token: 0x170007A5 RID: 1957
		// (get) Token: 0x06002287 RID: 8839 RVA: 0x0007078F File Offset: 0x0006E98F
		// (set) Token: 0x06002288 RID: 8840 RVA: 0x0007079C File Offset: 0x0006E99C
		public int MemoryIndexScale
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return 1 << (int)this.scale;
			}
			set
			{
				if (value == 1)
				{
					this.scale = 0;
					return;
				}
				if (value == 2)
				{
					this.scale = 1;
					return;
				}
				if (value == 4)
				{
					this.scale = 2;
					return;
				}
				this.scale = 3;
			}
		}

		// Token: 0x170007A6 RID: 1958
		// (get) Token: 0x06002289 RID: 8841 RVA: 0x000707C9 File Offset: 0x0006E9C9
		// (set) Token: 0x0600228A RID: 8842 RVA: 0x000707D1 File Offset: 0x0006E9D1
		internal int InternalMemoryIndexScale
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (int)this.scale;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.scale = (byte)value;
			}
		}

		// Token: 0x170007A7 RID: 1959
		// (get) Token: 0x0600228B RID: 8843 RVA: 0x000707DB File Offset: 0x0006E9DB
		// (set) Token: 0x0600228C RID: 8844 RVA: 0x000707E4 File Offset: 0x0006E9E4
		public uint MemoryDisplacement32
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (uint)this.memDispl;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.memDispl = (ulong)value;
			}
		}

		// Token: 0x170007A8 RID: 1960
		// (get) Token: 0x0600228D RID: 8845 RVA: 0x000707EE File Offset: 0x0006E9EE
		// (set) Token: 0x0600228E RID: 8846 RVA: 0x000707F6 File Offset: 0x0006E9F6
		public ulong MemoryDisplacement64
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return this.memDispl;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.memDispl = value;
			}
		}

		// Token: 0x0600228F RID: 8847 RVA: 0x00070800 File Offset: 0x0006EA00
		public readonly ulong GetImmediate(int operand)
		{
			ulong result;
			switch (this.GetOpKind(operand))
			{
			case OpKind.Immediate8:
				result = (ulong)this.Immediate8;
				break;
			case OpKind.Immediate8_2nd:
				result = (ulong)this.Immediate8_2nd;
				break;
			case OpKind.Immediate16:
				result = (ulong)this.Immediate16;
				break;
			case OpKind.Immediate32:
				result = (ulong)this.Immediate32;
				break;
			case OpKind.Immediate64:
				result = this.Immediate64;
				break;
			case OpKind.Immediate8to16:
				result = (ulong)((long)this.Immediate8to16);
				break;
			case OpKind.Immediate8to32:
				result = (ulong)((long)this.Immediate8to32);
				break;
			case OpKind.Immediate8to64:
				result = (ulong)this.Immediate8to64;
				break;
			case OpKind.Immediate32to64:
				result = (ulong)this.Immediate32to64;
				break;
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Op");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(" isn't an immediate operand");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "operand");
			}
			}
			return result;
		}

		// Token: 0x06002290 RID: 8848 RVA: 0x000708DA File Offset: 0x0006EADA
		public void SetImmediate(int operand, int immediate)
		{
			this.SetImmediate(operand, (ulong)((long)immediate));
		}

		// Token: 0x06002291 RID: 8849 RVA: 0x000708E5 File Offset: 0x0006EAE5
		public void SetImmediate(int operand, uint immediate)
		{
			this.SetImmediate(operand, (ulong)immediate);
		}

		// Token: 0x06002292 RID: 8850 RVA: 0x000708F0 File Offset: 0x0006EAF0
		public void SetImmediate(int operand, long immediate)
		{
			this.SetImmediate(operand, (ulong)immediate);
		}

		// Token: 0x06002293 RID: 8851 RVA: 0x000708FC File Offset: 0x0006EAFC
		public void SetImmediate(int operand, ulong immediate)
		{
			switch (this.GetOpKind(operand))
			{
			case OpKind.Immediate8:
				this.Immediate8 = (byte)immediate;
				return;
			case OpKind.Immediate8_2nd:
				this.Immediate8_2nd = (byte)immediate;
				return;
			case OpKind.Immediate16:
				this.Immediate16 = (ushort)immediate;
				return;
			case OpKind.Immediate32:
				this.Immediate32 = (uint)immediate;
				return;
			case OpKind.Immediate64:
				this.Immediate64 = immediate;
				return;
			case OpKind.Immediate8to16:
				this.Immediate8to16 = (short)immediate;
				return;
			case OpKind.Immediate8to32:
				this.Immediate8to32 = (int)immediate;
				return;
			case OpKind.Immediate8to64:
				this.Immediate8to64 = (long)immediate;
				return;
			case OpKind.Immediate32to64:
				this.Immediate32to64 = (long)immediate;
				return;
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Op");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(" isn't an immediate operand");
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "operand");
			}
			}
		}

		// Token: 0x170007A9 RID: 1961
		// (get) Token: 0x06002294 RID: 8852 RVA: 0x000709C8 File Offset: 0x0006EBC8
		// (set) Token: 0x06002295 RID: 8853 RVA: 0x000709D1 File Offset: 0x0006EBD1
		public byte Immediate8
		{
			readonly get
			{
				return (byte)this.immediate;
			}
			set
			{
				this.immediate = (uint)value;
			}
		}

		// Token: 0x170007AA RID: 1962
		// (set) Token: 0x06002296 RID: 8854 RVA: 0x000709D1 File Offset: 0x0006EBD1
		internal uint InternalImmediate8
		{
			set
			{
				this.immediate = value;
			}
		}

		// Token: 0x170007AB RID: 1963
		// (get) Token: 0x06002297 RID: 8855 RVA: 0x000709DA File Offset: 0x0006EBDA
		// (set) Token: 0x06002298 RID: 8856 RVA: 0x000707E4 File Offset: 0x0006E9E4
		public byte Immediate8_2nd
		{
			readonly get
			{
				return (byte)this.memDispl;
			}
			set
			{
				this.memDispl = (ulong)value;
			}
		}

		// Token: 0x170007AC RID: 1964
		// (set) Token: 0x06002299 RID: 8857 RVA: 0x000707E4 File Offset: 0x0006E9E4
		internal uint InternalImmediate8_2nd
		{
			set
			{
				this.memDispl = (ulong)value;
			}
		}

		// Token: 0x170007AD RID: 1965
		// (get) Token: 0x0600229A RID: 8858 RVA: 0x000709E3 File Offset: 0x0006EBE3
		// (set) Token: 0x0600229B RID: 8859 RVA: 0x000709D1 File Offset: 0x0006EBD1
		public ushort Immediate16
		{
			readonly get
			{
				return (ushort)this.immediate;
			}
			set
			{
				this.immediate = (uint)value;
			}
		}

		// Token: 0x170007AE RID: 1966
		// (set) Token: 0x0600229C RID: 8860 RVA: 0x000709D1 File Offset: 0x0006EBD1
		internal uint InternalImmediate16
		{
			set
			{
				this.immediate = value;
			}
		}

		// Token: 0x170007AF RID: 1967
		// (get) Token: 0x0600229D RID: 8861 RVA: 0x000709EC File Offset: 0x0006EBEC
		// (set) Token: 0x0600229E RID: 8862 RVA: 0x000709D1 File Offset: 0x0006EBD1
		public uint Immediate32
		{
			readonly get
			{
				return this.immediate;
			}
			set
			{
				this.immediate = value;
			}
		}

		// Token: 0x170007B0 RID: 1968
		// (get) Token: 0x0600229F RID: 8863 RVA: 0x000709F4 File Offset: 0x0006EBF4
		// (set) Token: 0x060022A0 RID: 8864 RVA: 0x00070A07 File Offset: 0x0006EC07
		public ulong Immediate64
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.memDispl << 32) | (ulong)this.immediate;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.immediate = (uint)value;
				this.memDispl = (ulong)((uint)(value >> 32));
			}
		}

		// Token: 0x170007B1 RID: 1969
		// (set) Token: 0x060022A1 RID: 8865 RVA: 0x000709D1 File Offset: 0x0006EBD1
		internal uint InternalImmediate64_lo
		{
			set
			{
				this.immediate = value;
			}
		}

		// Token: 0x170007B2 RID: 1970
		// (set) Token: 0x060022A2 RID: 8866 RVA: 0x000707E4 File Offset: 0x0006E9E4
		internal uint InternalImmediate64_hi
		{
			set
			{
				this.memDispl = (ulong)value;
			}
		}

		// Token: 0x170007B3 RID: 1971
		// (get) Token: 0x060022A3 RID: 8867 RVA: 0x00070A1D File Offset: 0x0006EC1D
		// (set) Token: 0x060022A4 RID: 8868 RVA: 0x00070A26 File Offset: 0x0006EC26
		public short Immediate8to16
		{
			readonly get
			{
				return (short)((sbyte)this.immediate);
			}
			set
			{
				this.immediate = (uint)((sbyte)value);
			}
		}

		// Token: 0x170007B4 RID: 1972
		// (get) Token: 0x060022A5 RID: 8869 RVA: 0x00070A1D File Offset: 0x0006EC1D
		// (set) Token: 0x060022A6 RID: 8870 RVA: 0x00070A26 File Offset: 0x0006EC26
		public int Immediate8to32
		{
			readonly get
			{
				return (int)((sbyte)this.immediate);
			}
			set
			{
				this.immediate = (uint)((sbyte)value);
			}
		}

		// Token: 0x170007B5 RID: 1973
		// (get) Token: 0x060022A7 RID: 8871 RVA: 0x00070A30 File Offset: 0x0006EC30
		// (set) Token: 0x060022A8 RID: 8872 RVA: 0x00070A26 File Offset: 0x0006EC26
		public long Immediate8to64
		{
			readonly get
			{
				return (long)((sbyte)this.immediate);
			}
			set
			{
				this.immediate = (uint)((sbyte)value);
			}
		}

		// Token: 0x170007B6 RID: 1974
		// (get) Token: 0x060022A9 RID: 8873 RVA: 0x00070A3A File Offset: 0x0006EC3A
		// (set) Token: 0x060022AA RID: 8874 RVA: 0x00070A43 File Offset: 0x0006EC43
		public long Immediate32to64
		{
			readonly get
			{
				return (long)this.immediate;
			}
			set
			{
				this.immediate = (uint)value;
			}
		}

		// Token: 0x170007B7 RID: 1975
		// (get) Token: 0x060022AB RID: 8875 RVA: 0x00070A4D File Offset: 0x0006EC4D
		// (set) Token: 0x060022AC RID: 8876 RVA: 0x000707E4 File Offset: 0x0006E9E4
		public ushort NearBranch16
		{
			readonly get
			{
				return (ushort)this.memDispl;
			}
			set
			{
				this.memDispl = (ulong)value;
			}
		}

		// Token: 0x170007B8 RID: 1976
		// (set) Token: 0x060022AD RID: 8877 RVA: 0x000707E4 File Offset: 0x0006E9E4
		internal uint InternalNearBranch16
		{
			set
			{
				this.memDispl = (ulong)value;
			}
		}

		// Token: 0x170007B9 RID: 1977
		// (get) Token: 0x060022AE RID: 8878 RVA: 0x000707DB File Offset: 0x0006E9DB
		// (set) Token: 0x060022AF RID: 8879 RVA: 0x000707E4 File Offset: 0x0006E9E4
		public uint NearBranch32
		{
			readonly get
			{
				return (uint)this.memDispl;
			}
			set
			{
				this.memDispl = (ulong)value;
			}
		}

		// Token: 0x170007BA RID: 1978
		// (get) Token: 0x060022B0 RID: 8880 RVA: 0x000707EE File Offset: 0x0006E9EE
		// (set) Token: 0x060022B1 RID: 8881 RVA: 0x000707F6 File Offset: 0x0006E9F6
		public ulong NearBranch64
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return this.memDispl;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.memDispl = value;
			}
		}

		// Token: 0x170007BB RID: 1979
		// (get) Token: 0x060022B2 RID: 8882 RVA: 0x00070A58 File Offset: 0x0006EC58
		public readonly ulong NearBranchTarget
		{
			get
			{
				ulong result;
				switch (this.Op0Kind)
				{
				case OpKind.NearBranch16:
					result = (ulong)this.NearBranch16;
					break;
				case OpKind.NearBranch32:
					result = (ulong)this.NearBranch32;
					break;
				case OpKind.NearBranch64:
					result = this.NearBranch64;
					break;
				default:
					result = 0UL;
					break;
				}
				return result;
			}
		}

		// Token: 0x170007BC RID: 1980
		// (get) Token: 0x060022B3 RID: 8883 RVA: 0x000709E3 File Offset: 0x0006EBE3
		// (set) Token: 0x060022B4 RID: 8884 RVA: 0x000709D1 File Offset: 0x0006EBD1
		public ushort FarBranch16
		{
			readonly get
			{
				return (ushort)this.immediate;
			}
			set
			{
				this.immediate = (uint)value;
			}
		}

		// Token: 0x170007BD RID: 1981
		// (set) Token: 0x060022B5 RID: 8885 RVA: 0x000709D1 File Offset: 0x0006EBD1
		internal uint InternalFarBranch16
		{
			set
			{
				this.immediate = value;
			}
		}

		// Token: 0x170007BE RID: 1982
		// (get) Token: 0x060022B6 RID: 8886 RVA: 0x000709EC File Offset: 0x0006EBEC
		// (set) Token: 0x060022B7 RID: 8887 RVA: 0x000709D1 File Offset: 0x0006EBD1
		public uint FarBranch32
		{
			readonly get
			{
				return this.immediate;
			}
			set
			{
				this.immediate = value;
			}
		}

		// Token: 0x170007BF RID: 1983
		// (get) Token: 0x060022B8 RID: 8888 RVA: 0x00070A4D File Offset: 0x0006EC4D
		// (set) Token: 0x060022B9 RID: 8889 RVA: 0x000707E4 File Offset: 0x0006E9E4
		public ushort FarBranchSelector
		{
			readonly get
			{
				return (ushort)this.memDispl;
			}
			set
			{
				this.memDispl = (ulong)value;
			}
		}

		// Token: 0x170007C0 RID: 1984
		// (set) Token: 0x060022BA RID: 8890 RVA: 0x000707E4 File Offset: 0x0006E9E4
		internal uint InternalFarBranchSelector
		{
			set
			{
				this.memDispl = (ulong)value;
			}
		}

		// Token: 0x170007C1 RID: 1985
		// (get) Token: 0x060022BB RID: 8891 RVA: 0x00070AA3 File Offset: 0x0006ECA3
		// (set) Token: 0x060022BC RID: 8892 RVA: 0x00070AAB File Offset: 0x0006ECAB
		public Register MemoryBase
		{
			readonly get
			{
				return (Register)this.memBaseReg;
			}
			set
			{
				this.memBaseReg = (byte)value;
			}
		}

		// Token: 0x170007C2 RID: 1986
		// (set) Token: 0x060022BD RID: 8893 RVA: 0x00070AAB File Offset: 0x0006ECAB
		internal Register InternalMemoryBase
		{
			set
			{
				this.memBaseReg = (byte)value;
			}
		}

		// Token: 0x170007C3 RID: 1987
		// (get) Token: 0x060022BE RID: 8894 RVA: 0x00070AB5 File Offset: 0x0006ECB5
		// (set) Token: 0x060022BF RID: 8895 RVA: 0x00070ABD File Offset: 0x0006ECBD
		public Register MemoryIndex
		{
			readonly get
			{
				return (Register)this.memIndexReg;
			}
			set
			{
				this.memIndexReg = (byte)value;
			}
		}

		// Token: 0x170007C4 RID: 1988
		// (set) Token: 0x060022C0 RID: 8896 RVA: 0x00070ABD File Offset: 0x0006ECBD
		internal Register InternalMemoryIndex
		{
			set
			{
				this.memIndexReg = (byte)value;
			}
		}

		// Token: 0x170007C5 RID: 1989
		// (get) Token: 0x060022C1 RID: 8897 RVA: 0x00070AC7 File Offset: 0x0006ECC7
		// (set) Token: 0x060022C2 RID: 8898 RVA: 0x00070ACF File Offset: 0x0006ECCF
		public Register Op0Register
		{
			readonly get
			{
				return (Register)this.reg0;
			}
			set
			{
				this.reg0 = (byte)value;
			}
		}

		// Token: 0x170007C6 RID: 1990
		// (set) Token: 0x060022C3 RID: 8899 RVA: 0x00070ACF File Offset: 0x0006ECCF
		internal Register InternalOp0Register
		{
			set
			{
				this.reg0 = (byte)value;
			}
		}

		// Token: 0x170007C7 RID: 1991
		// (get) Token: 0x060022C4 RID: 8900 RVA: 0x00070AD9 File Offset: 0x0006ECD9
		// (set) Token: 0x060022C5 RID: 8901 RVA: 0x00070AE1 File Offset: 0x0006ECE1
		public Register Op1Register
		{
			readonly get
			{
				return (Register)this.reg1;
			}
			set
			{
				this.reg1 = (byte)value;
			}
		}

		// Token: 0x170007C8 RID: 1992
		// (set) Token: 0x060022C6 RID: 8902 RVA: 0x00070AE1 File Offset: 0x0006ECE1
		internal Register InternalOp1Register
		{
			set
			{
				this.reg1 = (byte)value;
			}
		}

		// Token: 0x170007C9 RID: 1993
		// (get) Token: 0x060022C7 RID: 8903 RVA: 0x00070AEB File Offset: 0x0006ECEB
		// (set) Token: 0x060022C8 RID: 8904 RVA: 0x00070AF3 File Offset: 0x0006ECF3
		public Register Op2Register
		{
			readonly get
			{
				return (Register)this.reg2;
			}
			set
			{
				this.reg2 = (byte)value;
			}
		}

		// Token: 0x170007CA RID: 1994
		// (set) Token: 0x060022C9 RID: 8905 RVA: 0x00070AF3 File Offset: 0x0006ECF3
		internal Register InternalOp2Register
		{
			set
			{
				this.reg2 = (byte)value;
			}
		}

		// Token: 0x170007CB RID: 1995
		// (get) Token: 0x060022CA RID: 8906 RVA: 0x00070AFD File Offset: 0x0006ECFD
		// (set) Token: 0x060022CB RID: 8907 RVA: 0x00070B05 File Offset: 0x0006ED05
		public Register Op3Register
		{
			readonly get
			{
				return (Register)this.reg3;
			}
			set
			{
				this.reg3 = (byte)value;
			}
		}

		// Token: 0x170007CC RID: 1996
		// (set) Token: 0x060022CC RID: 8908 RVA: 0x00070B05 File Offset: 0x0006ED05
		internal Register InternalOp3Register
		{
			set
			{
				this.reg3 = (byte)value;
			}
		}

		// Token: 0x170007CD RID: 1997
		// (get) Token: 0x060022CD RID: 8909 RVA: 0x0001B69F File Offset: 0x0001989F
		// (set) Token: 0x060022CE RID: 8910 RVA: 0x00070B0F File Offset: 0x0006ED0F
		public Register Op4Register
		{
			readonly get
			{
				return Register.None;
			}
			set
			{
				if (value != Register.None)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException_value();
				}
			}
		}

		// Token: 0x060022CF RID: 8911 RVA: 0x00070B1C File Offset: 0x0006ED1C
		public readonly Register GetOpRegister(int operand)
		{
			switch (operand)
			{
			case 0:
				return this.Op0Register;
			case 1:
				return this.Op1Register;
			case 2:
				return this.Op2Register;
			case 3:
				return this.Op3Register;
			case 4:
				return this.Op4Register;
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_operand();
				return Register.None;
			}
		}

		// Token: 0x060022D0 RID: 8912 RVA: 0x00070B70 File Offset: 0x0006ED70
		public void SetOpRegister(int operand, Register register)
		{
			switch (operand)
			{
			case 0:
				this.Op0Register = register;
				return;
			case 1:
				this.Op1Register = register;
				return;
			case 2:
				this.Op2Register = register;
				return;
			case 3:
				this.Op3Register = register;
				return;
			case 4:
				this.Op4Register = register;
				return;
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_operand();
				return;
			}
		}

		// Token: 0x170007CE RID: 1998
		// (get) Token: 0x060022D1 RID: 8913 RVA: 0x00070BC8 File Offset: 0x0006EDC8
		// (set) Token: 0x060022D2 RID: 8914 RVA: 0x00070BF0 File Offset: 0x0006EDF0
		public Register OpMask
		{
			readonly get
			{
				int r = (int)((this.flags1 >> 15) & 7U);
				if (r != 0)
				{
					return r + Register.K0;
				}
				return Register.None;
			}
			set
			{
				uint r;
				if (value == Register.None)
				{
					r = 0U;
				}
				else
				{
					r = (uint)((value - Register.K0) & 7);
				}
				this.flags1 = (this.flags1 & 4294737919U) | (r << 15);
			}
		}

		// Token: 0x170007CF RID: 1999
		// (get) Token: 0x060022D3 RID: 8915 RVA: 0x00070C25 File Offset: 0x0006EE25
		// (set) Token: 0x060022D4 RID: 8916 RVA: 0x00070C32 File Offset: 0x0006EE32
		internal uint InternalOpMask
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 >> 15) & 7U;
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.flags1 |= value << 15;
			}
		}

		// Token: 0x170007D0 RID: 2000
		// (get) Token: 0x060022D5 RID: 8917 RVA: 0x00070C45 File Offset: 0x0006EE45
		public readonly bool HasOpMask
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (this.flags1 & 229376U) > 0U;
			}
		}

		// Token: 0x170007D1 RID: 2001
		// (get) Token: 0x060022D6 RID: 8918 RVA: 0x00070C56 File Offset: 0x0006EE56
		internal readonly bool HasOpMask_or_ZeroingMasking
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (this.flags1 & 268664832U) > 0U;
			}
		}

		// Token: 0x170007D2 RID: 2002
		// (get) Token: 0x060022D7 RID: 8919 RVA: 0x00070C67 File Offset: 0x0006EE67
		// (set) Token: 0x060022D8 RID: 8920 RVA: 0x00070C78 File Offset: 0x0006EE78
		public bool ZeroingMasking
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 268435456U) > 0U;
			}
			set
			{
				if (value)
				{
					this.flags1 |= 268435456U;
					return;
				}
				this.flags1 &= 4026531839U;
			}
		}

		// Token: 0x060022D9 RID: 8921 RVA: 0x00070CA2 File Offset: 0x0006EEA2
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetZeroingMasking()
		{
			this.flags1 |= 268435456U;
		}

		// Token: 0x170007D3 RID: 2003
		// (get) Token: 0x060022DA RID: 8922 RVA: 0x00070CB6 File Offset: 0x0006EEB6
		// (set) Token: 0x060022DB RID: 8923 RVA: 0x00070CC7 File Offset: 0x0006EEC7
		public bool MergingMasking
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 268435456U) == 0U;
			}
			set
			{
				if (value)
				{
					this.flags1 &= 4026531839U;
					return;
				}
				this.flags1 |= 268435456U;
			}
		}

		// Token: 0x170007D4 RID: 2004
		// (get) Token: 0x060022DC RID: 8924 RVA: 0x00070CF1 File Offset: 0x0006EEF1
		// (set) Token: 0x060022DD RID: 8925 RVA: 0x00070CFE File Offset: 0x0006EEFE
		public RoundingControl RoundingControl
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (RoundingControl)((this.flags1 >> 12) & 7U);
			}
			set
			{
				this.flags1 = (this.flags1 & 4294938623U) | (uint)((uint)value << 12);
			}
		}

		// Token: 0x170007D5 RID: 2005
		// (set) Token: 0x060022DE RID: 8926 RVA: 0x00070D17 File Offset: 0x0006EF17
		internal uint InternalRoundingControl
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.flags1 |= value << 12;
			}
		}

		// Token: 0x170007D6 RID: 2006
		// (get) Token: 0x060022DF RID: 8927 RVA: 0x00070D2A File Offset: 0x0006EF2A
		internal readonly bool HasRoundingControlOrSae
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return (this.flags1 & 134246400U) > 0U;
			}
		}

		// Token: 0x170007D7 RID: 2007
		// (get) Token: 0x060022E0 RID: 8928 RVA: 0x00070D3B File Offset: 0x0006EF3B
		// (set) Token: 0x060022E1 RID: 8929 RVA: 0x00070D4A File Offset: 0x0006EF4A
		public int DeclareDataCount
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (int)(((this.flags1 >> 8) & 15U) + 1U);
			}
			set
			{
				this.flags1 = (this.flags1 & 4294963455U) | (uint)((uint)((value - 1) & 15) << 8);
			}
		}

		// Token: 0x170007D8 RID: 2008
		// (set) Token: 0x060022E2 RID: 8930 RVA: 0x00070D67 File Offset: 0x0006EF67
		internal uint InternalDeclareDataCount
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				this.flags1 |= value - 1U << 8;
			}
		}

		// Token: 0x060022E3 RID: 8931 RVA: 0x00070D7B File Offset: 0x0006EF7B
		public void SetDeclareByteValue(int index, sbyte value)
		{
			this.SetDeclareByteValue(index, (byte)value);
		}

		// Token: 0x060022E4 RID: 8932 RVA: 0x00070D88 File Offset: 0x0006EF88
		public void SetDeclareByteValue(int index, byte value)
		{
			switch (index)
			{
			case 0:
				this.reg0 = value;
				return;
			case 1:
				this.reg1 = value;
				return;
			case 2:
				this.reg2 = value;
				return;
			case 3:
				this.reg3 = value;
				return;
			case 4:
				this.immediate = (this.immediate & 4294967040U) | (uint)value;
				return;
			case 5:
				this.immediate = (this.immediate & 4294902015U) | (uint)((uint)value << 8);
				return;
			case 6:
				this.immediate = (this.immediate & 4278255615U) | (uint)((uint)value << 16);
				return;
			case 7:
				this.immediate = (this.immediate & 16777215U) | (uint)((uint)value << 24);
				return;
			case 8:
				this.memDispl = (this.memDispl & 18446744073709551360UL) | (ulong)value;
				return;
			case 9:
				this.memDispl = (this.memDispl & 18446744073709486335UL) | ((ulong)value << 8);
				return;
			case 10:
				this.memDispl = (this.memDispl & 18446744073692839935UL) | ((ulong)value << 16);
				return;
			case 11:
				this.memDispl = (this.memDispl & 18446744069431361535UL) | ((ulong)value << 24);
				return;
			case 12:
				this.memDispl = (this.memDispl & 18446742978492891135UL) | ((ulong)value << 32);
				return;
			case 13:
				this.memDispl = (this.memDispl & 18446463698244468735UL) | ((ulong)value << 40);
				return;
			case 14:
				this.memDispl = (this.memDispl & 18374967954648334335UL) | ((ulong)value << 48);
				return;
			case 15:
				this.memDispl = (this.memDispl & 72057594037927935UL) | ((ulong)value << 56);
				return;
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
				return;
			}
		}

		// Token: 0x060022E5 RID: 8933 RVA: 0x00070F3C File Offset: 0x0006F13C
		public readonly byte GetDeclareByteValue(int index)
		{
			switch (index)
			{
			case 0:
				return this.reg0;
			case 1:
				return this.reg1;
			case 2:
				return this.reg2;
			case 3:
				return this.reg3;
			case 4:
				return (byte)this.immediate;
			case 5:
				return (byte)(this.immediate >> 8);
			case 6:
				return (byte)(this.immediate >> 16);
			case 7:
				return (byte)(this.immediate >> 24);
			case 8:
				return (byte)this.memDispl;
			case 9:
				return (byte)((uint)this.memDispl >> 8);
			case 10:
				return (byte)((uint)this.memDispl >> 16);
			case 11:
				return (byte)((uint)this.memDispl >> 24);
			case 12:
				return (byte)(this.memDispl >> 32);
			case 13:
				return (byte)(this.memDispl >> 40);
			case 14:
				return (byte)(this.memDispl >> 48);
			case 15:
				return (byte)(this.memDispl >> 56);
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
				return 0;
			}
		}

		// Token: 0x060022E6 RID: 8934 RVA: 0x00071035 File Offset: 0x0006F235
		public void SetDeclareWordValue(int index, short value)
		{
			this.SetDeclareWordValue(index, (ushort)value);
		}

		// Token: 0x060022E7 RID: 8935 RVA: 0x00071040 File Offset: 0x0006F240
		public void SetDeclareWordValue(int index, ushort value)
		{
			switch (index)
			{
			case 0:
				this.reg0 = (byte)value;
				this.reg1 = (byte)(value >> 8);
				return;
			case 1:
				this.reg2 = (byte)value;
				this.reg3 = (byte)(value >> 8);
				return;
			case 2:
				this.immediate = (this.immediate & 4294901760U) | (uint)value;
				return;
			case 3:
				this.immediate = (uint)((int)((ushort)this.immediate) | ((int)value << 16));
				return;
			case 4:
				this.memDispl = (this.memDispl & 18446744073709486080UL) | (ulong)value;
				return;
			case 5:
				this.memDispl = (this.memDispl & 18446744069414649855UL) | ((ulong)value << 16);
				return;
			case 6:
				this.memDispl = (this.memDispl & 18446462603027808255UL) | ((ulong)value << 32);
				return;
			case 7:
				this.memDispl = (this.memDispl & 281474976710655UL) | ((ulong)value << 48);
				return;
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
				return;
			}
		}

		// Token: 0x060022E8 RID: 8936 RVA: 0x0007113C File Offset: 0x0006F33C
		public readonly ushort GetDeclareWordValue(int index)
		{
			switch (index)
			{
			case 0:
				return (ushort)((int)this.reg0 | ((int)this.reg1 << 8));
			case 1:
				return (ushort)((int)this.reg2 | ((int)this.reg3 << 8));
			case 2:
				return (ushort)this.immediate;
			case 3:
				return (ushort)(this.immediate >> 16);
			case 4:
				return (ushort)this.memDispl;
			case 5:
				return (ushort)((uint)this.memDispl >> 16);
			case 6:
				return (ushort)(this.memDispl >> 32);
			case 7:
				return (ushort)(this.memDispl >> 48);
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
				return 0;
			}
		}

		// Token: 0x060022E9 RID: 8937 RVA: 0x000711D6 File Offset: 0x0006F3D6
		public void SetDeclareDwordValue(int index, int value)
		{
			this.SetDeclareDwordValue(index, (uint)value);
		}

		// Token: 0x060022EA RID: 8938 RVA: 0x000711E0 File Offset: 0x0006F3E0
		public void SetDeclareDwordValue(int index, uint value)
		{
			switch (index)
			{
			case 0:
				this.reg0 = (byte)value;
				this.reg1 = (byte)(value >> 8);
				this.reg2 = (byte)(value >> 16);
				this.reg3 = (byte)(value >> 24);
				return;
			case 1:
				this.immediate = value;
				return;
			case 2:
				this.memDispl = (this.memDispl & 18446744069414584320UL) | (ulong)value;
				return;
			case 3:
				this.memDispl = (this.memDispl & (ulong)(-1)) | ((ulong)value << 32);
				return;
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
				return;
			}
		}

		// Token: 0x060022EB RID: 8939 RVA: 0x0007126C File Offset: 0x0006F46C
		public readonly uint GetDeclareDwordValue(int index)
		{
			switch (index)
			{
			case 0:
				return (uint)((int)this.reg0 | ((int)this.reg1 << 8) | ((int)this.reg2 << 16) | ((int)this.reg3 << 24));
			case 1:
				return this.immediate;
			case 2:
				return (uint)this.memDispl;
			case 3:
				return (uint)(this.memDispl >> 32);
			default:
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
				return 0U;
			}
		}

		// Token: 0x060022EC RID: 8940 RVA: 0x000712D5 File Offset: 0x0006F4D5
		public void SetDeclareQwordValue(int index, long value)
		{
			this.SetDeclareQwordValue(index, (ulong)value);
		}

		// Token: 0x060022ED RID: 8941 RVA: 0x000712E0 File Offset: 0x0006F4E0
		public void SetDeclareQwordValue(int index, ulong value)
		{
			if (index == 0)
			{
				uint v = (uint)value;
				this.reg0 = (byte)v;
				this.reg1 = (byte)(v >> 8);
				this.reg2 = (byte)(v >> 16);
				this.reg3 = (byte)(v >> 24);
				this.immediate = (uint)(value >> 32);
				return;
			}
			if (index != 1)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
				return;
			}
			this.memDispl = value;
		}

		// Token: 0x060022EE RID: 8942 RVA: 0x0007133C File Offset: 0x0006F53C
		public readonly ulong GetDeclareQwordValue(int index)
		{
			if (index == 0)
			{
				return (ulong)this.reg0 | (ulong)((ulong)this.reg1 << 8) | (ulong)((ulong)this.reg2 << 16) | (ulong)((ulong)this.reg3 << 24) | ((ulong)this.immediate << 32);
			}
			if (index != 1)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_index();
				return 0UL;
			}
			return this.memDispl;
		}

		// Token: 0x170007D9 RID: 2009
		// (get) Token: 0x060022EF RID: 8943 RVA: 0x00071394 File Offset: 0x0006F594
		public readonly bool IsVsib
		{
			get
			{
				bool flag;
				return this.TryGetVsib64(out flag);
			}
		}

		// Token: 0x170007DA RID: 2010
		// (get) Token: 0x060022F0 RID: 8944 RVA: 0x000713AC File Offset: 0x0006F5AC
		public readonly bool IsVsib32
		{
			get
			{
				bool vsib64;
				return this.TryGetVsib64(out vsib64) && !vsib64;
			}
		}

		// Token: 0x170007DB RID: 2011
		// (get) Token: 0x060022F1 RID: 8945 RVA: 0x000713CC File Offset: 0x0006F5CC
		public readonly bool IsVsib64
		{
			get
			{
				bool vsib64;
				return this.TryGetVsib64(out vsib64) && vsib64;
			}
		}

		// Token: 0x060022F2 RID: 8946 RVA: 0x000713E4 File Offset: 0x0006F5E4
		public readonly bool TryGetVsib64(out bool vsib64)
		{
			Code code = this.Code;
			if (code <= Code.EVEX_Vscatterpf1qpd_vm64z_k1)
			{
				if (code <= Code.EVEX_Vscatterqpd_vm64z_k1_zmm)
				{
					switch (code)
					{
					case Code.VEX_Vpgatherdd_xmm_vm32x_xmm:
					case Code.VEX_Vpgatherdd_ymm_vm32y_ymm:
					case Code.VEX_Vpgatherdq_xmm_vm32x_xmm:
					case Code.VEX_Vpgatherdq_ymm_vm32x_ymm:
					case Code.EVEX_Vpgatherdd_xmm_k1_vm32x:
					case Code.EVEX_Vpgatherdd_ymm_k1_vm32y:
					case Code.EVEX_Vpgatherdd_zmm_k1_vm32z:
					case Code.EVEX_Vpgatherdq_xmm_k1_vm32x:
					case Code.EVEX_Vpgatherdq_ymm_k1_vm32x:
					case Code.EVEX_Vpgatherdq_zmm_k1_vm32y:
					case Code.VEX_Vgatherdps_xmm_vm32x_xmm:
					case Code.VEX_Vgatherdps_ymm_vm32y_ymm:
					case Code.VEX_Vgatherdpd_xmm_vm32x_xmm:
					case Code.VEX_Vgatherdpd_ymm_vm32x_ymm:
					case Code.EVEX_Vgatherdps_xmm_k1_vm32x:
					case Code.EVEX_Vgatherdps_ymm_k1_vm32y:
					case Code.EVEX_Vgatherdps_zmm_k1_vm32z:
					case Code.EVEX_Vgatherdpd_xmm_k1_vm32x:
					case Code.EVEX_Vgatherdpd_ymm_k1_vm32x:
					case Code.EVEX_Vgatherdpd_zmm_k1_vm32y:
						goto IL_17F;
					case Code.VEX_Vpgatherqd_xmm_vm64x_xmm:
					case Code.VEX_Vpgatherqd_xmm_vm64y_xmm:
					case Code.VEX_Vpgatherqq_xmm_vm64x_xmm:
					case Code.VEX_Vpgatherqq_ymm_vm64y_ymm:
					case Code.EVEX_Vpgatherqd_xmm_k1_vm64x:
					case Code.EVEX_Vpgatherqd_xmm_k1_vm64y:
					case Code.EVEX_Vpgatherqd_ymm_k1_vm64z:
					case Code.EVEX_Vpgatherqq_xmm_k1_vm64x:
					case Code.EVEX_Vpgatherqq_ymm_k1_vm64y:
					case Code.EVEX_Vpgatherqq_zmm_k1_vm64z:
					case Code.VEX_Vgatherqps_xmm_vm64x_xmm:
					case Code.VEX_Vgatherqps_xmm_vm64y_xmm:
					case Code.VEX_Vgatherqpd_xmm_vm64x_xmm:
					case Code.VEX_Vgatherqpd_ymm_vm64y_ymm:
					case Code.EVEX_Vgatherqps_xmm_k1_vm64x:
					case Code.EVEX_Vgatherqps_xmm_k1_vm64y:
					case Code.EVEX_Vgatherqps_ymm_k1_vm64z:
					case Code.EVEX_Vgatherqpd_xmm_k1_vm64x:
					case Code.EVEX_Vgatherqpd_ymm_k1_vm64y:
					case Code.EVEX_Vgatherqpd_zmm_k1_vm64z:
						break;
					default:
						switch (code)
						{
						case Code.EVEX_Vpscatterdd_vm32x_k1_xmm:
						case Code.EVEX_Vpscatterdd_vm32y_k1_ymm:
						case Code.EVEX_Vpscatterdd_vm32z_k1_zmm:
						case Code.EVEX_Vpscatterdq_vm32x_k1_xmm:
						case Code.EVEX_Vpscatterdq_vm32x_k1_ymm:
						case Code.EVEX_Vpscatterdq_vm32y_k1_zmm:
						case Code.EVEX_Vscatterdps_vm32x_k1_xmm:
						case Code.EVEX_Vscatterdps_vm32y_k1_ymm:
						case Code.EVEX_Vscatterdps_vm32z_k1_zmm:
						case Code.EVEX_Vscatterdpd_vm32x_k1_xmm:
						case Code.EVEX_Vscatterdpd_vm32x_k1_ymm:
						case Code.EVEX_Vscatterdpd_vm32y_k1_zmm:
							goto IL_17F;
						case Code.EVEX_Vpscatterqd_vm64x_k1_xmm:
						case Code.EVEX_Vpscatterqd_vm64y_k1_xmm:
						case Code.EVEX_Vpscatterqd_vm64z_k1_ymm:
						case Code.EVEX_Vpscatterqq_vm64x_k1_xmm:
						case Code.EVEX_Vpscatterqq_vm64y_k1_ymm:
						case Code.EVEX_Vpscatterqq_vm64z_k1_zmm:
						case Code.EVEX_Vscatterqps_vm64x_k1_xmm:
						case Code.EVEX_Vscatterqps_vm64y_k1_xmm:
						case Code.EVEX_Vscatterqps_vm64z_k1_ymm:
						case Code.EVEX_Vscatterqpd_vm64x_k1_xmm:
						case Code.EVEX_Vscatterqpd_vm64y_k1_ymm:
						case Code.EVEX_Vscatterqpd_vm64z_k1_zmm:
							break;
						default:
							goto IL_189;
						}
						break;
					}
				}
				else
				{
					if (code - Code.EVEX_Vgatherpf0dps_vm32z_k1 <= 7)
					{
						goto IL_17F;
					}
					if (code - Code.EVEX_Vgatherpf0qps_vm64z_k1 > 7)
					{
						goto IL_189;
					}
				}
				vsib64 = true;
				return true;
			}
			if (code <= Code.MVEX_Vscatterdpd_mvt_k1_zmm)
			{
				if (code - Code.MVEX_Vpgatherdd_zmm_k1_mvt > 3 && code - Code.MVEX_Vpscatterdd_mvt_k1_zmm > 3)
				{
					goto IL_189;
				}
			}
			else if (code - Code.MVEX_Undoc_zmm_k1_mvt_512_66_0F38_W0_B0 > 1 && code - Code.MVEX_Undoc_zmm_k1_mvt_512_66_0F38_W0_C0 > 8)
			{
				goto IL_189;
			}
			IL_17F:
			vsib64 = false;
			return true;
			IL_189:
			vsib64 = false;
			return false;
		}

		// Token: 0x170007DC RID: 2012
		// (get) Token: 0x060022F3 RID: 8947 RVA: 0x0007157E File Offset: 0x0006F77E
		// (set) Token: 0x060022F4 RID: 8948 RVA: 0x0007158F File Offset: 0x0006F78F
		public bool SuppressAllExceptions
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			readonly get
			{
				return (this.flags1 & 134217728U) > 0U;
			}
			set
			{
				if (value)
				{
					this.flags1 |= 134217728U;
					return;
				}
				this.flags1 &= 4160749567U;
			}
		}

		// Token: 0x060022F5 RID: 8949 RVA: 0x000715B9 File Offset: 0x0006F7B9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InternalSetSuppressAllExceptions()
		{
			this.flags1 |= 134217728U;
		}

		// Token: 0x170007DD RID: 2013
		// (get) Token: 0x060022F6 RID: 8950 RVA: 0x000715CD File Offset: 0x0006F7CD
		public readonly bool IsIPRelativeMemoryOperand
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this.MemoryBase == Register.RIP || this.MemoryBase == Register.EIP;
			}
		}

		// Token: 0x170007DE RID: 2014
		// (get) Token: 0x060022F7 RID: 8951 RVA: 0x000715E5 File Offset: 0x0006F7E5
		public readonly ulong IPRelativeMemoryAddress
		{
			get
			{
				if (this.MemoryBase != Register.EIP)
				{
					return this.MemoryDisplacement64;
				}
				return (ulong)this.MemoryDisplacement32;
			}
		}

		// Token: 0x060022F8 RID: 8952 RVA: 0x000715FF File Offset: 0x0006F7FF
		public override readonly string ToString()
		{
			return base.ToString() ?? string.Empty;
		}

		// Token: 0x060022F9 RID: 8953 RVA: 0x0007161C File Offset: 0x0006F81C
		public readonly ulong GetVirtualAddress(int operand, int elementIndex, VAGetRegisterValue getRegisterValue)
		{
			if (getRegisterValue == null)
			{
				throw new ArgumentNullException("getRegisterValue");
			}
			VARegisterValueProviderDelegateImpl provider = new VARegisterValueProviderDelegateImpl(getRegisterValue);
			ulong result;
			if (this.TryGetVirtualAddress(operand, elementIndex, provider, out result))
			{
				return result;
			}
			return 0UL;
		}

		// Token: 0x060022FA RID: 8954 RVA: 0x00071650 File Offset: 0x0006F850
		public readonly ulong GetVirtualAddress(int operand, int elementIndex, IVARegisterValueProvider registerValueProvider)
		{
			if (registerValueProvider == null)
			{
				throw new ArgumentNullException("registerValueProvider");
			}
			VARegisterValueProviderAdapter provider = new VARegisterValueProviderAdapter(registerValueProvider);
			ulong result;
			if (this.TryGetVirtualAddress(operand, elementIndex, provider, out result))
			{
				return result;
			}
			return 0UL;
		}

		// Token: 0x060022FB RID: 8955 RVA: 0x00071684 File Offset: 0x0006F884
		public readonly bool TryGetVirtualAddress(int operand, int elementIndex, out ulong result, VATryGetRegisterValue getRegisterValue)
		{
			if (getRegisterValue == null)
			{
				throw new ArgumentNullException("getRegisterValue");
			}
			VATryGetRegisterValueDelegateImpl provider = new VATryGetRegisterValueDelegateImpl(getRegisterValue);
			return this.TryGetVirtualAddress(operand, elementIndex, provider, out result);
		}

		// Token: 0x060022FC RID: 8956 RVA: 0x000716B4 File Offset: 0x0006F8B4
		public readonly bool TryGetVirtualAddress(int operand, int elementIndex, IVATryGetRegisterValueProvider registerValueProvider, out ulong result)
		{
			if (registerValueProvider == null)
			{
				throw new ArgumentNullException("registerValueProvider");
			}
			switch (this.GetOpKind(operand))
			{
			case OpKind.Register:
			case OpKind.NearBranch16:
			case OpKind.NearBranch32:
			case OpKind.NearBranch64:
			case OpKind.FarBranch16:
			case OpKind.FarBranch32:
			case OpKind.Immediate8:
			case OpKind.Immediate8_2nd:
			case OpKind.Immediate16:
			case OpKind.Immediate32:
			case OpKind.Immediate64:
			case OpKind.Immediate8to16:
			case OpKind.Immediate8to32:
			case OpKind.Immediate8to64:
			case OpKind.Immediate32to64:
				result = 0UL;
				return true;
			case OpKind.MemorySegSI:
			{
				ulong seg;
				ulong @base;
				if (registerValueProvider.TryGetRegisterValue(this.MemorySegment, 0, 0, out seg) && registerValueProvider.TryGetRegisterValue(Register.SI, 0, 0, out @base))
				{
					result = seg + (ulong)((ushort)@base);
					return true;
				}
				break;
			}
			case OpKind.MemorySegESI:
			{
				ulong seg;
				ulong @base;
				if (registerValueProvider.TryGetRegisterValue(this.MemorySegment, 0, 0, out seg) && registerValueProvider.TryGetRegisterValue(Register.ESI, 0, 0, out @base))
				{
					result = seg + (ulong)((uint)@base);
					return true;
				}
				break;
			}
			case OpKind.MemorySegRSI:
			{
				ulong seg;
				ulong @base;
				if (registerValueProvider.TryGetRegisterValue(this.MemorySegment, 0, 0, out seg) && registerValueProvider.TryGetRegisterValue(Register.RSI, 0, 0, out @base))
				{
					result = seg + @base;
					return true;
				}
				break;
			}
			case OpKind.MemorySegDI:
			{
				ulong seg;
				ulong @base;
				if (registerValueProvider.TryGetRegisterValue(this.MemorySegment, 0, 0, out seg) && registerValueProvider.TryGetRegisterValue(Register.DI, 0, 0, out @base))
				{
					result = seg + (ulong)((ushort)@base);
					return true;
				}
				break;
			}
			case OpKind.MemorySegEDI:
			{
				ulong seg;
				ulong @base;
				if (registerValueProvider.TryGetRegisterValue(this.MemorySegment, 0, 0, out seg) && registerValueProvider.TryGetRegisterValue(Register.EDI, 0, 0, out @base))
				{
					result = seg + (ulong)((uint)@base);
					return true;
				}
				break;
			}
			case OpKind.MemorySegRDI:
			{
				ulong seg;
				ulong @base;
				if (registerValueProvider.TryGetRegisterValue(this.MemorySegment, 0, 0, out seg) && registerValueProvider.TryGetRegisterValue(Register.RDI, 0, 0, out @base))
				{
					result = seg + @base;
					return true;
				}
				break;
			}
			case OpKind.MemoryESDI:
			{
				ulong seg;
				ulong @base;
				if (registerValueProvider.TryGetRegisterValue(Register.ES, 0, 0, out seg) && registerValueProvider.TryGetRegisterValue(Register.DI, 0, 0, out @base))
				{
					result = seg + (ulong)((ushort)@base);
					return true;
				}
				break;
			}
			case OpKind.MemoryESEDI:
			{
				ulong seg;
				ulong @base;
				if (registerValueProvider.TryGetRegisterValue(Register.ES, 0, 0, out seg) && registerValueProvider.TryGetRegisterValue(Register.EDI, 0, 0, out @base))
				{
					result = seg + (ulong)((uint)@base);
					return true;
				}
				break;
			}
			case OpKind.MemoryESRDI:
			{
				ulong seg;
				ulong @base;
				if (registerValueProvider.TryGetRegisterValue(Register.ES, 0, 0, out seg) && registerValueProvider.TryGetRegisterValue(Register.RDI, 0, 0, out @base))
				{
					result = seg + @base;
					return true;
				}
				break;
			}
			case OpKind.Memory:
			{
				Register baseReg = this.MemoryBase;
				Register indexReg = this.MemoryIndex;
				int addrSize = InstructionUtils.GetAddressSizeInBytes(baseReg, indexReg, this.MemoryDisplSize, this.CodeSize);
				ulong offset = this.MemoryDisplacement64;
				ulong offsetMask;
				if (addrSize == 8)
				{
					offsetMask = ulong.MaxValue;
				}
				else if (addrSize == 4)
				{
					offsetMask = (ulong)(-1);
				}
				else
				{
					offsetMask = 65535UL;
				}
				if (baseReg != Register.None && baseReg != Register.RIP && baseReg != Register.EIP)
				{
					ulong @base;
					if (!registerValueProvider.TryGetRegisterValue(baseReg, 0, 0, out @base))
					{
						break;
					}
					offset += @base;
				}
				Code code = this.Code;
				if (indexReg != Register.None && !code.IgnoresIndex() && !code.IsTileStrideIndex())
				{
					bool vsib64;
					if (this.TryGetVsib64(out vsib64))
					{
						ulong @base;
						bool b;
						if (vsib64)
						{
							b = registerValueProvider.TryGetRegisterValue(indexReg, elementIndex, 8, out @base);
						}
						else
						{
							b = registerValueProvider.TryGetRegisterValue(indexReg, elementIndex, 4, out @base);
							@base = (ulong)((long)((int)@base));
						}
						if (!b)
						{
							break;
						}
						offset += @base << this.InternalMemoryIndexScale;
					}
					else
					{
						ulong @base;
						if (!registerValueProvider.TryGetRegisterValue(indexReg, 0, 0, out @base))
						{
							break;
						}
						offset += @base << this.InternalMemoryIndexScale;
					}
				}
				offset &= offsetMask;
				if (!code.IgnoresSegment())
				{
					ulong seg;
					if (!registerValueProvider.TryGetRegisterValue(this.MemorySegment, 0, 0, out seg))
					{
						break;
					}
					offset += seg;
				}
				result = offset;
				return true;
			}
			default:
				throw new InvalidOperationException();
			}
			result = 0UL;
			return false;
		}

		// Token: 0x04002AC3 RID: 10947
		internal const int TOTAL_SIZE = 40;

		// Token: 0x04002AC4 RID: 10948
		private ulong nextRip;

		// Token: 0x04002AC5 RID: 10949
		private ulong memDispl;

		// Token: 0x04002AC6 RID: 10950
		private uint flags1;

		// Token: 0x04002AC7 RID: 10951
		private uint immediate;

		// Token: 0x04002AC8 RID: 10952
		private ushort code;

		// Token: 0x04002AC9 RID: 10953
		private byte memBaseReg;

		// Token: 0x04002ACA RID: 10954
		private byte memIndexReg;

		// Token: 0x04002ACB RID: 10955
		private byte reg0;

		// Token: 0x04002ACC RID: 10956
		private byte reg1;

		// Token: 0x04002ACD RID: 10957
		private byte reg2;

		// Token: 0x04002ACE RID: 10958
		private byte reg3;

		// Token: 0x04002ACF RID: 10959
		private byte opKind0;

		// Token: 0x04002AD0 RID: 10960
		private byte opKind1;

		// Token: 0x04002AD1 RID: 10961
		private byte opKind2;

		// Token: 0x04002AD2 RID: 10962
		private byte opKind3;

		// Token: 0x04002AD3 RID: 10963
		private byte scale;

		// Token: 0x04002AD4 RID: 10964
		private byte displSize;

		// Token: 0x04002AD5 RID: 10965
		private byte len;

		// Token: 0x04002AD6 RID: 10966
		private byte pad;

		// Token: 0x02000649 RID: 1609
		[Flags]
		private enum InstrFlags1 : uint
		{
			// Token: 0x04002AD8 RID: 10968
			SegmentPrefixMask = 7U,
			// Token: 0x04002AD9 RID: 10969
			SegmentPrefixShift = 5U,
			// Token: 0x04002ADA RID: 10970
			DataLengthMask = 15U,
			// Token: 0x04002ADB RID: 10971
			DataLengthShift = 8U,
			// Token: 0x04002ADC RID: 10972
			RoundingControlMask = 7U,
			// Token: 0x04002ADD RID: 10973
			RoundingControlShift = 12U,
			// Token: 0x04002ADE RID: 10974
			OpMaskMask = 7U,
			// Token: 0x04002ADF RID: 10975
			OpMaskShift = 15U,
			// Token: 0x04002AE0 RID: 10976
			CodeSizeMask = 3U,
			// Token: 0x04002AE1 RID: 10977
			CodeSizeShift = 18U,
			// Token: 0x04002AE2 RID: 10978
			Broadcast = 67108864U,
			// Token: 0x04002AE3 RID: 10979
			SuppressAllExceptions = 134217728U,
			// Token: 0x04002AE4 RID: 10980
			ZeroingMasking = 268435456U,
			// Token: 0x04002AE5 RID: 10981
			RepePrefix = 536870912U,
			// Token: 0x04002AE6 RID: 10982
			RepnePrefix = 1073741824U,
			// Token: 0x04002AE7 RID: 10983
			LockPrefix = 2147483648U,
			// Token: 0x04002AE8 RID: 10984
			EqualsIgnoreMask = 786432U
		}

		// Token: 0x0200064A RID: 1610
		[Flags]
		private enum MvexInstrFlags : uint
		{
			// Token: 0x04002AEA RID: 10986
			MvexRegMemConvShift = 16U,
			// Token: 0x04002AEB RID: 10987
			MvexRegMemConvMask = 31U,
			// Token: 0x04002AEC RID: 10988
			EvictionHint = 2147483648U
		}
	}
}
