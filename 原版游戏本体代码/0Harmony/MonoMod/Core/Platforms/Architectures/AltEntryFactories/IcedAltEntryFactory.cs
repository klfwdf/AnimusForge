using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Iced.Intel;
using MonoMod.Utils;

namespace MonoMod.Core.Platforms.Architectures.AltEntryFactories
{
	// Token: 0x02000562 RID: 1378
	internal sealed class IcedAltEntryFactory : IAltEntryFactory
	{
		// Token: 0x06001EF9 RID: 7929 RVA: 0x000657C5 File Offset: 0x000639C5
		[NullableContext(1)]
		public IcedAltEntryFactory(ISystem system, int bitness)
		{
			this.system = system;
			this.bitness = bitness;
			this.alloc = system.MemoryAllocator;
		}

		// Token: 0x06001EFA RID: 7930 RVA: 0x000657E8 File Offset: 0x000639E8
		[NullableContext(2)]
		public unsafe IntPtr CreateAlternateEntrypoint(IntPtr entrypoint, int minLength, out IDisposable handle)
		{
			IcedAltEntryFactory.PtrCodeReader codeReader = new IcedAltEntryFactory.PtrCodeReader(entrypoint);
			Decoder decoder = Decoder.Create(this.bitness, codeReader, (ulong)(long)entrypoint, DecoderOptions.NoInvalidCheck | DecoderOptions.AMD);
			InstructionList insns = new InstructionList();
			while (codeReader.Position < minLength)
			{
				decoder.Decode(insns.AllocUninitializedElement());
			}
			bool hasRipRelAddress = false;
			using (InstructionList.Enumerator enumerator = insns.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.IsIPRelativeMemoryOperand)
					{
						hasRipRelAddress = true;
						break;
					}
				}
			}
			Instruction lastInsn = *insns[insns.Count - 1];
			if (lastInsn.Mnemonic == Mnemonic.Call)
			{
				Encoder enc = Encoder.Create(this.bitness, new IcedAltEntryFactory.NullCodeWriter());
				Instruction jmpInsn = lastInsn;
				Code code = lastInsn.Code;
				Code code2;
				if (code <= Code.Call_ptr1632)
				{
					if (code == Code.Call_ptr1616)
					{
						code2 = Code.Jmp_ptr1616;
						goto IL_1B1;
					}
					if (code == Code.Call_ptr1632)
					{
						code2 = Code.Jmp_ptr1632;
						goto IL_1B1;
					}
				}
				else
				{
					switch (code)
					{
					case Code.Call_rel16:
						code2 = Code.Jmp_rel16;
						goto IL_1B1;
					case Code.Call_rel32_32:
						code2 = Code.Jmp_rel32_32;
						goto IL_1B1;
					case Code.Call_rel32_64:
						code2 = Code.Jmp_rel32_64;
						goto IL_1B1;
					default:
						switch (code)
						{
						case Code.Call_m1616:
							code2 = Code.Jmp_m1616;
							goto IL_1B1;
						case Code.Call_m1632:
							code2 = Code.Jmp_m1632;
							goto IL_1B1;
						case Code.Call_m1664:
							code2 = Code.Jmp_m1664;
							goto IL_1B1;
						case Code.Jmp_rm16:
							code2 = Code.Jmp_rm16;
							goto IL_1B1;
						case Code.Jmp_rm32:
							code2 = Code.Jmp_rm32;
							goto IL_1B1;
						case Code.Jmp_rm64:
							code2 = Code.Jmp_rm64;
							goto IL_1B1;
						}
						break;
					}
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unrecognized call opcode ");
				defaultInterpolatedStringHandler.AppendFormatted<Code>(lastInsn.Code);
				throw new InvalidOperationException(defaultInterpolatedStringHandler.ToStringAndClear());
				IL_1B1:
				jmpInsn.Code = code2;
				jmpInsn.Length = (int)enc.Encode(jmpInsn, jmpInsn.IP);
				ulong retAddr = lastInsn.NextIP;
				Instruction pushInsn;
				bool useQword;
				Instruction qword;
				if (this.bitness == 32)
				{
					pushInsn = Instruction.Create(Code.Pushd_imm32, (uint)retAddr);
					pushInsn.Length = (int)enc.Encode(pushInsn, jmpInsn.IP);
					pushInsn.IP = jmpInsn.IP;
					jmpInsn.IP += (ulong)((long)pushInsn.Length);
					useQword = false;
					qword = default(Instruction);
				}
				else
				{
					useQword = true;
					qword = Instruction.CreateDeclareQword(retAddr);
					Code code3 = Code.Push_rm64;
					MemoryOperand memoryOperand = new MemoryOperand(Register.RIP, (long)jmpInsn.NextIP);
					pushInsn = Instruction.Create(code3, memoryOperand);
					pushInsn.Length = (int)enc.Encode(pushInsn, jmpInsn.IP);
					pushInsn.IP = jmpInsn.IP;
					jmpInsn.IP += (ulong)((long)pushInsn.Length);
					qword.IP = jmpInsn.NextIP;
					pushInsn.MemoryDisplacement64 = qword.IP;
				}
				insns.RemoveAt(insns.Count - 1);
				insns.Add(pushInsn);
				insns.Add(jmpInsn);
				if (useQword)
				{
					insns.Add(qword);
				}
			}
			else
			{
				InstructionList instructionList = insns;
				Instruction instruction = Instruction.CreateBranch((this.bitness == 64) ? Code.Jmp_rel32_64 : Code.Jmp_rel32_32, decoder.IP);
				instructionList.Add(instruction);
			}
			int estTotalSize = codeReader.Position + 5;
			IntPtr baseAddress;
			using (IcedAltEntryFactory.BufferCodeWriter bufWriter = new IcedAltEntryFactory.BufferCodeWriter())
			{
				IAllocatedMemory allocated;
				string error;
				for (;;)
				{
					bufWriter.Reset();
					if (hasRipRelAddress)
					{
						Helpers.Assert(this.alloc.TryAllocateInRange(new PositionedAllocationRequest(entrypoint, entrypoint + (IntPtr)int.MinValue, entrypoint + (IntPtr)int.MaxValue, new AllocationRequest(estTotalSize)
						{
							Executable = true
						}), out allocated), null, "alloc.TryAllocateInRange(\n                        new(entrypoint, (nint)entrypoint + int.MinValue, (nint)entrypoint + int.MaxValue,\n                        new(estTotalSize) { Executable = true }), out allocated)");
					}
					else
					{
						Helpers.Assert(this.alloc.TryAllocate(new AllocationRequest(estTotalSize)
						{
							Executable = true
						}, out allocated), null, "alloc.TryAllocate(new(estTotalSize) { Executable = true }, out allocated)");
					}
					IntPtr target = allocated.BaseAddress;
					BlockEncoderResult blockEncoderResult;
					if (!BlockEncoder.TryEncode(this.bitness, new InstructionBlock(bufWriter, insns, (ulong)(long)target), out error, out blockEncoderResult, BlockEncoderOptions.None))
					{
						break;
					}
					if (bufWriter.Data.Length == allocated.Size)
					{
						goto IL_445;
					}
					estTotalSize = bufWriter.Data.Length;
					allocated.Dispose();
				}
				allocated.Dispose();
				bool flag;
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler debugLogErrorStringHandler = new <24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.DebugLogErrorStringHandler(44, 1, ref flag);
				if (flag)
				{
					debugLogErrorStringHandler.AppendLiteral("BlockEncoder failed to encode instructions: ");
					debugLogErrorStringHandler.AppendFormatted(error);
				}
				<24b3ba8a-00b7-40fc-a603-2711fa115297>MMDbgLog.Error(ref debugLogErrorStringHandler);
				throw new InvalidOperationException("BlockEncoder failed to encode instructions: " + error);
				IL_445:
				this.system.PatchData(PatchTargetKind.Executable, allocated.BaseAddress, bufWriter.Data.Span, default(Span<byte>));
				handle = allocated;
				baseAddress = allocated.BaseAddress;
			}
			return baseAddress;
		}

