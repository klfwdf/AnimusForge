using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Iced.Intel.DecoderInternal;

namespace Iced.Intel
{
	// Token: 0x0200063A RID: 1594
	internal sealed class Decoder : IEnumerable<Instruction>, IEnumerable
	{
		// Token: 0x1700076A RID: 1898
		// (get) Token: 0x06002116 RID: 8470 RVA: 0x00068D69 File Offset: 0x00066F69
		// (set) Token: 0x06002117 RID: 8471 RVA: 0x00068D71 File Offset: 0x00066F71
		public ulong IP
		{
			get
			{
				return this.instructionPointer;
			}
			set
			{
				this.instructionPointer = value;
			}
		}

		// Token: 0x1700076B RID: 1899
		// (get) Token: 0x06002118 RID: 8472 RVA: 0x00068D7A File Offset: 0x00066F7A
		public int Bitness { get; }

		// Token: 0x06002119 RID: 8473 RVA: 0x00068D84 File Offset: 0x00066F84
		static Decoder()
		{
			OpCodeHandler_Invalid instance = OpCodeHandler_Invalid.Instance;
			ReadOnlySpan<byte> sizesNormal = InstructionMemorySizes.SizesNormal;
			Code[] codeValues = OpCodeHandler_D3NOW.CodeValues;
			ReadOnlySpan<byte> opCount = InstructionOpCounts.OpCount;
			ushort[] toMnemonic = MnemonicUtilsData.toMnemonic;
		}

		// Token: 0x0600211A RID: 8474 RVA: 0x00068E38 File Offset: 0x00067038
		private Decoder(CodeReader reader, ulong ip, DecoderOptions options, int bitness)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			this.reader = reader;
			this.instructionPointer = ip;
			this.options = options;
			this.invalidCheckMask = (((options & DecoderOptions.NoInvalidCheck) == DecoderOptions.None) ? uint.MaxValue : 0U);
			this.memRegs16 = Decoder.s_memRegs16;
			this.Bitness = bitness;
			if (bitness == 64)
			{
				this.is64bMode = true;
				this.defaultCodeSize = CodeSize.Code64;
				this.defaultOperandSize = OpSize.Size32;
				this.defaultInvertedOperandSize = OpSize.Size16;
				this.defaultAddressSize = OpSize.Size64;
				this.defaultInvertedAddressSize = OpSize.Size32;
				this.maskE0 = 224U;
				this.rexMask = 240U;
			}
			else if (bitness == 32)
			{
				this.is64bMode = false;
				this.defaultCodeSize = CodeSize.Code32;
				this.defaultOperandSize = OpSize.Size32;
				this.defaultInvertedOperandSize = OpSize.Size16;
				this.defaultAddressSize = OpSize.Size32;
				this.defaultInvertedAddressSize = OpSize.Size16;
				this.maskE0 = 0U;
				this.rexMask = 0U;
			}
			else
			{
				this.is64bMode = false;
				this.defaultCodeSize = CodeSize.Code16;
				this.defaultOperandSize = OpSize.Size16;
				this.defaultInvertedOperandSize = OpSize.Size32;
				this.defaultAddressSize = OpSize.Size16;
				this.defaultInvertedAddressSize = OpSize.Size32;
				this.maskE0 = 0U;
				this.rexMask = 0U;
			}
			this.is64bMode_and_W = (this.is64bMode ? 128U : 0U);
			this.reg15Mask = (this.is64bMode ? 15U : 7U);
			this.handlers_MAP0 = OpCodeHandlersTables_Legacy.Handlers_MAP0;
			this.handlers_VEX_0F = OpCodeHandlersTables_VEX.Handlers_0F;
			this.handlers_VEX_0F38 = OpCodeHandlersTables_VEX.Handlers_0F38;
			this.handlers_VEX_0F3A = OpCodeHandlersTables_VEX.Handlers_0F3A;
			this.handlers_EVEX_0F = OpCodeHandlersTables_EVEX.Handlers_0F;
			this.handlers_EVEX_0F38 = OpCodeHandlersTables_EVEX.Handlers_0F38;
			this.handlers_EVEX_0F3A = OpCodeHandlersTables_EVEX.Handlers_0F3A;
			this.handlers_EVEX_MAP5 = OpCodeHandlersTables_EVEX.Handlers_MAP5;
			this.handlers_EVEX_MAP6 = OpCodeHandlersTables_EVEX.Handlers_MAP6;
			this.handlers_XOP_MAP8 = OpCodeHandlersTables_XOP.Handlers_MAP8;
			this.handlers_XOP_MAP9 = OpCodeHandlersTables_XOP.Handlers_MAP9;
			this.handlers_XOP_MAP10 = OpCodeHandlersTables_XOP.Handlers_MAP10;
		}

		// Token: 0x0600211B RID: 8475 RVA: 0x00069000 File Offset: 0x00067200
		[NullableContext(1)]
		public static Decoder Create(int bitness, CodeReader reader, ulong ip, DecoderOptions options = DecoderOptions.None)
		{
			if (bitness == 16 || bitness == 32 || bitness == 64)
			{
				return new Decoder(reader, ip, options, bitness);
			}
			throw new ArgumentOutOfRangeException("bitness");
		}

		// Token: 0x0600211C RID: 8476 RVA: 0x00069034 File Offset: 0x00067234
		[NullableContext(1)]
		public static Decoder Create(int bitness, byte[] data, ulong ip, DecoderOptions options = DecoderOptions.None)
		{
			return Decoder.Create(bitness, new ByteArrayCodeReader(data), ip, options);
		}

		// Token: 0x0600211D RID: 8477 RVA: 0x00069044 File Offset: 0x00067244
		[NullableContext(1)]
		public static Decoder Create(int bitness, CodeReader reader, DecoderOptions options = DecoderOptions.None)
		{
			return Decoder.Create(bitness, reader, 0UL, options);
		}

		// Token: 0x0600211E RID: 8478 RVA: 0x00069050 File Offset: 0x00067250
		[NullableContext(1)]
		public static Decoder Create(int bitness, byte[] data, DecoderOptions options = DecoderOptions.None)
		{
			return Decoder.Create(bitness, new ByteArrayCodeReader(data), 0UL, options);
		}

