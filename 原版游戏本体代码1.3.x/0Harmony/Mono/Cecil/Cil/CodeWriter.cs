using System;
using System.Collections.Generic;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002E7 RID: 743
	internal sealed class CodeWriter : ByteBuffer
	{
		// Token: 0x0600130D RID: 4877 RVA: 0x0003C712 File Offset: 0x0003A912
		public CodeWriter(MetadataBuilder metadata)
			: base(0)
		{
			this.code_base = metadata.text_map.GetRVA(TextSegment.Code);
			this.metadata = metadata;
			this.standalone_signatures = new Dictionary<uint, MetadataToken>();
			this.tiny_method_bodies = new Dictionary<ByteBuffer, uint>(new ByteBufferEqualityComparer());
		}

		// Token: 0x0600130E RID: 4878 RVA: 0x0003C750 File Offset: 0x0003A950
		public uint WriteMethodBody(MethodDefinition method)
		{
			uint rva;
			if (CodeWriter.IsUnresolved(method))
			{
				if (method.rva == 0U)
				{
					return 0U;
				}
				rva = this.WriteUnresolvedMethodBody(method);
			}
			else
			{
				if (CodeWriter.IsEmptyMethodBody(method.Body))
				{
					return 0U;
				}
				rva = this.WriteResolvedMethodBody(method);
			}
			return rva;
		}

		// Token: 0x0600130F RID: 4879 RVA: 0x0003C791 File Offset: 0x0003A991
		private static bool IsEmptyMethodBody(MethodBody body)
		{
			return body.instructions.IsNullOrEmpty<Instruction>() && body.variables.IsNullOrEmpty<VariableDefinition>();
		}

		// Token: 0x06001310 RID: 4880 RVA: 0x0003C7AD File Offset: 0x0003A9AD
		private static bool IsUnresolved(MethodDefinition method)
		{
			return method.HasBody && method.HasImage && method.body == null;
		}

		// Token: 0x06001311 RID: 4881 RVA: 0x0003C7CC File Offset: 0x0003A9CC
		private uint WriteUnresolvedMethodBody(MethodDefinition method)
		{
			int code_size;
			MetadataToken local_var_token;
			ByteBuffer raw_body = this.metadata.module.reader.code.PatchRawMethodBody(method, this, out code_size, out local_var_token);
			bool flag = (raw_body.buffer[0] & 3) == 3;
			if (flag)
			{
				this.Align(4);
			}
			uint rva = this.BeginMethod();
			if (flag || !this.GetOrMapTinyMethodBody(raw_body, ref rva))
			{
				base.WriteBytes(raw_body);
			}
			if (method.debug_info == null)
			{
				return rva;
			}
			ISymbolWriter symbol_writer = this.metadata.symbol_writer;
			if (symbol_writer != null)
			{
				method.debug_info.code_size = code_size;
				method.debug_info.local_var_token = local_var_token;
				symbol_writer.Write(method.debug_info);
			}
			return rva;
		}

		// Token: 0x06001312 RID: 4882 RVA: 0x0003C870 File Offset: 0x0003AA70
		private uint WriteResolvedMethodBody(MethodDefinition method)
		{
			this.body = method.Body;
			this.ComputeHeader();
			uint rva;
			if (this.RequiresFatHeader())
			{
				this.Align(4);
				rva = this.BeginMethod();
				this.WriteFatHeader();
				this.WriteInstructions();
				if (this.body.HasExceptionHandlers)
				{
					this.WriteExceptionHandlers();
				}
			}
			else
			{
				rva = this.BeginMethod();
				base.WriteByte((byte)(2 | (this.body.CodeSize << 2)));
				this.WriteInstructions();
				int start_position = (int)(rva - this.code_base);
				int body_size = this.position - start_position;
				byte[] body_bytes = new byte[body_size];
				Array.Copy(this.buffer, start_position, body_bytes, 0, body_size);
				if (this.GetOrMapTinyMethodBody(new ByteBuffer(body_bytes), ref rva))
				{
					this.position = start_position;
				}
			}
			ISymbolWriter symbol_writer = this.metadata.symbol_writer;
			if (symbol_writer != null && method.debug_info != null)
			{
				method.debug_info.code_size = this.body.CodeSize;
				method.debug_info.local_var_token = this.body.local_var_token;
				symbol_writer.Write(method.debug_info);
			}
			return rva;
		}

		// Token: 0x06001313 RID: 4883 RVA: 0x0003C97C File Offset: 0x0003AB7C
		private bool GetOrMapTinyMethodBody(ByteBuffer body, ref uint rva)
		{
			uint existing_rva;
			if (this.tiny_method_bodies.TryGetValue(body, out existing_rva))
			{
				rva = existing_rva;
				return true;
			}
			this.tiny_method_bodies.Add(body, rva);
			return false;
		}

		// Token: 0x06001314 RID: 4884 RVA: 0x0003C9B0 File Offset: 0x0003ABB0
		private void WriteFatHeader()
		{
			MethodBody body = this.body;
			byte flags = 3;
			if (body.InitLocals)
			{
				flags |= 16;
			}
			if (body.HasExceptionHandlers)
			{
				flags |= 8;
			}
			base.WriteByte(flags);
			base.WriteByte(48);
			base.WriteInt16((short)body.max_stack_size);
			base.WriteInt32(body.code_size);
			body.local_var_token = (body.HasVariables ? this.GetStandAloneSignature(body.Variables) : MetadataToken.Zero);
			this.WriteMetadataToken(body.local_var_token);
		}

		// Token: 0x06001315 RID: 4885 RVA: 0x0003CA38 File Offset: 0x0003AC38
		private void WriteInstructions()
		{
			Collection<Instruction> instructions = this.body.Instructions;
			Instruction[] items = instructions.items;
			int size = instructions.size;
			for (int i = 0; i < size; i++)
			{
				Instruction instruction = items[i];
				this.WriteOpCode(instruction.opcode);
				this.WriteOperand(instruction);
			}
		}

		// Token: 0x06001316 RID: 4886 RVA: 0x0003CA80 File Offset: 0x0003AC80
		private void WriteOpCode(OpCode opcode)
		{
			if (opcode.Size == 1)
			{
				base.WriteByte(opcode.Op2);
				return;
			}
			base.WriteByte(opcode.Op1);
			base.WriteByte(opcode.Op2);
		}

		// Token: 0x06001317 RID: 4887 RVA: 0x0003CAB4 File Offset: 0x0003ACB4
		private void WriteOperand(Instruction instruction)
		{
			OpCode opcode = instruction.opcode;
			OperandType operand_type = opcode.OperandType;
			if (operand_type == OperandType.InlineNone)
			{
				return;
			}
			object operand = instruction.operand;
			if (operand == null && operand_type != OperandType.InlineBrTarget && operand_type != OperandType.ShortInlineBrTarget)
			{
				throw new ArgumentException();
			}
			switch (operand_type)
			{
			case OperandType.InlineBrTarget:
			{
				Instruction target = (Instruction)operand;
				int offset = ((target != null) ? this.GetTargetOffset(target) : this.body.code_size);
				base.WriteInt32(offset - (instruction.Offset + opcode.Size + 4));
				return;
			}
			case OperandType.InlineField:
			case OperandType.InlineMethod:
			case OperandType.InlineTok:
			case OperandType.InlineType:
				this.WriteMetadataToken(this.metadata.LookupToken((IMetadataTokenProvider)operand));
				return;
			case OperandType.InlineI:
				base.WriteInt32((int)operand);
				return;
			case OperandType.InlineI8:
				base.WriteInt64((long)operand);
				return;
			case OperandType.InlineR:
				base.WriteDouble((double)operand);
				return;
			case OperandType.InlineSig:
				this.WriteMetadataToken(this.GetStandAloneSignature((CallSite)operand));
				return;
			case OperandType.InlineString:
				this.WriteMetadataToken(new MetadataToken(TokenType.String, this.GetUserStringIndex((string)operand)));
				return;
			case OperandType.InlineSwitch:
			{
				Instruction[] targets = (Instruction[])operand;
				base.WriteInt32(targets.Length);
				int diff = instruction.Offset + opcode.Size + 4 * (targets.Length + 1);
				for (int i = 0; i < targets.Length; i++)
				{
					base.WriteInt32(this.GetTargetOffset(targets[i]) - diff);
				}
				return;
			}
			case OperandType.InlineVar:
				base.WriteInt16((short)CodeWriter.GetVariableIndex((VariableDefinition)operand));
				return;
			case OperandType.InlineArg:
				base.WriteInt16((short)this.GetParameterIndex((ParameterDefinition)operand));
				return;
			case OperandType.ShortInlineBrTarget:
			{
				Instruction target2 = (Instruction)operand;
				int offset2 = ((target2 != null) ? this.GetTargetOffset(target2) : this.body.code_size);
				base.WriteSByte((sbyte)(offset2 - (instruction.Offset + opcode.Size + 1)));
				return;
			}
			case OperandType.ShortInlineI:
				if (opcode == OpCodes.Ldc_I4_S)
				{
					base.WriteSByte((sbyte)operand);
					return;
				}
				base.WriteByte((byte)operand);
				return;
			case OperandType.ShortInlineR:
				base.WriteSingle((float)operand);
				return;
			case OperandType.ShortInlineVar:
				base.WriteByte((byte)CodeWriter.GetVariableIndex((VariableDefinition)operand));
				return;
			case OperandType.ShortInlineArg:
				base.WriteByte((byte)this.GetParameterIndex((ParameterDefinition)operand));
				return;
			}
			throw new ArgumentException();
		}

		// Token: 0x06001318 RID: 4888 RVA: 0x0003CD08 File Offset: 0x0003AF08
		private int GetTargetOffset(Instruction instruction)
		{
			if (instruction == null)
			{
				Instruction last = this.body.instructions[this.body.instructions.size - 1];
				return last.offset + last.GetSize();
			}
			return instruction.offset;
		}

		// Token: 0x06001319 RID: 4889 RVA: 0x0003CD4F File Offset: 0x0003AF4F
		private uint GetUserStringIndex(string @string)
		{
			if (@string == null)
			{
				return 0U;
			}
			return this.metadata.user_string_heap.GetStringIndex(@string);
		}

		// Token: 0x0600131A RID: 4890 RVA: 0x0003CD67 File Offset: 0x0003AF67
		private static int GetVariableIndex(VariableDefinition variable)
		{
			return variable.Index;
		}

		// Token: 0x0600131B RID: 4891 RVA: 0x0003CD6F File Offset: 0x0003AF6F
		private int GetParameterIndex(ParameterDefinition parameter)
		{
			if (!this.body.method.HasThis)
			{
				return parameter.Index;
			}
			if (parameter == this.body.this_parameter)
			{
				return 0;
			}
			return parameter.Index + 1;
		}

		// Token: 0x0600131C RID: 4892 RVA: 0x0003CDA4 File Offset: 0x0003AFA4
		private bool RequiresFatHeader()
		{
			MethodBody body = this.body;
			return body.CodeSize >= 64 || body.InitLocals || body.HasVariables || body.HasExceptionHandlers || body.MaxStackSize > 8;
		}

		// Token: 0x0600131D RID: 4893 RVA: 0x0003CDE8 File Offset: 0x0003AFE8
		private void ComputeHeader()
		{
			int offset = 0;
			Collection<Instruction> instructions = this.body.instructions;
			Instruction[] items = instructions.items;
			int count = instructions.size;
			int stack_size = 0;
			int max_stack = 0;
			Dictionary<Instruction, int> stack_sizes = null;
			if (this.body.HasExceptionHandlers)
			{
				this.ComputeExceptionHandlerStackSize(ref stack_sizes);
			}
			for (int i = 0; i < count; i++)
			{
				Instruction instruction = items[i];
				instruction.offset = offset;
				offset += instruction.GetSize();
				CodeWriter.ComputeStackSize(instruction, ref stack_sizes, ref stack_size, ref max_stack);
			}
			this.body.code_size = offset;
			this.body.max_stack_size = max_stack;
		}

		// Token: 0x0600131E RID: 4894 RVA: 0x0003CE7C File Offset: 0x0003B07C
		private void ComputeExceptionHandlerStackSize(ref Dictionary<Instruction, int> stack_sizes)
		{
			Collection<ExceptionHandler> exception_handlers = this.body.ExceptionHandlers;
			for (int i = 0; i < exception_handlers.Count; i++)
			{
				ExceptionHandler exception_handler = exception_handlers[i];
				ExceptionHandlerType handlerType = exception_handler.HandlerType;
				if (handlerType != ExceptionHandlerType.Catch)
				{
					if (handlerType == ExceptionHandlerType.Filter)
					{
						CodeWriter.AddExceptionStackSize(exception_handler.FilterStart, ref stack_sizes);
						CodeWriter.AddExceptionStackSize(exception_handler.HandlerStart, ref stack_sizes);
					}
				}
				else
				{
					CodeWriter.AddExceptionStackSize(exception_handler.HandlerStart, ref stack_sizes);
				}
			}
		}

		// Token: 0x0600131F RID: 4895 RVA: 0x0003CEE4 File Offset: 0x0003B0E4
		private static void AddExceptionStackSize(Instruction handler_start, ref Dictionary<Instruction, int> stack_sizes)
		{
			if (handler_start == null)
			{
				return;
			}
			if (stack_sizes == null)
			{
				stack_sizes = new Dictionary<Instruction, int>();
			}
			stack_sizes[handler_start] = 1;
		}

		// Token: 0x06001320 RID: 4896 RVA: 0x0003CF00 File Offset: 0x0003B100
		private static void ComputeStackSize(Instruction instruction, ref Dictionary<Instruction, int> stack_sizes, ref int stack_size, ref int max_stack)
		{
			int computed_size;
			if (stack_sizes != null && stack_sizes.TryGetValue(instruction, out computed_size))
			{
				stack_size = computed_size;
			}
			max_stack = Math.Max(max_stack, stack_size);
			CodeWriter.ComputeStackDelta(instruction, ref stack_size);
			max_stack = Math.Max(max_stack, stack_size);
			CodeWriter.CopyBranchStackSize(instruction, ref stack_sizes, stack_size);
			CodeWriter.ComputeStackSize(instruction, ref stack_size);
		}

		// Token: 0x06001321 RID: 4897 RVA: 0x0003CF50 File Offset: 0x0003B150
		private static void CopyBranchStackSize(Instruction instruction, ref Dictionary<Instruction, int> stack_sizes, int stack_size)
		{
			if (stack_size == 0)
			{
				return;
			}
			OperandType operandType = instruction.opcode.OperandType;
			if (operandType != OperandType.InlineBrTarget)
			{
				if (operandType != OperandType.InlineSwitch)
				{
					if (operandType == OperandType.ShortInlineBrTarget)
					{
						goto IL_1D;
					}
				}
				else
				{
					Instruction[] targets = (Instruction[])instruction.operand;
					for (int i = 0; i < targets.Length; i++)
					{
						CodeWriter.CopyBranchStackSize(ref stack_sizes, targets[i], stack_size);
					}
				}
				return;
			}
			IL_1D:
			CodeWriter.CopyBranchStackSize(ref stack_sizes, (Instruction)instruction.operand, stack_size);
		}

		// Token: 0x06001322 RID: 4898 RVA: 0x0003CFB4 File Offset: 0x0003B1B4
		private static void CopyBranchStackSize(ref Dictionary<Instruction, int> stack_sizes, Instruction target, int stack_size)
		{
			if (stack_sizes == null)
			{
				stack_sizes = new Dictionary<Instruction, int>();
			}
			int branch_stack_size = stack_size;
			int computed_size;
			if (stack_sizes.TryGetValue(target, out computed_size))
			{
				branch_stack_size = Math.Max(branch_stack_size, computed_size);
			}
			stack_sizes[target] = branch_stack_size;
		}

		// Token: 0x06001323 RID: 4899 RVA: 0x0003CFEC File Offset: 0x0003B1EC
		private static void ComputeStackSize(Instruction instruction, ref int stack_size)
		{
			FlowControl flowControl = instruction.opcode.FlowControl;
			if (flowControl == FlowControl.Branch || flowControl - FlowControl.Return <= 1)
			{
				stack_size = 0;
			}
		}

		// Token: 0x06001324 RID: 4900 RVA: 0x0003D014 File Offset: 0x0003B214
		private static void ComputeStackDelta(Instruction instruction, ref int stack_size)
		{
			if (instruction.opcode.FlowControl == FlowControl.Call)
			{
				IMethodSignature method = (IMethodSignature)instruction.operand;
				if (method.HasImplicitThis() && instruction.opcode.Code != Code.Newobj)
				{
					stack_size--;
				}
				if (method.HasParameters)
				{
					stack_size -= method.Parameters.Count;
				}
				if (instruction.opcode.Code == Code.Calli)
				{
					stack_size--;
				}
				if (method.ReturnType.etype != ElementType.Void || instruction.opcode.Code == Code.Newobj)
				{
					stack_size++;
					return;
				}
			}
			else
			{
				CodeWriter.ComputePopDelta(instruction.opcode.StackBehaviourPop, ref stack_size);
				CodeWriter.ComputePushDelta(instruction.opcode.StackBehaviourPush, ref stack_size);
			}
		}

		// Token: 0x06001325 RID: 4901 RVA: 0x0003D0CC File Offset: 0x0003B2CC
		private static void ComputePopDelta(StackBehaviour pop_behavior, ref int stack_size)
		{
			switch (pop_behavior)
			{
			case StackBehaviour.Pop1:
			case StackBehaviour.Popi:
			case StackBehaviour.Popref:
				stack_size--;
				return;
			case StackBehaviour.Pop1_pop1:
			case StackBehaviour.Popi_pop1:
			case StackBehaviour.Popi_popi:
			case StackBehaviour.Popi_popi8:
			case StackBehaviour.Popi_popr4:
			case StackBehaviour.Popi_popr8:
			case StackBehaviour.Popref_pop1:
			case StackBehaviour.Popref_popi:
				stack_size -= 2;
				return;
			case StackBehaviour.Popi_popi_popi:
			case StackBehaviour.Popref_popi_popi:
			case StackBehaviour.Popref_popi_popi8:
			case StackBehaviour.Popref_popi_popr4:
			case StackBehaviour.Popref_popi_popr8:
			case StackBehaviour.Popref_popi_popref:
				stack_size -= 3;
				return;
			case StackBehaviour.PopAll:
				stack_size = 0;
				return;
			default:
				return;
			}
		}

		// Token: 0x06001326 RID: 4902 RVA: 0x0003D142 File Offset: 0x0003B342
		private static void ComputePushDelta(StackBehaviour push_behaviour, ref int stack_size)
		{
			switch (push_behaviour)
			{
			case StackBehaviour.Push1:
			case StackBehaviour.Pushi:
			case StackBehaviour.Pushi8:
			case StackBehaviour.Pushr4:
			case StackBehaviour.Pushr8:
			case StackBehaviour.Pushref:
				stack_size++;
				return;
			case StackBehaviour.Push1_push1:
				stack_size += 2;
				return;
			default:
				return;
			}
		}

		// Token: 0x06001327 RID: 4903 RVA: 0x0003D178 File Offset: 0x0003B378
		private void WriteExceptionHandlers()
		{
			this.Align(4);
			Collection<ExceptionHandler> handlers = this.body.ExceptionHandlers;
			if (handlers.Count < 21 && !CodeWriter.RequiresFatSection(handlers))
			{
				this.WriteSmallSection(handlers);
				return;
			}
			this.WriteFatSection(handlers);
		}

		// Token: 0x06001328 RID: 4904 RVA: 0x0003D1BC File Offset: 0x0003B3BC
		private static bool RequiresFatSection(Collection<ExceptionHandler> handlers)
		{
			for (int i = 0; i < handlers.Count; i++)
			{
				ExceptionHandler handler = handlers[i];
				if (CodeWriter.IsFatRange(handler.TryStart, handler.TryEnd))
				{
					return true;
				}
				if (CodeWriter.IsFatRange(handler.HandlerStart, handler.HandlerEnd))
				{
					return true;
				}
				if (handler.HandlerType == ExceptionHandlerType.Filter && CodeWriter.IsFatRange(handler.FilterStart, handler.HandlerStart))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001329 RID: 4905 RVA: 0x0003D22B File Offset: 0x0003B42B
		private static bool IsFatRange(Instruction start, Instruction end)
		{
			if (start == null)
			{
				throw new ArgumentException();
			}
			return end == null || end.Offset - start.Offset > 255 || start.Offset > 65535;
		}

		// Token: 0x0600132A RID: 4906 RVA: 0x0003D260 File Offset: 0x0003B460
		private void WriteSmallSection(Collection<ExceptionHandler> handlers)
		{
			base.WriteByte(1);
			base.WriteByte((byte)(handlers.Count * 12 + 4));
			base.WriteBytes(2);
			this.WriteExceptionHandlers(handlers, delegate(int i)
			{
				base.WriteUInt16((ushort)i);
			}, delegate(int i)
			{
				base.WriteByte((byte)i);
			});
		}

		// Token: 0x0600132B RID: 4907 RVA: 0x0003D2AC File Offset: 0x0003B4AC
		private void WriteFatSection(Collection<ExceptionHandler> handlers)
		{
			base.WriteByte(65);
			int size = handlers.Count * 24 + 4;
			base.WriteByte((byte)(size & 255));
			base.WriteByte((byte)((size >> 8) & 255));
			base.WriteByte((byte)((size >> 16) & 255));
			this.WriteExceptionHandlers(handlers, new Action<int>(base.WriteInt32), new Action<int>(base.WriteInt32));
		}

		// Token: 0x0600132C RID: 4908 RVA: 0x0003D31C File Offset: 0x0003B51C
		private void WriteExceptionHandlers(Collection<ExceptionHandler> handlers, Action<int> write_entry, Action<int> write_length)
		{
			for (int i = 0; i < handlers.Count; i++)
			{
				ExceptionHandler handler = handlers[i];
				write_entry((int)handler.HandlerType);
				write_entry(handler.TryStart.Offset);
				write_length(this.GetTargetOffset(handler.TryEnd) - handler.TryStart.Offset);
				write_entry(handler.HandlerStart.Offset);
				write_length(this.GetTargetOffset(handler.HandlerEnd) - handler.HandlerStart.Offset);
				this.WriteExceptionHandlerSpecific(handler);
			}
		}

		// Token: 0x0600132D RID: 4909 RVA: 0x0003D3B8 File Offset: 0x0003B5B8
		private void WriteExceptionHandlerSpecific(ExceptionHandler handler)
		{
			ExceptionHandlerType handlerType = handler.HandlerType;
			if (handlerType == ExceptionHandlerType.Catch)
			{
				this.WriteMetadataToken(this.metadata.LookupToken(handler.CatchType));
				return;
			}
			if (handlerType != ExceptionHandlerType.Filter)
			{
				base.WriteInt32(0);
				return;
			}
			base.WriteInt32(handler.FilterStart.Offset);
		}

		// Token: 0x0600132E RID: 4910 RVA: 0x0003D408 File Offset: 0x0003B608
		public MetadataToken GetStandAloneSignature(Collection<VariableDefinition> variables)
		{
			uint signature = this.metadata.GetLocalVariableBlobIndex(variables);
			return this.GetStandAloneSignatureToken(signature);
		}

		// Token: 0x0600132F RID: 4911 RVA: 0x0003D42C File Offset: 0x0003B62C
		public MetadataToken GetStandAloneSignature(CallSite call_site)
		{
			uint signature = this.metadata.GetCallSiteBlobIndex(call_site);
			MetadataToken token = this.GetStandAloneSignatureToken(signature);
			call_site.MetadataToken = token;
			return token;
		}

		// Token: 0x06001330 RID: 4912 RVA: 0x0003D458 File Offset: 0x0003B658
		private MetadataToken GetStandAloneSignatureToken(uint signature)
		{
			MetadataToken token;
			if (this.standalone_signatures.TryGetValue(signature, out token))
			{
				return token;
			}
			token = new MetadataToken(TokenType.Signature, this.metadata.AddStandAloneSignature(signature));
			this.standalone_signatures.Add(signature, token);
			return token;
		}

		// Token: 0x06001331 RID: 4913 RVA: 0x0003D49D File Offset: 0x0003B69D
		private uint BeginMethod()
		{
			return (uint)((ulong)this.code_base + (ulong)((long)this.position));
		}

		// Token: 0x06001332 RID: 4914 RVA: 0x0003D4AF File Offset: 0x0003B6AF
		private void WriteMetadataToken(MetadataToken token)
		{
			base.WriteUInt32(token.ToUInt32());
		}

		// Token: 0x06001333 RID: 4915 RVA: 0x0003AF0C File Offset: 0x0003910C
		private void Align(int align)
		{
			align--;
			base.WriteBytes(((this.position + align) & ~align) - this.position);
		}

		// Token: 0x04000878 RID: 2168
		private readonly uint code_base;

		// Token: 0x04000879 RID: 2169
		internal readonly MetadataBuilder metadata;

		// Token: 0x0400087A RID: 2170
		private readonly Dictionary<uint, MetadataToken> standalone_signatures;

		// Token: 0x0400087B RID: 2171
		private readonly Dictionary<ByteBuffer, uint> tiny_method_bodies;

		// Token: 0x0400087C RID: 2172
		private MethodBody body;
	}
}