		// Token: 0x040012D3 RID: 4819
		[Nullable(1)]
		private readonly ISystem system;

		// Token: 0x040012D4 RID: 4820
		[Nullable(1)]
		private readonly IMemoryAllocator alloc;

		// Token: 0x040012D5 RID: 4821
		private readonly int bitness;

		// Token: 0x02000563 RID: 1379
		private sealed class PtrCodeReader : CodeReader
		{
			// Token: 0x06001EFB RID: 7931 RVA: 0x00065CB8 File Offset: 0x00063EB8
			public PtrCodeReader(IntPtr basePtr)
			{
				this.Base = basePtr;
				this.Position = 0;
			}

			// Token: 0x170006C1 RID: 1729
			// (get) Token: 0x06001EFC RID: 7932 RVA: 0x00065CCE File Offset: 0x00063ECE
			public IntPtr Base { get; }

			// Token: 0x170006C2 RID: 1730
			// (get) Token: 0x06001EFD RID: 7933 RVA: 0x00065CD6 File Offset: 0x00063ED6
			// (set) Token: 0x06001EFE RID: 7934 RVA: 0x00065CDE File Offset: 0x00063EDE
			public int Position { get; private set; }

			// Token: 0x06001EFF RID: 7935 RVA: 0x00065CE8 File Offset: 0x00063EE8
			public unsafe override int ReadByte()
			{
				IntPtr @base = this.Base;
				int position = this.Position;
				this.Position = position + 1;
				return (int)(*(@base + (IntPtr)position));
			}
		}