		// Token: 0x0600211F RID: 8479 RVA: 0x00069064 File Offset: 0x00067264
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal uint ReadByte()
		{
			uint instrLen = this.state.zs.instructionLength;
			if (instrLen < 15U)
			{
				uint b = (uint)this.reader.ReadByte();
				if (b <= 255U)
				{
					this.state.zs.instructionLength = instrLen + 1U;
					return b;
				}
				this.state.zs.flags = this.state.zs.flags | StateFlags.NoMoreBytes;
			}
			this.state.zs.flags = this.state.zs.flags | StateFlags.IsInvalid;
			return 0U;
		}

		// Token: 0x06002120 RID: 8480 RVA: 0x000690E0 File Offset: 0x000672E0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal uint ReadUInt16()
		{
			return this.ReadByte() | (this.ReadByte() << 8);
		}

		// Token: 0x06002121 RID: 8481 RVA: 0x000690F1 File Offset: 0x000672F1
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal uint ReadUInt32()
		{
			return this.ReadByte() | (this.ReadByte() << 8) | (this.ReadByte() << 16) | (this.ReadByte() << 24);
		}

		// Token: 0x06002122 RID: 8482 RVA: 0x00069116 File Offset: 0x00067316
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ulong ReadUInt64()
		{
			return (ulong)this.ReadUInt32() | ((ulong)this.ReadUInt32() << 32);
		}

		// Token: 0x1700076C RID: 1900
		// (get) Token: 0x06002123 RID: 8483 RVA: 0x0006912A File Offset: 0x0006732A
		public DecoderError LastError
		{
			get
			{
				if ((this.state.zs.flags & StateFlags.NoMoreBytes) != (StateFlags)0U)
				{
					return DecoderError.NoMoreBytes;
				}
				if ((this.state.zs.flags & StateFlags.IsInvalid) != (StateFlags)0U)
				{
					return DecoderError.InvalidInstruction;
				}
				return DecoderError.None;
			}
		}

		// Token: 0x06002124 RID: 8484 RVA: 0x00069160 File Offset: 0x00067360
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Instruction Decode()
		{
			Instruction instr;
			this.Decode(out instr);
			return instr;
		}

		// Token: 0x06002125 RID: 8485 RVA: 0x00069178 File Offset: 0x00067378
		public void Decode(out Instruction instruction)
		{
			instruction = default(Instruction);
			this.state.zs = default(Decoder.ZState);
			this.state.operandSize = this.defaultOperandSize;
			this.state.addressSize = this.defaultAddressSize;
			uint b = this.ReadByte();
			if ((b & this.rexMask) == 64U)
			{
				StateFlags flags2 = this.state.zs.flags | StateFlags.HasRex;
				if ((b & 8U) != 0U)
				{
					flags2 |= StateFlags.W;
					this.state.operandSize = OpSize.Size64;
				}
				this.state.zs.flags = flags2;
				this.state.zs.extraRegisterBase = (b << 1) & 8U;
				this.state.zs.extraIndexRegisterBase = (b << 2) & 8U;
				this.state.zs.extraBaseRegisterBase = (b << 3) & 8U;
				b = this.ReadByte();
			}
			this.DecodeTable(this.handlers_MAP0[(int)b], ref instruction);
			instruction.InternalCodeSize = this.defaultCodeSize;
			uint instrLen = this.state.zs.instructionLength;
			instruction.Length = (int)instrLen;
			ulong ip = this.instructionPointer;
			ip += (ulong)instrLen;
			this.instructionPointer = ip;
			instruction.NextIP = ip;
			StateFlags flags3 = this.state.zs.flags;
			if ((flags3 & (StateFlags.IpRel64 | StateFlags.IpRel32 | StateFlags.IsInvalid | StateFlags.Lock)) != (StateFlags)0U)
			{
				ulong addr = instruction.MemoryDisplacement64 + ip;
				instruction.MemoryDisplacement64 = addr;
				if ((flags3 & (StateFlags.IpRel64 | StateFlags.IsInvalid | StateFlags.Lock)) == StateFlags.IpRel64)
				{
					return;
				}
				if ((flags3 & StateFlags.IpRel64) == (StateFlags)0U)
				{
					instruction.MemoryDisplacement64 = addr - ip;
				}
				if ((flags3 & StateFlags.IpRel32) != (StateFlags)0U)
				{
					instruction.MemoryDisplacement64 = (ulong)((uint)instruction.MemoryDisplacement64 + (uint)ip);
				}
				if ((flags3 & StateFlags.IsInvalid) != (StateFlags)0U || (flags3 & (StateFlags.Lock | StateFlags.AllowLock) & (StateFlags)this.invalidCheckMask) == StateFlags.Lock)
				{
					instruction = default(Instruction);
					this.state.zs.flags = flags3 | StateFlags.IsInvalid;
					instruction.InternalCodeSize = this.defaultCodeSize;
					instruction.Length = (int)instrLen;
					instruction.NextIP = ip;
				}
			}
		}

		// Token: 0x06002126 RID: 8486 RVA: 0x00069358 File Offset: 0x00067558
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ResetRexPrefixState()
		{
			this.state.zs.flags = this.state.zs.flags & ~(StateFlags.HasRex | StateFlags.W);
			if ((this.state.zs.flags & StateFlags.Has66) == (StateFlags)0U)
			{
				this.state.operandSize = this.defaultOperandSize;
			}
			else
			{
				this.state.operandSize = this.defaultInvertedOperandSize;
			}
			this.state.zs.extraRegisterBase = 0U;
			this.state.zs.extraIndexRegisterBase = 0U;
			this.state.zs.extraBaseRegisterBase = 0U;
		}

		// Token: 0x06002127 RID: 8487 RVA: 0x000693F0 File Offset: 0x000675F0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void CallOpCodeHandlerXXTable(ref Instruction instruction)
		{
			uint b = this.ReadByte();
			this.DecodeTable(this.handlers_MAP0[(int)b], ref instruction);
		}

		// Token: 0x06002128 RID: 8488 RVA: 0x00069413 File Offset: 0x00067613
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal uint GetCurrentInstructionPointer32()
		{
			return (uint)this.instructionPointer + this.state.zs.instructionLength;
		}

