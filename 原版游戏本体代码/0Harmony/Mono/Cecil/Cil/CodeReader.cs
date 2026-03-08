using System;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002E6 RID: 742
	internal sealed class CodeReader : BinaryStreamReader
	{
		// Token: 0x170004DA RID: 1242
		// (get) Token: 0x060012E3 RID: 4835 RVA: 0x0003B6C3 File Offset: 0x000398C3
		private int Offset
		{
			get
			{
				return base.Position - this.start;
			}
		}

		// Token: 0x060012E4 RID: 4836 RVA: 0x0003B6D2 File Offset: 0x000398D2
		public CodeReader(MetadataReader reader)
			: base(reader.image.Stream.value)
		{
			this.reader = reader;
		}

		// Token: 0x060012E5 RID: 4837 RVA: 0x0003B6F1 File Offset: 0x000398F1
		public int MoveTo(MethodDefinition method)
		{
			this.method = method;
			this.reader.context = method;
			int position = base.Position;
			base.Position = (int)this.reader.image.ResolveVirtualAddress((uint)method.RVA);
			return position;
		}

		// Token: 0x060012E6 RID: 4838 RVA: 0x0003B728 File Offset: 0x00039928
		public void MoveBackTo(int position)
		{
			this.reader.context = null;
			base.Position = position;
		}

		// Token: 0x060012E7 RID: 4839 RVA: 0x0003B740 File Offset: 0x00039940
		public MethodBody ReadMethodBody(MethodDefinition method)
		{
			int position = this.MoveTo(method);
			this.body = new MethodBody(method);
			this.ReadMethodBody();
			this.MoveBackTo(position);
			return this.body;
		}

		// Token: 0x060012E8 RID: 4840 RVA: 0x0003B774 File Offset: 0x00039974
		public int ReadCodeSize(MethodDefinition method)
		{
			int position = this.MoveTo(method);
			int result = this.ReadCodeSize();
			this.MoveBackTo(position);
			return result;
		}

		// Token: 0x060012E9 RID: 4841 RVA: 0x0003B798 File Offset: 0x00039998
		private int ReadCodeSize()
		{
			byte flags = this.ReadByte();
			int num = (int)(flags & 3);
			if (num == 2)
			{
				return flags >> 2;
			}
			if (num != 3)
			{
				throw new InvalidOperationException();
			}
			base.Advance(3);
			return (int)this.ReadUInt32();
		}

		// Token: 0x060012EA RID: 4842 RVA: 0x0003B7D4 File Offset: 0x000399D4
		private void ReadMethodBody()
		{
			byte flags = this.ReadByte();
			int num = (int)(flags & 3);
			if (num != 2)
			{
				if (num != 3)
				{
					throw new InvalidOperationException();
				}
				base.Advance(-1);
				this.ReadFatMethod();
			}
			else
			{
				this.body.code_size = flags >> 2;
				this.body.MaxStackSize = 8;
				this.ReadCode();
			}
			ISymbolReader symbol_reader = this.reader.module.symbol_reader;
			if (symbol_reader != null && this.method.debug_info == null)
			{
				this.method.debug_info = symbol_reader.Read(this.method);
			}
			if (this.method.debug_info != null)
			{
				this.ReadDebugInfo();
			}
		}

		// Token: 0x060012EB RID: 4843 RVA: 0x0003B878 File Offset: 0x00039A78
		private void ReadFatMethod()
		{
			ushort flags = this.ReadUInt16();
			this.body.max_stack_size = (int)this.ReadUInt16();
			this.body.code_size = (int)this.ReadUInt32();
			this.body.local_var_token = new MetadataToken(this.ReadUInt32());
			this.body.init_locals = (flags & 16) > 0;
			if (this.body.local_var_token.RID != 0U)
			{
				this.body.variables = this.ReadVariables(this.body.local_var_token);
			}
			this.ReadCode();
			if ((flags & 8) != 0)
			{
				this.ReadSection();
			}
		}

		// Token: 0x060012EC RID: 4844 RVA: 0x0003B918 File Offset: 0x00039B18
		public VariableDefinitionCollection ReadVariables(MetadataToken local_var_token)
		{
			int position = this.reader.position;
			VariableDefinitionCollection result = this.reader.ReadVariables(local_var_token, this.method);
			this.reader.position = position;
			return result;
		}

		// Token: 0x060012ED RID: 4845 RVA: 0x0003B950 File Offset: 0x00039B50
		private void ReadCode()
		{
			this.start = base.Position;
			int code_size = this.body.code_size;
			if (code_size < 0 || (long)base.Length <= (long)((ulong)(code_size + base.Position)))
			{
				code_size = 0;
			}
			int end = this.start + code_size;
			Collection<Instruction> instructions = (this.body.instructions = new InstructionCollection(this.method, (code_size + 1) / 2));
			while (base.Position < end)
			{
				int offset = base.Position - this.start;
				OpCode opcode = this.ReadOpCode();
				Instruction current = new Instruction(offset, opcode);
				if (opcode.OperandType != OperandType.InlineNone)
				{
					current.operand = this.ReadOperand(current);
				}
				instructions.Add(current);
			}
			this.ResolveBranches(instructions);
		}

		// Token: 0x060012EE RID: 4846 RVA: 0x0003BA08 File Offset: 0x00039C08
		private OpCode ReadOpCode()
		{
			byte il_opcode = this.ReadByte();
			if (il_opcode == 254)
			{
				return OpCodes.TwoBytesOpCode[(int)this.ReadByte()];
			}
			return OpCodes.OneByteOpCode[(int)il_opcode];
		}

		// Token: 0x060012EF RID: 4847 RVA: 0x0003BA40 File Offset: 0x00039C40
		private object ReadOperand(Instruction instruction)
		{
			switch (instruction.opcode.OperandType)
			{
			case OperandType.InlineBrTarget:
				return this.ReadInt32() + this.Offset;
			case OperandType.InlineField:
			case OperandType.InlineMethod:
			case OperandType.InlineTok:
			case OperandType.InlineType:
				return this.reader.LookupToken(this.ReadToken());
			case OperandType.InlineI:
				return this.ReadInt32();
			case OperandType.InlineI8:
				return this.ReadInt64();
			case OperandType.InlineR:
				return this.ReadDouble();
			case OperandType.InlineSig:
				return this.GetCallSite(this.ReadToken());
			case OperandType.InlineString:
				return this.GetString(this.ReadToken());
			case OperandType.InlineSwitch:
			{
				int length = this.ReadInt32();
				int base_offset = this.Offset + 4 * length;
				int[] branches = new int[length];
				for (int i = 0; i < length; i++)
				{
					branches[i] = base_offset + this.ReadInt32();
				}
				return branches;
			}
			case OperandType.InlineVar:
				return this.GetVariable((int)this.ReadUInt16());
			case OperandType.InlineArg:
				return this.GetParameter((int)this.ReadUInt16());
			case OperandType.ShortInlineBrTarget:
				return (int)this.ReadSByte() + this.Offset;
			case OperandType.ShortInlineI:
				if (instruction.opcode == OpCodes.Ldc_I4_S)
				{
					return this.ReadSByte();
				}
				return this.ReadByte();
			case OperandType.ShortInlineR:
				return this.ReadSingle();
			case OperandType.ShortInlineVar:
				return this.GetVariable((int)this.ReadByte());
			case OperandType.ShortInlineArg:
				return this.GetParameter((int)this.ReadByte());
			}
			throw new NotSupportedException();
		}

		// Token: 0x060012F0 RID: 4848 RVA: 0x0003BBD0 File Offset: 0x00039DD0
		public string GetString(MetadataToken token)
		{
			return this.reader.image.UserStringHeap.Read(token.RID);
		}

		// Token: 0x060012F1 RID: 4849 RVA: 0x0003BBEE File Offset: 0x00039DEE
		public ParameterDefinition GetParameter(int index)
		{
			return this.body.GetParameter(index);
		}

		// Token: 0x060012F2 RID: 4850 RVA: 0x0003BBFC File Offset: 0x00039DFC
		public VariableDefinition GetVariable(int index)
		{
			return this.body.GetVariable(index);
		}

		// Token: 0x060012F3 RID: 4851 RVA: 0x0003BC0A File Offset: 0x00039E0A
		public CallSite GetCallSite(MetadataToken token)
		{
			return this.reader.ReadCallSite(token);
		}

		// Token: 0x060012F4 RID: 4852 RVA: 0x0003BC18 File Offset: 0x00039E18
		private void ResolveBranches(Collection<Instruction> instructions)
		{
			Instruction[] items = instructions.items;
			int size = instructions.size;
			int i = 0;
			while (i < size)
			{
				Instruction instruction = items[i];
				OperandType operandType = instruction.opcode.OperandType;
				if (operandType == OperandType.InlineBrTarget)
				{
					goto IL_36;
				}
				if (operandType != OperandType.InlineSwitch)
				{
					if (operandType == OperandType.ShortInlineBrTarget)
					{
						goto IL_36;
					}
				}
				else
				{
					int[] offsets = (int[])instruction.operand;
					Instruction[] branches = new Instruction[offsets.Length];
					for (int j = 0; j < offsets.Length; j++)
					{
						branches[j] = this.GetInstruction(offsets[j]);
					}
					instruction.operand = branches;
				}
				IL_92:
				i++;
				continue;
				IL_36:
				instruction.operand = this.GetInstruction((int)instruction.operand);
				goto IL_92;
			}
		}

		// Token: 0x060012F5 RID: 4853 RVA: 0x0003BCC2 File Offset: 0x00039EC2
		private Instruction GetInstruction(int offset)
		{
			return CodeReader.GetInstruction(this.body.Instructions, offset);
		}

		// Token: 0x060012F6 RID: 4854 RVA: 0x0003BCD8 File Offset: 0x00039ED8
		private static Instruction GetInstruction(Collection<Instruction> instructions, int offset)
		{
			int size = instructions.size;
			Instruction[] items = instructions.items;
			if (offset < 0 || offset > items[size - 1].offset)
			{
				return null;
			}
			int min = 0;
			int max = size - 1;
			while (min <= max)
			{
				int mid = min + (max - min) / 2;
				Instruction instruction = items[mid];
				int instruction_offset = instruction.offset;
				if (offset == instruction_offset)
				{
					return instruction;
				}
				if (offset < instruction_offset)
				{
					max = mid - 1;
				}
				else
				{
					min = mid + 1;
				}
			}
			return null;
		}

		// Token: 0x060012F7 RID: 4855 RVA: 0x0003BD44 File Offset: 0x00039F44
		private void ReadSection()
		{
			base.Align(4);
			byte b = this.ReadByte();
			if ((b & 64) == 0)
			{
				this.ReadSmallSection();
			}
			else
			{
				this.ReadFatSection();
			}
			if ((b & 128) != 0)
			{
				this.ReadSection();
			}
		}

		// Token: 0x060012F8 RID: 4856 RVA: 0x0003BD78 File Offset: 0x00039F78
		private void ReadSmallSection()
		{
			int count = (int)(this.ReadByte() / 12);
			base.Advance(2);
			this.ReadExceptionHandlers(count, () => (int)this.ReadUInt16(), () => (int)this.ReadByte());
		}

		// Token: 0x060012F9 RID: 4857 RVA: 0x0003BDB8 File Offset: 0x00039FB8
		private void ReadFatSection()
		{
			base.Advance(-1);
			int count = (this.ReadInt32() >> 8) / 24;
			this.ReadExceptionHandlers(count, new Func<int>(this.ReadInt32), new Func<int>(this.ReadInt32));
		}

		// Token: 0x060012FA RID: 4858 RVA: 0x0003BDFC File Offset: 0x00039FFC
		private void ReadExceptionHandlers(int count, Func<int> read_entry, Func<int> read_length)
		{
			for (int i = 0; i < count; i++)
			{
				ExceptionHandler handler = new ExceptionHandler((ExceptionHandlerType)(read_entry() & 7));
				handler.TryStart = this.GetInstruction(read_entry());
				handler.TryEnd = this.GetInstruction(handler.TryStart.Offset + read_length());
				handler.HandlerStart = this.GetInstruction(read_entry());
				handler.HandlerEnd = this.GetInstruction(handler.HandlerStart.Offset + read_length());
				this.ReadExceptionHandlerSpecific(handler);
				this.body.ExceptionHandlers.Add(handler);
			}
		}

		// Token: 0x060012FB RID: 4859 RVA: 0x0003BEA4 File Offset: 0x0003A0A4
		private void ReadExceptionHandlerSpecific(ExceptionHandler handler)
		{
			ExceptionHandlerType handlerType = handler.HandlerType;
			if (handlerType == ExceptionHandlerType.Catch)
			{
				handler.CatchType = (TypeReference)this.reader.LookupToken(this.ReadToken());
				return;
			}
			if (handlerType != ExceptionHandlerType.Filter)
			{
				base.Advance(4);
				return;
			}
			handler.FilterStart = this.GetInstruction(this.ReadInt32());
		}

		// Token: 0x060012FC RID: 4860 RVA: 0x0003BEF8 File Offset: 0x0003A0F8
		public MetadataToken ReadToken()
		{
			return new MetadataToken(this.ReadUInt32());
		}

		// Token: 0x060012FD RID: 4861 RVA: 0x0003BF08 File Offset: 0x0003A108
		private void ReadDebugInfo()
		{
			if (this.method.debug_info.sequence_points != null)
			{
				this.ReadSequencePoints();
			}
			if (this.method.debug_info.scope != null)
			{
				this.ReadScope(this.method.debug_info.scope);
			}
			if (this.method.custom_infos != null)
			{
				this.ReadCustomDebugInformations(this.method);
			}
		}

		// Token: 0x060012FE RID: 4862 RVA: 0x0003BF70 File Offset: 0x0003A170
		private void ReadCustomDebugInformations(MethodDefinition method)
		{
			Collection<CustomDebugInformation> custom_infos = method.custom_infos;
			for (int i = 0; i < custom_infos.Count; i++)
			{
				StateMachineScopeDebugInformation state_machine_scope = custom_infos[i] as StateMachineScopeDebugInformation;
				if (state_machine_scope != null)
				{
					this.ReadStateMachineScope(state_machine_scope);
				}
				AsyncMethodBodyDebugInformation async_method = custom_infos[i] as AsyncMethodBodyDebugInformation;
				if (async_method != null)
				{
					this.ReadAsyncMethodBody(async_method);
				}
			}
		}

		// Token: 0x060012FF RID: 4863 RVA: 0x0003BFC4 File Offset: 0x0003A1C4
		private void ReadAsyncMethodBody(AsyncMethodBodyDebugInformation async_method)
		{
			if (async_method.catch_handler.Offset > -1)
			{
				async_method.catch_handler = new InstructionOffset(this.GetInstruction(async_method.catch_handler.Offset));
			}
			if (!async_method.yields.IsNullOrEmpty<InstructionOffset>())
			{
				for (int i = 0; i < async_method.yields.Count; i++)
				{
					async_method.yields[i] = new InstructionOffset(this.GetInstruction(async_method.yields[i].Offset));
				}
			}
			if (!async_method.resumes.IsNullOrEmpty<InstructionOffset>())
			{
				for (int j = 0; j < async_method.resumes.Count; j++)
				{
					async_method.resumes[j] = new InstructionOffset(this.GetInstruction(async_method.resumes[j].Offset));
				}
			}
		}

		// Token: 0x06001300 RID: 4864 RVA: 0x0003C098 File Offset: 0x0003A298
		private void ReadStateMachineScope(StateMachineScopeDebugInformation state_machine_scope)
		{
			if (state_machine_scope.scopes.IsNullOrEmpty<StateMachineScope>())
			{
				return;
			}
			foreach (StateMachineScope scope in state_machine_scope.scopes)
			{
				scope.start = new InstructionOffset(this.GetInstruction(scope.start.Offset));
				Instruction end_instruction = this.GetInstruction(scope.end.Offset);
				scope.end = ((end_instruction == null) ? default(InstructionOffset) : new InstructionOffset(end_instruction));
			}
		}

		// Token: 0x06001301 RID: 4865 RVA: 0x0003C13C File Offset: 0x0003A33C
		private void ReadSequencePoints()
		{
			MethodDebugInformation symbol = this.method.debug_info;
			for (int i = 0; i < symbol.sequence_points.Count; i++)
			{
				SequencePoint sequence_point = symbol.sequence_points[i];
				Instruction instruction = this.GetInstruction(sequence_point.Offset);
				if (instruction != null)
				{
					sequence_point.offset = new InstructionOffset(instruction);
				}
			}
		}

		// Token: 0x06001302 RID: 4866 RVA: 0x0003C194 File Offset: 0x0003A394
		private void ReadScopes(Collection<ScopeDebugInformation> scopes)
		{
			for (int i = 0; i < scopes.Count; i++)
			{
				this.ReadScope(scopes[i]);
			}
		}

		// Token: 0x06001303 RID: 4867 RVA: 0x0003C1C0 File Offset: 0x0003A3C0
		private void ReadScope(ScopeDebugInformation scope)
		{
			Instruction start_instruction = this.GetInstruction(scope.Start.Offset);
			if (start_instruction != null)
			{
				scope.Start = new InstructionOffset(start_instruction);
			}
			Instruction end_instruction = this.GetInstruction(scope.End.Offset);
			scope.End = ((end_instruction != null) ? new InstructionOffset(end_instruction) : default(InstructionOffset));
			if (!scope.variables.IsNullOrEmpty<VariableDebugInformation>())
			{
				for (int i = 0; i < scope.variables.Count; i++)
				{
					VariableDebugInformation variable_info = scope.variables[i];
					VariableDefinition variable = this.GetVariable(variable_info.Index);
					if (variable != null)
					{
						variable_info.index = new VariableIndex(variable);
					}
				}
			}
			if (!scope.scopes.IsNullOrEmpty<ScopeDebugInformation>())
			{
				this.ReadScopes(scope.scopes);
			}
		}

		// Token: 0x06001304 RID: 4868 RVA: 0x0003C28C File Offset: 0x0003A48C
		public ByteBuffer PatchRawMethodBody(MethodDefinition method, CodeWriter writer, out int code_size, out MetadataToken local_var_token)
		{
			int position = this.MoveTo(method);
			ByteBuffer buffer = new ByteBuffer();
			byte flags = this.ReadByte();
			int num = (int)(flags & 3);
			if (num != 2)
			{
				if (num != 3)
				{
					throw new NotSupportedException();
				}
				base.Advance(-1);
				this.PatchRawFatMethod(buffer, writer, out code_size, out local_var_token);
			}
			else
			{
				buffer.WriteByte(flags);
				local_var_token = MetadataToken.Zero;
				code_size = flags >> 2;
				this.PatchRawCode(buffer, code_size, writer);
			}
			this.MoveBackTo(position);
			return buffer;
		}

		// Token: 0x06001305 RID: 4869 RVA: 0x0003C304 File Offset: 0x0003A504
		private void PatchRawFatMethod(ByteBuffer buffer, CodeWriter writer, out int code_size, out MetadataToken local_var_token)
		{
			ushort flags = this.ReadUInt16();
			buffer.WriteUInt16(flags);
			buffer.WriteUInt16(this.ReadUInt16());
			code_size = this.ReadInt32();
			buffer.WriteInt32(code_size);
			local_var_token = this.ReadToken();
			if (local_var_token.RID > 0U)
			{
				VariableDefinitionCollection variables = this.ReadVariables(local_var_token);
				buffer.WriteUInt32((variables != null) ? writer.GetStandAloneSignature(variables).ToUInt32() : 0U);
			}
			else
			{
				buffer.WriteUInt32(0U);
			}
			this.PatchRawCode(buffer, code_size, writer);
			if ((flags & 8) != 0)
			{
				this.PatchRawSection(buffer, writer.metadata);
			}
		}

		// Token: 0x06001306 RID: 4870 RVA: 0x0003C3A0 File Offset: 0x0003A5A0
		private void PatchRawCode(ByteBuffer buffer, int code_size, CodeWriter writer)
		{
			MetadataBuilder metadata = writer.metadata;
			buffer.WriteBytes(this.ReadBytes(code_size));
			int end = buffer.position;
			buffer.position -= code_size;
			while (buffer.position < end)
			{
				byte il_opcode = buffer.ReadByte();
				OpCode opcode;
				if (il_opcode != 254)
				{
					opcode = OpCodes.OneByteOpCode[(int)il_opcode];
				}
				else
				{
					byte il_opcode2 = buffer.ReadByte();
					opcode = OpCodes.TwoBytesOpCode[(int)il_opcode2];
				}
				switch (opcode.OperandType)
				{
				case OperandType.InlineBrTarget:
				case OperandType.InlineI:
				case OperandType.ShortInlineR:
					buffer.position += 4;
					break;
				case OperandType.InlineField:
				case OperandType.InlineMethod:
				case OperandType.InlineTok:
				case OperandType.InlineType:
				{
					IMetadataTokenProvider provider = this.reader.LookupToken(new MetadataToken(buffer.ReadUInt32()));
					buffer.position -= 4;
					buffer.WriteUInt32(metadata.LookupToken(provider).ToUInt32());
					break;
				}
				case OperandType.InlineI8:
				case OperandType.InlineR:
					buffer.position += 8;
					break;
				case OperandType.InlineSig:
				{
					CallSite call_site = this.GetCallSite(new MetadataToken(buffer.ReadUInt32()));
					buffer.position -= 4;
					buffer.WriteUInt32(writer.GetStandAloneSignature(call_site).ToUInt32());
					break;
				}
				case OperandType.InlineString:
				{
					string @string = this.GetString(new MetadataToken(buffer.ReadUInt32()));
					buffer.position -= 4;
					buffer.WriteUInt32(new MetadataToken(TokenType.String, metadata.user_string_heap.GetStringIndex(@string)).ToUInt32());
					break;
				}
				case OperandType.InlineSwitch:
				{
					int length = buffer.ReadInt32();
					buffer.position += length * 4;
					break;
				}
				case OperandType.InlineVar:
				case OperandType.InlineArg:
					buffer.position += 2;
					break;
				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineI:
				case OperandType.ShortInlineVar:
				case OperandType.ShortInlineArg:
					buffer.position++;
					break;
				}
			}
		}

		// Token: 0x06001307 RID: 4871 RVA: 0x0003C5A8 File Offset: 0x0003A7A8
		private void PatchRawSection(ByteBuffer buffer, MetadataBuilder metadata)
		{
			int position = base.Position;
			base.Align(4);
			buffer.WriteBytes(base.Position - position);
			byte flags = this.ReadByte();
			if ((flags & 64) == 0)
			{
				buffer.WriteByte(flags);
				this.PatchRawSmallSection(buffer, metadata);
			}
			else
			{
				this.PatchRawFatSection(buffer, metadata);
			}
			if ((flags & 128) != 0)
			{
				this.PatchRawSection(buffer, metadata);
			}
		}

		// Token: 0x06001308 RID: 4872 RVA: 0x0003C608 File Offset: 0x0003A808
		private void PatchRawSmallSection(ByteBuffer buffer, MetadataBuilder metadata)
		{
			byte length = this.ReadByte();
			buffer.WriteByte(length);
			base.Advance(2);
			buffer.WriteUInt16(0);
			int count = (int)(length / 12);
			this.PatchRawExceptionHandlers(buffer, metadata, count, false);
		}

		// Token: 0x06001309 RID: 4873 RVA: 0x0003C640 File Offset: 0x0003A840
		private void PatchRawFatSection(ByteBuffer buffer, MetadataBuilder metadata)
		{
			base.Advance(-1);
			int length = this.ReadInt32();
			buffer.WriteInt32(length);
			int count = (length >> 8) / 24;
			this.PatchRawExceptionHandlers(buffer, metadata, count, true);
		}

		// Token: 0x0600130A RID: 4874 RVA: 0x0003C674 File Offset: 0x0003A874
		private void PatchRawExceptionHandlers(ByteBuffer buffer, MetadataBuilder metadata, int count, bool fat_entry)
		{
			for (int i = 0; i < count; i++)
			{
				ExceptionHandlerType handler_type;
				if (fat_entry)
				{
					uint type = this.ReadUInt32();
					handler_type = (ExceptionHandlerType)(type & 7U);
					buffer.WriteUInt32(type);
				}
				else
				{
					ushort type2 = this.ReadUInt16();
					handler_type = (ExceptionHandlerType)(type2 & 7);
					buffer.WriteUInt16(type2);
				}
				buffer.WriteBytes(this.ReadBytes(fat_entry ? 16 : 6));
				if (handler_type == ExceptionHandlerType.Catch)
				{
					IMetadataTokenProvider exception = this.reader.LookupToken(this.ReadToken());
					buffer.WriteUInt32(metadata.LookupToken(exception).ToUInt32());
				}
				else
				{
					buffer.WriteUInt32(this.ReadUInt32());
				}
			}
		}

		// Token: 0x04000874 RID: 2164
		internal readonly MetadataReader reader;

		// Token: 0x04000875 RID: 2165
		private int start;

		// Token: 0x04000876 RID: 2166
		private MethodDefinition method;

		// Token: 0x04000877 RID: 2167
		private MethodBody body;
	}
}