		// Token: 0x02000564 RID: 1380
		private sealed class NullCodeWriter : CodeWriter
		{
			// Token: 0x06001F00 RID: 7936 RVA: 0x0001B842 File Offset: 0x00019A42
			public override void WriteByte(byte value)
			{
			}
		}

		// Token: 0x02000565 RID: 1381
		private sealed class BufferCodeWriter : CodeWriter, IDisposable
		{
			// Token: 0x06001F02 RID: 7938 RVA: 0x00065D17 File Offset: 0x00063F17
			public BufferCodeWriter()
			{
				this.pool = ArrayPool<byte>.Shared;
			}

			// Token: 0x170006C3 RID: 1731
			// (get) Token: 0x06001F03 RID: 7939 RVA: 0x00065D2C File Offset: 0x00063F2C
			public ReadOnlyMemory<byte> Data
			{
				get
				{
					return this.buffer.AsMemory<byte>().Slice(0, this.pos);
				}
			}

			// Token: 0x06001F04 RID: 7940 RVA: 0x00065D58 File Offset: 0x00063F58
			public override void WriteByte(byte value)
			{
				if (this.buffer == null)
				{
					this.buffer = this.pool.Rent(8);
				}
				if (this.buffer.Length <= this.pos)
				{
					byte[] newBuf = this.pool.Rent(this.buffer.Length * 2);
					Array.Copy(this.buffer, newBuf, this.buffer.Length);
					this.pool.Return(this.buffer, false);
					this.buffer = newBuf;
				}
				byte[] array = this.buffer;
				int num = this.pos;
				this.pos = num + 1;
				array[num] = value;
			}

			// Token: 0x06001F05 RID: 7941 RVA: 0x00065DEB File Offset: 0x00063FEB
			public void Reset()
			{
				this.pos = 0;
			}

			// Token: 0x06001F06 RID: 7942 RVA: 0x00065DF4 File Offset: 0x00063FF4
			public void Dispose()
			{
				if (this.buffer != null)
				{
					byte[] buf = this.buffer;
					this.buffer = null;
					this.pool.Return(buf, false);
				}
			}

			// Token: 0x040012D8 RID: 4824
			[Nullable(1)]
			private readonly ArrayPool<byte> pool;

			// Token: 0x040012D9 RID: 4825
			[Nullable(2)]
			private byte[] buffer;

			// Token: 0x040012DA RID: 4826
			private int pos;
		}
	}
}