		// Token: 0x06002129 RID: 8489 RVA: 0x0006942D File Offset: 0x0006762D
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ulong GetCurrentInstructionPointer64()
		{
			return this.instructionPointer + (ulong)this.state.zs.instructionLength;
		}

		// Token: 0x0600212A RID: 8490 RVA: 0x00069447 File Offset: 0x00067647
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ClearMandatoryPrefix(ref Instruction instruction)
		{
			instruction.InternalClearHasRepeRepnePrefix();
		}

		// Token: 0x0600212B RID: 8491 RVA: 0x00069450 File Offset: 0x00067650
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void SetXacquireXrelease(ref Instruction instruction)
		{
			if (instruction.HasLockPrefix)
			{
				if (this.state.zs.mandatoryPrefix == MandatoryPrefixByte.PF2)
				{
					this.ClearMandatoryPrefixF2(ref instruction);
					instruction.InternalSetHasXacquirePrefix();
					return;
				}
				if (this.state.zs.mandatoryPrefix == MandatoryPrefixByte.PF3)
				{
					this.ClearMandatoryPrefixF3(ref instruction);
					instruction.InternalSetHasXreleasePrefix();
				}
			}
		}

		// Token: 0x0600212C RID: 8492 RVA: 0x000694A6 File Offset: 0x000676A6
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ClearMandatoryPrefixF3(ref Instruction instruction)
		{
			instruction.InternalClearHasRepePrefix();
		}

		// Token: 0x0600212D RID: 8493 RVA: 0x000694AE File Offset: 0x000676AE
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ClearMandatoryPrefixF2(ref Instruction instruction)
		{
			instruction.InternalClearHasRepnePrefix();
		}

		// Token: 0x0600212E RID: 8494 RVA: 0x000694B6 File Offset: 0x000676B6
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void SetInvalidInstruction()
		{
			this.state.zs.flags = this.state.zs.flags | StateFlags.IsInvalid;
		}

		// Token: 0x0600212F RID: 8495 RVA: 0x000694CE File Offset: 0x000676CE
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void DecodeTable(OpCodeHandler[] table, ref Instruction instruction)
		{
			this.DecodeTable(table[(int)this.ReadByte()], ref instruction);
		}

		// Token: 0x06002130 RID: 8496 RVA: 0x000694E0 File Offset: 0x000676E0
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DecodeTable(OpCodeHandler handler, ref Instruction instruction)
		{
			if (handler.HasModRM)
			{
				uint i = this.ReadByte();
				this.state.modrm = i;
				this.state.mod = i >> 6;
				this.state.reg = (i >> 3) & 7U;
				this.state.rm = i & 7U;
			}
			handler.Decode(this, ref instruction);
		}

		// Token: 0x06002131 RID: 8497 RVA: 0x0006953C File Offset: 0x0006773C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ReadModRM()
		{
			uint i = this.ReadByte();
			this.state.modrm = i;
			this.state.mod = i >> 6;
			this.state.reg = (i >> 3) & 7U;
			this.state.rm = i & 7U;
		}

		// Token: 0x06002132 RID: 8498 RVA: 0x00069588 File Offset: 0x00067788
		internal void VEX2(ref Instruction instruction)
		{
			if ((((this.state.zs.flags & StateFlags.HasRex) | (StateFlags)this.state.zs.mandatoryPrefix) & (StateFlags)this.invalidCheckMask) != (StateFlags)0U)
			{
				this.SetInvalidInstruction();
			}
			this.state.zs.flags = this.state.zs.flags & ~StateFlags.W;
			this.state.zs.extraIndexRegisterBase = 0U;
			this.state.zs.extraBaseRegisterBase = 0U;
			uint b = this.state.modrm;
			this.state.vectorLength = (b >> 2) & 1U;
			this.state.zs.mandatoryPrefix = (MandatoryPrefixByte)(b & 3U);
			b = ~b;
			this.state.zs.extraRegisterBase = (b >> 4) & 8U;
			b = (b >> 3) & 15U;
			this.state.vvvv = b;
			this.state.vvvv_invalidCheck = b;
			this.DecodeTable(this.handlers_VEX_0F, ref instruction);
		}

		// Token: 0x06002133 RID: 8499 RVA: 0x00069678 File Offset: 0x00067878
		internal void VEX3(ref Instruction instruction)
		{
			if ((((this.state.zs.flags & StateFlags.HasRex) | (StateFlags)this.state.zs.mandatoryPrefix) & (StateFlags)this.invalidCheckMask) != (StateFlags)0U)
			{
				this.SetInvalidInstruction();
			}
			this.state.zs.flags = this.state.zs.flags & ~StateFlags.W;
			uint b2 = this.ReadByte();
			this.state.zs.flags = this.state.zs.flags | (StateFlags)(b2 & 128U);
			this.state.vectorLength = (b2 >> 2) & 1U;
			this.state.zs.mandatoryPrefix = (MandatoryPrefixByte)(b2 & 3U);
			b2 = (~b2 >> 3) & 15U;
			this.state.vvvv_invalidCheck = b2;
			this.state.vvvv = b2 & this.reg15Mask;
			uint modrm = this.state.modrm;
			uint b1x = ~modrm & this.maskE0;
			this.state.zs.extraRegisterBase = (b1x >> 4) & 8U;
			this.state.zs.extraIndexRegisterBase = (b1x >> 3) & 8U;
			this.state.zs.extraBaseRegisterBase = (b1x >> 2) & 8U;
			uint b3 = this.ReadByte();
			int table = (int)(modrm & 31U);
			OpCodeHandler[] handlers;
			if (table == 1)
			{
				handlers = this.handlers_VEX_0F;
			}
			else if (table == 2)
			{
				handlers = this.handlers_VEX_0F38;
			}
			else
			{
				if (table != 3)
				{
					this.SetInvalidInstruction();
					return;
				}
				handlers = this.handlers_VEX_0F3A;
			}
			this.DecodeTable(handlers[(int)b3], ref instruction);
		}

