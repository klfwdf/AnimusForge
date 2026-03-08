using System;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002EF RID: 751
	internal sealed class ILProcessor
	{
		// Token: 0x170004ED RID: 1261
		// (get) Token: 0x0600135C RID: 4956 RVA: 0x0003D65C File Offset: 0x0003B85C
		public MethodBody Body
		{
			get
			{
				return this.body;
			}
		}

		// Token: 0x0600135D RID: 4957 RVA: 0x0003D664 File Offset: 0x0003B864
		internal ILProcessor(MethodBody body)
		{
			this.body = body;
			this.instructions = body.Instructions;
		}

		// Token: 0x0600135E RID: 4958 RVA: 0x0003D67F File Offset: 0x0003B87F
		public Instruction Create(OpCode opcode)
		{
			return Instruction.Create(opcode);
		}

		// Token: 0x0600135F RID: 4959 RVA: 0x0003D687 File Offset: 0x0003B887
		public Instruction Create(OpCode opcode, TypeReference type)
		{
			return Instruction.Create(opcode, type);
		}

		// Token: 0x06001360 RID: 4960 RVA: 0x0003D690 File Offset: 0x0003B890
		public Instruction Create(OpCode opcode, CallSite site)
		{
			return Instruction.Create(opcode, site);
		}

		// Token: 0x06001361 RID: 4961 RVA: 0x0003D699 File Offset: 0x0003B899
		public Instruction Create(OpCode opcode, MethodReference method)
		{
			return Instruction.Create(opcode, method);
		}

		// Token: 0x06001362 RID: 4962 RVA: 0x0003D6A2 File Offset: 0x0003B8A2
		public Instruction Create(OpCode opcode, FieldReference field)
		{
			return Instruction.Create(opcode, field);
		}

		// Token: 0x06001363 RID: 4963 RVA: 0x0003D6AB File Offset: 0x0003B8AB
		public Instruction Create(OpCode opcode, string value)
		{
			return Instruction.Create(opcode, value);
		}

		// Token: 0x06001364 RID: 4964 RVA: 0x0003D6B4 File Offset: 0x0003B8B4
		public Instruction Create(OpCode opcode, sbyte value)
		{
			return Instruction.Create(opcode, value);
		}

		// Token: 0x06001365 RID: 4965 RVA: 0x0003D6C0 File Offset: 0x0003B8C0
		public Instruction Create(OpCode opcode, byte value)
		{
			if (opcode.OperandType == OperandType.ShortInlineVar)
			{
				return Instruction.Create(opcode, this.body.Variables[(int)value]);
			}
			if (opcode.OperandType == OperandType.ShortInlineArg)
			{
				return Instruction.Create(opcode, this.body.GetParameter((int)value));
			}
			return Instruction.Create(opcode, value);
		}

		// Token: 0x06001366 RID: 4966 RVA: 0x0003D718 File Offset: 0x0003B918
		public Instruction Create(OpCode opcode, int value)
		{
			if (opcode.OperandType == OperandType.InlineVar)
			{
				return Instruction.Create(opcode, this.body.Variables[value]);
			}
			if (opcode.OperandType == OperandType.InlineArg)
			{
				return Instruction.Create(opcode, this.body.GetParameter(value));
			}
			return Instruction.Create(opcode, value);
		}

		// Token: 0x06001367 RID: 4967 RVA: 0x0003D76D File Offset: 0x0003B96D
		public Instruction Create(OpCode opcode, long value)
		{
			return Instruction.Create(opcode, value);
		}

		// Token: 0x06001368 RID: 4968 RVA: 0x0003D776 File Offset: 0x0003B976
		public Instruction Create(OpCode opcode, float value)
		{
			return Instruction.Create(opcode, value);
		}

		// Token: 0x06001369 RID: 4969 RVA: 0x0003D77F File Offset: 0x0003B97F
		public Instruction Create(OpCode opcode, double value)
		{
			return Instruction.Create(opcode, value);
		}

		// Token: 0x0600136A RID: 4970 RVA: 0x0003D788 File Offset: 0x0003B988
		public Instruction Create(OpCode opcode, Instruction target)
		{
			return Instruction.Create(opcode, target);
		}

		// Token: 0x0600136B RID: 4971 RVA: 0x0003D791 File Offset: 0x0003B991
		public Instruction Create(OpCode opcode, Instruction[] targets)
		{
			return Instruction.Create(opcode, targets);
		}

		// Token: 0x0600136C RID: 4972 RVA: 0x0003D79A File Offset: 0x0003B99A
		public Instruction Create(OpCode opcode, VariableDefinition variable)
		{
			return Instruction.Create(opcode, variable);
		}

		// Token: 0x0600136D RID: 4973 RVA: 0x0003D7A3 File Offset: 0x0003B9A3
		public Instruction Create(OpCode opcode, ParameterDefinition parameter)
		{
			return Instruction.Create(opcode, parameter);
		}

		// Token: 0x0600136E RID: 4974 RVA: 0x0003D7AC File Offset: 0x0003B9AC
		public void Emit(OpCode opcode)
		{
			this.Append(this.Create(opcode));
		}

		// Token: 0x0600136F RID: 4975 RVA: 0x0003D7BB File Offset: 0x0003B9BB
		public void Emit(OpCode opcode, TypeReference type)
		{
			this.Append(this.Create(opcode, type));
		}

		// Token: 0x06001370 RID: 4976 RVA: 0x0003D7CB File Offset: 0x0003B9CB
		public void Emit(OpCode opcode, MethodReference method)
		{
			this.Append(this.Create(opcode, method));
		}

		// Token: 0x06001371 RID: 4977 RVA: 0x0003D7DB File Offset: 0x0003B9DB
		public void Emit(OpCode opcode, CallSite site)
		{
			this.Append(this.Create(opcode, site));
		}

		// Token: 0x06001372 RID: 4978 RVA: 0x0003D7EB File Offset: 0x0003B9EB
		public void Emit(OpCode opcode, FieldReference field)
		{
			this.Append(this.Create(opcode, field));
		}

		// Token: 0x06001373 RID: 4979 RVA: 0x0003D7FB File Offset: 0x0003B9FB
		public void Emit(OpCode opcode, string value)
		{
			this.Append(this.Create(opcode, value));
		}

		// Token: 0x06001374 RID: 4980 RVA: 0x0003D80B File Offset: 0x0003BA0B
		public void Emit(OpCode opcode, byte value)
		{
			this.Append(this.Create(opcode, value));
		}

		// Token: 0x06001375 RID: 4981 RVA: 0x0003D81B File Offset: 0x0003BA1B
		public void Emit(OpCode opcode, sbyte value)
		{
			this.Append(this.Create(opcode, value));
		}

		// Token: 0x06001376 RID: 4982 RVA: 0x0003D82B File Offset: 0x0003BA2B
		public void Emit(OpCode opcode, int value)
		{
			this.Append(this.Create(opcode, value));
		}

		// Token: 0x06001377 RID: 4983 RVA: 0x0003D83B File Offset: 0x0003BA3B
		public void Emit(OpCode opcode, long value)
		{
			this.Append(this.Create(opcode, value));
		}

		// Token: 0x06001378 RID: 4984 RVA: 0x0003D84B File Offset: 0x0003BA4B
		public void Emit(OpCode opcode, float value)
		{
			this.Append(this.Create(opcode, value));
		}

		// Token: 0x06001379 RID: 4985 RVA: 0x0003D85B File Offset: 0x0003BA5B
		public void Emit(OpCode opcode, double value)
		{
			this.Append(this.Create(opcode, value));
		}

		// Token: 0x0600137A RID: 4986 RVA: 0x0003D86B File Offset: 0x0003BA6B
		public void Emit(OpCode opcode, Instruction target)
		{
			this.Append(this.Create(opcode, target));
		}

		// Token: 0x0600137B RID: 4987 RVA: 0x0003D87B File Offset: 0x0003BA7B
		public void Emit(OpCode opcode, Instruction[] targets)
		{
			this.Append(this.Create(opcode, targets));
		}

		// Token: 0x0600137C RID: 4988 RVA: 0x0003D88B File Offset: 0x0003BA8B
		public void Emit(OpCode opcode, VariableDefinition variable)
		{
			this.Append(this.Create(opcode, variable));
		}

		// Token: 0x0600137D RID: 4989 RVA: 0x0003D89B File Offset: 0x0003BA9B
		public void Emit(OpCode opcode, ParameterDefinition parameter)
		{
			this.Append(this.Create(opcode, parameter));
		}

		// Token: 0x0600137E RID: 4990 RVA: 0x0003D8AC File Offset: 0x0003BAAC
		public void InsertBefore(Instruction target, Instruction instruction)
		{
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}
			if (instruction == null)
			{
				throw new ArgumentNullException("instruction");
			}
			int index = this.instructions.IndexOf(target);
			if (index == -1)
			{
				throw new ArgumentOutOfRangeException("target");
			}
			this.instructions.Insert(index, instruction);
		}

		// Token: 0x0600137F RID: 4991 RVA: 0x0003D900 File Offset: 0x0003BB00
		public void InsertAfter(Instruction target, Instruction instruction)
		{
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}
			if (instruction == null)
			{
				throw new ArgumentNullException("instruction");
			}
			int index = this.instructions.IndexOf(target);
			if (index == -1)
			{
				throw new ArgumentOutOfRangeException("target");
			}
			this.instructions.Insert(index + 1, instruction);
		}

		// Token: 0x06001380 RID: 4992 RVA: 0x0003D954 File Offset: 0x0003BB54
		public void InsertAfter(int index, Instruction instruction)
		{
			if (index < 0 || index >= this.instructions.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (instruction == null)
			{
				throw new ArgumentNullException("instruction");
			}
			this.instructions.Insert(index + 1, instruction);
		}

		// Token: 0x06001381 RID: 4993 RVA: 0x0003D990 File Offset: 0x0003BB90
		public void Append(Instruction instruction)
		{
			if (instruction == null)
			{
				throw new ArgumentNullException("instruction");
			}
			this.instructions.Add(instruction);
		}

		// Token: 0x06001382 RID: 4994 RVA: 0x0003D9AC File Offset: 0x0003BBAC
		public void Replace(Instruction target, Instruction instruction)
		{
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}
			if (instruction == null)
			{
				throw new ArgumentNullException("instruction");
			}
			this.InsertAfter(target, instruction);
			this.Remove(target);
		}

		// Token: 0x06001383 RID: 4995 RVA: 0x0003D9D9 File Offset: 0x0003BBD9
		public void Replace(int index, Instruction instruction)
		{
			if (instruction == null)
			{
				throw new ArgumentNullException("instruction");
			}
			this.InsertAfter(index, instruction);
			this.RemoveAt(index);
		}

		// Token: 0x06001384 RID: 4996 RVA: 0x0003D9F8 File Offset: 0x0003BBF8
		public void Remove(Instruction instruction)
		{
			if (instruction == null)
			{
				throw new ArgumentNullException("instruction");
			}
			if (!this.instructions.Remove(instruction))
			{
				throw new ArgumentOutOfRangeException("instruction");
			}
		}

		// Token: 0x06001385 RID: 4997 RVA: 0x0003DA21 File Offset: 0x0003BC21
		public void RemoveAt(int index)
		{
			if (index < 0 || index >= this.instructions.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			this.instructions.RemoveAt(index);
		}

		// Token: 0x06001386 RID: 4998 RVA: 0x0003DA4C File Offset: 0x0003BC4C
		public void Clear()
		{
			this.instructions.Clear();
		}

		// Token: 0x040008A9 RID: 2217
		private readonly MethodBody body;

		// Token: 0x040008AA RID: 2218
		private readonly Collection<Instruction> instructions;
	}
}
