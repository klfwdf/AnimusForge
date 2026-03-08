using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Iced.Intel.EncoderInternal;

namespace Iced.Intel
{
	// Token: 0x02000641 RID: 1601
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class Encoder
	{
		// Token: 0x17000771 RID: 1905
		// (get) Token: 0x0600214D RID: 8525 RVA: 0x0006A717 File Offset: 0x00068917
		// (set) Token: 0x0600214E RID: 8526 RVA: 0x0006A722 File Offset: 0x00068922
		public bool PreventVEX2
		{
			get
			{
				return this.Internal_PreventVEX2 > 0U;
			}
			set
			{
				this.Internal_PreventVEX2 = (value ? uint.MaxValue : 0U);
			}
		}

		// Token: 0x17000772 RID: 1906
		// (get) Token: 0x0600214F RID: 8527 RVA: 0x0006A731 File Offset: 0x00068931
		// (set) Token: 0x06002150 RID: 8528 RVA: 0x0006A73D File Offset: 0x0006893D
		public uint VEX_WIG
		{
			get
			{
				return (this.Internal_VEX_WIG_LIG >> 7) & 1U;
			}
			set
			{
				this.Internal_VEX_WIG_LIG = (this.Internal_VEX_WIG_LIG & 4294967167U) | ((value & 1U) << 7);
			}
		}

		// Token: 0x17000773 RID: 1907
		// (get) Token: 0x06002151 RID: 8529 RVA: 0x0006A757 File Offset: 0x00068957
		// (set) Token: 0x06002152 RID: 8530 RVA: 0x0006A763 File Offset: 0x00068963
		public uint VEX_LIG
		{
			get
			{
				return (this.Internal_VEX_WIG_LIG >> 2) & 1U;
			}
			set
			{
				this.Internal_VEX_WIG_LIG = (this.Internal_VEX_WIG_LIG & 4294967291U) | ((value & 1U) << 2);
				this.Internal_VEX_LIG = (value & 1U) << 2;
			}
		}

		// Token: 0x17000774 RID: 1908
		// (get) Token: 0x06002153 RID: 8531 RVA: 0x0006A785 File Offset: 0x00068985
		// (set) Token: 0x06002154 RID: 8532 RVA: 0x0006A78F File Offset: 0x0006898F
		public uint EVEX_WIG
		{
			get
			{
				return this.Internal_EVEX_WIG >> 7;
			}
			set
			{
				this.Internal_EVEX_WIG = (value & 1U) << 7;
			}
		}

		// Token: 0x17000775 RID: 1909
		// (get) Token: 0x06002155 RID: 8533 RVA: 0x0006A79C File Offset: 0x0006899C
		// (set) Token: 0x06002156 RID: 8534 RVA: 0x0006A7A6 File Offset: 0x000689A6
		public uint EVEX_LIG
		{
			get
			{
				return this.Internal_EVEX_LIG >> 5;
			}
			set
			{
				this.Internal_EVEX_LIG = (value & 3U) << 5;
			}
		}

		// Token: 0x17000776 RID: 1910
		// (get) Token: 0x06002157 RID: 8535 RVA: 0x0006A7B3 File Offset: 0x000689B3
		public int Bitness
		{
			get
			{
				return this.bitness;
			}
		}

		// Token: 0x06002158 RID: 8536 RVA: 0x0006A7BC File Offset: 0x000689BC
		private Encoder(CodeWriter writer, int bitness)
		{
			if (writer == null)
			{
				ThrowHelper.ThrowArgumentNullException_writer();
			}
			this.immSizes = Encoder.s_immSizes;
			this.writer = writer;
			this.bitness = bitness;
			this.handlers = OpCodeHandlers.Handlers;
			this.handler = null;
			this.opSize16Flags = ((bitness != 16) ? EncoderFlags.P66 : EncoderFlags.None);
			this.opSize32Flags = ((bitness == 16) ? EncoderFlags.P66 : EncoderFlags.None);
			this.adrSize16Flags = ((bitness != 16) ? EncoderFlags.P67 : EncoderFlags.None);
			this.adrSize32Flags = ((bitness != 32) ? EncoderFlags.P67 : EncoderFlags.None);
		}

		// Token: 0x06002159 RID: 8537 RVA: 0x0006A850 File Offset: 0x00068A50
		public static Encoder Create(int bitness, CodeWriter writer)
		{
			if (bitness == 16 || bitness == 32 || bitness == 64)
			{
				return new Encoder(writer, bitness);
			}
			throw new ArgumentOutOfRangeException("bitness");
		}

		// Token: 0x0600215A RID: 8538 RVA: 0x0006A884 File Offset: 0x00068A84
		public uint Encode(in Instruction instruction, ulong rip)
		{
			uint result;
			string errorMessage;
			if (!this.TryEncode(instruction, rip, out result, out errorMessage))
			{
				Encoder.ThrowEncoderException(instruction, errorMessage);
			}
			return result;
		}

		// Token: 0x0600215B RID: 8539 RVA: 0x0006A8A7 File Offset: 0x00068AA7
		private static void ThrowEncoderException(in Instruction instruction, string errorMessage)
		{
			throw new EncoderException(errorMessage, ref instruction);
		}

		// Token: 0x0600215C RID: 8540 RVA: 0x0006A8B0 File Offset: 0x00068AB0
		[NullableContext(2)]
		public bool TryEncode(in Instruction instruction, ulong rip, out uint encodedLength, [<b37590d4-39fb-478a-88de-d293f3364852>NotNullWhen(false)] out string errorMessage)
		{
			this.currentRip = rip;
			this.eip = (uint)rip;
			this.errorMessage = null;
			this.EncoderFlags = EncoderFlags.None;
			this.DisplSize = DisplSize.None;
			this.ImmSize = ImmSize.None;
			this.ModRM = 0;
			OpCodeHandler handler = this.handlers[(int)instruction.Code];
			this.handler = handler;
			this.OpCode = handler.OpCode;
			if (handler.GroupIndex >= 0)
			{
				this.EncoderFlags = EncoderFlags.ModRM;
				this.ModRM = (byte)(handler.GroupIndex << 3);
			}
			if (handler.RmGroupIndex >= 0)
			{
				this.EncoderFlags = EncoderFlags.ModRM;
				this.ModRM |= (byte)(handler.RmGroupIndex | 192);
			}
			EncFlags3 encFlags = handler.EncFlags3 & (EncFlags3.Bit16or32 | EncFlags3.Bit64);
			if (encFlags != EncFlags3.Bit16or32)
			{
				if (encFlags != EncFlags3.Bit64)
				{
					if (encFlags != (EncFlags3.Bit16or32 | EncFlags3.Bit64))
					{
						throw new InvalidOperationException();
					}
				}
				else if (this.bitness != 64)
				{
					this.ErrorMessage = "The instruction can only be used in 64-bit mode";
				}
			}
			else if (this.bitness == 64)
			{
				this.ErrorMessage = "The instruction can only be used in 16/32-bit mode";
			}
			switch (handler.OpSize)
			{
			case CodeSize.Unknown:
				break;
			case CodeSize.Code16:
				this.EncoderFlags |= this.opSize16Flags;
				break;
			case CodeSize.Code32:
				this.EncoderFlags |= this.opSize32Flags;
				break;
			case CodeSize.Code64:
				if ((handler.EncFlags3 & EncFlags3.DefaultOpSize64) == EncFlags3.None)
				{
					this.EncoderFlags |= EncoderFlags.W;
				}
				break;
			default:
				throw new InvalidOperationException();
			}
			switch (handler.AddrSize)
			{
			case CodeSize.Unknown:
			case CodeSize.Code64:
				break;
			case CodeSize.Code16:
				this.EncoderFlags |= this.adrSize16Flags;
				break;
			case CodeSize.Code32:
				this.EncoderFlags |= this.adrSize32Flags;
				break;
			default:
				throw new InvalidOperationException();
			}
			if (!handler.IsSpecialInstr)
			{
				Op[] ops = handler.Operands;
				for (int i = 0; i < ops.Length; i++)
				{
					ops[i].Encode(this, instruction, i);
				}
				if ((handler.EncFlags3 & EncFlags3.Fwait) != EncFlags3.None)
				{
					this.WriteByteInternal(155U);
				}
				handler.Encode(this, instruction);
				uint opCode = this.OpCode;
				if (!handler.Is2ByteOpCode)
				{
					this.WriteByteInternal(opCode);
				}
				else
				{
					this.WriteByteInternal(opCode >> 8);
					this.WriteByteInternal(opCode);
				}
				if ((this.EncoderFlags & (EncoderFlags.ModRM | EncoderFlags.Displ)) != EncoderFlags.None)
				{
					this.WriteModRM();
				}
				if (this.ImmSize != ImmSize.None)
				{
					this.WriteImmediate();
				}
			}
			else
			{
				handler.Encode(this, instruction);
			}
			uint instrLen = (uint)this.currentRip - (uint)rip;
			if (instrLen > 15U && !handler.IsSpecialInstr)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Instruction length > ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(15);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes");
				this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			errorMessage = this.errorMessage;
			if (errorMessage != null)
			{
				encodedLength = 0U;
				return false;
			}
			encodedLength = instrLen;
			return true;
		}

		// Token: 0x17000777 RID: 1911
		// (set) Token: 0x0600215D RID: 8541 RVA: 0x0006AB87 File Offset: 0x00068D87
		[Nullable(2)]
		internal string ErrorMessage
		{
			[NullableContext(2)]
			set
			{
				if (this.errorMessage == null)
				{
					this.errorMessage = value;
				}
			}
		}

		// Token: 0x0600215E RID: 8542 RVA: 0x0006AB98 File Offset: 0x00068D98
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool Verify(int operand, OpKind expected, OpKind actual)
		{
			if (expected == actual)
			{
				return true;
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Operand ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
			defaultInterpolatedStringHandler.AppendLiteral(": Expected: ");
			defaultInterpolatedStringHandler.AppendFormatted<OpKind>(expected);
			defaultInterpolatedStringHandler.AppendLiteral(", actual: ");
			defaultInterpolatedStringHandler.AppendFormatted<OpKind>(actual);
			this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
			return false;
		}

		// Token: 0x0600215F RID: 8543 RVA: 0x0006AC00 File Offset: 0x00068E00
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool Verify(int operand, Register expected, Register actual)
		{
			if (expected == actual)
			{
				return true;
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Operand ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
			defaultInterpolatedStringHandler.AppendLiteral(": Expected: ");
			defaultInterpolatedStringHandler.AppendFormatted<Register>(expected);
			defaultInterpolatedStringHandler.AppendLiteral(", actual: ");
			defaultInterpolatedStringHandler.AppendFormatted<Register>(actual);
			this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
			return false;
		}

		// Token: 0x06002160 RID: 8544 RVA: 0x0006AC68 File Offset: 0x00068E68
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool Verify(int operand, Register register, Register regLo, Register regHi)
		{
			if (this.bitness != 64 && regHi > regLo + 7)
			{
				regHi = regLo + 7;
			}
			if (regLo <= register && register <= regHi)
			{
				return true;
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 4);
			defaultInterpolatedStringHandler.AppendLiteral("Operand ");
			defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
			defaultInterpolatedStringHandler.AppendLiteral(": Register ");
			defaultInterpolatedStringHandler.AppendFormatted<Register>(register);
			defaultInterpolatedStringHandler.AppendLiteral(" is not between ");
			defaultInterpolatedStringHandler.AppendFormatted<Register>(regLo);
			defaultInterpolatedStringHandler.AppendLiteral(" and ");
			defaultInterpolatedStringHandler.AppendFormatted<Register>(regHi);
			defaultInterpolatedStringHandler.AppendLiteral(" (inclusive)");
			this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
			return false;
		}

		// Token: 0x06002161 RID: 8545 RVA: 0x0006AD0C File Offset: 0x00068F0C
		internal void AddBranch(OpKind opKind, int immSize, in Instruction instruction, int operand)
		{
			if (!this.Verify(operand, opKind, instruction.GetOpKind(operand)))
			{
				return;
			}
			switch (immSize)
			{
			case 1:
				switch (opKind)
				{
				case OpKind.NearBranch16:
					this.EncoderFlags |= this.opSize16Flags;
					this.ImmSize = ImmSize.RipRelSize1_Target16;
					this.Immediate = (uint)instruction.NearBranch16;
					return;
				case OpKind.NearBranch32:
					this.EncoderFlags |= this.opSize32Flags;
					this.ImmSize = ImmSize.RipRelSize1_Target32;
					this.Immediate = instruction.NearBranch32;
					return;
				case OpKind.NearBranch64:
				{
					this.ImmSize = ImmSize.RipRelSize1_Target64;
					ulong target = instruction.NearBranch64;
					this.Immediate = (uint)target;
					this.ImmediateHi = (uint)(target >> 32);
					return;
				}
				default:
					throw new InvalidOperationException();
				}
				break;
			case 2:
				if (opKind == OpKind.NearBranch16)
				{
					this.EncoderFlags |= this.opSize16Flags;
					this.ImmSize = ImmSize.RipRelSize2_Target16;
					this.Immediate = (uint)instruction.NearBranch16;
					return;
				}
				throw new InvalidOperationException();
			case 4:
			{
				if (opKind == OpKind.NearBranch32)
				{
					this.EncoderFlags |= this.opSize32Flags;
					this.ImmSize = ImmSize.RipRelSize4_Target32;
					this.Immediate = instruction.NearBranch32;
					return;
				}
				if (opKind != OpKind.NearBranch64)
				{
					throw new InvalidOperationException();
				}
				this.ImmSize = ImmSize.RipRelSize4_Target64;
				ulong target = instruction.NearBranch64;
				this.Immediate = (uint)target;
				this.ImmediateHi = (uint)(target >> 32);
				return;
			}
			}
			throw new InvalidOperationException();
		}

		// Token: 0x06002162 RID: 8546 RVA: 0x0006AE6C File Offset: 0x0006906C
		internal void AddBranchX(int immSize, in Instruction instruction, int operand)
		{
			if (this.bitness == 64)
			{
				if (!this.Verify(operand, OpKind.NearBranch64, instruction.GetOpKind(operand)))
				{
					return;
				}
				ulong target = instruction.NearBranch64;
				if (immSize == 2)
				{
					this.EncoderFlags |= EncoderFlags.P66;
					this.ImmSize = ImmSize.RipRelSize2_Target64;
					this.Immediate = (uint)target;
					this.ImmediateHi = (uint)(target >> 32);
					return;
				}
				if (immSize != 4)
				{
					throw new InvalidOperationException();
				}
				this.ImmSize = ImmSize.RipRelSize4_Target64;
				this.Immediate = (uint)target;
				this.ImmediateHi = (uint)(target >> 32);
				return;
			}
			else
			{
				if (!this.Verify(operand, OpKind.NearBranch32, instruction.GetOpKind(operand)))
				{
					return;
				}
				if (immSize == 2)
				{
					this.EncoderFlags |= (EncoderFlags)((this.bitness & 32) << 2);
					this.ImmSize = ImmSize.RipRelSize2_Target32;
					this.Immediate = instruction.NearBranch32;
					return;
				}
				if (immSize != 4)
				{
					if (immSize != 8)
					{
					}
					throw new InvalidOperationException();
				}
				this.EncoderFlags |= (EncoderFlags)((this.bitness & 16) << 3);
				this.ImmSize = ImmSize.RipRelSize4_Target32;
				this.Immediate = instruction.NearBranch32;
				return;
			}
		}

		// Token: 0x06002163 RID: 8547 RVA: 0x0006AF78 File Offset: 0x00069178
		internal void AddBranchDisp(int displSize, in Instruction instruction, int operand)
		{
			OpKind opKind;
			if (displSize != 2)
			{
				if (displSize != 4)
				{
					throw new InvalidOperationException();
				}
				opKind = OpKind.NearBranch32;
				this.ImmSize = ImmSize.Size4;
				this.Immediate = instruction.NearBranch32;
			}
			else
			{
				opKind = OpKind.NearBranch16;
				this.ImmSize = ImmSize.Size2;
				this.Immediate = (uint)instruction.NearBranch16;
			}
			this.Verify(operand, opKind, instruction.GetOpKind(operand));
		}

		// Token: 0x06002164 RID: 8548 RVA: 0x0006AFD4 File Offset: 0x000691D4
		internal void AddFarBranch(in Instruction instruction, int operand, int size)
		{
			if (size == 2)
			{
				if (!this.Verify(operand, OpKind.FarBranch16, instruction.GetOpKind(operand)))
				{
					return;
				}
				this.ImmSize = ImmSize.Size2_2;
				this.Immediate = (uint)instruction.FarBranch16;
				this.ImmediateHi = (uint)instruction.FarBranchSelector;
			}
			else
			{
				if (!this.Verify(operand, OpKind.FarBranch32, instruction.GetOpKind(operand)))
				{
					return;
				}
				this.ImmSize = ImmSize.Size4_2;
				this.Immediate = instruction.FarBranch32;
				this.ImmediateHi = (uint)instruction.FarBranchSelector;
			}
			if (this.bitness != size * 8)
			{
				this.EncoderFlags |= EncoderFlags.P66;
			}
		}

		// Token: 0x06002165 RID: 8549 RVA: 0x0006B068 File Offset: 0x00069268
		internal void SetAddrSize(int regSize)
		{
			if (this.bitness == 64)
			{
				if (regSize == 2)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(49, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Invalid register size: ");
					defaultInterpolatedStringHandler.AppendFormatted<int>(regSize * 8);
					defaultInterpolatedStringHandler.AppendLiteral(", must be 32-bit or 64-bit");
					this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
					return;
				}
				if (regSize == 4)
				{
					this.EncoderFlags |= EncoderFlags.P67;
					return;
				}
			}
			else
			{
				if (regSize == 8)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(49, 1);
					defaultInterpolatedStringHandler2.AppendLiteral("Invalid register size: ");
					defaultInterpolatedStringHandler2.AppendFormatted<int>(regSize * 8);
					defaultInterpolatedStringHandler2.AppendLiteral(", must be 16-bit or 32-bit");
					this.ErrorMessage = defaultInterpolatedStringHandler2.ToStringAndClear();
					return;
				}
				if (this.bitness == 16)
				{
					if (regSize == 4)
					{
						this.EncoderFlags |= EncoderFlags.P67;
						return;
					}
				}
				else if (regSize == 2)
				{
					this.EncoderFlags |= EncoderFlags.P67;
				}
			}
		}

		// Token: 0x06002166 RID: 8550 RVA: 0x0006B14C File Offset: 0x0006934C
		internal void AddAbsMem(in Instruction instruction, int operand)
		{
			this.EncoderFlags |= EncoderFlags.Displ;
			OpKind opKind = instruction.GetOpKind(operand);
			if (opKind != OpKind.Memory)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(": Expected OpKind ");
				defaultInterpolatedStringHandler.AppendFormatted("Memory");
				defaultInterpolatedStringHandler.AppendLiteral(", actual: ");
				defaultInterpolatedStringHandler.AppendFormatted<OpKind>(opKind);
				this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
			if (instruction.MemoryBase != Register.None || instruction.MemoryIndex != Register.None)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(62, 1);
				defaultInterpolatedStringHandler2.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler2.AppendLiteral(": Absolute addresses can't have base and/or index regs");
				this.ErrorMessage = defaultInterpolatedStringHandler2.ToStringAndClear();
				return;
			}
			if (instruction.MemoryIndexScale != 1)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(50, 1);
				defaultInterpolatedStringHandler3.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler3.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler3.AppendLiteral(": Absolute addresses must have scale == *1");
				this.ErrorMessage = defaultInterpolatedStringHandler3.ToStringAndClear();
				return;
			}
			int memoryDisplSize = instruction.MemoryDisplSize;
			if (memoryDisplSize != 2)
			{
				if (memoryDisplSize != 4)
				{
					if (memoryDisplSize != 8)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler4 = new DefaultInterpolatedStringHandler(71, 3);
						defaultInterpolatedStringHandler4.AppendLiteral("Operand ");
						defaultInterpolatedStringHandler4.AppendFormatted<int>(operand);
						defaultInterpolatedStringHandler4.AppendLiteral(": ");
						defaultInterpolatedStringHandler4.AppendFormatted("Instruction");
						defaultInterpolatedStringHandler4.AppendLiteral(".");
						defaultInterpolatedStringHandler4.AppendFormatted("MemoryDisplSize");
						defaultInterpolatedStringHandler4.AppendLiteral(" must be initialized to 2 (16-bit), 4 (32-bit) or 8 (64-bit)");
						this.ErrorMessage = defaultInterpolatedStringHandler4.ToStringAndClear();
						return;
					}
					if (this.bitness != 64)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler5 = new DefaultInterpolatedStringHandler(61, 1);
						defaultInterpolatedStringHandler5.AppendLiteral("Operand ");
						defaultInterpolatedStringHandler5.AppendFormatted<int>(operand);
						defaultInterpolatedStringHandler5.AppendLiteral(": 64-bit abs address is only available in 64-bit mode");
						this.ErrorMessage = defaultInterpolatedStringHandler5.ToStringAndClear();
						return;
					}
					this.DisplSize = DisplSize.Size8;
					ulong addr = instruction.MemoryDisplacement64;
					this.Displ = (uint)addr;
					this.DisplHi = (uint)(addr >> 32);
					return;
				}
				else
				{
					this.EncoderFlags |= this.adrSize32Flags;
					this.DisplSize = DisplSize.Size4;
					if (instruction.MemoryDisplacement64 > (ulong)(-1))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler6 = new DefaultInterpolatedStringHandler(41, 1);
						defaultInterpolatedStringHandler6.AppendLiteral("Operand ");
						defaultInterpolatedStringHandler6.AppendFormatted<int>(operand);
						defaultInterpolatedStringHandler6.AppendLiteral(": Displacement must fit in a uint");
						this.ErrorMessage = defaultInterpolatedStringHandler6.ToStringAndClear();
						return;
					}
					this.Displ = instruction.MemoryDisplacement32;
					return;
				}
			}
			else
			{
				if (this.bitness == 64)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler7 = new DefaultInterpolatedStringHandler(59, 1);
					defaultInterpolatedStringHandler7.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler7.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler7.AppendLiteral(": 16-bit abs addresses can't be used in 64-bit mode");
					this.ErrorMessage = defaultInterpolatedStringHandler7.ToStringAndClear();
					return;
				}
				if (this.bitness == 32)
				{
					this.EncoderFlags |= EncoderFlags.P67;
				}
				this.DisplSize = DisplSize.Size2;
				if (instruction.MemoryDisplacement64 > 65535UL)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler8 = new DefaultInterpolatedStringHandler(43, 1);
					defaultInterpolatedStringHandler8.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler8.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler8.AppendLiteral(": Displacement must fit in a ushort");
					this.ErrorMessage = defaultInterpolatedStringHandler8.ToStringAndClear();
					return;
				}
				this.Displ = instruction.MemoryDisplacement32;
				return;
			}
		}

		// Token: 0x06002167 RID: 8551 RVA: 0x0006B46C File Offset: 0x0006966C
		internal void AddModRMRegister(in Instruction instruction, int operand, Register regLo, Register regHi)
		{
			if (!this.Verify(operand, OpKind.Register, instruction.GetOpKind(operand)))
			{
				return;
			}
			Register reg = instruction.GetOpRegister(operand);
			if (!this.Verify(operand, reg, regLo, regHi))
			{
				return;
			}
			uint regNum = (uint)(reg - regLo);
			if (regLo == Register.AL)
			{
				if (reg >= Register.SPL)
				{
					regNum -= 4U;
					this.EncoderFlags |= EncoderFlags.REX;
				}
				else if (reg >= Register.AH)
				{
					this.EncoderFlags |= EncoderFlags.HighLegacy8BitRegs;
				}
			}
			this.ModRM |= (byte)((regNum & 7U) << 3);
			this.EncoderFlags |= EncoderFlags.ModRM;
			this.EncoderFlags |= (EncoderFlags)((regNum & 8U) >> 1);
			this.EncoderFlags |= (EncoderFlags)((regNum & 16U) << 5);
		}

		// Token: 0x06002168 RID: 8552 RVA: 0x0006B524 File Offset: 0x00069724
		internal void AddReg(in Instruction instruction, int operand, Register regLo, Register regHi)
		{
			if (!this.Verify(operand, OpKind.Register, instruction.GetOpKind(operand)))
			{
				return;
			}
			Register reg = instruction.GetOpRegister(operand);
			if (!this.Verify(operand, reg, regLo, regHi))
			{
				return;
			}
			uint regNum = (uint)(reg - regLo);
			if (regLo == Register.AL)
			{
				if (reg >= Register.SPL)
				{
					regNum -= 4U;
					this.EncoderFlags |= EncoderFlags.REX;
				}
				else if (reg >= Register.AH)
				{
					this.EncoderFlags |= EncoderFlags.HighLegacy8BitRegs;
				}
			}
			this.OpCode |= regNum & 7U;
			this.EncoderFlags |= (EncoderFlags)(regNum >> 3);
		}

		// Token: 0x06002169 RID: 8553 RVA: 0x0006B5B4 File Offset: 0x000697B4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void AddRegOrMem(in Instruction instruction, int operand, Register regLo, Register regHi, bool allowMemOp, bool allowRegOp)
		{
			this.AddRegOrMem(instruction, operand, regLo, regHi, Register.None, Register.None, allowMemOp, allowRegOp);
		}

		// Token: 0x0600216A RID: 8554 RVA: 0x0006B5D4 File Offset: 0x000697D4
		internal void AddRegOrMem(in Instruction instruction, int operand, Register regLo, Register regHi, Register vsibIndexRegLo, Register vsibIndexRegHi, bool allowMemOp, bool allowRegOp)
		{
			OpKind opKind = instruction.GetOpKind(operand);
			this.EncoderFlags |= EncoderFlags.ModRM;
			if (opKind == OpKind.Register)
			{
				if (!allowRegOp)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler.AppendLiteral(": register operand is not allowed");
					this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
					return;
				}
				Register reg = instruction.GetOpRegister(operand);
				if (!this.Verify(operand, reg, regLo, regHi))
				{
					return;
				}
				uint regNum = (uint)(reg - regLo);
				if (regLo == Register.AL)
				{
					if (reg >= Register.R8L)
					{
						regNum -= 4U;
					}
					else if (reg >= Register.SPL)
					{
						regNum -= 4U;
						this.EncoderFlags |= EncoderFlags.REX;
					}
					else if (reg >= Register.AH)
					{
						this.EncoderFlags |= EncoderFlags.HighLegacy8BitRegs;
					}
				}
				this.ModRM |= (byte)(regNum & 7U);
				this.ModRM |= 192;
				this.EncoderFlags |= (EncoderFlags)((regNum >> 3) & 3U);
				return;
			}
			else
			{
				if (opKind != OpKind.Memory)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(63, 2);
					defaultInterpolatedStringHandler2.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler2.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler2.AppendLiteral(": Expected a register or memory operand, but opKind is ");
					defaultInterpolatedStringHandler2.AppendFormatted<OpKind>(opKind);
					this.ErrorMessage = defaultInterpolatedStringHandler2.ToStringAndClear();
					return;
				}
				if (!allowMemOp)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(39, 1);
					defaultInterpolatedStringHandler3.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler3.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler3.AppendLiteral(": memory operand is not allowed");
					this.ErrorMessage = defaultInterpolatedStringHandler3.ToStringAndClear();
					return;
				}
				if (instruction.MemorySize.IsBroadcast())
				{
					this.EncoderFlags |= EncoderFlags.Broadcast;
				}
				CodeSize codeSize = instruction.CodeSize;
				if (codeSize == CodeSize.Unknown)
				{
					if (this.bitness == 64)
					{
						codeSize = CodeSize.Code64;
					}
					else if (this.bitness == 32)
					{
						codeSize = CodeSize.Code32;
					}
					else
					{
						codeSize = CodeSize.Code16;
					}
				}
				int addrSize = InstructionUtils.GetAddressSizeInBytes(instruction.MemoryBase, instruction.MemoryIndex, instruction.MemoryDisplSize, codeSize) * 8;
				if (addrSize != this.bitness)
				{
					this.EncoderFlags |= EncoderFlags.P67;
				}
				if ((this.EncoderFlags & EncoderFlags.RegIsMemory) != EncoderFlags.None && Encoder.GetRegisterOpSize(instruction) != addrSize)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler4 = new DefaultInterpolatedStringHandler(76, 1);
					defaultInterpolatedStringHandler4.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler4.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler4.AppendLiteral(": Register operand size must equal memory addressing mode (16/32/64)");
					this.ErrorMessage = defaultInterpolatedStringHandler4.ToStringAndClear();
					return;
				}
				if (addrSize != 16)
				{
					this.AddMemOp(instruction, operand, addrSize, vsibIndexRegLo, vsibIndexRegHi);
					return;
				}
				if (vsibIndexRegLo != Register.None)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler5 = new DefaultInterpolatedStringHandler(91, 1);
					defaultInterpolatedStringHandler5.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler5.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler5.AppendLiteral(": VSIB operands can't use 16-bit addressing. It must be 32-bit or 64-bit addressing");
					this.ErrorMessage = defaultInterpolatedStringHandler5.ToStringAndClear();
					return;
				}
				this.AddMemOp16(instruction, operand);
				return;
			}
		}

		// Token: 0x0600216B RID: 8555 RVA: 0x0006B880 File Offset: 0x00069A80
		private static int GetRegisterOpSize(in Instruction instruction)
		{
			if (instruction.Op0Kind == OpKind.Register)
			{
				Register reg = instruction.Op0Register;
				if (reg.IsGPR64())
				{
					return 64;
				}
				if (reg.IsGPR32())
				{
					return 32;
				}
				if (reg.IsGPR16())
				{
					return 16;
				}
			}
			return 0;
		}

		// Token: 0x0600216C RID: 8556 RVA: 0x0006B8C0 File Offset: 0x00069AC0
		private bool TryConvertToDisp8N(in Instruction instruction, int displ, out sbyte compressedValue)
		{
			TryConvertToDisp8N tryConvertToDisp8N = this.handler.TryConvertToDisp8N;
			if (tryConvertToDisp8N != null)
			{
				return tryConvertToDisp8N(this, this.handler, instruction, displ, out compressedValue);
			}
			if (-128 <= displ && displ <= 127)
			{
				compressedValue = (sbyte)displ;
				return true;
			}
			compressedValue = 0;
			return false;
		}

		// Token: 0x0600216D RID: 8557 RVA: 0x0006B904 File Offset: 0x00069B04
		private void AddMemOp16(in Instruction instruction, int operand)
		{
			if (this.bitness == 64)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(56, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(": 16-bit addressing can't be used by 64-bit code");
				this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
			Register baseReg = instruction.MemoryBase;
			Register indexReg = instruction.MemoryIndex;
			int displSize = instruction.MemoryDisplSize;
			if (baseReg != Register.BX || indexReg != Register.SI)
			{
				if (baseReg == Register.BX && indexReg == Register.DI)
				{
					this.ModRM |= 1;
				}
				else if (baseReg == Register.BP && indexReg == Register.SI)
				{
					this.ModRM |= 2;
				}
				else if (baseReg == Register.BP && indexReg == Register.DI)
				{
					this.ModRM |= 3;
				}
				else if (baseReg == Register.SI && indexReg == Register.None)
				{
					this.ModRM |= 4;
				}
				else if (baseReg == Register.DI && indexReg == Register.None)
				{
					this.ModRM |= 5;
				}
				else if (baseReg == Register.BP && indexReg == Register.None)
				{
					this.ModRM |= 6;
				}
				else if (baseReg == Register.BX && indexReg == Register.None)
				{
					this.ModRM |= 7;
				}
				else
				{
					if (baseReg != Register.None || indexReg != Register.None)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(62, 3);
						defaultInterpolatedStringHandler2.AppendLiteral("Operand ");
						defaultInterpolatedStringHandler2.AppendFormatted<int>(operand);
						defaultInterpolatedStringHandler2.AppendLiteral(": Invalid 16-bit base + index registers: base=");
						defaultInterpolatedStringHandler2.AppendFormatted<Register>(baseReg);
						defaultInterpolatedStringHandler2.AppendLiteral(", index=");
						defaultInterpolatedStringHandler2.AppendFormatted<Register>(indexReg);
						this.ErrorMessage = defaultInterpolatedStringHandler2.ToStringAndClear();
						return;
					}
					this.ModRM |= 6;
					this.DisplSize = DisplSize.Size2;
					if (instruction.MemoryDisplacement64 > 65535UL)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(43, 1);
						defaultInterpolatedStringHandler3.AppendLiteral("Operand ");
						defaultInterpolatedStringHandler3.AppendFormatted<int>(operand);
						defaultInterpolatedStringHandler3.AppendLiteral(": Displacement must fit in a ushort");
						this.ErrorMessage = defaultInterpolatedStringHandler3.ToStringAndClear();
						return;
					}
					this.Displ = instruction.MemoryDisplacement32;
				}
			}
			if (baseReg != Register.None || indexReg != Register.None)
			{
				if (instruction.MemoryDisplacement64 < 18446744073709518848UL || instruction.MemoryDisplacement64 > 65535UL)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler4 = new DefaultInterpolatedStringHandler(54, 1);
					defaultInterpolatedStringHandler4.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler4.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler4.AppendLiteral(": Displacement must fit in a short or a ushort");
					this.ErrorMessage = defaultInterpolatedStringHandler4.ToStringAndClear();
					return;
				}
				this.Displ = instruction.MemoryDisplacement32;
				if (displSize == 0 && baseReg == Register.BP && indexReg == Register.None)
				{
					displSize = 1;
					if (this.Displ != 0U)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler5 = new DefaultInterpolatedStringHandler(50, 1);
						defaultInterpolatedStringHandler5.AppendLiteral("Operand ");
						defaultInterpolatedStringHandler5.AppendFormatted<int>(operand);
						defaultInterpolatedStringHandler5.AppendLiteral(": Displacement must be 0 if displSize == 0");
						this.ErrorMessage = defaultInterpolatedStringHandler5.ToStringAndClear();
						return;
					}
				}
				if (displSize == 1)
				{
					sbyte compressedValue;
					if (this.TryConvertToDisp8N(instruction, (int)((short)this.Displ), out compressedValue))
					{
						this.Displ = (uint)compressedValue;
					}
					else
					{
						displSize = 2;
					}
				}
				if (displSize == 0)
				{
					if (this.Displ != 0U)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler6 = new DefaultInterpolatedStringHandler(50, 1);
						defaultInterpolatedStringHandler6.AppendLiteral("Operand ");
						defaultInterpolatedStringHandler6.AppendFormatted<int>(operand);
						defaultInterpolatedStringHandler6.AppendLiteral(": Displacement must be 0 if displSize == 0");
						this.ErrorMessage = defaultInterpolatedStringHandler6.ToStringAndClear();
						return;
					}
				}
				else if (displSize == 1)
				{
					if (this.Displ < 4294967168U || this.Displ > 127U)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler7 = new DefaultInterpolatedStringHandler(43, 1);
						defaultInterpolatedStringHandler7.AppendLiteral("Operand ");
						defaultInterpolatedStringHandler7.AppendFormatted<int>(operand);
						defaultInterpolatedStringHandler7.AppendLiteral(": Displacement must fit in an sbyte");
						this.ErrorMessage = defaultInterpolatedStringHandler7.ToStringAndClear();
						return;
					}
					this.ModRM |= 64;
					this.DisplSize = DisplSize.Size1;
					return;
				}
				else
				{
					if (displSize == 2)
					{
						this.ModRM |= 128;
						this.DisplSize = DisplSize.Size2;
						return;
					}
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler8 = new DefaultInterpolatedStringHandler(57, 2);
					defaultInterpolatedStringHandler8.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler8.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler8.AppendLiteral(": Invalid displacement size: ");
					defaultInterpolatedStringHandler8.AppendFormatted<int>(displSize);
					defaultInterpolatedStringHandler8.AppendLiteral(", must be 0, 1, or 2");
					this.ErrorMessage = defaultInterpolatedStringHandler8.ToStringAndClear();
					return;
				}
			}
		}

		// Token: 0x0600216E RID: 8558 RVA: 0x0006BCF4 File Offset: 0x00069EF4
		private void AddMemOp(in Instruction instruction, int operand, int addrSize, Register vsibIndexRegLo, Register vsibIndexRegHi)
		{
			if (this.bitness != 64 && addrSize == 64)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler.AppendLiteral(": 64-bit addressing can only be used in 64-bit mode");
				this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				return;
			}
			Register baseReg = instruction.MemoryBase;
			Register indexReg = instruction.MemoryIndex;
			int displSize = instruction.MemoryDisplSize;
			Register baseRegLo;
			Register baseRegHi;
			if (addrSize == 64)
			{
				baseRegLo = Register.RAX;
				baseRegHi = Register.R15;
			}
			else
			{
				baseRegLo = Register.EAX;
				baseRegHi = Register.R15D;
			}
			Register indexRegLo;
			Register indexRegHi;
			if (vsibIndexRegLo != Register.None)
			{
				indexRegLo = vsibIndexRegLo;
				indexRegHi = vsibIndexRegHi;
			}
			else
			{
				indexRegLo = baseRegLo;
				indexRegHi = baseRegHi;
			}
			if (baseReg != Register.None && baseReg != Register.RIP && baseReg != Register.EIP && !this.Verify(operand, baseReg, baseRegLo, baseRegHi))
			{
				return;
			}
			if (indexReg != Register.None && !this.Verify(operand, indexReg, indexRegLo, indexRegHi))
			{
				return;
			}
			if (displSize != 0 && displSize != 1 && displSize != 4 && displSize != 8)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(50, 2);
				defaultInterpolatedStringHandler2.AppendLiteral("Operand ");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(operand);
				defaultInterpolatedStringHandler2.AppendLiteral(": Invalid displ size: ");
				defaultInterpolatedStringHandler2.AppendFormatted<int>(displSize);
				defaultInterpolatedStringHandler2.AppendLiteral(", must be 0, 1, 4, 8");
				this.ErrorMessage = defaultInterpolatedStringHandler2.ToStringAndClear();
				return;
			}
			if (baseReg == Register.RIP || baseReg == Register.EIP)
			{
				if (indexReg != Register.None)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(61, 1);
					defaultInterpolatedStringHandler3.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler3.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler3.AppendLiteral(": RIP relative addressing can't use an index register");
					this.ErrorMessage = defaultInterpolatedStringHandler3.ToStringAndClear();
					return;
				}
				if (instruction.InternalMemoryIndexScale != 0)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler4 = new DefaultInterpolatedStringHandler(51, 1);
					defaultInterpolatedStringHandler4.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler4.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler4.AppendLiteral(": RIP relative addressing must use scale *1");
					this.ErrorMessage = defaultInterpolatedStringHandler4.ToStringAndClear();
					return;
				}
				if (this.bitness != 64)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler5 = new DefaultInterpolatedStringHandler(70, 1);
					defaultInterpolatedStringHandler5.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler5.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler5.AppendLiteral(": RIP/EIP relative addressing is only available in 64-bit mode");
					this.ErrorMessage = defaultInterpolatedStringHandler5.ToStringAndClear();
					return;
				}
				if ((this.EncoderFlags & EncoderFlags.MustUseSib) != EncoderFlags.None)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler6 = new DefaultInterpolatedStringHandler(53, 1);
					defaultInterpolatedStringHandler6.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler6.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler6.AppendLiteral(": RIP/EIP relative addressing isn't supported");
					this.ErrorMessage = defaultInterpolatedStringHandler6.ToStringAndClear();
					return;
				}
				this.ModRM |= 5;
				ulong target = instruction.MemoryDisplacement64;
				if (baseReg == Register.RIP)
				{
					this.DisplSize = DisplSize.RipRelSize4_Target64;
					this.Displ = (uint)target;
					this.DisplHi = (uint)(target >> 32);
					return;
				}
				this.DisplSize = DisplSize.RipRelSize4_Target32;
				if (target > (ulong)(-1))
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler7 = new DefaultInterpolatedStringHandler(51, 2);
					defaultInterpolatedStringHandler7.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler7.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler7.AppendLiteral(": Target address doesn't fit in 32 bits: 0x");
					defaultInterpolatedStringHandler7.AppendFormatted<ulong>(target, "X");
					this.ErrorMessage = defaultInterpolatedStringHandler7.ToStringAndClear();
					return;
				}
				this.Displ = (uint)target;
				return;
			}
			else
			{
				int scale = instruction.InternalMemoryIndexScale;
				this.Displ = instruction.MemoryDisplacement32;
				if (addrSize == 64)
				{
					if (instruction.MemoryDisplacement64 < 18446744071562067968UL || instruction.MemoryDisplacement64 > 2147483647UL)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler8 = new DefaultInterpolatedStringHandler(41, 1);
						defaultInterpolatedStringHandler8.AppendLiteral("Operand ");
						defaultInterpolatedStringHandler8.AppendFormatted<int>(operand);
						defaultInterpolatedStringHandler8.AppendLiteral(": Displacement must fit in an int");
						this.ErrorMessage = defaultInterpolatedStringHandler8.ToStringAndClear();
						return;
					}
				}
				else if (instruction.MemoryDisplacement64 < 18446744071562067968UL || instruction.MemoryDisplacement64 > (ulong)(-1))
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler9 = new DefaultInterpolatedStringHandler(51, 1);
					defaultInterpolatedStringHandler9.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler9.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler9.AppendLiteral(": Displacement must fit in an int or a uint");
					this.ErrorMessage = defaultInterpolatedStringHandler9.ToStringAndClear();
					return;
				}
				if (baseReg != Register.None || indexReg != Register.None)
				{
					int baseNum = ((baseReg == Register.None) ? (-1) : (baseReg - baseRegLo));
					int indexNum = ((indexReg == Register.None) ? (-1) : (indexReg - indexRegLo));
					if (displSize == 0 && (baseNum & 7) == 5)
					{
						displSize = 1;
						if (this.Displ != 0U)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler10 = new DefaultInterpolatedStringHandler(50, 1);
							defaultInterpolatedStringHandler10.AppendLiteral("Operand ");
							defaultInterpolatedStringHandler10.AppendFormatted<int>(operand);
							defaultInterpolatedStringHandler10.AppendLiteral(": Displacement must be 0 if displSize == 0");
							this.ErrorMessage = defaultInterpolatedStringHandler10.ToStringAndClear();
							return;
						}
					}
					if (displSize == 1)
					{
						sbyte compressedValue;
						if (this.TryConvertToDisp8N(instruction, (int)this.Displ, out compressedValue))
						{
							this.Displ = (uint)compressedValue;
						}
						else
						{
							displSize = addrSize / 8;
						}
					}
					if (baseReg == Register.None)
					{
						this.DisplSize = DisplSize.Size4;
					}
					else if (displSize == 1)
					{
						if (this.Displ < 4294967168U || this.Displ > 127U)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler11 = new DefaultInterpolatedStringHandler(43, 1);
							defaultInterpolatedStringHandler11.AppendLiteral("Operand ");
							defaultInterpolatedStringHandler11.AppendFormatted<int>(operand);
							defaultInterpolatedStringHandler11.AppendLiteral(": Displacement must fit in an sbyte");
							this.ErrorMessage = defaultInterpolatedStringHandler11.ToStringAndClear();
							return;
						}
						this.ModRM |= 64;
						this.DisplSize = DisplSize.Size1;
					}
					else if (displSize == addrSize / 8)
					{
						this.ModRM |= 128;
						this.DisplSize = DisplSize.Size4;
					}
					else
					{
						if (displSize != 0)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler12 = new DefaultInterpolatedStringHandler(24, 2);
							defaultInterpolatedStringHandler12.AppendLiteral("Operand ");
							defaultInterpolatedStringHandler12.AppendFormatted<int>(operand);
							defaultInterpolatedStringHandler12.AppendLiteral(": Invalid ");
							defaultInterpolatedStringHandler12.AppendFormatted("MemoryDisplSize");
							defaultInterpolatedStringHandler12.AppendLiteral(" value");
							this.ErrorMessage = defaultInterpolatedStringHandler12.ToStringAndClear();
							return;
						}
						if (this.Displ != 0U)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler13 = new DefaultInterpolatedStringHandler(50, 1);
							defaultInterpolatedStringHandler13.AppendLiteral("Operand ");
							defaultInterpolatedStringHandler13.AppendFormatted<int>(operand);
							defaultInterpolatedStringHandler13.AppendLiteral(": Displacement must be 0 if displSize == 0");
							this.ErrorMessage = defaultInterpolatedStringHandler13.ToStringAndClear();
							return;
						}
					}
					if (indexReg == Register.None && (baseNum & 7) != 4 && scale == 0 && (this.EncoderFlags & EncoderFlags.MustUseSib) == EncoderFlags.None)
					{
						this.ModRM |= (byte)(baseNum & 7);
					}
					else
					{
						this.EncoderFlags |= EncoderFlags.Sib;
						this.Sib = (byte)(scale << 6);
						this.ModRM |= 4;
						if (indexReg == Register.RSP || indexReg == Register.ESP)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler14 = new DefaultInterpolatedStringHandler(52, 1);
							defaultInterpolatedStringHandler14.AppendLiteral("Operand ");
							defaultInterpolatedStringHandler14.AppendFormatted<int>(operand);
							defaultInterpolatedStringHandler14.AppendLiteral(": ESP/RSP can't be used as an index register");
							this.ErrorMessage = defaultInterpolatedStringHandler14.ToStringAndClear();
							return;
						}
						if (baseNum < 0)
						{
							this.Sib |= 5;
						}
						else
						{
							this.Sib |= (byte)(baseNum & 7);
						}
						if (indexNum < 0)
						{
							this.Sib |= 32;
						}
						else
						{
							this.Sib |= (byte)((indexNum & 7) << 3);
						}
					}
					if (baseNum >= 0)
					{
						this.EncoderFlags |= (EncoderFlags)(baseNum >> 3);
					}
					if (indexNum >= 0)
					{
						this.EncoderFlags |= (EncoderFlags)((indexNum >> 2) & 2);
						this.EncoderFlags |= (EncoderFlags)((indexNum & 16) << 27);
					}
					return;
				}
				if (vsibIndexRegLo != Register.None)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler15 = new DefaultInterpolatedStringHandler(58, 1);
					defaultInterpolatedStringHandler15.AppendLiteral("Operand ");
					defaultInterpolatedStringHandler15.AppendFormatted<int>(operand);
					defaultInterpolatedStringHandler15.AppendLiteral(": VSIB addressing can't use an offset-only address");
					this.ErrorMessage = defaultInterpolatedStringHandler15.ToStringAndClear();
					return;
				}
				if (this.bitness == 64 || scale != 0 || (this.EncoderFlags & EncoderFlags.MustUseSib) != EncoderFlags.None)
				{
					this.ModRM |= 4;
					this.DisplSize = DisplSize.Size4;
					this.EncoderFlags |= EncoderFlags.Sib;
					this.Sib = (byte)(37 | (scale << 6));
					return;
				}
				this.ModRM |= 5;
				this.DisplSize = DisplSize.Size4;
				return;
			}
		}

		// Token: 0x17000778 RID: 1912
		// (get) Token: 0x0600216F RID: 8559 RVA: 0x0006C417 File Offset: 0x0006A617
		[Nullable(0)]
		private unsafe static ReadOnlySpan<byte> SegmentOverrides
		{
			get
			{
				return new ReadOnlySpan<byte>((void*)(&<b37590d4-39fb-478a-88de-d293f3364852><PrivateImplementationDetails>.776E6876E834221736FF27BADB982C51F52B6A7D95C340DB983274E3E09E9C3D), 6);
			}
		}

		// Token: 0x06002170 RID: 8560 RVA: 0x0006C424 File Offset: 0x0006A624
		internal unsafe void WritePrefixes(in Instruction instruction, bool canWriteF3 = true)
		{
			Register seg = instruction.SegmentPrefix;
			if (seg != Register.None)
			{
				this.WriteByteInternal((uint)(*Encoder.SegmentOverrides[seg - Register.ES]));
			}
			if ((this.EncoderFlags & EncoderFlags.PF0) != EncoderFlags.None || instruction.HasLockPrefix)
			{
				this.WriteByteInternal(240U);
			}
			if ((this.EncoderFlags & EncoderFlags.P66) != EncoderFlags.None)
			{
				this.WriteByteInternal(102U);
			}
			if ((this.EncoderFlags & EncoderFlags.P67) != EncoderFlags.None)
			{
				this.WriteByteInternal(103U);
			}
			if (canWriteF3 && instruction.HasRepePrefix)
			{
				this.WriteByteInternal(243U);
			}
			if (instruction.HasRepnePrefix)
			{
				this.WriteByteInternal(242U);
			}
		}

		// Token: 0x06002171 RID: 8561 RVA: 0x0006C4CC File Offset: 0x0006A6CC
		private void WriteModRM()
		{
			if ((this.EncoderFlags & EncoderFlags.ModRM) != EncoderFlags.None)
			{
				this.WriteByteInternal((uint)this.ModRM);
				if ((this.EncoderFlags & EncoderFlags.Sib) != EncoderFlags.None)
				{
					this.WriteByteInternal((uint)this.Sib);
				}
			}
			this.displAddr = (uint)this.currentRip;
			switch (this.DisplSize)
			{
			case DisplSize.None:
				return;
			case DisplSize.Size1:
				this.WriteByteInternal(this.Displ);
				return;
			case DisplSize.Size2:
			{
				uint diff4 = this.Displ;
				this.WriteByteInternal(diff4);
				this.WriteByteInternal(diff4 >> 8);
				return;
			}
			case DisplSize.Size4:
			{
				uint diff4 = this.Displ;
				this.WriteByteInternal(diff4);
				this.WriteByteInternal(diff4 >> 8);
				this.WriteByteInternal(diff4 >> 16);
				this.WriteByteInternal(diff4 >> 24);
				return;
			}
			case DisplSize.Size8:
			{
				uint diff4 = this.Displ;
				this.WriteByteInternal(diff4);
				this.WriteByteInternal(diff4 >> 8);
				this.WriteByteInternal(diff4 >> 16);
				this.WriteByteInternal(diff4 >> 24);
				diff4 = this.DisplHi;
				this.WriteByteInternal(diff4);
				this.WriteByteInternal(diff4 >> 8);
				this.WriteByteInternal(diff4 >> 16);
				this.WriteByteInternal(diff4 >> 24);
				return;
			}
			case DisplSize.RipRelSize4_Target32:
			{
				uint eip = (uint)this.currentRip + 4U + this.immSizes[(int)this.ImmSize];
				uint diff4 = this.Displ - eip;
				this.WriteByteInternal(diff4);
				this.WriteByteInternal(diff4 >> 8);
				this.WriteByteInternal(diff4 >> 16);
				this.WriteByteInternal(diff4 >> 24);
				return;
			}
			case DisplSize.RipRelSize4_Target64:
			{
				ulong rip = this.currentRip + 4UL + (ulong)this.immSizes[(int)this.ImmSize];
				long diff5 = (long)((((ulong)this.DisplHi << 32) | (ulong)this.Displ) - rip);
				if (diff5 < -2147483648L || diff5 > 2147483647L)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(96, 4);
					defaultInterpolatedStringHandler.AppendLiteral("RIP relative distance is too far away: NextIP: 0x");
					defaultInterpolatedStringHandler.AppendFormatted<ulong>(rip, "X16");
					defaultInterpolatedStringHandler.AppendLiteral(" target: 0x");
					defaultInterpolatedStringHandler.AppendFormatted<uint>(this.DisplHi, "X8");
					defaultInterpolatedStringHandler.AppendFormatted<uint>(this.Displ, "X8");
					defaultInterpolatedStringHandler.AppendLiteral(", diff = ");
					defaultInterpolatedStringHandler.AppendFormatted<long>(diff5);
					defaultInterpolatedStringHandler.AppendLiteral(", diff must fit in an Int32");
					this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				}
				uint diff4 = (uint)diff5;
				this.WriteByteInternal(diff4);
				this.WriteByteInternal(diff4 >> 8);
				this.WriteByteInternal(diff4 >> 16);
				this.WriteByteInternal(diff4 >> 24);
				return;
			}
			default:
				throw new InvalidOperationException();
			}
		}

		// Token: 0x06002172 RID: 8562 RVA: 0x0006C724 File Offset: 0x0006A924
		private void WriteImmediate()
		{
			this.immAddr = (uint)this.currentRip;
			switch (this.ImmSize)
			{
			case ImmSize.None:
				return;
			case ImmSize.Size1:
			case ImmSize.SizeIbReg:
			case ImmSize.Size1OpCode:
				this.WriteByteInternal(this.Immediate);
				return;
			case ImmSize.Size2:
			{
				uint value = this.Immediate;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				return;
			}
			case ImmSize.Size4:
			{
				uint value = this.Immediate;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				this.WriteByteInternal(value >> 16);
				this.WriteByteInternal(value >> 24);
				return;
			}
			case ImmSize.Size8:
			{
				uint value = this.Immediate;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				this.WriteByteInternal(value >> 16);
				this.WriteByteInternal(value >> 24);
				value = this.ImmediateHi;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				this.WriteByteInternal(value >> 16);
				this.WriteByteInternal(value >> 24);
				return;
			}
			case ImmSize.Size2_1:
			{
				uint value = this.Immediate;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				this.WriteByteInternal(this.ImmediateHi);
				return;
			}
			case ImmSize.Size1_1:
				this.WriteByteInternal(this.Immediate);
				this.WriteByteInternal(this.ImmediateHi);
				return;
			case ImmSize.Size2_2:
			{
				uint value = this.Immediate;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				value = this.ImmediateHi;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				return;
			}
			case ImmSize.Size4_2:
			{
				uint value = this.Immediate;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				this.WriteByteInternal(value >> 16);
				this.WriteByteInternal(value >> 24);
				value = this.ImmediateHi;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				return;
			}
			case ImmSize.RipRelSize1_Target16:
			{
				ushort ip = (ushort)((uint)this.currentRip + 1U);
				short diff2 = (short)this.Immediate - (short)ip;
				if (diff2 < -128 || diff2 > 127)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(89, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Branch distance is too far away: NextIP: 0x");
					defaultInterpolatedStringHandler.AppendFormatted<ushort>(ip, "X4");
					defaultInterpolatedStringHandler.AppendLiteral(" target: 0x");
					defaultInterpolatedStringHandler.AppendFormatted<ushort>((ushort)this.Immediate, "X4");
					defaultInterpolatedStringHandler.AppendLiteral(", diff = ");
					defaultInterpolatedStringHandler.AppendFormatted<short>(diff2);
					defaultInterpolatedStringHandler.AppendLiteral(", diff must fit in an Int8");
					this.ErrorMessage = defaultInterpolatedStringHandler.ToStringAndClear();
				}
				this.WriteByteInternal((uint)diff2);
				return;
			}
			case ImmSize.RipRelSize1_Target32:
			{
				uint eip = (uint)this.currentRip + 1U;
				int diff3 = (int)(this.Immediate - eip);
				if (diff3 < -128 || diff3 > 127)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(89, 3);
					defaultInterpolatedStringHandler2.AppendLiteral("Branch distance is too far away: NextIP: 0x");
					defaultInterpolatedStringHandler2.AppendFormatted<uint>(eip, "X8");
					defaultInterpolatedStringHandler2.AppendLiteral(" target: 0x");
					defaultInterpolatedStringHandler2.AppendFormatted<uint>(this.Immediate, "X8");
					defaultInterpolatedStringHandler2.AppendLiteral(", diff = ");
					defaultInterpolatedStringHandler2.AppendFormatted<int>(diff3);
					defaultInterpolatedStringHandler2.AppendLiteral(", diff must fit in an Int8");
					this.ErrorMessage = defaultInterpolatedStringHandler2.ToStringAndClear();
				}
				this.WriteByteInternal((uint)diff3);
				return;
			}
			case ImmSize.RipRelSize1_Target64:
			{
				ulong rip = this.currentRip + 1UL;
				long diff4 = (long)((((ulong)this.ImmediateHi << 32) | (ulong)this.Immediate) - rip);
				if (diff4 < -128L || diff4 > 127L)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler3 = new DefaultInterpolatedStringHandler(89, 4);
					defaultInterpolatedStringHandler3.AppendLiteral("Branch distance is too far away: NextIP: 0x");
					defaultInterpolatedStringHandler3.AppendFormatted<ulong>(rip, "X16");
					defaultInterpolatedStringHandler3.AppendLiteral(" target: 0x");
					defaultInterpolatedStringHandler3.AppendFormatted<uint>(this.ImmediateHi, "X8");
					defaultInterpolatedStringHandler3.AppendFormatted<uint>(this.Immediate, "X8");
					defaultInterpolatedStringHandler3.AppendLiteral(", diff = ");
					defaultInterpolatedStringHandler3.AppendFormatted<long>(diff4);
					defaultInterpolatedStringHandler3.AppendLiteral(", diff must fit in an Int8");
					this.ErrorMessage = defaultInterpolatedStringHandler3.ToStringAndClear();
				}
				this.WriteByteInternal((uint)diff4);
				return;
			}
			case ImmSize.RipRelSize2_Target16:
			{
				uint eip = (uint)this.currentRip + 2U;
				uint value = this.Immediate - eip;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				return;
			}
			case ImmSize.RipRelSize2_Target32:
			{
				uint eip = (uint)this.currentRip + 2U;
				int diff3 = (int)(this.Immediate - eip);
				if (diff3 < -32768 || diff3 > 32767)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler4 = new DefaultInterpolatedStringHandler(90, 3);
					defaultInterpolatedStringHandler4.AppendLiteral("Branch distance is too far away: NextIP: 0x");
					defaultInterpolatedStringHandler4.AppendFormatted<uint>(eip, "X8");
					defaultInterpolatedStringHandler4.AppendLiteral(" target: 0x");
					defaultInterpolatedStringHandler4.AppendFormatted<uint>(this.Immediate, "X8");
					defaultInterpolatedStringHandler4.AppendLiteral(", diff = ");
					defaultInterpolatedStringHandler4.AppendFormatted<int>(diff3);
					defaultInterpolatedStringHandler4.AppendLiteral(", diff must fit in an Int16");
					this.ErrorMessage = defaultInterpolatedStringHandler4.ToStringAndClear();
				}
				uint value = (uint)diff3;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				return;
			}
			case ImmSize.RipRelSize2_Target64:
			{
				ulong rip = this.currentRip + 2UL;
				long diff4 = (long)((((ulong)this.ImmediateHi << 32) | (ulong)this.Immediate) - rip);
				if (diff4 < -32768L || diff4 > 32767L)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler5 = new DefaultInterpolatedStringHandler(90, 4);
					defaultInterpolatedStringHandler5.AppendLiteral("Branch distance is too far away: NextIP: 0x");
					defaultInterpolatedStringHandler5.AppendFormatted<ulong>(rip, "X16");
					defaultInterpolatedStringHandler5.AppendLiteral(" target: 0x");
					defaultInterpolatedStringHandler5.AppendFormatted<uint>(this.ImmediateHi, "X8");
					defaultInterpolatedStringHandler5.AppendFormatted<uint>(this.Immediate, "X8");
					defaultInterpolatedStringHandler5.AppendLiteral(", diff = ");
					defaultInterpolatedStringHandler5.AppendFormatted<long>(diff4);
					defaultInterpolatedStringHandler5.AppendLiteral(", diff must fit in an Int16");
					this.ErrorMessage = defaultInterpolatedStringHandler5.ToStringAndClear();
				}
				uint value = (uint)diff4;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				return;
			}
			case ImmSize.RipRelSize4_Target32:
			{
				uint eip = (uint)this.currentRip + 4U;
				uint value = this.Immediate - eip;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				this.WriteByteInternal(value >> 16);
				this.WriteByteInternal(value >> 24);
				return;
			}
			case ImmSize.RipRelSize4_Target64:
			{
				ulong rip = this.currentRip + 4UL;
				long diff4 = (long)((((ulong)this.ImmediateHi << 32) | (ulong)this.Immediate) - rip);
				if (diff4 < -2147483648L || diff4 > 2147483647L)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler6 = new DefaultInterpolatedStringHandler(90, 4);
					defaultInterpolatedStringHandler6.AppendLiteral("Branch distance is too far away: NextIP: 0x");
					defaultInterpolatedStringHandler6.AppendFormatted<ulong>(rip, "X16");
					defaultInterpolatedStringHandler6.AppendLiteral(" target: 0x");
					defaultInterpolatedStringHandler6.AppendFormatted<uint>(this.ImmediateHi, "X8");
					defaultInterpolatedStringHandler6.AppendFormatted<uint>(this.Immediate, "X8");
					defaultInterpolatedStringHandler6.AppendLiteral(", diff = ");
					defaultInterpolatedStringHandler6.AppendFormatted<long>(diff4);
					defaultInterpolatedStringHandler6.AppendLiteral(", diff must fit in an Int32");
					this.ErrorMessage = defaultInterpolatedStringHandler6.ToStringAndClear();
				}
				uint value = (uint)diff4;
				this.WriteByteInternal(value);
				this.WriteByteInternal(value >> 8);
				this.WriteByteInternal(value >> 16);
				this.WriteByteInternal(value >> 24);
				return;
			}
			default:
				throw new InvalidOperationException();
			}
		}

		// Token: 0x06002173 RID: 8563 RVA: 0x0006CDD0 File Offset: 0x0006AFD0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteByte(byte value)
		{
			this.WriteByteInternal((uint)value);
		}

		// Token: 0x06002174 RID: 8564 RVA: 0x0006CDD9 File Offset: 0x0006AFD9
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void WriteByteInternal(uint value)
		{
			this.writer.WriteByte((byte)value);
			this.currentRip += 1UL;
		}

		// Token: 0x06002175 RID: 8565 RVA: 0x0006CDF8 File Offset: 0x0006AFF8
		public ConstantOffsets GetConstantOffsets()
		{
			ConstantOffsets constantOffsets = default(ConstantOffsets);
			switch (this.DisplSize)
			{
			case DisplSize.None:
				break;
			case DisplSize.Size1:
				constantOffsets.DisplacementSize = 1;
				constantOffsets.DisplacementOffset = (byte)(this.displAddr - this.eip);
				break;
			case DisplSize.Size2:
				constantOffsets.DisplacementSize = 2;
				constantOffsets.DisplacementOffset = (byte)(this.displAddr - this.eip);
				break;
			case DisplSize.Size4:
			case DisplSize.RipRelSize4_Target32:
			case DisplSize.RipRelSize4_Target64:
				constantOffsets.DisplacementSize = 4;
				constantOffsets.DisplacementOffset = (byte)(this.displAddr - this.eip);
				break;
			case DisplSize.Size8:
				constantOffsets.DisplacementSize = 8;
				constantOffsets.DisplacementOffset = (byte)(this.displAddr - this.eip);
				break;
			default:
				throw new InvalidOperationException();
			}
			switch (this.ImmSize)
			{
			case ImmSize.None:
			case ImmSize.SizeIbReg:
			case ImmSize.Size1OpCode:
				break;
			case ImmSize.Size1:
			case ImmSize.RipRelSize1_Target16:
			case ImmSize.RipRelSize1_Target32:
			case ImmSize.RipRelSize1_Target64:
				constantOffsets.ImmediateSize = 1;
				constantOffsets.ImmediateOffset = (byte)(this.immAddr - this.eip);
				break;
			case ImmSize.Size2:
			case ImmSize.RipRelSize2_Target16:
			case ImmSize.RipRelSize2_Target32:
			case ImmSize.RipRelSize2_Target64:
				constantOffsets.ImmediateSize = 2;
				constantOffsets.ImmediateOffset = (byte)(this.immAddr - this.eip);
				break;
			case ImmSize.Size4:
			case ImmSize.RipRelSize4_Target32:
			case ImmSize.RipRelSize4_Target64:
				constantOffsets.ImmediateSize = 4;
				constantOffsets.ImmediateOffset = (byte)(this.immAddr - this.eip);
				break;
			case ImmSize.Size8:
				constantOffsets.ImmediateSize = 8;
				constantOffsets.ImmediateOffset = (byte)(this.immAddr - this.eip);
				break;
			case ImmSize.Size2_1:
				constantOffsets.ImmediateSize = 2;
				constantOffsets.ImmediateOffset = (byte)(this.immAddr - this.eip);
				constantOffsets.ImmediateSize2 = 1;
				constantOffsets.ImmediateOffset2 = (byte)(this.immAddr - this.eip + 2U);
				break;
			case ImmSize.Size1_1:
				constantOffsets.ImmediateSize = 1;
				constantOffsets.ImmediateOffset = (byte)(this.immAddr - this.eip);
				constantOffsets.ImmediateSize2 = 1;
				constantOffsets.ImmediateOffset2 = (byte)(this.immAddr - this.eip + 1U);
				break;
			case ImmSize.Size2_2:
				constantOffsets.ImmediateSize = 2;
				constantOffsets.ImmediateOffset = (byte)(this.immAddr - this.eip);
				constantOffsets.ImmediateSize2 = 2;
				constantOffsets.ImmediateOffset2 = (byte)(this.immAddr - this.eip + 2U);
				break;
			case ImmSize.Size4_2:
				constantOffsets.ImmediateSize = 4;
				constantOffsets.ImmediateOffset = (byte)(this.immAddr - this.eip);
				constantOffsets.ImmediateSize2 = 2;
				constantOffsets.ImmediateOffset2 = (byte)(this.immAddr - this.eip + 4U);
				break;
			default:
				throw new InvalidOperationException();
			}
			return constantOffsets;
		}

		// Token: 0x04002A5F RID: 10847
		private static readonly uint[] s_immSizes = new uint[]
		{
			0U, 1U, 2U, 4U, 8U, 3U, 2U, 4U, 6U, 1U,
			1U, 1U, 2U, 2U, 2U, 4U, 4U, 1U, 1U
		};

		// Token: 0x04002A60 RID: 10848
		internal uint Internal_PreventVEX2;

		// Token: 0x04002A61 RID: 10849
		internal uint Internal_VEX_WIG_LIG;

		// Token: 0x04002A62 RID: 10850
		internal uint Internal_VEX_LIG;

		// Token: 0x04002A63 RID: 10851
		internal uint Internal_EVEX_WIG;

		// Token: 0x04002A64 RID: 10852
		internal uint Internal_EVEX_LIG;

		// Token: 0x04002A65 RID: 10853
		internal const string ERROR_ONLY_1632_BIT_MODE = "The instruction can only be used in 16/32-bit mode";

		// Token: 0x04002A66 RID: 10854
		internal const string ERROR_ONLY_64_BIT_MODE = "The instruction can only be used in 64-bit mode";

		// Token: 0x04002A67 RID: 10855
		private readonly CodeWriter writer;

		// Token: 0x04002A68 RID: 10856
		private readonly int bitness;

		// Token: 0x04002A69 RID: 10857
		private readonly OpCodeHandler[] handlers;

		// Token: 0x04002A6A RID: 10858
		private readonly uint[] immSizes;

		// Token: 0x04002A6B RID: 10859
		private ulong currentRip;

		// Token: 0x04002A6C RID: 10860
		private string errorMessage;

		// Token: 0x04002A6D RID: 10861
		private OpCodeHandler handler;

		// Token: 0x04002A6E RID: 10862
		private uint eip;

		// Token: 0x04002A6F RID: 10863
		private uint displAddr;

		// Token: 0x04002A70 RID: 10864
		private uint immAddr;

		// Token: 0x04002A71 RID: 10865
		internal uint Immediate;

		// Token: 0x04002A72 RID: 10866
		internal uint ImmediateHi;

		// Token: 0x04002A73 RID: 10867
		private uint Displ;

		// Token: 0x04002A74 RID: 10868
		private uint DisplHi;

		// Token: 0x04002A75 RID: 10869
		private readonly EncoderFlags opSize16Flags;

		// Token: 0x04002A76 RID: 10870
		private readonly EncoderFlags opSize32Flags;

		// Token: 0x04002A77 RID: 10871
		private readonly EncoderFlags adrSize16Flags;

		// Token: 0x04002A78 RID: 10872
		private readonly EncoderFlags adrSize32Flags;

		// Token: 0x04002A79 RID: 10873
		internal uint OpCode;

		// Token: 0x04002A7A RID: 10874
		internal EncoderFlags EncoderFlags;

		// Token: 0x04002A7B RID: 10875
		private DisplSize DisplSize;

		// Token: 0x04002A7C RID: 10876
		internal ImmSize ImmSize;

		// Token: 0x04002A7D RID: 10877
		private byte ModRM;

		// Token: 0x04002A7E RID: 10878
		private byte Sib;
	}
}