		// Token: 0x06002134 RID: 8500 RVA: 0x000697D8 File Offset: 0x000679D8
		internal void XOP(ref Instruction instruction)
		{
			if ((((this.state.zs.flags & StateFlags.HasRex) | (StateFlags)this.state.zs.mandatoryPrefix) & (StateFlags)this.invalidCheckMask) != (StateFlags)0U)
			{
				this.SetInvalidInstruction();
			}
			this.state.zs.flags = this.state.zs.flags & ~StateFlags.W;
			uint b2 = this.ReadByte();
			this.state.zs.flags = this.state.zs.flags | (StateFlags)(b2 & 128U);
			this.state.vectorLength = (b2 >> 2) & 1U;
			this.state.zs.mandatoryPrefix = (MandatoryPrefixByte)(b2 & 3U);
			b2 = (~b2 >> 3) & 15U;
			this.state.vvvv_invalidCheck = b2;
			this.state.vvvv = b2 & this.reg15Mask;
			uint modrm = this.state.modrm;
			uint b1x = ~modrm & this.maskE0;
			this.state.zs.extraRegisterBase = (b1x >> 4) & 8U;
			this.state.zs.extraIndexRegisterBase = (b1x >> 3) & 8U;
			this.state.zs.extraBaseRegisterBase = (b1x >> 2) & 8U;
			uint b3 = this.ReadByte();
			int table = (int)(modrm & 31U);
			OpCodeHandler[] handlers;
			if (table == 8)
			{
				handlers = this.handlers_XOP_MAP8;
			}
			else if (table == 9)
			{
				handlers = this.handlers_XOP_MAP9;
			}
			else
			{
				if (table != 10)
				{
					this.SetInvalidInstruction();
					return;
				}
				handlers = this.handlers_XOP_MAP10;
			}
			this.DecodeTable(handlers[(int)b3], ref instruction);
		}

		// Token: 0x06002135 RID: 8501 RVA: 0x0006993C File Offset: 0x00067B3C
		internal void EVEX_MVEX(ref Instruction instruction)
		{
			if ((((this.state.zs.flags & StateFlags.HasRex) | (StateFlags)this.state.zs.mandatoryPrefix) & (StateFlags)this.invalidCheckMask) != (StateFlags)0U)
			{
				this.SetInvalidInstruction();
			}
			this.state.zs.flags = this.state.zs.flags & ~StateFlags.W;
			uint p0 = this.state.modrm;
			uint p = this.ReadByte();
			uint p2 = this.ReadByte();
			uint p3 = this.ReadByte();
			uint p4 = this.ReadByte();
			if ((p & 4U) == 0U)
			{
				this.SetInvalidInstruction();
				return;
			}
			if ((p0 & 8U) == 0U)
			{
				this.state.zs.mandatoryPrefix = (MandatoryPrefixByte)(p & 3U);
				this.state.zs.flags = this.state.zs.flags | (StateFlags)(p & 128U);
				uint aaa = p2 & 7U;
				this.state.aaa = aaa;
				instruction.InternalOpMask = aaa;
				if ((p2 & 128U) != 0U)
				{
					if ((aaa ^ this.invalidCheckMask) == 4294967295U)
					{
						this.SetInvalidInstruction();
					}
					this.state.zs.flags = this.state.zs.flags | StateFlags.z;
					instruction.InternalSetZeroingMasking();
				}
				this.state.zs.flags = this.state.zs.flags | (StateFlags)(p2 & 16U);
				this.state.vectorLength = (p2 >> 5) & 3U;
				p = (~p >> 3) & 15U;
				if (this.is64bMode)
				{
					uint tmp = (~p2 & 8U) << 1;
					this.state.zs.extraIndexRegisterBaseVSIB = tmp;
					tmp += p;
					this.state.vvvv = tmp;
					this.state.vvvv_invalidCheck = tmp;
					uint p0x = ~p0;
					this.state.zs.extraRegisterBase = (p0x >> 4) & 8U;
					this.state.zs.extraIndexRegisterBase = (p0x >> 3) & 8U;
					this.state.extraRegisterBaseEVEX = p0x & 16U;
					p0x >>= 2;
					this.state.extraBaseRegisterBaseEVEX = p0x & 24U;
					this.state.zs.extraBaseRegisterBase = p0x & 8U;
				}
				else
				{
					this.state.vvvv_invalidCheck = p;
					this.state.vvvv = p & 7U;
					this.state.zs.flags = this.state.zs.flags | (StateFlags)((~p2 & 8U) << 3);
				}
				OpCodeHandler[] handlers;
				switch (p0 & 7U)
				{
				case 1U:
					handlers = this.handlers_EVEX_0F;
					goto IL_279;
				case 2U:
					handlers = this.handlers_EVEX_0F38;
					goto IL_279;
				case 3U:
					handlers = this.handlers_EVEX_0F3A;
					goto IL_279;
				case 5U:
					handlers = this.handlers_EVEX_MAP5;
					goto IL_279;
				case 6U:
					handlers = this.handlers_EVEX_MAP6;
					goto IL_279;
				}
				this.SetInvalidInstruction();
				return;
				IL_279:
				OpCodeHandler opCodeHandler = handlers[(int)p3];
				this.state.modrm = p4;
				this.state.mod = p4 >> 6;
				this.state.reg = (p4 >> 3) & 7U;
				this.state.rm = p4 & 7U;
				if ((((this.state.zs.flags & StateFlags.b) | (StateFlags)this.state.vectorLength) & (StateFlags)this.invalidCheckMask) == (StateFlags.IpRel64 | StateFlags.IpRel32))
				{
					this.SetInvalidInstruction();
				}
				opCodeHandler.Decode(this, ref instruction);
				return;
			}
			this.SetInvalidInstruction();
		}

		// Token: 0x06002136 RID: 8502 RVA: 0x00069C48 File Offset: 0x00067E48
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Register ReadOpSegReg()
		{
			uint reg = this.state.reg;
			if (reg < 6U)
			{
				return Register.ES + (int)reg;
			}
			this.SetInvalidInstruction();
			return Register.None;
		}

		// Token: 0x06002137 RID: 8503 RVA: 0x00069C74 File Offset: 0x00067E74
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool ReadOpMem(ref Instruction instruction)
		{
			if (this.state.addressSize == OpSize.Size64)
			{
				return this.ReadOpMem32Or64(ref instruction, Register.RAX, Register.RAX, TupleType.N1, false);
			}
			if (this.state.addressSize == OpSize.Size32)
			{
				return this.ReadOpMem32Or64(ref instruction, Register.EAX, Register.EAX, TupleType.N1, false);
			}
			this.ReadOpMem16(ref instruction, TupleType.N1);
			return false;
		}

		// Token: 0x06002138 RID: 8504 RVA: 0x00069CC4 File Offset: 0x00067EC4
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ReadOpMemSib(ref Instruction instruction)
		{
			bool isValid;
			if (this.state.addressSize == OpSize.Size64)
			{
				isValid = this.ReadOpMem32Or64(ref instruction, Register.RAX, Register.RAX, TupleType.N1, false);
			}
			else if (this.state.addressSize == OpSize.Size32)
			{
				isValid = this.ReadOpMem32Or64(ref instruction, Register.EAX, Register.EAX, TupleType.N1, false);
			}
			else
			{
				this.ReadOpMem16(ref instruction, TupleType.N1);
				isValid = false;
			}
			if (this.invalidCheckMask != 0U && !isValid)
			{
				this.SetInvalidInstruction();
			}
		}

		// Token: 0x06002139 RID: 8505 RVA: 0x00069D28 File Offset: 0x00067F28
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ReadOpMem_MPX(ref Instruction instruction)
		{
			if (this.is64bMode)
			{
				this.state.addressSize = OpSize.Size64;
				this.ReadOpMem32Or64(ref instruction, Register.RAX, Register.RAX, TupleType.N1, false);
				return;
			}
			if (this.state.addressSize == OpSize.Size32)
			{
				this.ReadOpMem32Or64(ref instruction, Register.EAX, Register.EAX, TupleType.N1, false);
				return;
			}
			this.ReadOpMem16(ref instruction, TupleType.N1);
			if (this.invalidCheckMask != 0U)
			{
				this.SetInvalidInstruction();
			}
		}

		// Token: 0x0600213A RID: 8506 RVA: 0x00069D8C File Offset: 0x00067F8C
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ReadOpMem(ref Instruction instruction, TupleType tupleType)
		{
			if (this.state.addressSize == OpSize.Size64)
			{
				this.ReadOpMem32Or64(ref instruction, Register.RAX, Register.RAX, tupleType, false);
				return;
			}
			if (this.state.addressSize == OpSize.Size32)
			{
				this.ReadOpMem32Or64(ref instruction, Register.EAX, Register.EAX, tupleType, false);
				return;
			}
			this.ReadOpMem16(ref instruction, tupleType);
		}

		// Token: 0x0600213B RID: 8507 RVA: 0x00069DDC File Offset: 0x00067FDC
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ReadOpMem_VSIB(ref Instruction instruction, Register vsibIndex, TupleType tupleType)
		{
			bool isValid;
			if (this.state.addressSize == OpSize.Size64)
			{
				isValid = this.ReadOpMem32Or64(ref instruction, Register.RAX, vsibIndex, tupleType, true);
			}
			else if (this.state.addressSize == OpSize.Size32)
			{
				isValid = this.ReadOpMem32Or64(ref instruction, Register.EAX, vsibIndex, tupleType, true);
			}
			else
			{
				this.ReadOpMem16(ref instruction, tupleType);
				isValid = false;
			}
			if (this.invalidCheckMask != 0U && !isValid)
			{
				this.SetInvalidInstruction();
			}
		}

		// Token: 0x0600213C RID: 8508 RVA: 0x00069E40 File Offset: 0x00068040
		private void ReadOpMem16(ref Instruction instruction, TupleType tupleType)
		{
			Decoder.RegInfo2 regInfo = this.memRegs16[(int)this.state.rm];
			Register register;
			Register register2;
			regInfo.Deconstruct(out register, out register2);
			Register baseReg = register;
			Register indexReg = register2;
			int mod = (int)this.state.mod;
			if (mod != 0)
			{
				if (mod != 1)
				{
					instruction.InternalSetMemoryDisplSize(2U);
					this.displIndex = this.state.zs.instructionLength;
					instruction.MemoryDisplacement64 = (ulong)this.ReadUInt16();
				}
				else
				{
					instruction.InternalSetMemoryDisplSize(1U);
					this.displIndex = this.state.zs.instructionLength;
					if (tupleType == TupleType.N1)
					{
						instruction.MemoryDisplacement64 = (ulong)((ushort)((sbyte)this.ReadByte()));
					}
					else
					{
						instruction.MemoryDisplacement64 = (ulong)((ushort)(this.GetDisp8N(tupleType) * (uint)((sbyte)this.ReadByte())));
					}
				}
			}
			else if (this.state.rm == 6U)
			{
				instruction.InternalSetMemoryDisplSize(2U);
				this.displIndex = this.state.zs.instructionLength;
				instruction.MemoryDisplacement64 = (ulong)this.ReadUInt16();
				baseReg = Register.None;
			}
			instruction.InternalMemoryBase = baseReg;
			instruction.InternalMemoryIndex = indexReg;
		}

		// Token: 0x0600213D RID: 8509 RVA: 0x00069F50 File Offset: 0x00068150
		private bool ReadOpMem32Or64(ref Instruction instruction, Register baseReg, Register indexReg, TupleType tupleType, bool isVsib)
		{
			int mod = (int)this.state.mod;
			uint sib;
			uint displSizeScale;
			uint displ;
			if (mod != 0)
			{
				if (mod != 1)
				{
					if (this.state.rm != 4U)
					{
						this.displIndex = this.state.zs.instructionLength;
						if (this.state.addressSize == OpSize.Size64)
						{
							instruction.MemoryDisplacement64 = (ulong)((long)this.ReadUInt32());
							instruction.InternalSetMemoryDisplSize(4U);
						}
						else
						{
							instruction.MemoryDisplacement64 = (ulong)this.ReadUInt32();
							instruction.InternalSetMemoryDisplSize(3U);
						}
						instruction.InternalMemoryBase = (int)(this.state.zs.extraBaseRegisterBase + this.state.rm) + baseReg;
						return false;
					}
					sib = this.ReadByte();
					displSizeScale = ((this.state.addressSize == OpSize.Size64) ? 4U : 3U);
					this.displIndex = this.state.zs.instructionLength;
					displ = this.ReadUInt32();
				}
				else
				{
					if (this.state.rm != 4U)
					{
						instruction.InternalSetMemoryDisplSize(1U);
						this.displIndex = this.state.zs.instructionLength;
						if (this.state.addressSize == OpSize.Size64)
						{
							if (tupleType == TupleType.N1)
							{
								instruction.MemoryDisplacement64 = (ulong)((long)((sbyte)this.ReadByte()));
							}
							else
							{
								instruction.MemoryDisplacement64 = (ulong)this.GetDisp8N(tupleType) * (ulong)((long)((sbyte)this.ReadByte()));
							}
						}
						else if (tupleType == TupleType.N1)
						{
							instruction.MemoryDisplacement64 = (ulong)((sbyte)this.ReadByte());
						}
						else
						{
							instruction.MemoryDisplacement64 = (ulong)(this.GetDisp8N(tupleType) * (uint)((sbyte)this.ReadByte()));
						}
						instruction.InternalMemoryBase = (int)(this.state.zs.extraBaseRegisterBase + this.state.rm) + baseReg;
						return false;
					}
					sib = this.ReadByte();
					displSizeScale = 1U;
					this.displIndex = this.state.zs.instructionLength;
					if (tupleType == TupleType.N1)
					{
						displ = (uint)((sbyte)this.ReadByte());
					}
					else
					{
						displ = this.GetDisp8N(tupleType) * (uint)((sbyte)this.ReadByte());
					}
				}
			}
			else if (this.state.rm == 4U)
			{
				sib = this.ReadByte();
				displSizeScale = 0U;
				displ = 0U;
			}
			else
			{
				if (this.state.rm == 5U)
				{
					this.displIndex = this.state.zs.instructionLength;
					if (this.state.addressSize == OpSize.Size64)
					{
						instruction.MemoryDisplacement64 = (ulong)((long)this.ReadUInt32());
						instruction.InternalSetMemoryDisplSize(4U);
					}
					else
					{
						instruction.MemoryDisplacement64 = (ulong)this.ReadUInt32();
						instruction.InternalSetMemoryDisplSize(3U);
					}
					if (this.is64bMode)
					{
						if (this.state.addressSize == OpSize.Size64)
						{
							this.state.zs.flags = this.state.zs.flags | StateFlags.IpRel64;
							instruction.InternalMemoryBase = Register.RIP;
						}
						else
						{
							this.state.zs.flags = this.state.zs.flags | StateFlags.IpRel32;
							instruction.InternalMemoryBase = Register.EIP;
						}
					}
					return false;
				}
				instruction.InternalMemoryBase = (int)(this.state.zs.extraBaseRegisterBase + this.state.rm) + baseReg;
				return false;
			}
			uint index = ((sib >> 3) & 7U) + this.state.zs.extraIndexRegisterBase;
			uint @base = sib & 7U;
			instruction.InternalMemoryIndexScale = (int)(sib >> 6);
			if (!isVsib)
			{
				if (index != 4U)
				{
					instruction.InternalMemoryIndex = (int)index + indexReg;
				}
			}
			else
			{
				instruction.InternalMemoryIndex = (int)(index + this.state.zs.extraIndexRegisterBaseVSIB) + indexReg;
			}
			if (@base == 5U && this.state.mod == 0U)
			{
				this.displIndex = this.state.zs.instructionLength;
				if (this.state.addressSize == OpSize.Size64)
				{
					instruction.MemoryDisplacement64 = (ulong)((long)this.ReadUInt32());
					instruction.InternalSetMemoryDisplSize(4U);
				}
				else
				{
					instruction.MemoryDisplacement64 = (ulong)this.ReadUInt32();
					instruction.InternalSetMemoryDisplSize(3U);
				}
			}
			else
			{
				instruction.InternalMemoryBase = (int)(@base + this.state.zs.extraBaseRegisterBase) + baseReg;
				instruction.InternalSetMemoryDisplSize(displSizeScale);
				if (this.state.addressSize == OpSize.Size64)
				{
					instruction.MemoryDisplacement64 = (ulong)((long)displ);
				}
				else
				{
					instruction.MemoryDisplacement64 = (ulong)displ;
				}
			}
			return true;
		}

		// Token: 0x0600213E RID: 8510 RVA: 0x0006A322 File Offset: 0x00068522
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private uint GetDisp8N(TupleType tupleType)
		{
			return TupleTypeTable.GetDisp8N(tupleType, (this.state.zs.flags & StateFlags.b) > (StateFlags)0U);
		}

		// Token: 0x0600213F RID: 8511 RVA: 0x0006A340 File Offset: 0x00068540
		public ConstantOffsets GetConstantOffsets(in Instruction instruction)
		{
			ConstantOffsets constantOffsets = default(ConstantOffsets);
			int displSize = instruction.MemoryDisplSize;
			if (displSize != 0)
			{
				constantOffsets.DisplacementOffset = (byte)this.displIndex;
				if (displSize == 8 && (this.state.zs.flags & StateFlags.Addr64) == (StateFlags)0U)
				{
					constantOffsets.DisplacementSize = 4;
				}
				else
				{
					constantOffsets.DisplacementSize = (byte)displSize;
				}
			}
			if ((this.state.zs.flags & StateFlags.NoImm) == (StateFlags)0U)
			{
				int extraImmSub = 0;
				for (int i = instruction.OpCount - 1; i >= 0; i--)
				{
					switch (instruction.GetOpKind(i))
					{
					case OpKind.NearBranch16:
						if ((this.state.zs.flags & StateFlags.BranchImm8) != (StateFlags)0U)
						{
							constantOffsets.ImmediateOffset = (byte)(instruction.Length - 1);
							constantOffsets.ImmediateSize = 1;
						}
						else if ((this.state.zs.flags & StateFlags.Xbegin) == (StateFlags)0U)
						{
							constantOffsets.ImmediateOffset = (byte)(instruction.Length - 2);
							constantOffsets.ImmediateSize = 2;
						}
						else if (this.state.operandSize != OpSize.Size16)
						{
							constantOffsets.ImmediateOffset = (byte)(instruction.Length - 4);
							constantOffsets.ImmediateSize = 4;
						}
						else
						{
							constantOffsets.ImmediateOffset = (byte)(instruction.Length - 2);
							constantOffsets.ImmediateSize = 2;
						}
						break;
					case OpKind.NearBranch32:
					case OpKind.NearBranch64:
						if ((this.state.zs.flags & StateFlags.BranchImm8) != (StateFlags)0U)
						{
							constantOffsets.ImmediateOffset = (byte)(instruction.Length - 1);
							constantOffsets.ImmediateSize = 1;
						}
						else if ((this.state.zs.flags & StateFlags.Xbegin) == (StateFlags)0U)
						{
							constantOffsets.ImmediateOffset = (byte)(instruction.Length - 4);
							constantOffsets.ImmediateSize = 4;
						}
						else if (this.state.operandSize != OpSize.Size16)
						{
							constantOffsets.ImmediateOffset = (byte)(instruction.Length - 4);
							constantOffsets.ImmediateSize = 4;
						}
						else
						{
							constantOffsets.ImmediateOffset = (byte)(instruction.Length - 2);
							constantOffsets.ImmediateSize = 2;
						}
						break;
					case OpKind.FarBranch16:
						constantOffsets.ImmediateOffset = (byte)(instruction.Length - 4);
						constantOffsets.ImmediateSize = 2;
						constantOffsets.ImmediateOffset2 = (byte)(instruction.Length - 2);
						constantOffsets.ImmediateSize2 = 2;
						break;
					case OpKind.FarBranch32:
						constantOffsets.ImmediateOffset = (byte)(instruction.Length - 6);
						constantOffsets.ImmediateSize = 4;
						constantOffsets.ImmediateOffset2 = (byte)(instruction.Length - 2);
						constantOffsets.ImmediateSize2 = 2;
						break;
					case OpKind.Immediate8:
					case OpKind.Immediate8to16:
					case OpKind.Immediate8to32:
					case OpKind.Immediate8to64:
						constantOffsets.ImmediateOffset = (byte)(instruction.Length - extraImmSub - 1);
						constantOffsets.ImmediateSize = 1;
						return constantOffsets;
					case OpKind.Immediate8_2nd:
						constantOffsets.ImmediateOffset2 = (byte)(instruction.Length - 1);
						constantOffsets.ImmediateSize2 = 1;
						extraImmSub = 1;
						break;
					case OpKind.Immediate16:
						constantOffsets.ImmediateOffset = (byte)(instruction.Length - extraImmSub - 2);
						constantOffsets.ImmediateSize = 2;
						return constantOffsets;
					case OpKind.Immediate32:
					case OpKind.Immediate32to64:
						constantOffsets.ImmediateOffset = (byte)(instruction.Length - extraImmSub - 4);
						constantOffsets.ImmediateSize = 4;
						return constantOffsets;
					case OpKind.Immediate64:
						constantOffsets.ImmediateOffset = (byte)(instruction.Length - extraImmSub - 8);
						constantOffsets.ImmediateSize = 8;
						return constantOffsets;
					}
				}
			}
			return constantOffsets;
		}

		// Token: 0x06002140 RID: 8512 RVA: 0x0006A67B File Offset: 0x0006887B
		public Decoder.Enumerator GetEnumerator()
		{
			return new Decoder.Enumerator(this);
		}

		// Token: 0x06002141 RID: 8513 RVA: 0x0006A683 File Offset: 0x00068883
		IEnumerator<Instruction> IEnumerable<Instruction>.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x06002142 RID: 8514 RVA: 0x0006A683 File Offset: 0x00068883
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x04002A08 RID: 10760
		private ulong instructionPointer;

		// Token: 0x04002A09 RID: 10761
		private readonly CodeReader reader;

		// Token: 0x04002A0A RID: 10762
		private readonly Decoder.RegInfo2[] memRegs16;

		// Token: 0x04002A0B RID: 10763
		private readonly OpCodeHandler[] handlers_MAP0;

		// Token: 0x04002A0C RID: 10764
		private readonly OpCodeHandler[] handlers_VEX_0F;

		// Token: 0x04002A0D RID: 10765
		private readonly OpCodeHandler[] handlers_VEX_0F38;

		// Token: 0x04002A0E RID: 10766
		private readonly OpCodeHandler[] handlers_VEX_0F3A;

		// Token: 0x04002A0F RID: 10767
		private readonly OpCodeHandler[] handlers_EVEX_0F;

		// Token: 0x04002A10 RID: 10768
		private readonly OpCodeHandler[] handlers_EVEX_0F38;

		// Token: 0x04002A11 RID: 10769
		private readonly OpCodeHandler[] handlers_EVEX_0F3A;

		// Token: 0x04002A12 RID: 10770
		private readonly OpCodeHandler[] handlers_EVEX_MAP5;

		// Token: 0x04002A13 RID: 10771
		private readonly OpCodeHandler[] handlers_EVEX_MAP6;

		// Token: 0x04002A14 RID: 10772
		private readonly OpCodeHandler[] handlers_XOP_MAP8;

		// Token: 0x04002A15 RID: 10773
		private readonly OpCodeHandler[] handlers_XOP_MAP9;

		// Token: 0x04002A16 RID: 10774
		private readonly OpCodeHandler[] handlers_XOP_MAP10;

		// Token: 0x04002A17 RID: 10775
		internal Decoder.State state;

		// Token: 0x04002A18 RID: 10776
		internal uint displIndex;

		// Token: 0x04002A19 RID: 10777
		internal readonly DecoderOptions options;

		// Token: 0x04002A1A RID: 10778
		internal readonly uint invalidCheckMask;

		// Token: 0x04002A1B RID: 10779
		internal readonly uint is64bMode_and_W;

		// Token: 0x04002A1C RID: 10780
		internal readonly uint reg15Mask;

		// Token: 0x04002A1D RID: 10781
		private readonly uint maskE0;

		// Token: 0x04002A1E RID: 10782
		private readonly uint rexMask;

		// Token: 0x04002A1F RID: 10783
		internal readonly CodeSize defaultCodeSize;

		// Token: 0x04002A20 RID: 10784
		internal readonly OpSize defaultOperandSize;

		// Token: 0x04002A21 RID: 10785
		private readonly OpSize defaultAddressSize;

		// Token: 0x04002A22 RID: 10786
		internal readonly OpSize defaultInvertedOperandSize;

		// Token: 0x04002A23 RID: 10787
		internal readonly OpSize defaultInvertedAddressSize;

		// Token: 0x04002A24 RID: 10788
		internal readonly bool is64bMode;

		// Token: 0x04002A26 RID: 10790
		private static readonly Decoder.RegInfo2[] s_memRegs16 = new Decoder.RegInfo2[]
		{
			new Decoder.RegInfo2(Register.BX, Register.SI),
			new Decoder.RegInfo2(Register.BX, Register.DI),
			new Decoder.RegInfo2(Register.BP, Register.SI),
			new Decoder.RegInfo2(Register.BP, Register.DI),
			new Decoder.RegInfo2(Register.SI, Register.None),
			new Decoder.RegInfo2(Register.DI, Register.None),
			new Decoder.RegInfo2(Register.BP, Register.None),
			new Decoder.RegInfo2(Register.BX, Register.None)
		};

		// Token: 0x0200063B RID: 1595
		internal struct ZState
		{
			// Token: 0x04002A27 RID: 10791
			public uint instructionLength;

			// Token: 0x04002A28 RID: 10792
			public uint extraRegisterBase;

			// Token: 0x04002A29 RID: 10793
			public uint extraIndexRegisterBase;

			// Token: 0x04002A2A RID: 10794
			public uint extraBaseRegisterBase;

			// Token: 0x04002A2B RID: 10795
			public uint extraIndexRegisterBaseVSIB;

			// Token: 0x04002A2C RID: 10796
			public StateFlags flags;

			// Token: 0x04002A2D RID: 10797
			public MandatoryPrefixByte mandatoryPrefix;

			// Token: 0x04002A2E RID: 10798
			public byte segmentPrio;
		}

		// Token: 0x0200063C RID: 1596
		internal struct State
		{
			// Token: 0x1700076D RID: 1901
			// (get) Token: 0x06002143 RID: 8515 RVA: 0x0006A690 File Offset: 0x00068890
			public readonly EncodingKind Encoding
			{
				get
				{
					return (EncodingKind)((this.zs.flags >> 29) & StateFlags.MvexSssMask);
				}
			}

			// Token: 0x04002A2F RID: 10799
			public uint modrm;

			// Token: 0x04002A30 RID: 10800
			public uint mod;

			// Token: 0x04002A31 RID: 10801
			public uint reg;

			// Token: 0x04002A32 RID: 10802
			public uint rm;

			// Token: 0x04002A33 RID: 10803
			public Decoder.ZState zs;

			// Token: 0x04002A34 RID: 10804
			public uint vvvv;

			// Token: 0x04002A35 RID: 10805
			public uint vvvv_invalidCheck;

			// Token: 0x04002A36 RID: 10806
			public uint aaa;

			// Token: 0x04002A37 RID: 10807
			public uint extraRegisterBaseEVEX;

			// Token: 0x04002A38 RID: 10808
			public uint extraBaseRegisterBaseEVEX;

			// Token: 0x04002A39 RID: 10809
			public uint vectorLength;

			// Token: 0x04002A3A RID: 10810
			public OpSize operandSize;

			// Token: 0x04002A3B RID: 10811
			public OpSize addressSize;
		}

		// Token: 0x0200063D RID: 1597
		private readonly struct RegInfo2
		{
			// Token: 0x06002144 RID: 8516 RVA: 0x0006A6A2 File Offset: 0x000688A2
			public RegInfo2(Register baseReg, Register indexReg)
			{
				this.baseReg = baseReg;
				this.indexReg = indexReg;
			}

			// Token: 0x06002145 RID: 8517 RVA: 0x0006A6B2 File Offset: 0x000688B2
			public void Deconstruct(out Register baseReg, out Register indexReg)
			{
				baseReg = this.baseReg;
				indexReg = this.indexReg;
			}

			// Token: 0x04002A3C RID: 10812
			public readonly Register baseReg;

			// Token: 0x04002A3D RID: 10813
			public readonly Register indexReg;
		}

		// Token: 0x0200063E RID: 1598
		public struct Enumerator : IEnumerator<Instruction>, IDisposable, IEnumerator
		{
			// Token: 0x06002146 RID: 8518 RVA: 0x0006A6C4 File Offset: 0x000688C4
			[NullableContext(1)]
			internal Enumerator(Decoder decoder)
			{
				this.decoder = decoder;
				this.instruction = default(Instruction);
			}

			// Token: 0x1700076E RID: 1902
			// (get) Token: 0x06002147 RID: 8519 RVA: 0x0006A6D9 File Offset: 0x000688D9
			public Instruction Current
			{
				get
				{
					return this.instruction;
				}
			}

			// Token: 0x1700076F RID: 1903
			// (get) Token: 0x06002148 RID: 8520 RVA: 0x0006A6E1 File Offset: 0x000688E1
			Instruction IEnumerator<Instruction>.Current
			{
				get
				{
					return this.Current;
				}
			}

			// Token: 0x17000770 RID: 1904
			// (get) Token: 0x06002149 RID: 8521 RVA: 0x0006A6E9 File Offset: 0x000688E9
			[Nullable(1)]
			object IEnumerator.Current
			{
				get
				{
					return this.Current;
				}
			}

			// Token: 0x0600214A RID: 8522 RVA: 0x0006A6F6 File Offset: 0x000688F6
			public bool MoveNext()
			{
				this.decoder.Decode(out this.instruction);
				return this.instruction.Length != 0;
			}

			// Token: 0x0600214B RID: 8523 RVA: 0x0001C627 File Offset: 0x0001A827
			void IEnumerator.Reset()
			{
				throw new InvalidOperationException();
			}

			// Token: 0x0600214C RID: 8524 RVA: 0x0001B842 File Offset: 0x00019A42
			public void Dispose()
			{
			}

			// Token: 0x04002A3E RID: 10814
			private readonly Decoder decoder;

			// Token: 0x04002A3F RID: 10815
			private Instruction instruction;
		}
	}
}
